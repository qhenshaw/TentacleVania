using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Concussive Slam")]
    public sealed class ConcussiveSlamMutation : MutationDefinition
    {
        [SerializeField, Min(0.1f)] private float _radius = 4f;
        [SerializeField, Min(0f)] private float _damage = 5f;
        [SerializeField, Min(0f)] private float _stagger = 40f;
        [SerializeField] private EventReference _sound;

        public override MutationEffect CreateEffect() => new Effect(this);

        private sealed class Effect : MutationEffect
        {
            private readonly ConcussiveSlamMutation _data;
            private MutationHost _host;
            private bool _busy;

            public Effect(ConcussiveSlamMutation data) => _data = data;

            public override void Enable(MutationHost host)
            {
                _host = host;
                CombatEvents.StaggerBroken += OnBroken;
            }

            public override void Disable(MutationHost host) => CombatEvents.StaggerBroken -= OnBroken;

            private void OnBroken(Health target)
            {
                if (_busy || _host.Health == null || target == null || target.Team == _host.Health.Team)
                    return;
                _busy = true;
                var center = target.transform.position;
                MutationStrikes.Play(_data._sound, center);
                MutationStrikes.Ring(_host.Health, center, _data._radius, _data._damage, _data._stagger, DamageType.Physical, HitFlags.Heavy | HitFlags.Reflected, StatusKind.None, target);
                _busy = false;
            }
        }
    }
}
