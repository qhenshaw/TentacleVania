#!/usr/bin/env bash
# Adds a placeholder FMOD event: generates a synthesized sound into FMOD/Assets/Placeholder/, creates the event (and
# any missing event folders) with the FMOD Studio command-line tool, assigns it to a bank, builds the banks, and
# refreshes FMOD's event list in the open Unity Editor. FMOD Studio must not have the project open.
#
# Usage: Tools/FMOD/fmod-placeholder.sh <event:/Path/Name> [--type blip|impact|whoosh|loop] [--bank Master] [--no-build]
#   --type      blip (default, UI and pickups), impact (hits, explosions), whoosh (dashes, swings), loop (engines, beams;
#               the event loops until stopped)
#   --no-build  skip the bank build, e.g. when adding several events in a row; build with the last one
# Exits 0 on success, 1 when the event or build failed, 2 for bad arguments or when FMOD Studio is open.

set -u
cd "$(dirname "$0")/../.."

event="" type=blip bank=Master build=true
while (( $# )); do
    case "$1" in
        --type) type="$2"; shift ;;
        --bank) bank="$2"; shift ;;
        --no-build) build=false ;;
        event:/*) event="$1" ;;
        *) echo "Unknown option: $1"; exit 2 ;;
    esac
    shift
done

if [[ ! "$event" =~ ^event:/[A-Za-z0-9_\ /-]+$ ]]; then
    echo "Usage: Tools/FMOD/fmod-placeholder.sh <event:/Path/Name> [--type blip|impact|whoosh|loop] [--bank Master] [--no-build]"
    exit 2
fi
case "$type" in blip|impact|whoosh|loop) ;; *) echo "Unknown type: $type"; exit 2 ;; esac
if tasklist 2>/dev/null | grep -qi "^FMOD Studio.exe"; then
    echo "FMOD Studio is running. Close it first so it doesn't overwrite the changes."
    exit 2
fi

version=$(sed -n 's/.*serializationModel="Studio\.0*\([0-9]*\)\.\([0-9]*\)\..*/\1.\2/p' FMOD/FMOD.fspro)
cli=$(ls -d "/c/Program Files/FMOD SoundSystem/FMOD Studio $version".* 2>/dev/null | sort -V | tail -1)/fmodstudiocl.exe
if [[ ! -f "$cli" ]]; then
    echo "FMOD Studio $version command-line tool not found under C:/Program Files/FMOD SoundSystem."
    exit 2
fi

relative=${event#event:/}
audio="FMOD/Assets/Placeholder/${relative//\//_}.wav"
if [[ -e "$audio" ]]; then
    echo "$audio already exists."
    exit 1
fi
mkdir -p "$(dirname "$audio")" Temp
python -I Tools/FMOD/placeholder_sound.py "$type" "$audio" || exit 1

loop=false
[[ "$type" == loop ]] && loop=true
script=Temp/fmod-placeholder.js
{
    echo "var Params = { eventPath: \"$event\", bank: \"$bank\", audioFile: \"$(cygpath -m "$PWD/$audio")\", loop: $loop };"
    cat Tools/FMOD/add_placeholder_event.js
} >"$script"

out=$("$cli" -script "$(cygpath -w "$script")" "$(cygpath -w FMOD/FMOD.fspro)" 2>&1)
result=$(sed -n 's/.*FMOD_PLACEHOLDER: //p' <<<"$out")
if [[ "$result" != ok* ]]; then
    rm -f "$audio"
    echo "Could not create $event: ${result:-no result from the script}"
    [[ -z "$result" ]] && tail -5 <<<"$out"
    exit 1
fi
echo "Created $event ($type) in bank $bank, sound $audio"

if $build; then
    out=$("$cli" -build "$(cygpath -w FMOD/FMOD.fspro)" 2>&1)
    if ! grep -q "Build completed successfully" <<<"$out"; then
        echo "Bank build failed:"
        tail -10 <<<"$out" | sed 's/^/  /'
        exit 1
    fi
    echo "Banks built."
    unity command eval --caller plugin --skill unity-cli 'FMODUnity.EventManager.RefreshBanks(); return 0;' >/dev/null 2>&1 \
        && echo "Refreshed FMOD events in Unity."
fi
exit 0
