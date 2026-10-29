#!/usr/bin/env bash
# Enters play mode, lets the game run, then prints the errors and exceptions it logged and leaves play mode. Play mode
# starts in whichever scene is open. The editor doesn't run play mode frames while it isn't the focused application,
# so whenever it is unfocused the script pauses play mode and steps it ten frames per poll.
#
# Usage: Tools/UnityCheck/unity-play-check.sh [seconds]   (default 10 seconds)
# Exits 0 when no errors were logged, 1 when some were, 2 when Unity can't be reached or the game didn't start.

set -u
source "$(dirname "$0")/common.sh"

seconds=10
for arg in "$@"; do
    case "$arg" in
        [0-9]*) seconds="$arg" ;;
        *) echo "Unknown option: $arg"; exit 2 ;;
    esac
done

# Advances play mode: lets it run while the editor is focused, otherwise pauses it and steps ten frames.
pump() {
    eval_result 'if (!UnityEditor.EditorApplication.isPlaying) return 0; if (!UnityEditorInternal.InternalEditorUtility.isApplicationActive) { UnityEditor.EditorApplication.isPaused = true; for (int i = 0; i < 10; i++) UnityEditor.EditorApplication.Step(); } else UnityEditor.EditorApplication.isPaused = false; return UnityEngine.Time.frameCount;' >/dev/null 2>&1
}

wait_until() { # <timeout> <C# bool expression>
    local start=$SECONDS
    until [[ "$(eval_result "return $2;" 2>/dev/null)" == true ]]; do
        (( SECONDS - start > $1 )) && return 1
        pump
        sleep 0.5
    done
}

if [[ "$(eval_result 'return UnityEditor.EditorApplication.isPlaying;' 2>/dev/null)" == true ]]; then
    echo "Unity is already in play mode; stop it first."
    exit 2
fi
wait_idle 180 || exit 2
read -r cursor session < <(console_cursor) || { echo "Could not reach the Unity Editor."; exit 2; }

trap 'eval_result "UnityEditor.EditorApplication.isPaused = false; return 0;" >/dev/null 2>&1; "${Unity[@]}" editor.playmode stop >/dev/null 2>&1; wait_until 30 "!UnityEditor.EditorApplication.isPlaying" >/dev/null 2>&1; eval_result "PlayFromMainMenu.Bypass = false; return 0;" >/dev/null 2>&1' EXIT
eval_result 'PlayFromMainMenu.Bypass = true; return 0;' >/dev/null 2>&1
"${Unity[@]}" editor.playmode start >/dev/null 2>&1
if ! wait_until 60 'UnityEditor.EditorApplication.isPlaying'; then
    echo "Play mode didn't start within 60s."
    exit 2
fi

frame0=$(eval_result 'return UnityEngine.Time.frameCount;' 2>/dev/null)
start=$SECONDS
while (( SECONDS - start < seconds )); do
    pump
    sleep 0.5
done
frame1=$(eval_result 'return UnityEngine.Time.frameCount;' 2>/dev/null)
scene=$(eval_result 'return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;' 2>/dev/null)
echo "Ran ${seconds}s in $scene ($(( ${frame1:-0} - ${frame0:-0} )) frames)."

errors=$(console_since error "$cursor" "$session")
if [[ -n "$errors" ]]; then
    echo "Errors:"
    sed 's/^/  /' <<<"$errors"
    exit 1
fi
echo "No errors."
exit 0
