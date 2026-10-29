using System.Diagnostics;
using System.IO;
using TentacleVania.Core;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public sealed partial class GameVersionBuild : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    public const string ResourceFile = "Assets/Game/Core/Version/Resources/GameVersion.txt";
    public const string BuildFile = "version.txt";

    public int callbackOrder => 0;

    [OnCodeLoaded]
    private static void OnLoad()
    {
        Refresh();
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.ExitingEditMode)
            Refresh();
    }

    public static string Refresh() => GameVersion.Override = Compute();

    public static string Compute()
    {
        var count = Git("rev-list --count HEAD");
        var hash = Git("rev-parse --short HEAD");
        return int.TryParse(count, out var commits) ? GameVersion.Format(commits, hash) : GameVersion.Fallback;
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        var version = Refresh();
        Directory.CreateDirectory(Path.GetDirectoryName(ResourceFile));
        File.WriteAllText(ResourceFile, version);
        AssetDatabase.ImportAsset(ResourceFile, ImportAssetOptions.ForceSynchronousImport);
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        var folder = Path.GetDirectoryName(report.summary.outputPath);
        if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
            File.WriteAllText(Path.Combine(folder, BuildFile), GameVersion.Text);
    }

    private static string Git(string arguments)
    {
        try
        {
            var info = new ProcessStartInfo("git", arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Directory.GetCurrentDirectory()
            };
            using var process = Process.Start(info);
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(5000);
            return process.HasExited && process.ExitCode == 0 ? output : string.Empty;
        }
        catch (System.Exception)
        {
            return string.Empty;
        }
    }
}
