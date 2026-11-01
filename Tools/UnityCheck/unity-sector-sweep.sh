#!/usr/bin/env bash
# Sweeps every sector in MapAtlas (or the given sector ids): opens its root scene, plays it (bypassing the main menu)
# and runs evals/sector-doors.cs (every plan doorway raycast clear, plans from World.Editor.SectorPlans) and then
# evals/sector-walk.cs (the creature moved through every room; each must load, become current and not fall), polling
# each result rather than timing frames. Prints PASS or FAIL per sector with the evals' findings. A sector added to
# MapAtlas also needs its plan in SectorPlans for the door check ("no plan" passes without it).
#
# Usage: Tools/UnityCheck/unity-sector-sweep.sh [sector id]...   (e.g. SH BL; default every sector)
# Exits 0 when every sector passed, 1 when any failed, 2 when Unity can't be reached or a run didn't finish.

set -u
source "$(dirname "$0")/common.sh"
Evals=Tools/UnityCheck/evals

run_file() {
    local out
    out=$("${Unity[@]}" eval_file --file "$1" 2>&1)
    grep -q '"success": true' <<<"$out" || { echo "ERROR $(json_string message <<<"$out")"; return 1; }
    json_string result <<<"$out"
}

# polls a SessionState string until it's set, up to $2 seconds
wait_log() {
    local key="$1" timeout="$2" start=$SECONDS value
    while (( SECONDS - start < timeout )); do
        sleep 2
        value=$(eval_result "return UnityEditor.SessionState.GetString(\"$key\", \"\");" 2>/dev/null) || continue
        [[ -n "$value" ]] && { echo "$value"; return 0; }
    done
    return 1
}

stop_play() {
    eval_result 'UnityEditor.EditorApplication.ExitPlaymode(); return 0;' >/dev/null 2>&1
    for _ in $(seq 1 30); do
        sleep 1
        [[ "$(eval_result 'return UnityEditor.EditorApplication.isPlaying;' 2>/dev/null)" == false ]] && break
    done
    eval_result 'PlayFromMainMenu.Bypass = false; return 0;' >/dev/null 2>&1
}

wait_idle 180 || exit 2
[[ "$(eval_result 'return UnityEditor.EditorApplication.isPlaying;')" == false ]] || { echo "Unity is in play mode; stop it first."; exit 2; }
eval_result 'UnityEditor.EditorPrefs.SetInt("InteractionMode", 1); return 0;' >/dev/null
sectors=$(eval_result 'var s = ""; foreach (var r in TentacleVania.World.Map.MapAtlas.Instance.Sectors) if (r != null) s += r.Sector + ":" + r.RootScene + ":" + r.Rooms.Count + "\n"; return s;') || exit 2

failed=0 ran=0
while IFS=: read -r sector root rooms; do
    [[ -z "$sector" ]] && continue
    [[ -n "${*:-}" && " $* " != *" $sector "* ]] && continue
    ran=$((ran + 1))
    started=$(eval_result "var p = System.Linq.Enumerable.FirstOrDefault(System.Linq.Enumerable.Select(UnityEditor.AssetDatabase.FindAssets(\"t:Scene $root\"), UnityEditor.AssetDatabase.GUIDToAssetPath), x => System.IO.Path.GetFileNameWithoutExtension(x) == \"$root\"); if (p == null) return \"missing\"; UnityEditor.SceneManagement.EditorSceneManager.OpenScene(p); UnityEditor.SessionState.EraseString(\"doorsLog\"); UnityEditor.SessionState.EraseString(\"walkLog\"); PlayFromMainMenu.Bypass = true; UnityEditor.EditorApplication.EnterPlaymode(); return \"ok\";") || exit 2
    if [[ "$started" != ok ]]; then
        echo "FAIL $sector: root scene $root not found"
        failed=$((failed + 1))
        continue
    fi

    ready=""
    for _ in $(seq 1 60); do
        sleep 2
        ready=$(eval_result 'return TentacleVania.World.Streaming.RoomStreamer.Active != null && TentacleVania.Creature.CreatureSpawner.Spawned != null;' 2>/dev/null) || continue
        [[ "$ready" == true ]] && break
    done
    if [[ "$ready" != true ]]; then
        echo "FAIL $sector: the creature never spawned in $root"
        failed=$((failed + 1))
        stop_play
        continue
    fi
    sleep 3

    doors=$(run_file "$Evals/sector-doors.cs") && doors=$(wait_log doorsLog 60) || doors="${doors:-timed out}"
    walk=$(run_file "$Evals/sector-walk.cs") && walk=$(wait_log walkLog $((rooms * 20 + 30))) || walk="${walk:-timed out}"
    stop_play

    if [[ ( "$doors" == *"all clear"* || "$doors" == "no plan"* ) && "$walk" == *done && "$walk" != *FAIL* ]]; then
        echo "PASS $sector ($rooms rooms): $doors"
    else
        echo "FAIL $sector ($rooms rooms)"
        echo "  doors: $doors"
        echo "  walk: $walk"
        failed=$((failed + 1))
    fi
done <<<"$sectors"

(( ran == 0 )) && { echo "No sector matched."; exit 2; }
(( failed == 0 ))
