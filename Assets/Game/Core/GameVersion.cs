using UnityEngine;

namespace TentacleVania.Core
{
    public static class GameVersion
    {
        public const string ResourcePath = "GameVersion";
        public const string Fallback = "dev";

        private static string _loaded;

        public static string Override { get; set; }

        public static string Text => string.IsNullOrEmpty(Override) ? _loaded ??= Load() : Override;

        public static string Format(int commitCount, string hash) =>
            string.IsNullOrEmpty(hash) ? $"v0.{commitCount}" : $"v0.{commitCount} ({hash})";

        private static string Load()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            var text = asset != null ? asset.text.Trim() : string.Empty;
            return text.Length > 0 ? text : Fallback;
        }
    }
}
