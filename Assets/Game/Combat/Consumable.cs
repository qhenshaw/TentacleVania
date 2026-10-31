using System;
using UnityEngine;

namespace TentacleVania.Combat
{
    [RequireComponent(typeof(Health))]
    public sealed class Consumable : MonoBehaviour
    {
        public const float DefaultGrabThreshold = 0.25f;

        [SerializeField, Range(0f, 1f)] private float _healFraction = 0.1f;
        [SerializeField, Min(0)] private int _biomass = 5;

        private Health _health;

        public event Action<Consumable> Grabbed;
        public event Action<Consumable> Consumed;

        public static float GrabThreshold { get; set; } = DefaultGrabThreshold;

        public Health Health => _health;
        public float HealFraction => _healFraction;
        public int Biomass => _biomass;
        public bool IsBeingConsumed { get; private set; }
        public bool Locked { get; set; }
        public bool IsGrabbable => CanBeGrabbed(GrabThreshold);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetThreshold() => GrabThreshold = DefaultGrabThreshold;

        private void Awake() => _health = GetComponent<Health>();

        public void Configure(float healFraction, int biomass)
        {
            _healFraction = healFraction;
            _biomass = biomass;
        }

        public bool CanBeGrabbed(float threshold) =>
            !Locked && !IsBeingConsumed && _health.IsAlive && _health.Normalized < threshold;

        public void BeginConsume()
        {
            IsBeingConsumed = true;
            Grabbed?.Invoke(this);
            CombatEvents.RaiseConsumeStarted(this);
        }

        public void Bite() => CombatEvents.RaiseConsumeBitten(this);

        public void FinishConsume()
        {
            Consumed?.Invoke(this);
            CombatEvents.RaiseConsumed(this);
            _health.Kill();
        }
    }
}
