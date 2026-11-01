using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;

namespace TentacleVania.Core.Settings
{
    public static class SettingsStore
    {
        public const string FileName = "settings.json";

        private static string _appliedBindings;
        private static string _appliedDisplay;

        public static SettingsData Data { get; private set; } = new();
        public static event Action<SettingsData> Applied;

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Data = new();
            Applied = null;
            _appliedBindings = null;
            _appliedDisplay = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            if (!Application.isPlaying)
                return;
            Data = Load(FilePath);
            Apply();
        }

        public static SettingsData Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new();
            try
            {
                var data = JsonUtility.FromJson<SettingsData>(json) ?? new SettingsData();
                data.Clamp();
                return data;
            }
            catch (ArgumentException)
            {
                return new();
            }
        }

        public static string Serialize(SettingsData data) => JsonUtility.ToJson(data, true);

        public static SettingsData Load(string path) => File.Exists(path) ? Parse(File.ReadAllText(path, Encoding.UTF8)) : new SettingsData();

        public static void Save()
        {
            var actions = InputSystem.actions;
            if (actions != null)
                _appliedBindings = Data.Bindings = actions.SaveBindingOverridesAsJson();
            var path = FilePath;
            var temp = path + ".tmp";
            File.WriteAllText(temp, Serialize(Data), Encoding.UTF8);
            if (File.Exists(path))
                File.Replace(temp, path, null);
            else
                File.Move(temp, path);
        }

        public static void ResetToDefaults()
        {
            var bindings = Data.Bindings;
            Data = new() { Bindings = bindings };
            Apply();
        }

        public static void Apply()
        {
            var data = Data;
            data.Clamp();
            GameSettings.ScreenShake = data.ScreenShake;
            GameSettings.Flashes = data.Flashes;
            GameSettings.Gore = data.Gore;
            GameSettings.Rumble = data.Rumble;
            GameSettings.Subtitles = data.Subtitles;
            GameSettings.SpeedrunTimer = data.SpeedrunTimer;
            GameSettings.KeepBiomassOnDeath = data.KeepBiomassOnDeath;
            GameSettings.DamageTakenScale = data.DamageTakenScale;
            GameSettings.DamageDealtScale = data.DamageDealtScale;
            GameSettings.EnemySpeedScale = data.EnemySpeedScale;
            GameSettings.Colorblind = data.Colorblind;
            ApplyBindings(data.Bindings);
            if (Application.isPlaying)
            {
                ApplyDisplay(data);
                ApplyLanguage(data.Language);
            }
            Applied?.Invoke(data);
        }

        private static void ApplyBindings(string json)
        {
            var actions = InputSystem.actions;
            if (actions == null || json == _appliedBindings)
                return;
            _appliedBindings = json;
            actions.RemoveAllBindingOverrides();
            if (!string.IsNullOrEmpty(json))
                actions.LoadBindingOverridesFromJson(json);
        }

        private static void ApplyDisplay(SettingsData data)
        {
            var key = $"{data.DisplayMode}|{data.Width}|{data.Height}|{data.VSync}|{data.FrameCap}";
            if (key == _appliedDisplay)
                return;
            _appliedDisplay = key;
            QualitySettings.vSyncCount = data.VSync ? 1 : 0;
            Application.targetFrameRate = data.VSync || data.FrameCap <= 0 ? -1 : data.FrameCap;
            if (Application.isEditor)
                return;
            var mode = data.DisplayMode switch
            {
                DisplayModeSetting.Exclusive => FullScreenMode.ExclusiveFullScreen,
                DisplayModeSetting.Windowed => FullScreenMode.Windowed,
                _ => FullScreenMode.FullScreenWindow,
            };
            var width = data.Width > 0 ? data.Width : Screen.currentResolution.width;
            var height = data.Height > 0 ? data.Height : Screen.currentResolution.height;
            Screen.SetResolution(width, height, mode);
        }

        private static void ApplyLanguage(string code)
        {
            if (string.IsNullOrEmpty(code) || !LocalizationSettings.InitializationOperation.IsDone)
                return;
            foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
            {
                if (locale.Identifier.Code != code)
                    continue;
                LocalizationSettings.SelectedLocale = locale;
                return;
            }
        }
    }
}
