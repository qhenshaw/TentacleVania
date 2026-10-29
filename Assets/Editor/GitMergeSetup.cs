using System;
using System.Diagnostics;
using System.IO;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

static partial class GitMergeSetup
{
    const string DriverKey = "merge.unityyamlmerge.driver";

    [OnCodeLoaded]
    static void OnCodeLoaded()
        => EditorApplication.delayCall += Configure;

    static void Configure()
    {
        var projectRoot = Path.GetDirectoryName(Application.dataPath);
        if (!Directory.Exists(Path.Combine(projectRoot, ".git")))
            return;

        var toolName = Application.platform == RuntimePlatform.WindowsEditor ? "UnityYAMLMerge.exe" : "UnityYAMLMerge";
        var toolPath = Path.Combine(EditorApplication.applicationContentsPath, "Tools", toolName).Replace('\\', '/');
        if (!File.Exists(toolPath))
        {
            Debug.LogWarning($"GitMergeSetup: UnityYAMLMerge not found at {toolPath}");
            return;
        }

        var driver = $"\"{toolPath}\" merge -h -p --force %O %B %A %A";
        if (!TryRunGit(projectRoot, $"config --local --get {DriverKey}", out var current))
            return;
        if (current == driver)
            return;

        if (TryRunGit(projectRoot, "config --local merge.unityyamlmerge.name \"Unity SmartMerge (UnityYAMLMerge)\"", out _)
            && TryRunGit(projectRoot, $"config --local {DriverKey} {Quote(driver)}", out _)
            && TryRunGit(projectRoot, "config --local merge.unityyamlmerge.recursive binary", out _))
        {
            Debug.Log($"GitMergeSetup: configured UnityYAMLMerge merge driver ({toolPath})");
        }
    }

    static bool TryRunGit(string workingDirectory, string arguments, out string output)
    {
        output = string.Empty;
        ProcessStartInfo startInfo = new("git", arguments)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        try
        {
            using var process = Process.Start(startInfo);
            output = process.StandardOutput.ReadToEnd().Trim();
            process.StandardError.ReadToEnd();
            if (!process.WaitForExit(10000))
            {
                process.Kill();
                return false;
            }
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"GitMergeSetup: couldn't run git ({e.Message})");
            return false;
        }
    }

    static string Quote(string argument)
        => $"\"{argument.Replace("\"", "\\\"")}\"";
}
