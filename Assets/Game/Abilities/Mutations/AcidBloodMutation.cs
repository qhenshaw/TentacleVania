using TentacleVania.Combat.Status;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Acid Blood")]
    public sealed class AcidBloodMutation : MutationDefinition
    {
        [SerializeField, Range(0f, 1f)] private float _below = 0.3f;
        [SerializeField, Min(0.05f)] private float _interval = 0.5f;
        [SerializeField, Min(0.1f)] private float _radius = 1f;
        [SerializeField, Min(0.1f)] private float _duration = 3f;
        [SerializeField, Min(0f)] private float _damagePerSecond = 3f;

        public override MutationEffect CreateEffect() => new Effect(this);

        private sealed class Effect : MutationEffect
        {
            private readonly AcidBloodMutation _data;
            private float _next;

            public Effect(AcidBloodMutation data) => _data = data;

            public override void Enable(MutationHost host) => _next = 0f;

            public override void Disable(MutationHost host) { }

            public override void Tick(MutationHost host, float now)
            {
                var health = host.Health;
                if (health == null || !health.IsAlive || health.Normalized >= _data._below || now < _next)
                    return;
                _next = now + _data._interval;
                AcidPools.Spawn(health.transform.position, health.Team, _data._radius, _data._duration, _data._damagePerSecond);
            }
        }
    }
}
