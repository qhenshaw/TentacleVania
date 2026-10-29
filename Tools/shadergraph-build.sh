#!/usr/bin/env bash
# Generates a Shader Graph from a JSON spec in the open Unity Editor, so shader logic lives in HLSL custom functions
# while the asset stays a Shader Graph. X.shadergraph.json produces X.shadergraph next to it (overwritten in place, so
# its GUID and material references survive). Then checks the shader for compile errors.
#
# Spec fields: target (Unlit | Lit), hlsl (asset path), vertexFunction, fragmentFunction, castShadows, properties
# [{ name, type: Texture2D | Float | Color | Vector, value | color | vector }]. See Assets/Editor/ShaderGraphTool.
#
# Usage: Tools/shadergraph-build.sh <Assets/.../Name.shadergraph.json>
# Exits 0 when the shader built and compiled, 1 on errors, 2 when Unity can't be reached.

set -u
spec="${1:?Usage: Tools/shadergraph-build.sh <Assets/.../Name.shadergraph.json>}"
source "$(dirname "$0")/UnityCheck/common.sh"

[[ -f "$spec" ]] || { echo "No spec at $spec"; exit 2; }
wait_idle 180 || exit 2
"${Unity[@]}" eval "UnityEditor.AssetDatabase.Refresh(); return 0;" >/dev/null 2>&1
wait_idle 180 || exit 2

graph=$(eval_result "return $ROOT_NAMESPACE.Editor.ShaderGraphTool.ShaderGraphBuilder.BuildFromSpec(\"$spec\");") || exit 1
messages=$(eval_result "var s = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Shader>(\"$graph\"); if (s == null) return \"missing shader\"; return string.Join(\"\\n\", System.Linq.Enumerable.Select(UnityEditor.ShaderUtil.GetShaderMessages(s), m => m.severity + \": \" + m.message + \" (\" + m.file + \":\" + m.line + \")\"));") || exit 1

echo "Built $graph"
if [[ -n "$messages" ]]; then
    sed 's/^/  /' <<<"$messages"
    grep -q "^Error\|missing shader" <<<"$messages" && exit 1
fi
exit 0
