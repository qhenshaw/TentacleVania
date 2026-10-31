#!/usr/bin/env bash
# Builds the Windows player from the open Unity Editor into Builds/<label>/ (ignored by Git), waits for the build to
# finish, and prints the result, duration, size and any errors from the BuildReport. Uses the scenes in Build Settings.
# Afterwards it restores ProjectSettings.asset when the build only added preloadedAssets entries (Input System actions, Localization settings).
#
# Builds are Development builds (profiler, debug log) for testing; pass --release for a non-development player.
#
# After a successful build it zips the build and uploads it to Google Drive with unity-build-upload.sh (--no-upload skips that).
#
# Usage: Tools/UnityCheck/unity-build.sh [label] [timeout-seconds] [--release] [--no-upload]  (flags may go anywhere)   (default label: the short commit hash, 1800 s)
# Exits 0 when the build succeeded, 1 when it failed, 2 when Unity can't be reached or the build timed out.

set -u
source "$(dirname "$0")/common.sh"

options='["Development","DetailedBuildReport"]'
positional=()
upload=()
upload_enabled=1
for arg in "$@"; do
    if [[ "$arg" == --release ]]; then
        options='["DetailedBuildReport"]'
        upload+=(--release)
    elif [[ "$arg" == --no-upload ]]; then
        upload_enabled=0
    else
        positional+=("$arg")
    fi
done
label="${positional[0]:-$(git rev-parse --short HEAD)}"
timeout="${positional[1]:-1800}"
output="Builds/$label/$PROJECT_NAME.exe"

wait_idle 180 || exit 2
# files the build regenerates (URP variant prefiltering, preloaded assets, FMOD banks and Addressables output): restore
# the tracked ones that were clean and remove the untracked ones that didn't exist before, so the tree stays clean
generated=(Assets/Settings/PC_RPAsset.asset Assets/Settings/Mobile_RPAsset.asset Assets/Settings/UniversalRenderPipelineGlobalSettings.asset ProjectSettings/GraphicsSettings.asset)
clean=()
for path in "${generated[@]}"; do git diff --quiet HEAD -- "$path" 2>/dev/null && clean+=("$path"); done
created=(Assets/StreamingAssets Assets/StreamingAssets.meta Assets/AddressableAssetsData/link.xml Assets/AddressableAssetsData/link.xml.meta Assets/AddressableAssetsData/Windows Assets/AddressableAssetsData/Windows.meta Assets/AddressableAssetsData/ProfileDataSourceSettings.asset Assets/AddressableAssetsData/ProfileDataSourceSettings.asset.meta)
absent=()
for path in "${created[@]}"; do [[ -e "$path" ]] || absent+=("$path"); done
# Addressables asks once per user, in a modal dialog, whether to turn on its build report; unanswered, it blocks the build
eval_result 'var p = typeof(UnityEditor.AddressableAssets.Settings.ProjectConfigData).GetProperty("UserHasBeenInformedAboutBuildReportSettingPreBuild", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic); if (p != null) p.SetValue(null, true); return p != null;' >/dev/null || true
mkdir -p "Builds/$label"
queued=$("${Unity[@]}" build run --target StandaloneWindows64 --outputPath "$output" --options "$options" --confirm true 2>&1)
if ! grep -q '"success": true\|queued\|building' <<<"$queued"; then
    echo "Could not start the build:"
    sed 's/^/  /' <<<"$queued" | head -20
    exit 2
fi

start=$SECONDS
while true; do
    sleep 10
    status=$("${Unity[@]}" build status 2>/dev/null)
    grep -q '"status": "completed"' <<<"$status" && break
    if (( SECONDS - start > timeout )); then
        echo "Timed out after ${timeout}s waiting for the build."
        exit 2
    fi
done

result=$(json_string result <<<"$status")
[[ -z "$result" ]] && result=$(sed -n 's/^ *"result": "\{0,1\}\([A-Za-z]*\)"\{0,1\},\{0,1\}$/\1/p' <<<"$status" | head -1)
size=$(du -sh "Builds/$label" 2>/dev/null | cut -f1)
echo "Build $result in $(( SECONDS - start ))s: $output ($size)"
settings=ProjectSettings/ProjectSettings.asset
if git diff --quiet HEAD -- "$settings" 2>/dev/null; then :; elif ! git diff -U0 HEAD -- "$settings" | grep '^[-+] ' | grep -qv 'preloadedAssets\|{fileID: .*guid: .*type: [23]}'; then
    git checkout -q HEAD -- "$settings"
fi
(( ${#clean[@]} )) && git checkout -q HEAD -- "${clean[@]}" 2>/dev/null
for path in "${absent[@]}"; do rm -rf "$path"; done
errors=$(awk '/"errors": \[/,/\]/' <<<"$status" | sed -n 's/^ *"\(.*\)",\{0,1\}$/\1/p' | head -20)
if [[ "$result" != Succeeded ]]; then
    [[ -n "$errors" ]] && { echo "Errors:"; sed 's/^/  /' <<<"$errors"; }
    exit 1
fi
if (( upload_enabled )); then
    "$(dirname "$0")/unity-build-upload.sh" "$label" "${upload[@]}" || echo "Upload skipped or failed (see above); the build itself succeeded."
fi
exit 0
