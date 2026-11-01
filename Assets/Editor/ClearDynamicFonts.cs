using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine.TextCore.Text;

public static partial class ClearDynamicFonts
{
    public const string FontFolder = "Assets/Game/UI/Fonts";

    [OnCodeLoaded]
    private static void OnLoad() => EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

    private static void OnPlayModeStateChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredEditMode)
            Clear();
    }

    [MenuItem("TentacleVania/UI/Clear Dynamic Font Atlases")]
    public static void Clear()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:FontAsset", new[] { FontFolder }))
        {
            var font = AssetDatabase.LoadAssetAtPath<FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
            if (font == null || font.atlasPopulationMode != AtlasPopulationMode.Dynamic || font.glyphTable.Count == 0)
                continue;
            font.ClearFontAssetData(true);
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssetIfDirty(font);
        }
    }
}
