using TentacleVania.Combat;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Shatterpoint")]
    public sealed class ShatterpointMutation : MutationDefinition
    {
        [SerializeField, Min(0f)] private float _extraStun = 1f;
        [SerializeField, Min(1f)] private float _damageScale = 1.3f;

        public override MutationEffect CreateEffect() => new Effect(this);

        private sealed class Effect : MutationEffect
        {
            private readonly ShatterpointMutation _data;

            public Effect(ShatterpointMutation data) => _data = data;

            public override void Enable(MutationHost host)
            {
                StaggerRules.ExtraStun += _data._extraStun;
                StaggerRules.WindowDamageScale *= _data._damageScale;
            }

            public override void Disable(MutationHost host)
            {
                StaggerRules.ExtraStun = Mathf.Max(0f, StaggerRules.ExtraStun - _data._extraStun);
                StaggerRules.WindowDamageScale /= _data._damageScale;
            }
        }
    }
}
