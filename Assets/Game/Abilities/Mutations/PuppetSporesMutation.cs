using System.Collections.Generic;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Combat.Status;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Puppet Spores")]
    public sealed class PuppetSporesMutation : MutationDefinition
    {
        private const int MaxPuppets = 4;

        [SerializeField, Range(0f, 1f)] private float _below = 0.3f;
        [SerializeField, Min(0.1f)] private float _duration = 4f;
        [SerializeField, Min(0.1f)] private float _strikeInterval = 0.8f;
        [SerializeField, Min(0.1f)] private float _reach = 3.5f;
        [SerializeField, Range(0f, 1f)] private float _strikeFraction = 0.15f;

        public static int Puppets { get; private set; }

        public override MutationEffect CreateEffect() => new Effect(this);

        private sealed class Effect : MutationEffect
        {
            private readonly PuppetSporesMutation _data;
            private readonly HashSet<Health> _used = new();
            private readonly Health[] _puppets = new Health[MaxPuppets];
            private readonly float[] _until = new float[MaxPuppets];
            private readonly float[] _next = new float[MaxPuppets];
            private readonly List<Health> _near = new(32);
            private MutationHost _host;

            public Effect(PuppetSporesMutation data) => _data = data;

            public override void Enable(MutationHost host)
            {
                _host = host;
                _used.Clear();
                CombatEvents.HitLanded += OnHit;
            }

            public override void Disable(MutationHost host)
            {
                CombatEvents.HitLanded -= OnHit;
                for (var i = 0; i < MaxPuppets; i++)
                    _puppets[i] = null;
                Puppets = 0;
            }

            private void OnHit(Health target, in DamageInfo info, in DamageResult result)
            {
                if (_host.Health == null || target == _host.Health || target.Team == _host.Health.Team || !target.IsAlive || target.Normalized >= _data._below || _used.Contains(target))
                    return;
                if (!target.TryGetComponent<StatusEffects>(out var status) || !status.IsActive(StatusKind.Parasite))
                    return;
                for (var i = 0; i < MaxPuppets; i++)
                {
                    if (_puppets[i] != null && _puppets[i].IsAlive)
                        continue;
                    _used.Add(target);
                    _puppets[i] = target;
                    _until[i] = Time.time + _data._duration;
                    _next[i] = Time.time + 0.2f;
                    Puppets++;
                    if (target.TryGetComponent<IStunnable>(out var stunnable))
                        stunnable.Stun(_data._duration);
                    return;
                }
            }

            public override void Tick(MutationHost host, float now)
            {
                for (var i = 0; i < MaxPuppets; i++)
                {
                    var puppet = _puppets[i];
                    if (puppet == null)
                        continue;
                    if (!puppet.IsAlive || now >= _until[i])
                    {
                        _puppets[i] = null;
                        continue;
                    }
                    if (now < _next[i])
                        continue;
                    _next[i] = now + _data._strikeInterval;
                    Strike(puppet);
                }
            }

            private void Strike(Health puppet)
            {
                MutationStrikes.Overlap(_host.Health, puppet.transform.position, _data._reach, _near, puppet);
                Health best = null;
                var bestDistance = float.MaxValue;
                for (var i = 0; i < _near.Count; i++)
                {
                    var distance = (_near[i].transform.position - puppet.transform.position).sqrMagnitude;
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = _near[i];
                    }
                }
                if (best == null)
                    return;
                var info = DamageInfo.Create(puppet.Max * _data._strikeFraction, _host.Health.Team, DamageType.Physical, puppet.Max * _data._strikeFraction);
                info.Flags = HitFlags.Reflected;
                info.Attacker = puppet;
                info.Point = best.transform.position + Vector3.up;
                var away = best.transform.position - puppet.transform.position;
                away.y = 0f;
                info.Direction = away.sqrMagnitude > 1e-4f ? away.normalized : Vector3.forward;
                ChainLightningLines.Draw(puppet.transform.position + Vector3.up, best.transform.position + Vector3.up);
                best.TakeDamage(info);
            }
        }
    }
}
