#!/usr/bin/env bash
# Smoke-tests a Windows player built by unity-build.sh: runs Builds/<label>/<ProjectName>.exe windowed at 1280x800
# with -logFile, has it save a screenshot after the given number of seconds and quit (CommandLineCapture), then lists
# exceptions and errors from the player log and prints the screenshot path. Read the PNG to check how the build looks.
#
# Usage: Tools/UnityCheck/unity-build-smoke.sh <label> [seconds]   (default 15 s)
# Exits 0 when the log is clean, 1 when it has exceptions or errors, 2 when the build is missing or no screenshot came.

set -u
cd "$(dirname "$0")/../.."
source Tools/project.sh

label="${1:?Usage: unity-build-smoke.sh <label> [seconds]}"
seconds="${2:-15}"
dir="Builds/$label"
exe="$dir/$PROJECT_NAME.exe"
log="$dir/smoke.log"
shot="$dir/smoke.png"

if [[ ! -f "$exe" ]]; then
    echo "No build at $exe; run Tools/UnityCheck/unity-build.sh $label first."
    exit 2
fi
rm -f "$log" "$shot"

"$exe" -screen-fullscreen 0 -screen-width 1280 -screen-height 800 -logFile "$(cygpath -aw "$log")" \
    -captureAfter "$seconds" "$(cygpath -aw "$shot")" >/dev/null 2>&1 &
pid=$!
limit=$((seconds + 30))
start=$SECONDS
while kill -0 "$pid" 2>/dev/null; do
    if (( SECONDS - start > limit )); then
        taskkill //IM "$PROJECT_NAME.exe" //F >/dev/null 2>&1
        echo "The player didn't quit within ${limit}s; killed it."
        break
    fi
    sleep 1
done

problems=$(grep -E "Exception|Error|error CS|not supported or has been stripped|Missing" "$log" 2>/dev/null \
    | grep -v "d3d12: failed to query info queue" | sort | uniq -c | sort -rn)
if [[ -n "$problems" ]]; then
    echo "Player log problems ($log):"
    sed 's/^/  /' <<<"$problems" | head -30
fi
if [[ ! -f "$shot" ]]; then
    echo "No screenshot was saved."
    exit 2
fi
echo "$shot"
[[ -z "$problems" ]]
