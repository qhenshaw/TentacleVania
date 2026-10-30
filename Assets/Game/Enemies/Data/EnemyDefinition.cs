using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.Enemies.Data
{
    [CreateAssetMenu(menuName = "TentacleVania/Enemies/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Enemy";

        [Header("Vitals (Balance Sheet)")]
        [SerializeField, Min(1f)] private float _maxHealth = 40f;
        [SerializeField, Min(0f)] private float _maxArmor;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float _moveSpeed = 4.5f;
        [SerializeField, Min(0f)] private float _turnSpeed = 540f;
        [SerializeField, Min(0f)] private float _spawnDelay = 0.5f;
        [SerializeField] private Vector2 _circleRange = new(3.5f, 5.5f);

        [Header("Attacks")]
        [SerializeField] private List<EnemyAttack> _attacks = new();

        [Header("Death")]
        [SerializeField, Min(0f)] private float _despawnDelay = 2f;

        public string DisplayName => _displayName;
        public float MaxHealth => _maxHealth;
        public float MaxArmor => _maxArmor;
        public float MoveSpeed => _moveSpeed;
        public float TurnSpeed => _turnSpeed;
        public float SpawnDelay => _spawnDelay;
        public Vector2 CircleRange => _circleRange;
        public IReadOnlyList<EnemyAttack> Attacks => _attacks;
        public float DespawnDelay => _despawnDelay;
    }
}
