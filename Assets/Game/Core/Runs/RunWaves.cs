using System;
using System.Collections.Generic;

namespace TentacleVania.Core.Runs
{
    public readonly struct WaveSlot
    {
        public WaveSlot(int wave, int enemy, bool prey, bool elite)
        {
            Wave = wave;
            Enemy = enemy;
            Prey = prey;
            Elite = elite;
        }

        public int Wave { get; }
        public int Enemy { get; }
        public bool Prey { get; }
        public bool Elite { get; }
    }

    public static class RunWaves
    {
        public const int BaseCount = 2;
        public const int StepsPerExtra = 3;
        public const int MaxCount = 8;
        public const int EliteWaves = 2;

        public static int WaveCount(RoomRole role, int step) => role == RoomRole.Elite ? EliteWaves : step < 4 ? 2 : 3;

        public static int EnemiesIn(int wave, int step) => Math.Min(MaxCount, BaseCount + Math.Max(0, step) / StepsPerExtra + wave);

        public static List<WaveSlot> Plan(Random rng, int enemyKinds, int preyKinds, RoomRole role, int step, List<WaveSlot> into = null)
        {
            into ??= new List<WaveSlot>(32);
            into.Clear();
            if (enemyKinds <= 0)
                return into;
            var waves = WaveCount(role, step);
            for (var wave = 0; wave < waves; wave++)
            {
                if (preyKinds > 0)
                    into.Add(new WaveSlot(wave, rng.Next(preyKinds), true, false));
                var count = EnemiesIn(wave, step);
                for (var i = 0; i < count; i++)
                    into.Add(new WaveSlot(wave, rng.Next(enemyKinds), false, role == RoomRole.Elite && i == 0));
            }
            return into;
        }
    }
}
