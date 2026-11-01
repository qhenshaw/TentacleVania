using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Combat.Status;
using TentacleVania.Creature;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Host Feeding")]
    public sealed class HostFeedingMutation : MutationDefinition
    {
        [SerializeField, Range(0f, 1f)] private float _chargeRefund = 0.15f;
        [SerializeField, Min(0.1f)] private float _spreadRadius = 5f;

        public override MutationEffect CreateEffect() => new Effect(this);

        private sealed class Effect : MutationEffect
        {
            private readonly HostFeedingMutation _data;
            private MutationHost _host;
            private CreatureCharge _charge;

            public Effect(HostFeedingMutation data) => _data = data;

            public override void Enable(MutationHost host)
            {
                _host = host;
                _charge = host.Health != null ? host.Health.GetComponent<CreatureCharge>() : null;
                CombatEvents.ConsumeStarted += OnConsume;
            }

            public override void Disable(MutationHost host) => CombatEvents.ConsumeStarted -= OnConsume;

            private void OnConsume(Consumable victim)
            {
                if (_host.Health == null || victim == null || !victim.TryGetComponent<StatusEffects>(out var status) || !status.IsActive(StatusKind.Parasite))
                    return;
                if (_charge != null)
                    _charge.Add(_charge.Max * _data._chargeRefund);
                MutationStrikes.Ring(_host.Health, victim.transform.position, _data._spreadRadius, 1f, 0f, DamageType.Physical, HitFlags.Reflected, StatusKind.Parasite, victim.Health);
            }
        }
    }
}
