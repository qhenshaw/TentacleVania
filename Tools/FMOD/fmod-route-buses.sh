#!/usr/bin/env bash
# Routes every FMOD event to a volume bus: event:/Music/ to bus:/Music, event:/VO/ to bus:/VO, all others to bus:/SFX (all
# created under the master bus when missing). The game's audio settings set the volumes of bus:/ (master), bus:/Music
# and bus:/SFX. fmod-placeholder.sh and fmod-music-event.sh route new events themselves; rerun this after adding events
# any other way. Drives the FMOD Studio command-line tool; FMOD Studio must not have the project open.
#
# Usage: Tools/FMOD/fmod-route-buses.sh [--no-build] [--dry-run]
# Exits 0 on success, 1 when the routing or build failed, 2 when FMOD Studio is open or its tool is missing.

set -u
cd "$(dirname "$0")/../.."

build=true dry=false
for arg in "$@"; do
    case "$arg" in
        --no-build) build=false ;;
        --dry-run) dry=true; build=false ;;
        *) echo "Unknown option: $arg"; exit 2 ;;
    esac
done
if tasklist 2>/dev/null | grep -qi "^FMOD Studio.exe"; then
    echo "FMOD Studio is running. Close it first so it doesn't overwrite the changes."
    exit 2
fi
version=$(sed -n 's/.*serializationModel="Studio\.0*\([0-9]*\)\.\([0-9]*\)\..*/\1.\2/p' FMOD/FMOD.fspro)
cli=$(ls -d "/c/Program Files/FMOD SoundSystem/FMOD Studio $version".* 2>/dev/null | sort -V | tail -1)/fmodstudiocl.exe
[[ -f "$cli" ]] || { echo "FMOD Studio $version command-line tool not found."; exit 2; }

mkdir -p Temp
script=Temp/fmod-route-buses.js
{
    echo "var Params = { dryRun: $dry };"
    cat Tools/FMOD/route_buses.js
} >"$script"
out=$("$cli" -script "$(cygpath -w "$script")" "$(cygpath -w FMOD/FMOD.fspro)" 2>&1)
result=$(sed -n 's/.*FMOD_ROUTE: //p' <<<"$out")
if [[ "$result" != ok* ]]; then
    echo "Routing failed: ${result:-no result from the script}"
    [[ -z "$result" ]] && tail -5 <<<"$out"
    exit 1
fi
echo "Routed ${result#ok }"

if $build; then
    out=$("$cli" -build "$(cygpath -w FMOD/FMOD.fspro)" 2>&1)
    grep -q "Build completed successfully" <<<"$out" || { echo "Bank build failed:"; tail -10 <<<"$out" | sed 's/^/  /'; exit 1; }
    echo "Banks built."
    unity command eval --caller plugin --skill unity-cli 'FMODUnity.EventManager.RefreshBanks(); return 0;' >/dev/null 2>&1 \
        && echo "Refreshed FMOD events in Unity."
fi
exit 0
