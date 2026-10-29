using System;
using System.Globalization;
using UnityEngine;

namespace TentacleVania.Core.Diagnostics
{
    public sealed class CommandLineCapture : MonoBehaviour
    {
        private const string Argument = "-captureAfter";
        private const float QuitDelay = 1f;

        private float _captureTime;
        private string _path;
        private bool _captured;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, Argument);
            if (index < 0 || index + 2 >= args.Length || !float.TryParse(args[index + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
                return;
            var host = new GameObject(nameof(CommandLineCapture));
            DontDestroyOnLoad(host);
            var capture = host.AddComponent<CommandLineCapture>();
            capture._captureTime = seconds;
            capture._path = args[index + 2];
        }

        private void Update()
        {
            var time = Time.realtimeSinceStartup;
            if (!_captured && time >= _captureTime)
            {
                _captured = true;
                ScreenCapture.CaptureScreenshot(_path);
                Debug.Log($"CommandLineCapture: saved {_path}");
            }
            if (_captured && time >= _captureTime + QuitDelay)
                Application.Quit();
        }
    }
}
