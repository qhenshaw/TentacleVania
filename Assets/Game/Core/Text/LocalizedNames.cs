using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace TentacleVania.Core.Text
{
    public static class LocalizedNames
    {
        public const string Table = "UI";

        private static readonly Dictionary<string, string> Cache = new();
        private static bool _hooked;

        public static string Get(string key, string fallback)
        {
            if (string.IsNullOrEmpty(key) || !Application.isPlaying)
                return fallback;
            Hook();
            if (Cache.TryGetValue(key, out var cached))
                return cached;
            var value = LocalizationSettings.StringDatabase.GetLocalizedString(Table, key);
            var result = string.IsNullOrEmpty(value) || value.StartsWith("No translation", System.StringComparison.Ordinal) ? fallback : value;
            Cache[key] = result;
            return result;
        }

        private static void Hook()
        {
            if (_hooked)
                return;
            _hooked = true;
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }

        private static void OnLocaleChanged(Locale locale) => Cache.Clear();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Cache.Clear();
            if (_hooked)
                LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            _hooked = false;
        }
    }
}
