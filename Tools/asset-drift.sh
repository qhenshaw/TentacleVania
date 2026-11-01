#!/usr/bin/env bash
# Finds asset changes in the working tree that no task meant to make, the editor churn CLAUDE.md warns about:
#   - URP global settings whose m_RuntimeSettings changed (any editor rewrites it on import or save)
#   - ProjectSettings.asset whose preloadedAssets changed (builds add the Input System and Localization settings)
#   - dynamic font atlases under Assets/Game/UI/Fonts filled in by play mode
#   - an asset saved back to an older committed version (an open editor writing its stale copy over a pulled change)
#   - Creature.prefab tentacle renderers switched to cast shadows (LoadPrefabContents loads them On; use PrefabEdit)
# With --fix it restores the first four from HEAD (index and working tree) and reimports them in the open editor;
# the shadow change it only reports. A file changed for more than churn (other lines too) is reported, not restored.
# task-finish.sh runs it with --fix. To keep a flagged change on purpose, list its path in DRIFT_KEEP (space-separated).
#
# Usage: Tools/asset-drift.sh [--fix]
# Exits 0 when nothing is left to fix, 1 when something needs a hand fix (or, without --fix, any drift is found).

set -u
cd "$(dirname "$0")/.."
fix=0
[[ "${1:-}" == --fix ]] && fix=1

UrpSettings=Assets/Settings/UniversalRenderPipelineGlobalSettings.asset
ProjectSettings=ProjectSettings/ProjectSettings.asset
FontFolder=Assets/Game/UI/Fonts/
CreaturePrefab=Assets/Game/Creature/Prefabs/Creature.prefab

keep=" ${DRIFT_KEEP:-} "
restore=()
problems=0

# prints the YAML block that starts at key $2 (the key line and its deeper-indented lines), or with $3 = rest, the
# file without it
yaml_block() {
    awk -v key="$2" -v rest="${3:-}" '
        inside && match($0, /^ */) && RLENGTH <= depth && $0 !~ /^ *- / { inside = 0 }
        !inside && $0 ~ "^ *" key ":" { inside = 1; match($0, /^ */); depth = RLENGTH }
        (inside && rest == "") || (!inside && rest != "") { print }
    ' <<<"$1"
}

block_drift() {
    local path="$1" key="$2" head work
    head=$(git show "HEAD:$path" 2>/dev/null | tr -d '\r') || return 0
    work=$(tr -d '\r' <"$path")
    [[ "$(yaml_block "$head" "$key")" == "$(yaml_block "$work" "$key")" ]] && return 0
    if [[ "$(yaml_block "$head" "$key" rest)" == "$(yaml_block "$work" "$key" rest)" ]]; then
        echo "churn: $path ($key changed)"
        restore+=("$path")
    else
        echo "check: $path changes its $key block along with other lines; set the block back to HEAD's by hand"
        problems=$((problems + 1))
    fi
}

stale_version() {
    local path="$1" now commit blob
    now=$(git hash-object -- "$path" 2>/dev/null) || return 1
    [[ "$now" == "$(git rev-parse -q --verify "HEAD:$path" 2>/dev/null)" ]] && return 1
    for commit in $(git log -n 20 --format=%H -- "$path"); do
        blob=$(git rev-parse -q --verify "$commit^:$path" 2>/dev/null) || continue
        if [[ "$blob" == "$now" ]]; then
            echo "stale: $path matches its version before $(git log -1 --format='%h %s' "$commit")"
            return 0
        fi
    done
    return 1
}

while IFS= read -r path; do
    [[ -f "$path" && "$keep" != *" $path "* ]] || continue
    case "$path" in
        "$UrpSettings") block_drift "$path" m_RuntimeSettings; continue ;;
        "$ProjectSettings") block_drift "$path" preloadedAssets; continue ;;
    esac
    case "$path" in
        *.asset | *.prefab | *.unity | *.mat | *.controller | *.anim) ;;
        *) continue ;;
    esac
    if [[ "$path" == "$FontFolder"*.asset ]]; then
        echo "churn: $path (dynamic font atlas)"
        restore+=("$path")
    elif stale_version "$path"; then
        restore+=("$path")
    fi
    if [[ "$path" == "$CreaturePrefab" ]] && git diff HEAD -- "$path" | grep -q '^+ *m_CastShadows: 1'; then
        echo "check: $path turns on m_CastShadows; tentacle VisualEffect renderers must stay Off (edit it through PrefabEdit.Edit)"
        problems=$((problems + 1))
    fi
done < <(git diff --name-only HEAD 2>/dev/null)

if (( ${#restore[@]} )); then
    if (( fix )); then
        git restore --source=HEAD --staged --worktree -- "${restore[@]}"
        list=$(printf '"%s",' "${restore[@]}")
        unity command --caller plugin --skill unity-cli --result-only eval \
            "foreach (var p in new[] { ${list%,} }) UnityEditor.AssetDatabase.ImportAsset(p, UnityEditor.ImportAssetOptions.ForceUpdate); return 0;" >/dev/null 2>&1 \
            && echo "Restored and reimported ${#restore[@]} file(s)." \
            || echo "Restored ${#restore[@]} file(s); the editor couldn't be reached to reimport them."
    else
        problems=$((problems + ${#restore[@]}))
        echo "Run with --fix to restore them from HEAD."
    fi
fi
(( problems == 0 ))
