#!/usr/bin/env bash
# Refreshes the open Unity Editor, waits for compilation and asset import to finish, then prints C# compile errors
# and shader / compute shader errors. Exits 0 when clean, 1 when there are errors, 2 when Unity can't be reached.
#
# Usage: Tools/UnityCheck/unity-check.sh [timeout-seconds]   (default 180)

set -u
source "$(dirname "$0")/common.sh"

Timeout="${1:-180}"

if ! "${Unity[@]}" eval 'UnityEditor.AssetDatabase.Refresh(); return 0;' >/dev/null 2>&1; then
    echo "Could not reach the Unity Editor. Is it open on this project?"
    exit 2
fi
wait_idle "$Timeout" || exit 2

status=0

compile=$("${Unity[@]}" recompile_status 2>/dev/null)
compileErrors=$(sed -n '/"errors": \[/,/\]/p' <<<"$compile" | sed -n 's/^ *"\(.*\)",\?$/\1/p' | sed 's/\\"/"/g; s/\\\\/\\/g')
if [[ -n "$compileErrors" ]]; then
    echo "Compile errors:"
    sed 's/^/  /' <<<"$compileErrors"
    status=1
elif grep -q '"compilationFailed": true' <<<"$compile"; then
    echo "Compilation failed (no messages reported; check the Unity console)."
    status=1
else
    echo "Scripts compiled cleanly."
fi

shaders=$("${Unity[@]}" eval_file --file Tools/UnityCheck/ShaderErrors.cs 2>&1)
if ! grep -q '"success": true' <<<"$shaders"; then
    echo "Shader check failed to run:"
    sed 's/^/  /' <<<"$shaders"
    exit 2
fi
shaderErrors=$(json_string result <<<"$shaders")
if [[ -n "$shaderErrors" ]]; then
    echo "Shader errors:"
    sed 's/^/  /' <<<"$shaderErrors"
    status=1
else
    echo "Shaders compiled cleanly."
fi

exit $status
