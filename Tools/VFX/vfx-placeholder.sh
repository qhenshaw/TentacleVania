#!/usr/bin/env bash
# Creates a placeholder VFX Graph asset from one of the VFX package templates, for features whose real effect
# the developer authors later: copies the template, makes a burst one-shot (spawner loop count 1) and optionally adds
# exposed float properties (unlinked, default 1) for the code to set. Existing assets are left untouched.
#
# Usage: Tools/VFX/vfx-placeholder.sh <Assets/.../Name.vfx> [burst|loop] [--float Name]...
#   burst (default): Simple_Burst, one-shot; play it with Reinit() + Play(), and give its prefab an empty initialEventName.
#   loop: Simple_Loop, plays continuously while enabled.
# Exits 0 on success, 1 on failure, 2 when the editor can't be reached.

set -u
cd "$(dirname "$0")/../.."
path="${1:?Usage: Tools/VFX/vfx-placeholder.sh <Assets/.../Name.vfx> [burst|loop] [--float Name]...}"
shift
kind=burst
floats=()
while (( $# )); do
    case "$1" in
        burst|loop) kind="$1" ;;
        --float) floats+=("$2"); shift ;;
        *) echo "Unknown argument: $1"; exit 1 ;;
    esac
    shift
done
template=Simple_Burst
[[ "$kind" == loop ]] && template=Simple_Loop
float_list=$(IFS=,; echo "${floats[*]:-}")

mkdir -p Temp
eval_file="Temp/vfx-placeholder-eval.cs"
cat > "$eval_file" <<EOF
var path = "$path";
var oneShot = "$kind" == "burst";
var floats = "$float_list".Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
if (UnityEditor.AssetDatabase.LoadMainAssetAtPath(path) != null) return "exists, left untouched: " + path;
var folder = System.IO.Path.GetDirectoryName(path).Replace('\\\\', '/');
System.IO.Directory.CreateDirectory(folder);
UnityEditor.AssetDatabase.Refresh();
if (!UnityEditor.AssetDatabase.CopyAsset("Packages/com.unity.visualeffectgraph/Editor/Templates/$template.vfx", path)) return "ERROR copy failed";
var asm = System.AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Unity.VisualEffectGraph.Editor");
var resType = System.AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEditor.VFX.VisualEffectResource")).First(t => t != null);
var resource = resType.GetMethod("GetResourceAtPath", flags).Invoke(null, new object[] { path });
var graph = asm.GetType("UnityEditor.VFX.VisualEffectResourceExtensions").GetMethod("GetGraph", flags).Invoke(null, new object[] { resource });
var modelType = asm.GetType("UnityEditor.VFX.VFXModel");
var setting = modelType.GetMethod("SetSettingValue", flags, null, new[] { typeof(string), typeof(object) }, null);
if (oneShot)
{
    var spawner = ((System.Collections.IEnumerable)graph.GetType().GetProperty("children", flags).GetValue(graph)).Cast<object>().First(c => c.GetType().Name == "VFXBasicSpawner");
    setting.Invoke(spawner, new object[] { "loopCount", System.Enum.Parse(spawner.GetType().GetField("loopCount", flags).FieldType, "Constant") });
    foreach (var s in ((System.Collections.IEnumerable)spawner.GetType().GetProperty("inputSlots", flags).GetValue(spawner)).Cast<object>())
        if ((string)s.GetType().GetProperty("name", flags).GetValue(s) == "LoopCount")
            s.GetType().GetProperty("value", flags).SetValue(s, 1);
}
var paramType = asm.GetType("UnityEditor.VFX.VFXParameter");
var y = -200f;
foreach (var name in floats)
{
    var p = UnityEngine.ScriptableObject.CreateInstance(paramType);
    paramType.GetMethod("Init", flags).Invoke(p, new object[] { typeof(float) });
    setting.Invoke(p, new object[] { "m_ExposedName", name });
    setting.Invoke(p, new object[] { "m_Exposed", true });
    paramType.GetMethod("CreateDefaultNode", flags).Invoke(p, new object[] { new UnityEngine.Vector2(-600f, y += 80f) });
    var outSlot = ((System.Collections.IEnumerable)paramType.GetProperty("outputSlots", flags).GetValue(p)).Cast<object>().First();
    outSlot.GetType().GetProperty("value", flags).SetValue(outSlot, 1f);
    modelType.GetMethod("AddChild", flags).Invoke(graph, new object[] { p, -1, true });
}
resType.GetMethod("WriteAsset", flags).Invoke(resource, null);
UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceUpdate);
var props = new System.Collections.Generic.List<UnityEngine.VFX.VFXExposedProperty>();
UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.VFX.VisualEffectAsset>(path).GetExposedProperties(props);
return "created " + path + " ($kind" + (oneShot ? ", one-shot" : "") + ") exposed: " + string.Join(",", props.Select(x => x.name));
EOF
Tools/UnityCheck/unity-long-eval.sh "$eval_file"
