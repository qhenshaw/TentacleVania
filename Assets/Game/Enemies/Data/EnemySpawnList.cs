using UnityEngine;

namespace TentacleVania.Enemies.Data
{
    [CreateAssetMenu(menuName = "TentacleVania/Enemies/Debug Spawn List")]
    public sealed class EnemySpawnList : ScriptableObject
    {
        public const string ResourcePath = "DebugEnemySpawnList";

        [SerializeField] private Enemy[] _prefabs;

        public Enemy[] Prefabs => _prefabs;
    }
}
