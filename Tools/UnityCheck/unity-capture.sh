#!/usr/bin/env bash
# Captures the Scene View to a PNG outside Assets/, optionally aiming it at a world position or a named GameObject
# first, and removes the Assets/Temp folder the capture command writes through. Prints the PNG's path. If shaders are
# still compiling after the first capture (new variants draw as placeholders), it waits and captures again.
#
# Usage: Tools/UnityCheck/unity-capture.sh [--at x,y,z | --object Name] [--size m] [--yaw deg] [--pitch deg]
#                                          [--w 640] [--h 360] [out.png]
#   --size   view size in metres around the target (default 100)
#   --yaw / --pitch   view direction in degrees (default 0 / 35)
#   out.png  defaults to Temp/Captures/<timestamp>.png at the project root

set -u
source "$(dirname "$0")/common.sh"

at="" object="" size=100 yaw=0 pitch=35 width=640 height=360 out=""
while (( $# )); do
    case "$1" in
        --at) at="$2"; shift ;;
        --object) object="$2"; shift ;;
        --size) size="$2"; shift ;;
        --yaw) yaw="$2"; shift ;;
        --pitch) pitch="$2"; shift ;;
        --w) width="$2"; shift ;;
        --h) height="$2"; shift ;;
        -*) echo "Unknown option: $1"; exit 2 ;;
        *) out="$1" ;;
    esac
    shift
done
[[ -z "$out" ]] && out="Temp/Captures/$(date +%Y%m%d-%H%M%S).png"

if [[ -n "$at" || -n "$object" ]]; then
    if [[ -n "$at" ]]; then
        pivot="new UnityEngine.Vector3(${at//,/f,}f)"
    else
        pivot="(UnityEngine.GameObject.Find(\"${object//\"/}\") ?? throw new System.Exception(\"No GameObject named ${object//\"/}\")).transform.position"
    fi
    aimed=$("${Unity[@]}" eval "var view = UnityEditor.SceneView.lastActiveSceneView; if (view == null) return \"no scene view\"; view.LookAt($pivot, UnityEngine.Quaternion.Euler(${pitch}f, ${yaw}f, 0f), ${size}f, false, true); view.Repaint(); return \"ok\";" 2>&1)
    if [[ "$(json_string result <<<"$aimed")" != ok ]]; then
        echo "Could not aim the Scene View:"
        sed 's/^/  /' <<<"$aimed"
        exit 2
    fi
    sleep 1
fi

result=$("${Unity[@]}" capture scene_view --width "$width" --height "$height" --save_path Temp/unity-capture.png 2>&1)
# a first render of new shader variants draws placeholders while they compile; wait them out and capture again
if [[ "$(eval_result 'return UnityEditor.ShaderUtil.anythingCompiling;' 2>/dev/null)" == true ]]; then
    for _ in $(seq 1 30); do
        sleep 1
        [[ "$(eval_result 'return UnityEditor.ShaderUtil.anythingCompiling;' 2>/dev/null)" == false ]] && break
    done
    result=$("${Unity[@]}" capture scene_view --width "$width" --height "$height" --save_path Temp/unity-capture.png 2>&1)
fi
if [[ ! -f Assets/Temp/unity-capture.png ]]; then
    echo "Capture failed:"
    sed 's/^/  /' <<<"$result"
    exit 2
fi

mkdir -p "$(dirname "$out")"
mv -f Assets/Temp/unity-capture.png "$out"
rm -rf Assets/Temp Assets/Temp.meta
echo "$out"
