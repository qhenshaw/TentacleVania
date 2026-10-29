#!/usr/bin/env bash
# Replaces the sound of an existing FMOD event with one or more audio files, keeping the event path, banks and any
# loop region, so code that plays the event needs no change. One file becomes a Single Instrument; several become a
# Multi Instrument that picks one at random per play (good for shots, hits and footsteps). Files outside the FMOD
# project are copied into FMOD/Assets/ by FMOD Studio on import. Drives the FMOD Studio command-line tool; FMOD Studio
# must not have the project open. Builds the banks and refreshes FMOD's event list in Unity unless told not to.
#
# Usage: Tools/FMOD/fmod-swap-sound.sh <event:/Path/Name> <audio file>... [--volume dB] [--layer] [--track n] [--no-build] [--dry-run]
#   --volume   instrument volume in dB (default: leave FMOD's default, 0 dB)
#   --layer    add the files on a new track that plays alongside the existing sound instead of replacing it
#   --track n  replace the sound on audio track n (from 1; default 1), e.g. one stem of a music event
#   --no-build skip the bank build, e.g. when swapping several events in a row; build with the last one
#   --dry-run  run the change without saving the project, to check paths and files
# Exits 0 on success, 1 when the swap or build failed, 2 for bad arguments or when FMOD Studio is open.

set -u
cd "$(dirname "$0")/../.."

event="" volume=null build=true dry=false layer=false track=1
files=()
while (( $# )); do
    case "$1" in
        --volume) volume="$2"; shift ;;
        --track) track="$2"; shift ;;
        --layer) layer=true ;;
        --no-build) build=false ;;
        --dry-run) dry=true; build=false ;;
        event:/*) event="$1" ;;
        *) files+=("$1") ;;
    esac
    shift
done

if [[ -z "$event" || ${#files[@]} -eq 0 ]]; then
    echo "Usage: Tools/FMOD/fmod-swap-sound.sh <event:/Path/Name> <audio file>... [--volume dB] [--layer] [--track n] [--no-build] [--dry-run]"
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

list=""
for f in "${files[@]}"; do
    [[ -f "$f" ]] || { echo "No file $f"; exit 2; }
    list+="\"$(cygpath -m "$(cd "$(dirname "$f")" && pwd)/$(basename "$f")")\","
done
mkdir -p Temp
script=Temp/fmod-swap-sound.js
{
    echo "var Params = { eventPath: \"$event\", audioFiles: [${list%,}], volume: $volume, layer: $layer, track: $track, dryRun: $dry };"
    cat Tools/FMOD/swap_event_sound.js
} >"$script"

out=$("$cli" -script "$(cygpath -w "$script")" "$(cygpath -w FMOD/FMOD.fspro)" 2>&1)
result=$(sed -n 's/.*FMOD_SWAP: //p' <<<"$out")
if [[ "$result" != ok* ]]; then
    echo "Could not swap $event: ${result:-no result from the script}"
    [[ -z "$result" ]] && tail -5 <<<"$out"
    exit 1
fi
echo "Swapped ${result#ok }"

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
