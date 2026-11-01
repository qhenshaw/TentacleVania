using System.Collections.Generic;
using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Saves;
using UnityEngine;
using UnityEngine.VFX;

namespace TentacleVania.Abilities.TendrilSnare
{
    public sealed class TendrilSnareRuntime : AbilityRuntime
    {
        private const float Height = 1f;
        private static readonly Collider[] Hits = new Collider[32];

        private readonly TendrilSnareDefinition _definition;
        private readonly AbilityContext _context;
        private readonly List<Health> _struck = new(16);
        private readonly VisualEffect _burst;
        private Vector3 _center;
        private float _eruptIn = -1f;
        private int _tier;

        public int LastHits { get; private set; }
        public Vector3 LastCenter => _center;

        public TendrilSnareRuntime(TendrilSnareDefinition definition, AbilityContext context)
        {
            _definition = definition;
            _context = context;
            if (definition.Burst != null)
            {
                _burst = Object.Instantiate(definition.Burst);
                _burst.gameObject.SetActive(false);
            }
        }

        public override bool IsActive => _eruptIn >= 0f;

        public override void Activate()
        {
            _tier = Upgrades.AbilityTier(ContentCatalog.IdOf(_definition));
            _center = Target();
            _eruptIn = _definition.Delay;
            LastHits = 0;
            if (!_definition.ActivateSound.IsNull)
                RuntimeManager.PlayOneShot(_definition.ActivateSound, _context.Owner.position);
        }

        private Vector3 Target()
        {
            var owner = _context.Owner.position;
            var forward = _context.Owner.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 1e-4f ? forward.normalized : Vector3.forward;
            var count = Physics.OverlapSphereNonAlloc(owner + Vector3.up * Height, _definition.Reach, Hits, _definition.HitLayers, QueryTriggerInteraction.Collide);
            var best = owner + forward * _definition.FallbackDistance;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < count; i++)
            {
                if (!Hurtbox.TryGet(Hits[i], out var hurtbox) || hurtbox.Health == _context.Health || !hurtbox.Health.IsAlive)
                    continue;
                var to = hurtbox.Health.transform.position - owner;
                to.y = 0f;
                var distance = to.sqrMagnitude;
                if (distance >= bestDistance || Vector3.Angle(forward, to) > _definition.AimCone * 0.5f)
                    continue;
                bestDistance = distance;
                best = hurtbox.Health.transform.position;
            }
            return best;
        }

        public override void Tick(float dt)
        {
            if (_eruptIn < 0f)
                return;
            _eruptIn -= dt;
            if (_eruptIn < 0f)
                Erupt();
        }

        private void Erupt()
        {
            _eruptIn = -1f;
            var radius = _definition.RadiusAt(_tier);
            if (!_definition.EruptSound.IsNull)
                RuntimeManager.PlayOneShot(_definition.EruptSound, _center);
            if (_burst != null)
            {
                _burst.transform.position = _center;
                _burst.transform.localScale = Vector3.one * radius;
                _burst.gameObject.SetActive(true);
                _burst.Reinit();
                _burst.Play();
            }
            _struck.Clear();
            var origin = _center + Vector3.up * Height;
            var count = Physics.OverlapCapsuleNonAlloc(_center, origin + Vector3.up, radius, Hits, _definition.HitLayers, QueryTriggerInteraction.Collide);
            for (var i = 0; i < count; i++)
            {
                if (!Hurtbox.TryGet(Hits[i], out var hurtbox) || hurtbox.Health == _context.Health || _struck.Contains(hurtbox.Health))
                    continue;
                var target = hurtbox.Health;
                var info = DamageInfo.Create(_definition.Damage, Team.Creature, DamageType.Physical);
                info.Flags = HitFlags.Ability;
                info.Status = StatusKind.Root;
                info.StatusDuration = _definition.RootTimeAt(_tier);
                info.Attacker = _context.Health;
                info.Point = target.transform.position;
                info.Direction = Vector3.up;
                if (target.TakeDamage(info).Outcome != DamageOutcome.Ignored)
                    _struck.Add(target);
            }
            LastHits = _struck.Count;
        }

        public override void Cancel() => _eruptIn = -1f;

        public override void Dispose()
        {
            if (_burst != null)
                Object.Destroy(_burst.gameObject);
        }
    }
}
