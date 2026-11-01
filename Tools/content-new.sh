#!/usr/bin/env bash
# Scaffolds a new piece of content with everything wired, so it shows up in play and passes unity-validate.sh:
#   enemy     prefab copied from an existing enemy (its Enemy subclass, so pick a source that behaves alike), its own
#             EnemyDefinition, CharacterMaster material(s) and EnemyVoice with placeholder voice events
#             (event:/Enemies/Voice/<Name>/<Moment>), added to the debug enemy spawn list (F5 Enemies)
#   boss      the same from a boss prefab into Bosses/<Name>/, plus a BossDefinition (id <Name>, key boss.<name>),
#             a Stagger voice event and the debug boss spawn list
#   weapon    WeaponDefinition copied from an existing weapon (moves, VFX, sounds), modifiers cleared, in ContentCatalog
#   ability   <Name>Definition + <Name>Runtime stubs in Abilities/<Name>/ from Tools/templates, the asset, an
#             event:/Abilities/<Name>/Activate placeholder, ContentCatalog
#   mutation  <Name>Mutation stub from Tools/templates, the asset in Abilities/Mutations/Data, ContentCatalog
# and English UI strings for the keys it uses. The editor work is in Assets/Editor/ContentScaffold.cs. Builds FMOD
# banks when it adds events, so FMOD Studio must be closed; finish and merge right after, as with any bank build.
#
# Usage: Tools/content-new.sh <enemy|boss|weapon|ability|mutation> <PascalName> [--from <Source>] [--title "Name"]
#        [--pitch <Hz>] [--machine] [--no-fmod]
#   --from     source prefab or weapon to copy (defaults Handler, WardenDrone, CrusherMass)
#   --title    the English display name (default: the name split into words)
#   --pitch    voice pitch for enemies and bosses (default 200, machines 160); --machine buzzes like the turret
# Exits 0 when created and validated, 1 on a failure (what was created so far stays; delete it to retry).

set -u
cd "$(dirname "$0")/.."
usage() { sed -n '/^# Usage/,/^# Exits/p' "$0" | sed 's/^# \{0,1\}//'; exit 1; }
kind="${1:-}" name="${2:-}"
[[ -n "$kind" && -n "$name" ]] || usage
shift 2
from="" title="" pitch="" machine=0 fmod=1
while (( $# )); do
    case "$1" in
        --from) from="$2"; shift ;;
        --title) title="$2"; shift ;;
        --pitch) pitch="$2"; shift ;;
        --machine) machine=1 ;;
        --no-fmod) fmod=0 ;;
        *) echo "Unknown option: $1"; usage ;;
    esac
    shift
done
case "$kind" in enemy|boss|weapon|ability|mutation) ;; *) echo "Unknown kind: $kind"; usage ;; esac
[[ "$name" =~ ^[A-Z][A-Za-z0-9]*$ ]] || { echo "Name must be PascalCase letters and digits: $name"; exit 1; }
[[ -n "$title" ]] || title=$(sed 's/\([a-z0-9]\)\([A-Z]\)/\1 \2/g' <<<"$name")
lower="$(tr '[:upper:]' '[:lower:]' <<<"${name:0:1}")${name:1}"

template() {
    local src="Tools/templates/$1" dst="$2"
    [[ -e "$dst" ]] && { echo "$dst already exists"; exit 1; }
    mkdir -p "$(dirname "$dst")"
    sed -e "s/__NAME__/$name/g" -e "s/__TITLE__/$title/g" "$src" >"$dst"
    echo "created $dst"
}

if [[ "$kind" == ability ]]; then
    template AbilityDefinition.cs.txt "Assets/Game/Abilities/$name/${name}Definition.cs"
    template AbilityRuntime.cs.txt "Assets/Game/Abilities/$name/${name}Runtime.cs"
elif [[ "$kind" == mutation ]]; then
    template Mutation.cs.txt "Assets/Game/Abilities/Mutations/${name}Mutation.cs"
fi
if [[ "$kind" == ability || "$kind" == mutation ]]; then
    bash Tools/UnityCheck/unity-check.sh || exit 1
fi

if (( fmod )); then
    events=()
    if [[ "$kind" == enemy || "$kind" == boss ]]; then
        moments=(alert pain death grabbed attack)
        [[ "$kind" == boss ]] && moments+=(stagger)
        [[ -n "$pitch" ]] || pitch=$(( machine ? 160 : 200 ))
        for moment in "${moments[@]}"; do
            events+=("event:/Enemies/Voice/$name/${moment^}|--voice $moment --pitch $pitch$( (( machine )) && echo " --machine")")
        done
    elif [[ "$kind" == ability ]]; then
        events+=("event:/Abilities/$name/Activate|--type whoosh")
    fi
    for i in "${!events[@]}"; do
        path="${events[$i]%%|*}" args="${events[$i]#*|}"
        build=--no-build
        (( i == ${#events[@]} - 1 )) && build=""
        # shellcheck disable=SC2086
        bash Tools/FMOD/fmod-placeholder.sh "$path" $args $build || { echo "FMOD placeholder $path failed"; exit 1; }
    done
fi

mkdir -p Temp
printf 'return ContentScaffold.Create("%s", "%s", "%s", "%s");\n' "$kind" "$name" "$from" "${title//\"/}" >Temp/content-new.cs
out=$(bash Tools/UnityCheck/unity-long-eval.sh Temp/content-new.cs 300 2>&1)
echo "$out"
grep -qE '^ERROR|EXCEPTION|^Could not start|^Timed out' <<<"$out" && exit 1

strings=Temp/content-new.tsv
: >"$strings"
case "$kind" in
    boss) printf 'boss.%s\t%s\n' "$lower" "$title" >>"$strings" ;;
    weapon) printf 'weapon.%s.name\t%s\n' "$name" "$title" >>"$strings" ;;
    ability) printf 'ability.%s.name\t%s\n' "$name" "$title" >>"$strings" ;;
    mutation) printf 'mutation.%s.name\t%s\nmutation.%s.effect\t%s (effect text to come)\n' "$lower" "$title" "$lower" "$title" >>"$strings" ;;
esac
if [[ -s "$strings" ]]; then
    python -I Tools/loc-add.py UI "$strings" || exit 1
fi

bash Tools/UnityCheck/unity-validate.sh
