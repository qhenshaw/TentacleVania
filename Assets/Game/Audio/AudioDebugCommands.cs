using TentacleVania.Core;
using TentacleVania.Core.Debugging;
using TentacleVania.Core.Saves;
using UnityEngine;

namespace TentacleVania.Audio
{
    public static class AudioDebugCommands
    {
        public const string Tab = "Audio";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            DebugMenu.Value(Tab, "Music", () =>
            {
                var music = MusicDirector.Instance;
                return music != null ? $"{music.State}, intensity {music.Intensity:F2}" : "no music director";
            });
            DebugMenu.Toggle(Tab, "Mute music", () => MusicDirector.Muted, v => MusicDirector.Muted = v);
            DebugMenu.Value(Tab, "PA", () =>
            {
                var pa = PaDirector.Instance;
                if (pa == null)
                    return "no PA director";
                return pa.Speaking ? $"{pa.CurrentChannel}: {pa.CurrentText} (+{pa.QueuedCount} queued)" : $"idle, {pa.QueuedCount} queued";
            });
            var script = Resources.Load<PaScript>(PaScript.ResourcePath);
            if (script != null && script.Sequences.Count > 0)
            {
                var ids = new string[script.Sequences.Count];
                for (var i = 0; i < ids.Length; i++)
                    ids[i] = script.Sequences[i].Id;
                DebugMenu.Choice(Tab, "Play PA sequence", ids, index =>
                {
                    if (PaDirector.Instance == null)
                        return;
                    SaveSystem.Data.World.PlayedLines.Clear();
                    PaDirector.Instance.Play(ids[index], "SH");
                });
            }
            DebugMenu.Button(Tab, "Skip PA lines", () =>
            {
                if (PaDirector.Instance != null)
                    PaDirector.Instance.SkipAll();
            });
            DebugMenu.Button(Tab, "Reset played PA lines", () => SaveSystem.Data.World.PlayedLines.Clear());
            DebugMenu.Toggle(Tab, "Subtitles", () => GameSettings.Subtitles, v => GameSettings.Subtitles = v);
        }
    }
}
