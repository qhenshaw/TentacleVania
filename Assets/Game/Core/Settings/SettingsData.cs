using System;

namespace TentacleVania.Core.Settings
{
    public enum DisplayModeSetting
    {
        Borderless,
        Exclusive,
        Windowed,
    }

    public enum ColorblindMode
    {
        Off,
        Protanopia,
        Deuteranopia,
        Tritanopia,
    }

    [Serializable]
    public sealed class SettingsData
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public float MasterVolume = 1f;
        public float MusicVolume = 0.8f;
        public float SfxVolume = 1f;
        public float VoiceVolume = 1f;
        public float DamageTakenScale = 1f;
        public float DamageDealtScale = 1f;
        public float EnemySpeedScale = 1f;
        public bool KeepBiomassOnDeath;
        public bool ScreenShake = true;
        public bool Flashes = true;
        public bool Gore = true;
        public bool Rumble = true;
        public bool Subtitles = true;
        public bool SpeedrunTimer;
        public ColorblindMode Colorblind;
        public string Language = "";
        public DisplayModeSetting DisplayMode;
        public int Width;
        public int Height;
        public bool VSync = true;
        public int FrameCap;
        public string Bindings = "";

        public void Clamp()
        {
            MasterVolume = Clamp01(MasterVolume);
            MusicVolume = Clamp01(MusicVolume);
            SfxVolume = Clamp01(SfxVolume);
            VoiceVolume = Clamp01(VoiceVolume);
            DamageTakenScale = Range(DamageTakenScale, 0.25f, 2f);
            DamageDealtScale = Range(DamageDealtScale, 0.5f, 3f);
            EnemySpeedScale = Range(EnemySpeedScale, 0.5f, 1f);
            if (FrameCap < 0)
                FrameCap = 0;
        }

        private static float Clamp01(float value) => Range(value, 0f, 1f);

        private static float Range(float value, float min, float max) => float.IsNaN(value) ? max : Math.Min(max, Math.Max(min, value));
    }
}
