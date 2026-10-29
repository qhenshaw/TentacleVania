#!/usr/bin/env bash
# Captures an open editor window (an EditorWindow such as BalanceSimulatorWindow) to a PNG from the window's own
# backbuffer, so it works while the editor is unfocused or covered and never reads the desktop. Prints the PNG path.
# Open the window first (its menu item, or EditorWindow.GetWindow in an eval).
#
# Usage: Tools/UnityCheck/unity-window-capture.sh <WindowTypeName> [out.png]
#   out.png defaults to Temp/Captures/window-<timestamp>.png at the project root
# Exits 0 with the path, 1 when no such window is open, 2 when Unity can't be reached.

set -u
source "$(dirname "$0")/common.sh"

[[ $# -lt 1 ]] && { echo "Usage: $0 <WindowTypeName> [out.png]"; exit 2; }
type="$1"
out="${2:-Temp/Captures/window-$(date +%Y%m%d-%H%M%S).png}"
wait_idle 120 || exit 2
mkdir -p Temp/Captures
eval_file="Temp/Captures/window-capture-eval.cs"
sed -e "s|__TYPE__|$type|g" -e "s|__OUT__|$out|g" "$(dirname "$0")/evals/window-capture.cs" > "$eval_file"
result=$("$(dirname "$0")/unity-long-eval.sh" "$eval_file") || exit 2
rm -f "$eval_file"
if [[ "$result" == ERROR* ]]; then echo "$result"; exit 1; fi
echo "$result"
