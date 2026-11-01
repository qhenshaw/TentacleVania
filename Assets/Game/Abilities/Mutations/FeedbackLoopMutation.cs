using System.Collections.Generic;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Creature;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Feedback Loop")]
    public sealed class FeedbackLoopMutation : MutationDefinition
    {
        [SerializeField, Min(1)] private int _targets = 3;
        [SerializeField, Range(0f, 1f)] private float _refund = 0.3f;
        [SerializeField, Min(0.05f)] private float _window = 1.5f;

        public static int Refunds { get; private set; }

        public override MutationEffect CreateEffect() => new Effect(this);

        private sealed class Effect : MutationEffect
        {
            private readonly FeedbackLoopMutation _data;
            private readonly List<Health> _struck = new(16);
            private MutationHost _host;
            private AbilityController _abilities;
            private CreatureCharge _charge;
            private float _until = -1f;
            private float _cost;

            public Effect(FeedbackLoopMutation data) => _data = data;

            public override void Enable(MutationHost host)
            {
                _host = host;
                _abilities = host.Health != null ? host.Health.GetComponent<AbilityController>() : null;
                _charge = host.Health != null ? host.Health.GetComponent<CreatureCharge>() : null;
                if (_abilities != null)
                    _abilities.Activated += OnActivated;
                CombatEvents.HitLanded += OnHit;
            }

            public override void Disable(MutationHost host)
            {
                if (_abilities != null)
                    _abilities.Activated -= OnActivated;
                CombatEvents.HitLanded -= OnHit;
            }

            private void OnActivated(AbilityController controller)
            {
                _struck.Clear();
                _cost = controller.Equipped != null ? controller.Equipped.Cost : 0f;
                _until = Time.time + _data._window;
            }

            private void OnHit(Health target, in DamageInfo info, in DamageResult result)
            {
                if (Time.time > _until || info.Attacker != _host.Health || target == _host.Health || (info.Flags & HitFlags.Ability) == 0 || _struck.Contains(target))
                    return;
                _struck.Add(target);
                if (_struck.Count < _data._targets)
                    return;
                _until = -1f;
                Refunds++;
                if (_charge != null)
                    _charge.Add(_cost * _data._refund);
            }
        }
    }
}
