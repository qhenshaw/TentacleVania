using TentacleVania.Creature;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Adrenal Surge")]
    public sealed class AdrenalSurgeMutation : MutationDefinition
    {
        [SerializeField, Range(0f, 1f)] private float _below = 0.3f;
        [SerializeField, Min(0f)] private float _cooldown = 10f;

        public override MutationEffect CreateEffect() => new Effect(this);

        private sealed class Effect : MutationEffect
        {
            private readonly AdrenalSurgeMutation _data;
            private CreatureCharge _charge;
            private bool _wasLow;
            private float _ready;

            public Effect(AdrenalSurgeMutation data) => _data = data;

            public override void Enable(MutationHost host)
            {
                _charge = host.Health != null ? host.Health.GetComponent<CreatureCharge>() : null;
                _wasLow = host.Health != null && host.Health.Normalized < _data._below;
                _ready = 0f;
            }

            public override void Disable(MutationHost host) { }

            public override void Tick(MutationHost host, float now)
            {
                if (host.Health == null || _charge == null)
                    return;
                var low = host.Health.IsAlive && host.Health.Normalized < _data._below;
                if (low && !_wasLow && now >= _ready)
                {
                    _ready = now + _data._cooldown;
                    _charge.Set(_charge.Max);
                }
                _wasLow = low;
            }
        }
    }
}
