using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;

public static class PrefabEdit
{
    public static GameObject Edit(string path, Action<GameObject> edit)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null)
            throw new ArgumentException($"No prefab at {path}");
        var shadows = new Dictionary<string, ShadowCastingMode>();
        foreach (var effect in asset.GetComponentsInChildren<VisualEffect>(true))
            if (effect.TryGetComponent<Renderer>(out var renderer))
                shadows[PathOf(effect.transform, asset.transform)] = renderer.shadowCastingMode;

        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            edit(root);
            foreach (var effect in root.GetComponentsInChildren<VisualEffect>(true))
                if (effect.TryGetComponent<Renderer>(out var renderer)
                    && shadows.TryGetValue(PathOf(effect.transform, root.transform), out var mode))
                    renderer.shadowCastingMode = mode;
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static string PathOf(Transform transform, Transform root)
    {
        var path = transform.name;
        while (transform != root && transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }
}
