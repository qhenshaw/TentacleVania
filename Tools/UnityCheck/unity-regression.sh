#!/usr/bin/env bash
# Regression sweep for release candidates: plays GAMEPLAY_TEST_SCENE (Tools/project.sh) once per content asset (each
# applied to the scene before play, under the regression setup), then runs the full flow from BOOTSTRAP_SCENE (main
# menu into gameplay, a few seconds of play), and prints PASS or FAIL per case with the console errors each one logged.
# Edits to the open scene are in memory only; GAMEPLAY_TEST_SCENE is reopened from disk at the end.
#
# Copy the content block once per kind of content worth sweeping (characters, weapons, levels...), each with its own
# asset glob, apply statement and case name, and add each kind to the usage line.
#
# Usage: Tools/UnityCheck/unity-regression.sh [content|flow|all]   (default all)
# Exits 0 when every case passed, 1 when any logged errors, 2 when the editor can't be reached.

set -u
cd "$(dirname "$0")/../.."
source Tools/UnityCheck/common.sh
what="${1:-all}"
if grep -q '{{Regression' "$0" && [[ "$what" != flow ]]; then
    if [[ "$what" == content ]]; then
        echo "The content hooks in $0 aren't filled in yet ({{Regression...}} placeholders)."
        exit 2
    fi
    echo "SKIP content (hooks not filled in yet)"
    what=flow
fi

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
{{RegressionSetup}}
return "ok";
EOF
cat > "$dir/report.cs" <<'EOF'
{{RegressionReport}}
EOF

if [[ "$what" == all || "$what" == content ]]; then
    for asset in {{RegressionContentGlob}}; do
        name=$(basename "$asset" .asset)
        open_scene "$GAMEPLAY_TEST_SCENE"
        printf '%s\n' "var asset = UnityEditor.AssetDatabase.LoadMainAssetAtPath(\"$asset\");" \
            '{{RegressionApplyContent}}' 'return 0;' > "$dir/content.cs"
        long "$dir/content.cs"
        run_case "content-$name" 900 --setup "$dir/setup.cs" --at 800 "$dir/report.cs"
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
