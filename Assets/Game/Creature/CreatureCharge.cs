using System;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Core.Characters;
using UnityEngine;

namespace TentacleVania.Creature
{
    [RequireComponent(typeof(Health))]
    public sealed class CreatureCharge : MonoBehaviour
    {
        [SerializeField] private CreatureChargeSettings _settings;

        private Health _health;
        private CharacterMaterials _materials;
        private bool _showingFull;

        public event Action<CreatureCharge> Changed;

        public ChargeMeter Meter { get; private set; }
        public float Current => Meter.Current;
        public float Max => Meter.Max;
        public bool IsFull => Meter.IsFull;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _materials = GetComponent<CharacterMaterials>();
            Meter = new(_settings.Max);
        }

        private void OnEnable() => CombatEvents.HitLanded += OnHitLanded;

        private void OnDisable() => CombatEvents.HitLanded -= OnHitLanded;

        public void AddConsume() => Add(_settings.PerConsume);

        public void Add(float amount)
        {
            if (Meter.Add(amount) > 0f)
                Refresh();
        }

        public bool TrySpend(float cost)
        {
            if (!Meter.TrySpend(cost))
                return false;
            Refresh();
            return true;
        }

        public void Set(float value)
        {
            Meter.Set(value);
            Refresh();
        }

        private void OnHitLanded(Health target, in DamageInfo info, in DamageResult result)
        {
            if (info.Attacker == _health && target != _health && (info.Flags & HitFlags.Hazard) == 0)
                Add(_settings.PerWeaponHit);
        }

        public void RefreshGlow()
        {
            _showingFull = !Meter.IsFull;
            Refresh();
        }

        private void Refresh()
        {
            if (_materials != null && Meter.IsFull != _showingFull)
            {
                _showingFull = Meter.IsFull;
                if (_showingFull)
                    _materials.SetStatus(_settings.FullGlow, _settings.FullGlowAmount, _settings.FullGlowPulse, _settings.FullGlowFresnel, _settings.FullGlowFill);
                else
                    _materials.ClearStatus();
            }
            Changed?.Invoke(this);
        }
    }
}
