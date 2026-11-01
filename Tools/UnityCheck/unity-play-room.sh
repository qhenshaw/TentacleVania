#!/usr/bin/env bash
# Plays a room for testing: opens the root scene of the sector that holds the room, enters play mode (bypassing the
# main menu), moves the creature to the room's centre (snapped to its NavMesh) once it loads, and optionally turns on
# cheats (god mode, grapple unlocked) and starts the room's arena at a wave. Play mode stays on so you can play it or
# drive it with evals; the editor menu TentacleVania > World > Play Active Room does the same for the open room scene
# with cheats. The logic is in Assets/Editor/PlayRoom.cs.
#
# Usage: Tools/UnityCheck/unity-play-room.sh <room scene> [--cheats] [--wave n]   (e.g. SH_A5 --cheats --wave 3)
# Exits 0 once the creature is in the room, 1 when the room or its arena isn't found, 2 when Unity can't be reached.

set -u
source "$(dirname "$0")/common.sh"
room="${1:?Usage: unity-play-room.sh <room scene> [--cheats] [--wave n]}"
shift
cheats=false wave=0
while (( $# )); do
    case "$1" in
        --cheats) cheats=true ;;
        --wave) wave="$2"; shift ;;
        *) echo "Unknown option: $1"; exit 2 ;;
    esac
    shift
done

wait_idle 180 || exit 2
started=$(eval_result "return PlayRoom.Start(\"$room\", $cheats, $wave);") || exit 2
echo "$started"
[[ "$started" == ERROR* ]] && exit 1

for _ in $(seq 1 60); do
    sleep 1
    log=$(eval_result "return UnityEditor.SessionState.GetString(PlayRoom.LogKey, \"\");" 2>/dev/null) || continue
    [[ -z "$log" || "$log" == starting ]] && continue
    echo "$log"
    [[ "$log" == *ERROR* ]] && exit 1
    exit 0
done
echo "Play mode didn't report back within 60 s."
exit 2
