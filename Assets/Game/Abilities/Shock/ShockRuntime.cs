using System.Collections.Generic;
using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Saves;
using UnityEngine;
using UnityEngine.VFX;

namespace TentacleVania.Abilities.Shock
{
    public sealed class ShockRuntime : AbilityRuntime
    {
        private const float Height = 1f;
        private const float ActiveTime = 0.3f;
        private static readonly Collider[] Hits = new Collider[32];

        private readonly ShockDefinition _definition;
        private readonly AbilityContext _context;
        private readonly List<Health> _struck = new(16);
        private readonly VisualEffect _burst;
        private float _remaining;

        public int LastHits { get; private set; }

        public ShockRuntime(ShockDefinition definition, AbilityContext context)
        {
            _definition = definition;
            _context = context;
            if (definition.Burst != null)
            {
                _burst = Object.Instantiate(definition.Burst);
                _burst.gameObject.SetActive(false);
            }
        }

        public override bool IsActive => _remaining > 0f;

        public override void Activate()
        {
            var tier = Upgrades.AbilityTier(ContentCatalog.IdOf(_definition));
            var center = _context.Owner.position;
            _remaining = ActiveTime;
            if (!_definition.ActivateSound.IsNull)
                RuntimeManager.PlayOneShot(_definition.ActivateSound, center);
            if (_context.Materials != null)
                _context.Materials.Flash(_definition.FlashColor, ActiveTime);
            if (_burst != null)
            {
                _burst.transform.position = center + Vector3.up * Height;
                _burst.transform.localScale = Vector3.one * _definition.RadiusAt(tier);
                _burst.gameObject.SetActive(true);
                _burst.Reinit();
                _burst.Play();
            }
            LastHits = Strike(center, tier);
        }

        private int Strike(Vector3 center, int tier)
        {
            _struck.Clear();
            var origin = center + Vector3.up * Height;
            var count = Physics.OverlapSphereNonAlloc(origin, _definition.RadiusAt(tier), Hits, _definition.HitLayers, QueryTriggerInteraction.Collide);
            for (var i = 0; i < count; i++)
            {
                if (!Hurtbox.TryGet(Hits[i], out var hurtbox) || hurtbox.Health == _context.Health || _struck.Contains(hurtbox.Health))
                    continue;
                var target = hurtbox.Health;
                var direction = target.transform.position - center;
                direction.y = 0f;
                var info = DamageInfo.Create(_definition.DamageAt(tier), Team.Creature, DamageType.Electric, _definition.Stagger);
                info.Flags = HitFlags.Ability;
                info.Status = StatusKind.Static;
                info.StatusMagnitude = _definition.StaticAt(tier);
                info.Attacker = _context.Health;
                info.Point = Hits[i].ClosestPoint(origin);
                info.Direction = direction.sqrMagnitude > 1e-4f ? direction.normalized : _context.Owner.forward;
                if (target.TakeDamage(info).Outcome != DamageOutcome.Ignored)
                    _struck.Add(target);
            }
            return _struck.Count;
        }

        public override void Tick(float dt) => _remaining = Mathf.Max(0f, _remaining - dt);

        public override void Cancel() => _remaining = 0f;

        public override void Dispose()
        {
            if (_burst != null)
                Object.Destroy(_burst.gameObject);
        }
    }
}
