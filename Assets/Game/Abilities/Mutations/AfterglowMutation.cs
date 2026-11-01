using TentacleVania.Combat.Damage;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Afterglow")]
    public sealed class AfterglowMutation : MutationDefinition
    {
        [SerializeField, Min(1f)] private float _damageScale = 1.5f;
        [SerializeField, Min(1f)] private float _staggerScale = 1.5f;

        public static bool Armed { get; private set; }

        public override MutationEffect CreateEffect() => new Effect(this);

        private sealed class Effect : MutationEffect, IDamageModifier
        {
            private readonly AfterglowMutation _data;
            private AbilityController _abilities;
            private MutationHost _host;
            private int _usedFrame = -1;

            public Effect(AfterglowMutation data) => _data = data;

            public int Order => 340;

            public override void Enable(MutationHost host)
            {
                _host = host;
                Armed = false;
                _abilities = host.Health != null ? host.Health.GetComponent<AbilityController>() : null;
                if (_abilities != null)
                    _abilities.Activated += OnActivated;
                if (host.Health != null)
                    host.Health.Outgoing.Add(this);
            }

            public override void Disable(MutationHost host)
            {
                if (_abilities != null)
                    _abilities.Activated -= OnActivated;
                if (host.Health != null)
                    host.Health.Outgoing.Remove(this);
                Armed = false;
            }

            private void OnActivated(AbilityController controller) => Armed = true;

            public void Modify(ref DamageContext context)
            {
                if ((context.Info.Flags & HitFlags.Finisher) == 0 || (!Armed && Time.frameCount != _usedFrame))
                    return;
                if (Armed)
                {
                    Armed = false;
                    _usedFrame = Time.frameCount;
                }
                context.HealthScale *= _data._damageScale;
                context.StaggerScale *= _data._staggerScale;
            }
        }
    }
}
