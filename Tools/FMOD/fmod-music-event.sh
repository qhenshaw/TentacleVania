#!/usr/bin/env bash
# Creates a layered, looping FMOD music event: one audio track per stem file (all starting together and looping over
# the longest stem), driven by the global-preset user parameter Intensity (0-1). The first stem always plays; each
# further stem fades in over its own Intensity range (defaults for 4 stems: 0.15-0.35, 0.4-0.6, 0.7-0.9). Licensed stems
# replace the placeholders later with: Tools/FMOD/fmod-swap-sound.sh <event> <file> --track <n> (n from 1).
# Drives the FMOD Studio command-line tool; FMOD Studio must not have the project open.
#
# Usage: Tools/FMOD/fmod-music-event.sh <event:/Music/Name> <stem file>... [--bank Master] [--fades a-b,a-b,...]
#        [--no-build] [--dry-run]
#   --fades  Intensity ranges for stems 2..n, e.g. 0.15-0.35,0.4-0.6,0.7-0.9
# Exits 0 on success, 1 when the event or build failed, 2 for bad arguments or when FMOD Studio is open.

set -u
cd "$(dirname "$0")/../.."

event="" bank=Master build=true dry=false fades=""
files=()
while (( $# )); do
    case "$1" in
        --bank) bank="$2"; shift ;;
        --fades) fades="$2"; shift ;;
        --no-build) build=false ;;
        --dry-run) dry=true; build=false ;;
        event:/*) event="$1" ;;
        *) files+=("$1") ;;
    esac
    shift
done

if [[ -z "$event" || ${#files[@]} -eq 0 ]]; then
    echo "Usage: Tools/FMOD/fmod-music-event.sh <event:/Music/Name> <stem file>... [--bank Master] [--fades a-b,...] [--no-build] [--dry-run]"
    exit 2
fi
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

count=${#files[@]}
if [[ -z "$fades" ]]; then
    case $count in
        2) fades="0.3-0.6" ;;
        3) fades="0.2-0.4,0.55-0.8" ;;
        *) fades="0.15-0.35,0.4-0.6,0.7-0.9" ;;
    esac
fi
IFS=, read -ra ranges <<<"$fades"
list="" names="" fadelist="null"
for (( i = 0; i < count; i++ )); do
    f="${files[$i]}"
    [[ -f "$f" ]] || { echo "No file $f"; exit 2; }
    list+="\"$(cygpath -m "$(cd "$(dirname "$f")" && pwd)/$(basename "$f")")\","
    stem=$(basename "${f%.*}")
    names+="\"${stem##*_}\","
    if (( i > 0 )); then
        range="${ranges[$((i - 1))]:-}"
        [[ "$range" =~ ^([0-9.]+)-([0-9.]+)$ ]] || { echo "Missing or bad fade range for stem $((i + 1)): '$range'"; exit 2; }
        fadelist+=",[${BASH_REMATCH[1]},${BASH_REMATCH[2]}]"
    fi
done

mkdir -p Temp
script=Temp/fmod-music-event.js
{
    echo "var Params = { eventPath: \"$event\", bank: \"$bank\", parameter: \"Intensity\", audioFiles: [${list%,}], trackNames: [${names%,}], fades: [$fadelist], dryRun: $dry };"
    cat Tools/FMOD/add_music_event.js
} >"$script"

out=$("$cli" -script "$(cygpath -w "$script")" "$(cygpath -w FMOD/FMOD.fspro)" 2>&1)
result=$(sed -n 's/.*FMOD_MUSIC: //p' <<<"$out")
if [[ "$result" != ok* ]]; then
    echo "Could not create $event: ${result:-no result from the script}"
    [[ -z "$result" ]] && tail -5 <<<"$out"
    exit 1
fi
echo "Created ${result#ok }"

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
