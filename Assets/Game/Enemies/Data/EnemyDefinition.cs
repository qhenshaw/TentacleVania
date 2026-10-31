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

        [Header("Stagger (bar defaults to Max Health when 0)")]
        [SerializeField, Min(0f)] private float _maxStagger;
        [SerializeField, Min(0f)] private float _staggerStun = 2f;
        [SerializeField, Min(0f)] private float _staggerDamageTaken = 1.5f;

        [Header("Consume")]
        [SerializeField, Min(0f)] private float _feedableDamageTaken = 0.5f;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float _moveSpeed = 4.5f;
        [SerializeField, Min(0f)] private float _turnSpeed = 540f;
        [SerializeField, Min(0f)] private float _spawnDelay = 0.5f;
        [SerializeField] private Vector2 _circleRange = new(3.5f, 5.5f);
        [Tooltip("Multiplies the hit feedback knockback (0 = immovable).")]
        [SerializeField, Min(0f)] private float _knockbackScale = 1f;

        [Header("Attacks")]
        [SerializeField] private List<EnemyAttack> _attacks = new();

        [Header("Consume (Balance Sheet: heal % of the creature's max HP, biomass)")]
        [SerializeField, Range(0f, 1f)] private float _consumeHeal = 0.12f;
        [SerializeField, Min(0)] private int _biomass = 10;

        [Header("Spawning")]
        [Tooltip("Spawns at the arena's wall mounts instead of its hatches (turrets).")]
        [SerializeField] private bool _spawnOnWall;

        [Header("Death")]
        [SerializeField, Min(0f)] private float _despawnDelay = 2f;

        [Header("Scientist behaviour")]
        [SerializeField, Min(0f)] private float _panicRange = 7f;
        [SerializeField, Min(0f)] private float _fleeDistance = 6f;
        [SerializeField, Min(0f)] private float _corneredDistance = 1.2f;

        [Header("Guard behaviour")]
        [SerializeField] private Vector2 _preferredRange = new(5f, 8f);

        [Header("Kind")]
        [Tooltip("Machines are never healed by Med-Drones.")]
        [SerializeField] private bool _machine;

        [Header("Med-Drone behaviour")]
        [SerializeField, Min(0f)] private float _healPerSecond = 8f;
        [SerializeField, Min(0f)] private float _healRange = 6f;
        [SerializeField, Min(0f)] private float _keepAwayRange = 7f;

        public string DisplayName => _displayName;
        public float MaxHealth => _maxHealth;
        public float MaxArmor => _maxArmor;
        public float MaxStagger => _maxStagger > 0f ? _maxStagger : _maxHealth;
        public float StaggerStun => _staggerStun;
        public float StaggerDamageTaken => _staggerDamageTaken;
        public float FeedableDamageTaken => _feedableDamageTaken;
        public float MoveSpeed => _moveSpeed;
        public float TurnSpeed => _turnSpeed;
        public float SpawnDelay => _spawnDelay;
        public Vector2 CircleRange => _circleRange;
        public float KnockbackScale => _knockbackScale;
        public IReadOnlyList<EnemyAttack> Attacks => _attacks;
        public float DespawnDelay => _despawnDelay;
        public bool SpawnOnWall => _spawnOnWall;
        public float ConsumeHeal => _consumeHeal;
        public int Biomass => _biomass;
        public float PanicRange => _panicRange;
        public float FleeDistance => _fleeDistance;
        public float CorneredDistance => _corneredDistance;
        public Vector2 PreferredRange => _preferredRange;
        public bool Machine => _machine;
        public float HealPerSecond => _healPerSecond;
        public float HealRange => _healRange;
        public float KeepAwayRange => _keepAwayRange;
    }
}
