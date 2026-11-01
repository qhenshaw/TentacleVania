#!/usr/bin/env bash
# Runs a C# file in the Unity Editor without the CLI's 30 s limit. The file's statements are wrapped in a lambda that
# runs once on the next EditorApplication.update (delayCall doesn't fire while the editor is unfocused) and writes its
# result or exception to EditorPrefs; this script polls for that.
# Calls fail while the main thread is busy running the work, so a failed poll just means "not done yet".
#
# Usage: Tools/UnityCheck/unity-long-eval.sh <file.cs> [timeout-seconds]   (default 600)
# The file holds statements ending in `return <value>;`, like ShaderErrors.cs. It can't have `using` directives, so
# use fully qualified names.

set -u
file="${1:?Usage: unity-long-eval.sh <file.cs> [timeout-seconds]}"
file=$(cd "$(dirname "$file")" && pwd)/$(basename "$file")
source "$(dirname "$0")/common.sh"

Timeout="${2:-600}"
Key="UnityCheck.LongEval.$$.$RANDOM"
Wrapper=Temp/unity-long-eval.cs

if grep -qE '^\s*using\s+[A-Za-z_.]+\s*;' "$file"; then
    echo "$file has using directives; use fully qualified names instead."
    exit 2
fi

mkdir -p Temp
{
    echo 'System.Func<object> work = () => {'
    cat "$file"
    echo '};'
    echo 'UnityEditor.EditorApplication.CallbackFunction once = null;'
    echo 'once = () => {'
    echo '    UnityEditor.EditorApplication.update -= once;'
    echo '    string outcome;'
    echo '    try { outcome = "ok\n" + work(); }'
    echo '    catch (System.Exception e) { outcome = "error\n" + e; }'
    echo "    UnityEditor.EditorPrefs.SetString(\"$Key\", outcome);"
    echo '};'
    echo 'UnityEditor.EditorApplication.update += once;'
    echo 'return "scheduled";'
} >"$Wrapper"

# right after a domain reload eval_file can answer "File not found" for a file that exists, so wait and retry
wait_idle 180 || exit 2
for attempt in 1 2 3 4 5; do
    scheduled=$("${Unity[@]}" eval_file --file "$Wrapper" 2>&1)
    [[ "$(json_string result <<<"$scheduled")" == scheduled ]] && break
    sleep 3
done
if [[ "$(json_string result <<<"$scheduled")" != scheduled ]]; then
    echo "Could not start $file:"
    sed 's/^/  /' <<<"$scheduled"
    exit 2
fi

start=$SECONDS
while true; do
    sleep 3
    # a busy main thread fails the poll; that only means "not done yet", so its error stays quiet
    outcome=$(eval_result "return UnityEditor.EditorPrefs.GetString(\"$Key\", \"\");" 2>/dev/null)
    [[ -n "$outcome" ]] && break
    if (( SECONDS - start > Timeout )); then
        echo "Timed out after ${Timeout}s. The work may still be running; its result will land in EditorPrefs \"$Key\"."
        exit 2
    fi
done

eval_result "UnityEditor.EditorPrefs.DeleteKey(\"$Key\"); return 0;" >/dev/null 2>&1
tail -n +2 <<<"$outcome"
[[ "$(head -1 <<<"$outcome")" == ok ]]
