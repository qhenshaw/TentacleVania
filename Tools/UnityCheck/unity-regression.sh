#!/usr/bin/env bash
# Regression sweep for release candidates: plays GAMEPLAY_TEST_SCENE (Tools/project.sh) once per content asset (each
# applied to the scene before play, under the regression setup), then runs the full flow from BOOTSTRAP_SCENE (main
# menu into gameplay, a few seconds of play), and prints PASS or FAIL per case with the console errors each one logged.
# Edits to the open scene are in memory only; GAMEPLAY_TEST_SCENE is reopened from disk at the end.
#
# The content block sweeps every weapon (Weapons/Data/Weapons): the setup equips it, spawns three paused guards in front
# of the creature and drives taps and charged heavies from TestGamepad; the report fails a weapon that landed no hits.
# Copy the block for other kinds of content (enemies, bosses), with their own glob, apply statement and report.
#
# Usage: Tools/UnityCheck/unity-regression.sh [content|flow|all]   (default all)
# Exits 0 when every case passed, 1 when any logged errors, 2 when the editor can't be reached.

set -u
cd "$(dirname "$0")/../.."
source Tools/UnityCheck/common.sh
what="${1:-all}"

dir=Temp/Regression
mkdir -p "$dir"
failed=0
long() { Tools/UnityCheck/unity-long-eval.sh "$1" 120 >/dev/null 2>&1; }

open_scene() {
    printf '%s\n' "UnityEditor.SceneManagement.EditorSceneManager.OpenScene(\"$1\");" 'return 0;' > "$dir/open.cs"
    long "$dir/open.cs"
}

run_case() {
    local name="$1" frames="$2"
    shift 2
    Tools/UnityCheck/unity-console.sh >/dev/null 2>&1
    local out errors
    out=$(Tools/UnityCheck/unity-play-capture.sh --frames "$frames" "$@" "Temp/Captures/regression-$name.png" 2>&1)
    errors=$(Tools/UnityCheck/unity-console.sh 2>&1 | grep -v "^Nothing new")
    if [[ -n "$errors" ]] || grep -q "ERROR" <<<"$out"; then
        failed=1
        echo "FAIL $name"
        grep "ERROR" <<<"$out" | sed 's/^/    /'
        sed 's/^/    /' <<<"$errors" | head -8
    else
        echo "PASS $name $(grep -o 'Frame [0-9]* [a-z-]*\.cs: .*' <<<"$out" | tail -1 | cut -c1-120)"
    fi
}

cat > "$dir/setup.cs" <<'EOF'
UnityEngine.Time.timeScale = 1f;
TentacleVania.Creature.CreatureVitals.GodMode = true;
var creature = TentacleVania.Combat.CombatTargets.Creature;
var weaponName = UnityEditor.SessionState.GetString("regressionWeapon", "");
var weapon = UnityEditor.AssetDatabase.LoadAssetAtPath<TentacleVania.Weapons.Data.WeaponDefinition>("Assets/Game/Weapons/Data/Weapons/" + weaponName + ".asset");
var controller = creature.GetComponent<TentacleVania.Weapons.WeaponController>();
if (weapon != null && controller != null)
    controller.Equip(0, weapon);
TentacleVania.Enemies.EnemyManager.Instance.Paused = true;
var guard = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Game/Enemies/Prefabs/SecurityGuard.prefab");
for (var i = 0; i < 3; i++)
{
    var dummy = UnityEngine.Object.Instantiate(guard, creature.transform.position + new UnityEngine.Vector3((i - 1) * 1.2f, 0f, 2.2f), UnityEngine.Quaternion.identity);
    dummy.name = "RegressionDummy" + i;
}
UnityEditor.SessionState.SetInt("regressionHits", 0);
var self = creature.GetComponent<TentacleVania.Combat.Health>();
TentacleVania.Combat.DamageHandler counter = null;
counter = (TentacleVania.Combat.Health target, in TentacleVania.Combat.Damage.DamageInfo info, in TentacleVania.Combat.Damage.DamageResult result) =>
{
    if (info.Attacker == self && target != self)
        UnityEditor.SessionState.SetInt("regressionHits", UnityEditor.SessionState.GetInt("regressionHits", 0) + 1);
};
TentacleVania.Combat.CombatEvents.HitLanded += counter;
var start = UnityEngine.Time.time;
UnityEditor.EditorApplication.CallbackFunction tick = null;
tick = () =>
{
    if (!UnityEngine.Application.isPlaying)
    {
        UnityEditor.EditorApplication.update -= tick;
        TentacleVania.Combat.CombatEvents.HitLanded -= counter;
        return;
    }
    var t = (UnityEngine.Time.time - start) % 2.6f;
    var press = t < 0.08f || (t > 0.4f && t < 0.48f) || (t > 0.8f && t < 0.88f) || (t > 1.3f && t < 1.9f);
    if (press)
        TestGamepad.Set(UnityEngine.Vector2.zero, UnityEngine.Vector2.up, UnityEngine.InputSystem.LowLevel.GamepadButton.West);
    else
        TestGamepad.Set(UnityEngine.Vector2.zero, UnityEngine.Vector2.up);
};
UnityEditor.EditorApplication.update += tick;
return "ok";
EOF
cat > "$dir/report.cs" <<'EOF'
var alive = 0;
foreach (var enemy in UnityEngine.Object.FindObjectsByType<TentacleVania.Enemies.Enemy>())
    if (enemy.name.StartsWith("RegressionDummy") && enemy.Health.IsAlive)
        alive++;
var hits = UnityEditor.SessionState.GetInt("regressionHits", 0);
return (hits > 0 ? "hits " : "ERROR no hits ") + hits + " with " + UnityEditor.SessionState.GetString("regressionWeapon", "?") + ", dummies alive " + alive;
EOF

if [[ "$what" == all || "$what" == content ]]; then
    for asset in Assets/Game/Weapons/Data/Weapons/*.asset; do
        name=$(basename "$asset" .asset)
        open_scene "$GAMEPLAY_TEST_SCENE"
        printf '%s\n' "var asset = UnityEditor.AssetDatabase.LoadMainAssetAtPath(\"$asset\");" \
            "UnityEditor.SessionState.SetString(\"regressionWeapon\", asset.name);" 'return 0;' > "$dir/content.cs"
        long "$dir/content.cs"
        run_case "content-$name" 1500 --setup "$dir/setup.cs" --at 1400 "$dir/report.cs"
    done
fi

if [[ "$what" == all || "$what" == flow ]]; then
    open_scene "$BOOTSTRAP_SCENE"
    printf '%s\n' 'TentacleVania.UI.Menu.MainMenuScreen.Instance.Play();' 'return "start";' > "$dir/flow-start.cs"
    printf '%s\n' 'var r = "";' 'for (var i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++) r += UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).name + " ";' 'return r;' > "$dir/scenes.cs"
    run_case "flow" 1400 --at 150 "$dir/flow-start.cs" --at 1300 "$dir/scenes.cs"
fi

open_scene "$GAMEPLAY_TEST_SCENE"
exit "$failed"
