using TentacleVania.Combat.Damage;
using TentacleVania.Creature;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Marrow Store")]
    public sealed class MarrowStoreMutation : MutationDefinition
    {
        [SerializeField, Range(0f, 1f)] private float _maxShield = 0.3f;
        [SerializeField, Min(0.1f)] private float _decaySeconds = 4f;

        public static float Shield { get; private set; }

        public override MutationEffect CreateEffect() => new Effect(this);

        private sealed class Effect : MutationEffect, IDamageModifier
        {
            private readonly MarrowStoreMutation _data;
            private MutationHost _host;
            private float _peak;

            public Effect(MarrowStoreMutation data) => _data = data;

            public int Order => 900;

            public override void Enable(MutationHost host)
            {
                _host = host;
                Shield = 0f;
                host.Consume.Overhealed += OnOverhealed;
                if (host.Health != null)
                    host.Health.Incoming.Add(this);
            }

            public override void Disable(MutationHost host)
            {
                host.Consume.Overhealed -= OnOverhealed;
                if (host.Health != null)
                    host.Health.Incoming.Remove(this);
                Shield = 0f;
            }

            public override void Tick(MutationHost host, float now)
            {
                if (Shield > 0f)
                    Shield = Mathf.Max(0f, Shield - _peak / _data._decaySeconds * Time.deltaTime);
            }

            public void Modify(ref DamageContext context)
            {
                if (Shield <= 0f || context.Cancelled)
                    return;
                var incoming = context.Info.Amount * context.HealthScale;
                if (incoming <= 0f)
                    return;
                var absorbed = Mathf.Min(Shield, incoming);
                Shield -= absorbed;
                context.HealthScale *= (incoming - absorbed) / incoming;
            }

            private void OnOverhealed(CreatureConsume consume, float amount)
            {
                if (_host.Health == null)
                    return;
                Shield = Mathf.Min(_host.Health.Max * _data._maxShield, Shield + amount);
                _peak = Mathf.Max(Shield, 1f);
            }
        }
    }
}
