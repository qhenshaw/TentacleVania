using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Quill Burst")]
    public sealed class QuillBurstMutation : MutationDefinition
    {
        [SerializeField, Min(0.1f)] private float _radius = 3.5f;
        [SerializeField, Min(0f)] private float _damage = 8f;
        [SerializeField, Min(0f)] private float _cooldown = 1.5f;
        [SerializeField] private EventReference _sound;

        public override MutationEffect CreateEffect() => new Effect(this);

        private sealed class Effect : MutationEffect
        {
            private readonly QuillBurstMutation _data;
            private float _ready;

            public Effect(QuillBurstMutation data) => _data = data;

            public override void Enable(MutationHost host)
            {
                _ready = 0f;
                if (host.Health != null)
                    host.Health.Damaged += OnDamaged;
            }

            public override void Disable(MutationHost host)
            {
                if (host.Health != null)
                    host.Health.Damaged -= OnDamaged;
            }

            private void OnDamaged(Health target, in DamageInfo info, in DamageResult result)
            {
                if (Time.time < _ready || result.HealthDamage + result.ArmorDamage <= 0f || (info.Flags & (HitFlags.DamageOverTime | HitFlags.Reflected)) != 0)
                    return;
                _ready = Time.time + _data._cooldown;
                var center = target.transform.position;
                MutationStrikes.Play(_data._sound, center);
                MutationStrikes.Ring(target, center, _data._radius, _data._damage, _data._damage * MutationStrikes.ThornsStaggerScale, DamageType.Physical, HitFlags.Reflected);
            }
        }
    }
}
