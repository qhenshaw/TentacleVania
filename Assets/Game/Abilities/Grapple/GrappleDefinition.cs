using FMODUnity;
using UnityEngine;

namespace TentacleVania.Abilities.Grapple
{
    [CreateAssetMenu(menuName = "TentacleVania/Abilities/Tentacle Grapple")]
    public sealed class GrappleDefinition : AbilityDefinition
    {
        [Header("Tentacle Grapple (design: Tentacle Grapple page)")]
        [SerializeField, Min(1f)] private float _range = 10f;
        [SerializeField, Min(0f)] private float _cooldown = 2f;
        [Tooltip("Targets further than this from the aim direction are ignored.")]
        [SerializeField, Range(10f, 180f)] private float _maxAimAngle = 70f;
        [SerializeField, Min(0.01f)] private float _shootTime = 0.12f;
        [SerializeField, Min(1f)] private float _zipSpeed = 28f;
        [SerializeField] private Vector2 _zipDuration = new(0.2f, 0.4f);
        [SerializeField, Min(0f)] private float _anchorArc = 1.6f;
        [SerializeField, Min(0f)] private float _enemyArc = 0.3f;
        [Tooltip("How far short of an enemy the zip stops.")]
        [SerializeField, Min(0f)] private float _enemyStopDistance = 1.6f;
        [SerializeField] private EventReference _shootSound;
        [SerializeField] private EventReference _zipSound;
        [SerializeField] private EventReference _latchSound;

        public override bool SlotAbility => false;
        public float Range => _range;
        public float Cooldown => _cooldown;
        public float MaxAimAngle => _maxAimAngle;
        public float ShootTime => _shootTime;
        public float ZipSpeed => _zipSpeed;
        public Vector2 ZipDuration => _zipDuration;
        public float AnchorArc => _anchorArc;
        public float EnemyArc => _enemyArc;
        public float EnemyStopDistance => _enemyStopDistance;
        public EventReference ShootSound => _shootSound;
        public EventReference ZipSound => _zipSound;
        public EventReference LatchSound => _latchSound;

        public override AbilityRuntime CreateRuntime(AbilityContext context) => null;
    }
}
