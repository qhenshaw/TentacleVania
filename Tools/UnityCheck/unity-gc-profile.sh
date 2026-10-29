#!/usr/bin/env bash
# Finds per-frame GC allocations in play: plays the open scene (normally GAMEPLAY_TEST_SCENE from Tools/project.sh)
# under the worst-case load set up at the top of evals/gc-profile-start.cs, records the Profiler, and prints the
# GC.Alloc samples of the last ~120 frames grouped by their three calling markers (bytes, sample count, path).
# Gameplay should show nothing but occasional UI text updates.
#
# Usage: Tools/UnityCheck/unity-gc-profile.sh [frames]   (default 1300; the report runs 200 frames before the end)
# Exits like unity-play-capture.sh.

set -u
cd "$(dirname "$0")/../.."
frames="${1:-1300}"
Tools/UnityCheck/unity-play-capture.sh --frames "$frames" --setup Tools/UnityCheck/evals/gc-profile-start.cs \
    --at $((frames - 200)) Tools/UnityCheck/evals/gc-profile-report.cs Temp/Captures/gc-profile.png 2>&1 \
    | sed -n '/gc-profile-report/,$p' | grep -v "gc-profile.png"
