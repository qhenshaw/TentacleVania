namespace TentacleVania.Core
{
    public static class GameSettings
    {
        public static bool ScreenShake { get; set; } = true;
        public static bool Flashes { get; set; } = true;
        public static bool Rumble { get; set; } = true;
        public static bool Gore { get; set; } = true;
        public static bool Subtitles { get; set; } = true;
        public static bool KeepBiomassOnDeath { get; set; }
        public static bool TentacleMerge { get; set; } = true;
        public static float DamageDealtScale { get; set; } = 1f;
        public static float DamageTakenScale { get; set; } = 1f;
        public static float EnemySpeedScale { get; set; } = 1f;
        public static Settings.ColorblindMode Colorblind { get; set; }
    }
}
