#!/usr/bin/env bash
# Emergency restart: force-closes every running Unity Editor (all projects and worktrees, plus their worker
# processes), kills any Tools/UnityCheck scripts still waiting on them, reopens this folder's project and waits until
# the editor answers and has finished compiling. Use it when the editor stops responding (checks or tests hang with an
# idle-looking editor, evals time out). Unsaved scene changes in any editor are lost.
#
# Usage: Tools/UnityCheck/unity-restart.sh [timeout-seconds]   (default 600)
# Exits 0 when the reopened editor is idle, 1 when it didn't come back in time.

set -u
source "$(dirname "$0")/common.sh"
timeout="${1:-600}"
project="$(cd "$(dirname "$0")/../.." && pwd)"

for pid in $(ps -ef | grep -E "UnityCheck/unity-(test|check|play|console|long-eval)" | grep -v grep | grep -v unity-restart | awk '{print $2}'); do
    kill "$pid" 2>/dev/null
done
taskkill //F //IM Unity.exe //T >/dev/null 2>&1
for worker in "Unity.ILPP.Runner.exe" "UnityShaderCompiler.exe" "Unity.Licensing.Client.exe"; do
    taskkill //F //IM "$worker" //T >/dev/null 2>&1
done
start=$SECONDS
while tasklist //FI "IMAGENAME eq Unity.exe" 2>/dev/null | grep -qi "Unity.exe"; do
    (( SECONDS - start > 60 )) && { echo "Unity.exe is still running after 60 s."; exit 1; }
    sleep 1
done
echo "Closed all Unity Editors. Reopening $project."
unity projects open "$(cygpath -w "$project")" --non-interactive >/dev/null

start=$SECONDS
until [[ "$(eval_result 'return UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating;' 2>/dev/null)" == false ]]; do
    if (( SECONDS - start > timeout )); then
        echo "The editor didn't become idle within ${timeout}s."
        exit 1
    fi
    sleep 5
done
echo "Unity is back and idle after $((SECONDS - start))s."
