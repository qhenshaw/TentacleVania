using System;
using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Saves;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TentacleVania.Abilities.SpineVolley
{
    public sealed class SpineVolleyRuntime : AbilityRuntime
    {
        private const float Height = 1f;
        private const int MaxPierced = 4;
        private static readonly RaycastHit[] Hits = new RaycastHit[8];
        private static readonly Collider[] Nearby = new Collider[32];

        public static event Action<Health, Vector3> SpineHit;

        private sealed class Spine
        {
            public Transform Body;
            public Vector3 Position;
            public Vector3 Velocity;
            public float Travelled;
            public bool Live;
            public readonly Health[] Pierced = new Health[MaxPierced];
            public int PiercedCount;
        }

        private readonly SpineVolleyDefinition _definition;
        private readonly AbilityContext _context;
        private readonly Spine[] _spines;
        private int _tier;

        public int LastHits { get; private set; }

        public SpineVolleyRuntime(SpineVolleyDefinition definition, AbilityContext context)
        {
            _definition = definition;
            _context = context;
            _spines = new Spine[definition.MaxSpines * 2];
            for (var i = 0; i < _spines.Length; i++)
            {
                _spines[i] = new Spine();
                if (definition.SpinePrefab != null)
                {
                    _spines[i].Body = Object.Instantiate(definition.SpinePrefab);
                    _spines[i].Body.gameObject.SetActive(false);
                }
            }
        }

        public override bool IsActive
        {
            get
            {
                foreach (var spine in _spines)
                    if (spine.Live)
                        return true;
                return false;
            }
        }

        public override void Activate()
        {
            _tier = Upgrades.AbilityTier(ContentCatalog.IdOf(_definition));
            LastHits = 0;
            var origin = _context.Owner.position + Vector3.up * Height;
            var forward = Aim(origin);
            var count = _definition.SpinesAt(_tier);
            if (!_definition.ActivateSound.IsNull)
                RuntimeManager.PlayOneShot(_definition.ActivateSound, origin);
            for (var i = 0; i < count; i++)
                Launch(origin, SpineVolleyDefinition.FanDirection(forward, _definition.Spread, i, count));
        }

        private Vector3 Aim(Vector3 origin)
        {
            var forward = _context.Owner.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 1e-4f ? forward.normalized : Vector3.forward;
            var count = Physics.OverlapSphereNonAlloc(origin, _definition.Range, Nearby, _definition.HitLayers, QueryTriggerInteraction.Collide);
            var best = forward;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < count; i++)
            {
                if (!Hurtbox.TryGet(Nearby[i], out var hurtbox) || hurtbox.Health == _context.Health || !hurtbox.Health.IsAlive)
                    continue;
                var to = hurtbox.Health.transform.position - _context.Owner.position;
                to.y = 0f;
                var distance = to.sqrMagnitude;
                if (distance < 1e-4f || distance >= bestDistance || Vector3.Angle(forward, to) > _definition.AimCone * 0.5f)
                    continue;
                bestDistance = distance;
                best = to.normalized;
            }
            return best;
        }

        private void Launch(Vector3 origin, Vector3 direction)
        {
            foreach (var spine in _spines)
            {
                if (spine.Live)
                    continue;
                spine.Live = true;
                spine.Position = origin;
                spine.Velocity = direction * _definition.Speed;
                spine.Travelled = 0f;
                spine.PiercedCount = 0;
                if (spine.Body != null)
                {
                    spine.Body.SetPositionAndRotation(origin, Quaternion.LookRotation(direction));
                    spine.Body.gameObject.SetActive(true);
                }
                return;
            }
        }

        public override void Tick(float dt)
        {
            var piercing = MutationLoadout.IsEquipped(_definition.PiercingMutation);
            var bleed = MutationLoadout.IsEquipped(_definition.BleedMutation);
            foreach (var spine in _spines)
                if (spine.Live)
                    Step(spine, dt, piercing, bleed);
        }

        private void Step(Spine spine, float dt, bool piercing, bool bleed)
        {
            var step = spine.Velocity * dt;
            var distance = step.magnitude;
            var direction = distance > 1e-5f ? step / distance : Vector3.forward;
            if (Physics.Raycast(spine.Position, direction, out var wall, distance, _definition.WallLayers, QueryTriggerInteraction.Ignore))
                distance = wall.distance;
            var count = Physics.SphereCastNonAlloc(spine.Position, _definition.Radius, direction, Hits, distance, _definition.HitLayers, QueryTriggerInteraction.Collide);
            for (var i = 0; i < count && spine.Live; i++)
            {
                if (!Hurtbox.TryGet(Hits[i].collider, out var hurtbox) || hurtbox.Health == _context.Health || AlreadyPierced(spine, hurtbox.Health))
                    continue;
                Strike(spine, hurtbox.Health, Hits[i].point.sqrMagnitude > 0f ? Hits[i].point : spine.Position, direction, bleed);
                if (!piercing || spine.PiercedCount >= MaxPierced)
                    Retire(spine);
            }
            if (!spine.Live)
                return;
            spine.Position += direction * distance;
            spine.Travelled += distance;
            if (spine.Body != null)
                spine.Body.position = spine.Position;
            if (wall.collider != null || spine.Travelled >= _definition.Range)
                Retire(spine);
        }

        private static bool AlreadyPierced(Spine spine, Health health)
        {
            for (var i = 0; i < spine.PiercedCount; i++)
                if (spine.Pierced[i] == health)
                    return true;
            return false;
        }

        private void Strike(Spine spine, Health target, Vector3 point, Vector3 direction, bool bleed)
        {
            if (spine.PiercedCount < MaxPierced)
                spine.Pierced[spine.PiercedCount++] = target;
            var info = DamageInfo.Create(_definition.DamageAt(_tier), Team.Creature, DamageType.Physical, _definition.Stagger);
            info.Flags = HitFlags.Ability;
            info.Attacker = _context.Health;
            info.Point = point;
            info.Direction = direction;
            if (bleed)
                info.Status = StatusKind.Bleed;
            if (target.TakeDamage(info).Outcome == DamageOutcome.Ignored)
                return;
            LastHits++;
            if (!_definition.HitSound.IsNull)
                RuntimeManager.PlayOneShot(_definition.HitSound, point);
            SpineHit?.Invoke(target, point);
        }

        private static void Retire(Spine spine)
        {
            spine.Live = false;
            if (spine.Body != null)
                spine.Body.gameObject.SetActive(false);
        }

        public override void Cancel()
        {
            foreach (var spine in _spines)
                if (spine.Live)
                    Retire(spine);
        }

        public override void Dispose()
        {
            foreach (var spine in _spines)
                if (spine.Body != null)
                    Object.Destroy(spine.Body.gameObject);
        }
    }
}
