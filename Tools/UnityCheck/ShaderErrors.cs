// Lists error messages for every shader and compute shader under Assets/ and in local or embedded packages (the ones
// being developed alongside the project), one line each, for unity-check.sh.
// This file lives outside Assets/ so Unity never compiles it; it is run through `unity command eval_file`.
var lines = new System.Collections.Generic.List<string>();
var folders = new System.Collections.Generic.List<string> { "Assets" };
foreach (var package in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
    if (package.source == UnityEditor.PackageManager.PackageSource.Local || package.source == UnityEditor.PackageManager.PackageSource.Embedded)
        folders.Add(package.assetPath);

foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:Shader", folders.ToArray()))
{
    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
    var shader = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Shader>(path);
    if (shader == null || !UnityEditor.ShaderUtil.ShaderHasError(shader)) continue;
    foreach (var m in UnityEditor.ShaderUtil.GetShaderMessages(shader))
        if (m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)
            lines.Add($"{path}: {m.message} ({m.file}:{m.line}, {m.platform})");
}

foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:ComputeShader", folders.ToArray()))
{
    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
    var compute = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.ComputeShader>(path);
    if (compute == null) continue;
    foreach (var m in UnityEditor.ShaderUtil.GetComputeShaderMessages(compute))
        if (m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)
            lines.Add($"{path}: {m.message} ({m.file}:{m.line}, {m.platform})");
}

return string.Join("\n", System.Linq.Enumerable.Distinct(lines));
