using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Combat.Status;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Pyre Feast")]
    public sealed class PyreFeastMutation : MutationDefinition
    {
        [SerializeField, Min(0.1f)] private float _radius = 3.5f;
        [SerializeField, Min(0f)] private float _damage = 8f;
        [SerializeField] private EventReference _sound;

        public override MutationEffect CreateEffect() => new Effect(this);

        private sealed class Effect : MutationEffect
        {
            private readonly PyreFeastMutation _data;
            private MutationHost _host;

            public Effect(PyreFeastMutation data) => _data = data;

            public override void Enable(MutationHost host)
            {
                _host = host;
                CombatEvents.ConsumeStarted += OnConsume;
            }

            public override void Disable(MutationHost host) => CombatEvents.ConsumeStarted -= OnConsume;

            private void OnConsume(Consumable victim)
            {
                if (_host.Health == null || victim == null || !victim.TryGetComponent<StatusEffects>(out var status) || !status.IsActive(StatusKind.Burn))
                    return;
                var center = victim.transform.position;
                MutationStrikes.Play(_data._sound, center);
                MutationStrikes.Ring(_host.Health, center, _data._radius, _data._damage, 0f, DamageType.Fire, HitFlags.Reflected, StatusKind.Burn, victim.Health);
            }
        }
    }
}
