#!/usr/bin/env bash
# Runs the project's tests in the open Unity Editor and prints a one-line summary plus each failure with its message.
# Only <ROOT_NAMESPACE>.* tests (Tools/project.sh) run unless a different filter is given.
#
# Usage: Tools/UnityCheck/unity-test.sh [--mode EditMode|PlayMode] [--filter text] [timeout-seconds]   (default EditMode, 300)
# Exits 0 when every test passed, 1 when any failed, 2 when Unity can't be reached or the run didn't finish.

set -u
source "$(dirname "$0")/common.sh"

mode=EditMode filter=$ROOT_NAMESPACE timeout=300
while (( $# )); do
    case "$1" in
        --mode) mode="$2"; shift ;;
        --filter) filter="$2"; shift ;;
        [0-9]*) timeout="$1" ;;
        *) echo "Unknown option: $1"; exit 2 ;;
    esac
    shift
done

# right after a recompile the runner can report 0 tests before it has found them, so a run of 0 is retried
for attempt in 1 2 3; do
    wait_idle 180 || exit 2
    out=$("${Unity[@]}" test run --mode "$mode" --filter "$filter" --timeout "$timeout" 2>&1)
    [[ "$(json_number Total <<<"$out")" != 0 ]] && break
    sleep 5
done
if ! grep -q '"Summary"' <<<"$out"; then
    echo "Test run didn't finish:"
    sed 's/^/  /' <<<"$out" | head -20
    exit 2
fi

total=$(json_number Total <<<"$out")
passed=$(json_number Passed <<<"$out")
failed=$(json_number Failed <<<"$out")
skipped=$(json_number Skipped <<<"$out")
echo "$mode tests: $passed/$total passed, $failed failed, $skipped skipped."
if [[ "$total" == 0 && "$filter" == "$ROOT_NAMESPACE" ]]; then
    echo "No tests ran; the runner didn't find the project's tests."
    exit 2
fi

if (( ${failed:-0} > 0 )); then
    awk '
        /"FullName": "/ { name = $0; sub(/^ *"FullName": "/, "", name); sub(/",?$/, "", name) }
        /"Status": "Failed"/ { failing = 1 }
        /"Message": / && failing { msg = $0; sub(/^ *"Message": "?/, "", msg); sub(/"?,?$/, "", msg)
                                    gsub(/\\r/, "", msg); gsub(/\\n/, " ", msg); gsub(/  +/, " ", msg); printf "  FAILED %s: %s\n", name, substr(msg, 1, 300); failing = 0 }
    ' <<<"$out"
    exit 1
fi
exit 0
