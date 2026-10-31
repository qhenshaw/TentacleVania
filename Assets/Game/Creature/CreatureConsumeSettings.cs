using FMODUnity;
using TentacleVania.Combat;
using UnityEngine;

namespace TentacleVania.Creature
{
    [CreateAssetMenu(menuName = "TentacleVania/Creature/Consume Settings")]
    public sealed class CreatureConsumeSettings : ScriptableObject
    {
        [SerializeField, Min(0f)] private float _reach = 2.2f;
        [SerializeField, Range(0f, 1f)] private float _grabThreshold = Consumable.DefaultGrabThreshold;
        [SerializeField, Min(0.01f)] private float _healDuration = 1.5f;
        [Tooltip("Machines (non-bleeding, not boss cores) give this fraction of the charge meter instead of healing.")]
        [SerializeField, Range(0f, 1f)] private float _machineCharge = 0.25f;
        [SerializeField, Range(0f, 1f)] private float _damageTakenWhileConsuming = 0.5f;

        [Header("Sounds")]
        [SerializeField] private EventReference _openSound;
        [SerializeField] private EventReference _chompSound;
        [SerializeField] private EventReference _swallowSound;

        public float Reach => _reach;
        public float GrabThreshold => _grabThreshold;
        public float HealDuration => _healDuration;
        public float MachineCharge => _machineCharge;
        public float DamageTakenWhileConsuming => _damageTakenWhileConsuming;
        public EventReference OpenSound => _openSound;
        public EventReference ChompSound => _chompSound;
        public EventReference SwallowSound => _swallowSound;
    }
}
