using TentacleVania.Combat;
using TentacleVania.Core.Progression;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Hungry Grasp")]
    public sealed class HungryGraspMutation : MutationDefinition
    {
        [SerializeField, Range(0f, 1f)] private float _threshold = 0.35f;
        [SerializeField, Range(0f, 0.2f)] private float _perStage = 0.05f;

        public override MutationEffect CreateEffect() => new Effect(this);

        public float ThresholdAt(int sizeStage) => _threshold + _perStage * Mathf.Max(0, sizeStage);

        private sealed class Effect : MutationEffect
        {
            private readonly HungryGraspMutation _data;

            public Effect(HungryGraspMutation data) => _data = data;

            public override void Enable(MutationHost host) => Consumable.EliteThreshold = _data.ThresholdAt(Stats.SizeStage);

            public override void Disable(MutationHost host) => Consumable.EliteThreshold = 0f;

            public override void Tick(MutationHost host, float now) => Consumable.EliteThreshold = _data.ThresholdAt(Stats.SizeStage);
        }
    }
}
