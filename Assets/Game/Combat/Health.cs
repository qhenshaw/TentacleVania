using System;
using TentacleVania.Combat.Damage;
using TentacleVania.Core;
using UnityEngine;

namespace TentacleVania.Combat
{
    public delegate void DamageHandler(Health target, in DamageInfo info, in DamageResult result);

    public sealed class Health : MonoBehaviour
    {
        [SerializeField] private Team _team = Team.Site;
        [SerializeField, Min(1f)] private float _maxHealth = 100f;
        [SerializeField, Min(0f)] private float _maxArmor;

        private HealthPool _pool;

        public event DamageHandler Damaged;
        public event Action<Health, float> Healed;
        public event Action<Health> Died;

        public Team Team => _team;
        public ModifierStack Incoming { get; } = new();
        public ModifierStack Outgoing { get; } = new();
        public float Current => _pool.Health;
        public float Max => _pool.MaxHealth;
        public float Armor => _pool.Armor;
        public float MaxArmor => _pool.MaxArmor;
        public float Normalized => _pool.MaxHealth > 0f ? _pool.Health / _pool.MaxHealth : 0f;
        public bool IsAlive => _pool.IsAlive;

        private void Awake() => ResetToFull();

        public void Configure(Team team, float maxHealth, float maxArmor = 0f)
        {
            _team = team;
            _maxHealth = maxHealth;
            _maxArmor = maxArmor;
            ResetToFull();
        }

        public void ResetToFull()
        {
            var below = _pool.ProtectedBelow;
            var scale = _pool.ProtectedScale;
            _pool = new(_maxHealth, _maxArmor);
            SetProtection(below, scale);
        }

        public void SetProtection(float below, float scale)
        {
            _pool.ProtectedBelow = below;
            _pool.ProtectedScale = scale;
        }

        public DamageResult TakeDamage(in DamageInfo info)
        {
            ModifierStack outgoing = null;
            if (info.Attacker != null)
                outgoing = info.Attacker.Outgoing;
            var result = DamagePipeline.Resolve(info, _team, ref _pool, outgoing, Incoming, GameSettings.DamageDealtScale, GameSettings.DamageTakenScale);
            if (!result.Applied)
                return result;

            Damaged?.Invoke(this, info, result);
            CombatEvents.RaiseHit(this, info, result);
            if (result.Killed)
                Died?.Invoke(this);
            return result;
        }

        public float Heal(float amount)
        {
            if (!IsAlive || amount <= 0f)
                return 0f;
            var healed = Mathf.Min(amount, _pool.MaxHealth - _pool.Health);
            _pool.Health += healed;
            if (healed > 0f)
                Healed?.Invoke(this, healed);
            return healed;
        }

        public void Kill()
        {
            if (!IsAlive)
                return;
            var info = DamageInfo.Create(_pool.Health, Team.Environment);
            _pool.Health = 0f;
            var result = new DamageResult(DamageOutcome.Applied, info.Amount, 0f, 0f, true);
            Damaged?.Invoke(this, info, result);
            Died?.Invoke(this);
        }
    }
}
