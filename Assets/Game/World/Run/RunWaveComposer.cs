using System.Collections.Generic;
using TentacleVania.Core.Runs;
using TentacleVania.World.Arenas;
using UnityEngine;

namespace TentacleVania.World.Run
{
    public static class RunWaveComposer
    {
        public static ArenaWaveSet Compose(System.Random rng, RunKit.FloorPool pool, RoomRole role, int step)
        {
            var waves = new List<ArenaWaveSet.Wave>();
            if (pool == null || pool.Enemies.Count == 0)
                return ArenaWaveSet.Create(waves);
            var elite = role == RoomRole.Elite;
            var waveCount = elite ? 2 : step < 4 ? 2 : 3;
            for (var w = 0; w < waveCount; w++)
            {
                var wave = new ArenaWaveSet.Wave();
                var count = 2 + step / 3 + w;
                if (pool.Prey.Count > 0)
                    wave.Enemies.Add(new ArenaWaveSet.Entry { Prefab = pool.Prey[rng.Next(pool.Prey.Count)], Count = 1 });
                for (var i = 0; i < count; i++)
                {
                    var prefab = pool.Enemies[rng.Next(pool.Enemies.Count)];
                    var makeElite = elite && i == 0;
                    Add(wave, prefab, makeElite);
                }
                waves.Add(wave);
            }
            return ArenaWaveSet.Create(waves);
        }

        private static void Add(ArenaWaveSet.Wave wave, TentacleVania.Enemies.Enemy prefab, bool elite)
        {
            foreach (var entry in wave.Enemies)
                if (entry.Prefab == prefab && entry.Elite == elite)
                {
                    entry.Count++;
                    return;
                }
            wave.Enemies.Add(new ArenaWaveSet.Entry { Prefab = prefab, Count = 1, Elite = elite });
        }
    }
}
