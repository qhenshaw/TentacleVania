using TentacleVania.Abilities.SpineVolley;
using TentacleVania.Core.Characters;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Spined Charge")]
    public sealed class SpinedChargeMutation : MutationDefinition
    {
        [SerializeField] private SpineVolleyDefinition _volley;
        [SerializeField, Min(1)] private int _spines = 5;
        [SerializeField, Min(0f)] private float _spread = 50f;

        public static int Fans { get; private set; }

        public override MutationEffect CreateEffect() => new Effect(this);

        private sealed class Effect : MutationEffect
        {
            private readonly SpinedChargeMutation _data;
            private SpineVolleyRuntime _runtime;

            public Effect(SpinedChargeMutation data) => _data = data;

            public override void Enable(MutationHost host)
            {
                if (_data._volley != null && host.Health != null && _runtime == null)
                    _runtime = new(_data._volley, new AbilityContext(host.Health.transform, host.Health, host.Health.GetComponent<CharacterMaterials>(), null));
                CreatureSignals.ChargedHeavy += OnHeavy;
            }

            public override void Disable(MutationHost host)
            {
                CreatureSignals.ChargedHeavy -= OnHeavy;
                if (_runtime == null)
                    return;
                _runtime.Dispose();
                _runtime = null;
            }

            public override void Tick(MutationHost host, float now)
            {
                if (_runtime != null)
                    _runtime.Tick(Time.deltaTime);
            }

            private void OnHeavy(Vector3 forward)
            {
                if (_runtime == null)
                    return;
                Fans++;
                _runtime.FireFan(forward, _data._spread, _data._spines);
            }
        }
    }
}
