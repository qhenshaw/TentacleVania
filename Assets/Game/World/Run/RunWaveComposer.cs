using System.Collections.Generic;
using TentacleVania.Core.Runs;
using TentacleVania.Enemies;
using TentacleVania.World.Arenas;

namespace TentacleVania.World.Run
{
    public static class RunWaveComposer
    {
        private static readonly List<WaveSlot> Slots = new(32);

        public static ArenaWaveSet Compose(System.Random rng, RunKit.FloorPool pool, RoomRole role, int step)
        {
            var waves = new List<ArenaWaveSet.Wave>();
            if (pool == null)
                return ArenaWaveSet.Create(waves);
            RunWaves.Plan(rng, pool.Enemies.Count, pool.Prey.Count, role, step, Slots);
            foreach (var slot in Slots)
            {
                while (waves.Count <= slot.Wave)
                    waves.Add(new ArenaWaveSet.Wave());
                var prefab = slot.Prey ? pool.Prey[slot.Enemy] : pool.Enemies[slot.Enemy];
                Add(waves[slot.Wave], prefab, slot.Elite, slot.Prey);
            }
            return ArenaWaveSet.Create(waves);
        }

        private static void Add(ArenaWaveSet.Wave wave, Enemy prefab, bool elite, bool prey)
        {
            foreach (var entry in wave.Enemies)
                if (entry.Prefab == prefab && entry.Elite == elite && entry.AnyHealth == prey)
                {
                    entry.Count++;
                    return;
                }
            wave.Enemies.Add(new ArenaWaveSet.Entry { Prefab = prefab, Count = 1, Elite = elite, AnyHealth = prey });
        }
    }
}
