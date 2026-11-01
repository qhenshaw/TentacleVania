using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Combat.Status;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Melting Point")]
    public sealed class MeltingPointMutation : MutationDefinition
    {
        [SerializeField, Min(1f)] private float _corrodedStagger = 1.5f;
        [SerializeField, Min(0.1f)] private float _splashRadius = 3f;
        [SerializeField, Min(0f)] private float _splashDamage = 10f;
        [SerializeField] private EventReference _sound;

        public override MutationEffect CreateEffect() => new Effect(this);

        private sealed class Effect : MutationEffect
        {
            private readonly MeltingPointMutation _data;
            private MutationHost _host;

            public Effect(MeltingPointMutation data) => _data = data;

            public override void Enable(MutationHost host)
            {
                _host = host;
                StaggerRules.CorrodedScale *= _data._corrodedStagger;
                CombatEvents.StaggerBroken += OnBroken;
            }

            public override void Disable(MutationHost host)
            {
                StaggerRules.CorrodedScale /= _data._corrodedStagger;
                CombatEvents.StaggerBroken -= OnBroken;
            }

            private void OnBroken(Health target)
            {
                if (target == null || _host.Health == null || target.Team == _host.Health.Team)
                    return;
                if (!target.TryGetComponent<StatusEffects>(out var status) || !status.IsActive(StatusKind.Corrode))
                    return;
                var center = target.transform.position;
                MutationStrikes.Play(_data._sound, center);
                MutationStrikes.Ring(_host.Health, center, _data._splashRadius, _data._splashDamage, 0f, DamageType.Acid, HitFlags.Reflected, StatusKind.Corrode, target);
            }
        }
    }
}
