using System;
using System.Collections.Generic;
using TentacleVania.Enemies;
using UnityEngine;

namespace TentacleVania.World.Arenas
{
    [CreateAssetMenu(menuName = "TentacleVania/World/Arena Wave Set")]
    public sealed class ArenaWaveSet : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public Enemy Prefab;
            [Min(1)] public int Count = 1;
        }

        [Serializable]
        public sealed class Wave
        {
            public List<Entry> Enemies = new();

            public int Total
            {
                get
                {
                    var total = 0;
                    foreach (var entry in Enemies)
                        total += entry.Count;
                    return total;
                }
            }
        }

        [SerializeField] private List<Wave> _waves = new();
        [SerializeField, Min(0f)] private float _delayBetweenWaves = 2f;
        [SerializeField, Min(0f)] private float _spawnInterval = 0.3f;

        public IReadOnlyList<Wave> Waves => _waves;
        public float DelayBetweenWaves => _delayBetweenWaves;
        public float SpawnInterval => _spawnInterval;
    }
}
