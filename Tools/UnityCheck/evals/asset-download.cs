var asm = typeof(UnityEditor.EditorApplication).Assembly;
var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance;
var containerType = asm.GetType("UnityEditor.PackageManager.UI.Internal.ServicesContainer");
var container = containerType.BaseType.GetProperty("instance", flags).GetValue(null);
var managerType = asm.GetType("UnityEditor.PackageManager.UI.Internal.IAssetStoreDownloadManager");
var manager = containerType.GetMethod("Resolve").MakeGenericMethod(managerType).Invoke(container, null);
var ids = new System.Collections.Generic.List<long> { PRODUCT_IDS };
var mode = "MODE";
var result = new System.Text.StringBuilder();
if (mode == "start")
    managerType.GetMethod("Download").Invoke(manager, new object[] { ids });
foreach (var id in ids)
{
    var op = managerType.GetMethod("GetDownloadOperation").Invoke(manager, new object[] { (long?)id });
    if (op == null)
    {
        result.Append(id).Append(" idle\n");
        continue;
    }
    var type = op.GetType();
    result.Append(id)
        .Append(' ').Append(type.GetProperty("state", flags).GetValue(op))
        .Append(' ').Append(((float)type.GetProperty("progressPercentage", flags).GetValue(op) * 100f).ToString("F0")).Append('%')
        .Append(' ').Append(type.GetProperty("packageNewPath", flags).GetValue(op))
        .Append(' ').Append(type.GetProperty("errorMessage", flags).GetValue(op))
        .Append('\n');
}
return result.ToString();
