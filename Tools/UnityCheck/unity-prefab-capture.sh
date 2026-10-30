#!/usr/bin/env bash
# Stages prefabs in a row away from the open scene's geometry (around 200,0,200, in edit mode, never saved), optionally
# poses each one's Animator child with an animation clip, captures the Scene View through unity-capture.sh and removes
# the staged objects again. Use it to check character models, skins, props and poses without entering play mode.
#
# Usage: Tools/UnityCheck/unity-prefab-capture.sh [--clip Assets/...fbx] [--time s] [--face-camera|--face-away]
#                                                 [--size m] [--yaw deg] [--pitch deg] [--w px] [--h px]
#                                                 <Assets/...prefab>... [-o out.png]
#   --clip   an AnimationClip asset (or the model file holding one) sampled on every staged Animator at --time
#   --size / --yaw / --pitch / --w / --h   passed to unity-capture.sh (defaults 1 m per prefab, 0, 15, 1280, 720)

set -u
source "$(dirname "$0")/common.sh"

clip="" time=0.3 facing=180 size="" yaw=0 pitch=15 width=1280 height=720 out="" prefabs=()
while (( $# )); do
    case "$1" in
        --clip) clip="$2"; shift ;;
        --time) time="$2"; shift ;;
        --face-camera) facing=180 ;;
        --face-away) facing=0 ;;
        --size) size="$2"; shift ;;
        --yaw) yaw="$2"; shift ;;
        --pitch) pitch="$2"; shift ;;
        --w) width="$2"; shift ;;
        --h) height="$2"; shift ;;
        -o) out="$2"; shift ;;
        -*) echo "Unknown option: $1"; exit 2 ;;
        *) prefabs+=("$1") ;;
    esac
    shift
done
(( ${#prefabs[@]} )) || { echo "Usage: $0 [options] <Assets/...prefab>..."; exit 2; }
[[ -z "$size" ]] && size=$(( ${#prefabs[@]} < 3 ? 3 : ${#prefabs[@]} ))

list=""
for p in "${prefabs[@]}"; do list+="\"$p\","; done
staged=$(eval_result "var paths = new string[] { ${list%,} };
var clip = \"$clip\" == \"\" ? null : UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AnimationClip>(\"$clip\");
var old = UnityEngine.GameObject.Find(\"__PrefabCapture\"); if (old != null) UnityEngine.Object.DestroyImmediate(old);
var holder = new UnityEngine.GameObject(\"__PrefabCapture\"); holder.hideFlags = UnityEngine.HideFlags.DontSave;
var spacing = 1.6f; var first = 200f - spacing * (paths.Length - 1) * 0.5f; var count = 0;
for (var i = 0; i < paths.Length; i++) {
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(paths[i]);
    if (prefab == null) continue;
    var e = UnityEngine.Object.Instantiate(prefab, new UnityEngine.Vector3(first + i * spacing, 0f, 200f), UnityEngine.Quaternion.Euler(0f, ${facing}f, 0f), holder.transform);
    if (clip != null) foreach (var a in e.GetComponentsInChildren<UnityEngine.Animator>()) clip.SampleAnimation(a.gameObject, ${time}f);
    count++;
}
return count;") || exit 2
[[ "$staged" == 0 ]] && { echo "No prefabs found."; eval_result 'var h = UnityEngine.GameObject.Find("__PrefabCapture"); if (h != null) UnityEngine.Object.DestroyImmediate(h); return 0;' >/dev/null; exit 2; }

args=(--at 200,0.9,200 --size "$size" --yaw "$yaw" --pitch "$pitch" --w "$width" --h "$height")
[[ -n "$out" ]] && args+=("$out")
bash Tools/UnityCheck/unity-capture.sh "${args[@]}"
status=$?
# the capture can coincide with a domain reload, so retry the cleanup until the editor answers
for _ in $(seq 1 30); do
    eval_result 'var h = UnityEngine.GameObject.Find("__PrefabCapture"); if (h != null) UnityEngine.Object.DestroyImmediate(h); return 0;' >/dev/null 2>&1 && break
    sleep 1
done
exit $status
