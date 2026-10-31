using TentacleVania.Creature.Rig;
using TentacleVania.Enemies;
using UnityEngine;

namespace TentacleVania.Benchmark
{
    [CreateAssetMenu(menuName = "TentacleVania/Benchmark Settings")]
    public sealed class BenchmarkSettings : ScriptableObject
    {
        public const string ResourcePath = "BenchmarkSettings";

        [Tooltip("Room scene the benchmark fights in; its arena is held so the waves don't start.")]
        [SerializeField] private string _room = "SH_A5";
        [Tooltip("Placeholder Apex rig: the largest size stage, near the 20-chain budget.")]
        [SerializeField] private CreatureRigSettings _apexRig;
        [SerializeField] private Enemy[] _enemies = System.Array.Empty<Enemy>();
        [Tooltip("Spawned once for the boss-size effect load (beams, music, boss bar).")]
        [SerializeField] private Enemy _boss;
        [SerializeField, Min(1)] private int _enemyCount = 12;
        [SerializeField, Min(1f)] private float _ringRadius = 5f;
        [SerializeField, Min(0f)] private float _warmUp = 15f;
        [Tooltip("Seconds between weapon taps from the virtual pad.")]
        [SerializeField, Min(0.05f)] private float _tapInterval = 0.35f;
        [Tooltip("Seconds between killing one enemy (gore, orb) and spawning a replacement.")]
        [SerializeField, Min(0.5f)] private float _killInterval = 3f;
        [Tooltip("Seconds between consumes (maw chains active).")]
        [SerializeField, Min(1f)] private float _consumeInterval = 7f;

        public string Room => _room;
        public CreatureRigSettings ApexRig => _apexRig;
        public Enemy[] Enemies => _enemies;
        public Enemy Boss => _boss;
        public int EnemyCount => _enemyCount;
        public float RingRadius => _ringRadius;
        public float WarmUp => _warmUp;
        public float TapInterval => _tapInterval;
        public float KillInterval => _killInterval;
        public float ConsumeInterval => _consumeInterval;
    }
}
