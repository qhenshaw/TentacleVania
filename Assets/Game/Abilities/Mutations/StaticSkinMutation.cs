using System.Collections.Generic;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Static Skin")]
    public sealed class StaticSkinMutation : MutationDefinition
    {
        [SerializeField, Min(0.1f)] private float _radius = 1.3f;
        [SerializeField, Min(0f)] private float _damage = 3f;

        public override MutationEffect CreateEffect() => new Effect(this);

        private sealed class Effect : MutationEffect
        {
            private readonly StaticSkinMutation _data;
            private readonly List<Health> _hit = new(16);
            private readonly List<Health> _near = new(32);
            private bool _dashing;

            public Effect(StaticSkinMutation data) => _data = data;

            public override void Enable(MutationHost host) => _hit.Clear();

            public override void Disable(MutationHost host) => _hit.Clear();

            public override void Tick(MutationHost host, float now)
            {
                if (host.Motor == null || host.Health == null || !host.Motor.IsDashing)
                {
                    _dashing = false;
                    return;
                }
                if (!_dashing)
                {
                    _dashing = true;
                    _hit.Clear();
                }
                MutationStrikes.Overlap(host.Health, host.Motor.transform.position, _data._radius, _near);
                for (var i = 0; i < _near.Count; i++)
                {
                    var target = _near[i];
                    if (_hit.Contains(target))
                        continue;
                    _hit.Add(target);
                    var info = DamageInfo.Create(_data._damage, host.Health.Team, DamageType.Electric);
                    info.Status = StatusKind.Static;
                    info.Flags = HitFlags.Reflected;
                    info.Attacker = host.Health;
                    info.Point = target.transform.position + Vector3.up;
                    info.Direction = host.Motor.DashDirection;
                    target.TakeDamage(info);
                }
            }
        }
    }
}
