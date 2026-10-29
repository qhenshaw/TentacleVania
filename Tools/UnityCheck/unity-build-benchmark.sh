#!/usr/bin/env bash
# Runs the performance benchmark on a Windows player built by unity-build.sh: starts Builds/<label>/<ProjectName>.exe
# with `-benchmark <seconds> <report path>`, waits for it to quit and prints the report. The game implements the
# benchmark itself (a runtime component, not built yet): on that argument it sets up a repeatable heavy scene,
# warms up, records frame times and GC allocations for the given seconds, writes the report and quits. On a Steam Deck
# use the Steam launch options `-benchmark 60` instead (the report lands in the save folder as benchmark.txt). Desktop
# numbers don't stand in for the Deck budget.
#
# Usage: Tools/UnityCheck/unity-build-benchmark.sh <label> [seconds]   (default 60 s; use a --release build)
# Exits 0 when the report was written, 2 when the build is missing or no report came.

set -u
cd "$(dirname "$0")/../.."
source Tools/project.sh

label="${1:?Usage: unity-build-benchmark.sh <label> [seconds]}"
seconds="${2:-60}"
dir="Builds/$label"
exe="$dir/$PROJECT_NAME.exe"
log="$dir/benchmark.log"
report="$dir/benchmark.txt"

if [[ ! -f "$exe" ]]; then
    echo "No build at $exe; run Tools/UnityCheck/unity-build.sh $label --release first."
    exit 2
fi
rm -f "$log" "$report"

"$exe" -screen-fullscreen 0 -screen-width 1280 -screen-height 800 -logFile "$(cygpath -aw "$log")" \
    -benchmark "$seconds" "$(cygpath -aw "$report")" >/dev/null 2>&1 &
pid=$!
limit=$((seconds + 90))
start=$SECONDS
while kill -0 "$pid" 2>/dev/null; do
    if (( SECONDS - start > limit )); then
        taskkill //IM "$PROJECT_NAME.exe" //F >/dev/null 2>&1
        echo "The player didn't quit within ${limit}s; killed it."
        break
    fi
    sleep 1
done

if [[ ! -f "$report" ]]; then
    echo "No benchmark report was written; see $log."
    exit 2
fi
cat "$report"
