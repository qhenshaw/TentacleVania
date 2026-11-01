using TentacleVania.Combat.Damage;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Crushing Coils")]
    public sealed class CrushingCoilsMutation : MutationDefinition
    {
        [SerializeField, Min(1f)] private float _staggerScale = 1.4f;

        public override MutationEffect CreateEffect() => new Effect(_staggerScale);

        private sealed class Effect : MutationEffect
        {
            private readonly ScaleModifier _modifier;

            public Effect(float scale) => _modifier = new(300) { StaggerScale = scale };

            public override void Enable(MutationHost host)
            {
                if (host.Health != null)
                    host.Health.Outgoing.Add(_modifier);
            }

            public override void Disable(MutationHost host)
            {
                if (host.Health != null)
                    host.Health.Outgoing.Remove(_modifier);
            }
        }
    }
}
