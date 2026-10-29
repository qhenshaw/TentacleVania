#!/usr/bin/env bash
# Prints Unity console errors (and optionally warnings) logged since the last call, one line per distinct message with
# a repeat count and the first Assets/ stack frame, so a console of thousands of warnings stays readable.
#
# Usage: Tools/UnityCheck/unity-console.sh [--all] [--warnings]
#   --all       ignore the saved cursor and show everything the buffer still holds
#   --warnings  include warnings as well as errors and exceptions

set -u
source "$(dirname "$0")/common.sh"

CursorFile=Temp/unity-console.cursor
all=false warnings=false
for arg in "$@"; do
    case "$arg" in
        --all) all=true ;;
        --warnings) warnings=true ;;
        *) echo "Unknown option: $arg"; exit 2 ;;
    esac
done

since=0 session=""
if ! $all && [[ -f "$CursorFile" ]]; then read -r since session <"$CursorFile"; fi

now=$(console_cursor) || { echo "Could not reach the Unity Editor. Is it open on this project?"; exit 2; }

errors=$(console_since error "$since" "$session")
[[ -n "$errors" ]] && { echo "Errors:"; sed 's/^/  /' <<<"$errors"; }
if $warnings; then
    warns=$(console_since warn "$since" "$session")
    [[ -n "$warns" ]] && { echo "Warnings:"; sed 's/^/  /' <<<"$warns"; }
fi
[[ -z "$errors" && ( ! $warnings || -z "${warns:-}" ) ]] && echo "Nothing new."

mkdir -p Temp && echo "$now" >"$CursorFile"
exit 0
