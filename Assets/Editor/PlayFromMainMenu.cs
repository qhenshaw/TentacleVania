using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

public static partial class PlayFromMainMenu
{
    public const string BootstrapScene = "Assets/Game/Scenes/Bootstrap.unity";
    public const string BypassKey = "TentacleVania.PlayOpenScene";

    [OnCodeLoaded]
    private static void OnLoad() => EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

    private static void OnPlayModeStateChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.ExitingEditMode)
            Apply();
    }

    public static bool Bypass
    {
        get => SessionState.GetBool(BypassKey, false);
        set
        {
            SessionState.SetBool(BypassKey, value);
            Apply();
        }
    }

    public static void Apply() =>
        EditorSceneManager.playModeStartScene = Bypass ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScene);
}
