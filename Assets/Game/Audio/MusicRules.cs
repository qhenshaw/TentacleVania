using UnityEngine;

namespace TentacleVania.Audio
{
    public enum MusicState
    {
        Silent,
        Arena,
        Boss,
        NestRest,
    }

    public static class MusicRules
    {
        public const float FirstWave = 0.25f;

        public static float ArenaIntensity(int wave, int waveCount)
        {
            if (wave <= 0 || waveCount <= 0)
                return 0f;
            if (waveCount == 1)
                return 1f;
            return Mathf.Lerp(FirstWave, 1f, (Mathf.Min(wave, waveCount) - 1f) / (waveCount - 1f));
        }

        public static float BossIntensity(int phase, int phaseCount) =>
            phaseCount <= 1 ? 0.75f : Mathf.Lerp(0.5f, 1f, Mathf.Clamp01((float)phase / (phaseCount - 1)));

        public static MusicState Pick(bool bossFighting, bool arenaFighting, bool resting) =>
            bossFighting ? MusicState.Boss : arenaFighting ? MusicState.Arena : resting ? MusicState.NestRest : MusicState.Silent;
    }
}
