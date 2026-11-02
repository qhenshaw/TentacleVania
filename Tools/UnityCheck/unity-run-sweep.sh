#!/usr/bin/env bash
# Autopilot run sweep (replaces unity-sector-sweep.sh for the roguelike): opens the Run scene, plays it (bypassing the
# main menu) and, for each seed, runs evals/run-sweep.cs, which restarts the run on that seed at 3x speed with god
# mode, breaks out of the pod, answers pick screens and skips every room through all floors and the finale. Each room
# must place the creature on the NavMesh and no error or exception may be logged. Prints PASS or FAIL per seed.
#
# Usage: Tools/UnityCheck/unity-run-sweep.sh [runs] [first seed]   (default 3 runs from seed 1)
# Exits 0 when every run passed, 1 when any failed, 2 when Unity can't be reached or a run didn't finish.

set -u
source "$(dirname "$0")/common.sh"
Evals=Tools/UnityCheck/evals
runs="${1:-3}"
first="${2:-1}"

run_file() {
    local out
    out=$("${Unity[@]}" eval_file --file "$1" 2>&1)
    grep -q '"success": true' <<<"$out" || { echo "ERROR $(json_string message <<<"$out")"; return 1; }
    json_string result <<<"$out"
}

wait_log() {
    local key="$1" timeout="$2" start=$SECONDS value
    while (( SECONDS - start < timeout )); do
        sleep 3
        value=$(eval_result "return UnityEditor.SessionState.GetString(\"$key\", \"\");" 2>/dev/null) || continue
        [[ -n "$value" ]] && { echo "$value"; return 0; }
    done
    return 1
}

stop_play() {
    eval_result 'UnityEngine.Time.timeScale = 1f; UnityEditor.EditorApplication.ExitPlaymode(); return 0;' >/dev/null 2>&1
    for _ in $(seq 1 30); do
        sleep 1
        [[ "$(eval_result 'return UnityEditor.EditorApplication.isPlaying;' 2>/dev/null)" == false ]] && break
    done
    eval_result 'PlayFromMainMenu.Bypass = false; return 0;' >/dev/null 2>&1
}

wait_idle 180 || exit 2
[[ "$(eval_result 'return UnityEditor.EditorApplication.isPlaying;')" == false ]] || { echo "Unity is in play mode; stop it first."; exit 2; }
eval_result 'UnityEditor.EditorPrefs.SetInt("InteractionMode", 1); return 0;' >/dev/null

failed=0
for (( seed = first; seed < first + runs; seed++ )); do
    started=$(eval_result "UnityEditor.SceneManagement.EditorSceneManager.OpenScene(\"Assets/Game/Scenes/Run/Run.unity\"); UnityEditor.SessionState.SetInt(\"runSeed\", $seed); UnityEditor.SessionState.EraseString(\"sweepLog\"); PlayFromMainMenu.Bypass = true; UnityEditor.EditorApplication.EnterPlaymode(); return \"ok\";") || exit 2
    [[ "$started" == ok ]] || { echo "FAIL seed $seed: $started"; failed=$((failed + 1)); continue; }

    ready=""
    for _ in $(seq 1 60); do
        sleep 2
        ready=$(eval_result 'return TentacleVania.World.Run.RunDirector.Active != null && !TentacleVania.World.Run.RunDirector.Active.Loading && TentacleVania.Creature.CreatureSpawner.Spawned != null;' 2>/dev/null) || continue
        [[ "$ready" == true ]] && break
    done
    if [[ "$ready" != true ]]; then
        echo "FAIL seed $seed: the run never started"
        failed=$((failed + 1))
        stop_play
        continue
    fi

    log=$(run_file "$Evals/run-sweep.cs") && log=$(wait_log sweepLog 420) || log="${log:-timed out}"
    stop_play
    if [[ "$log" == done*"won=True"*"errors 0"* && "$log" != *"|"* ]]; then
        echo "PASS ${log#done }"
    else
        echo "FAIL seed $seed: $log"
        failed=$((failed + 1))
    fi
done

(( failed == 0 ))
