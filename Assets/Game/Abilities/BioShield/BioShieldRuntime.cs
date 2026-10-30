using System.Collections.Generic;
using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using UnityEngine;

namespace TentacleVania.Abilities.BioShield
{
    public sealed class BioShieldRuntime : AbilityRuntime
    {
        private const float ShellHeight = 0.4f;
        private const float PulseDecay = 10f;
        private const float BaseAlpha = 0.35f;

        private static readonly Collider[] Hits = new Collider[32];
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        private readonly BioShieldDefinition _definition;
        private readonly AbilityContext _context;
        private readonly ShieldAbsorb _absorb = new();
        private readonly List<Health> _struck = new(16);
        private readonly Transform _shell;
        private readonly Material _shellMaterial;
        private readonly Color _shellColor;

        private bool _active;
        private float _elapsed;
        private float _pulse;
        private float _burstElapsed = -1f;
        private Vector3 _burstCenter;

        public BioShieldRuntime(BioShieldDefinition definition, AbilityContext context)
        {
            _definition = definition;
            _context = context;
            if (definition.ShellPrefab != null)
            {
                _shell = Object.Instantiate(definition.ShellPrefab).transform;
                _shell.name = "BioShieldShell";
                var renderer = _shell.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    _shellMaterial = renderer.material;
                    _shellColor = _shellMaterial.GetColor(BaseColor);
                }
                _shell.gameObject.SetActive(false);
            }
        }

        public override bool IsActive => _active;
        public float Remaining => _absorb.Remaining;
        public float TimeLeft => _active ? Mathf.Max(0f, _definition.Duration - _elapsed) : 0f;
        public int LastBurstHits { get; private set; }

        public override void Activate()
        {
            _absorb.Fill(_definition.Absorb);
            _context.Health.Incoming.Add(_absorb);
            _active = true;
            _elapsed = 0f;
            _pulse = 1f;
            _burstElapsed = -1f;
            if (_context.Materials != null)
                _context.Materials.SetStatus(_definition.StatusColor, _definition.StatusAmount);
            Play(_definition.ActivateSound);
            if (_shell != null)
                _shell.gameObject.SetActive(true);
        }

        public override void Tick(float dt)
        {
            if (_active)
            {
                _elapsed += dt;
                if (_absorb.AbsorbedThisFrame > 0f)
                {
                    _pulse = 1f;
                    Play(_definition.AbsorbSound);
                    _absorb.ClearFrame();
                }
                _pulse = Mathf.Max(0f, _pulse - PulseDecay * dt);
                PlaceShell(_context.Owner.position, _definition.ShellRadius * (0.85f + 0.15f * Fraction() + 0.15f * _pulse), BaseAlpha + 0.4f * _pulse);
                if (_elapsed >= _definition.Duration || _absorb.IsDepleted)
                    Burst();
            }
            else if (_burstElapsed >= 0f)
            {
                _burstElapsed += dt;
                var t = _burstElapsed / _definition.BurstVisualTime;
                if (t >= 1f)
                {
                    _burstElapsed = -1f;
                    if (_shell != null)
                        _shell.gameObject.SetActive(false);
                    return;
                }
                var eased = 1f - (1f - t) * (1f - t);
                PlaceShell(_burstCenter, Mathf.Lerp(_definition.ShellRadius, _definition.BurstRadius, eased), (1f - t) * 0.6f);
            }
        }

        public override void Cancel()
        {
            if (_active)
                End();
            _burstElapsed = -1f;
            if (_shell != null)
                _shell.gameObject.SetActive(false);
        }

        public override void Dispose()
        {
            Cancel();
            if (_shell != null)
                Object.Destroy(_shell.gameObject);
            if (_shellMaterial != null)
                Object.Destroy(_shellMaterial);
        }

        private void Burst()
        {
            End();
            _burstCenter = _context.Owner.position;
            _burstElapsed = 0f;
            Play(_definition.BurstSound);
            LastBurstHits = Strike(_burstCenter);
        }

        private void End()
        {
            _active = false;
            _context.Health.Incoming.Remove(_absorb);
            if (_context.Materials != null)
                _context.Materials.ClearStatus();
            _context.RefreshStatus?.Invoke();
        }

        private int Strike(Vector3 center)
        {
            _struck.Clear();
            var origin = center + Vector3.up * ShellHeight;
            var count = Physics.OverlapSphereNonAlloc(origin, _definition.BurstRadius, Hits, _definition.HitLayers, QueryTriggerInteraction.Collide);
            for (var i = 0; i < count; i++)
            {
                if (!Hurtbox.TryGet(Hits[i], out var hurtbox) || hurtbox.Health == _context.Health || _struck.Contains(hurtbox.Health))
                    continue;
                var target = hurtbox.Health;
                var direction = target.transform.position - center;
                direction.y = 0f;
                var info = DamageInfo.Create(_definition.BurstDamage, Team.Creature, _definition.BurstType, _definition.BurstStagger);
                info.Flags = HitFlags.Heavy;
                info.Attacker = _context.Health;
                info.Point = Hits[i].ClosestPoint(origin);
                info.Direction = direction.sqrMagnitude > 1e-4f ? direction.normalized : _context.Owner.forward;
                if (target.TakeDamage(info).Outcome != DamageOutcome.Ignored)
                    _struck.Add(target);
            }
            return _struck.Count;
        }

        private float Fraction() => _absorb.Capacity > 0f ? _absorb.Remaining / _absorb.Capacity : 0f;

        private void PlaceShell(Vector3 position, float radius, float alpha)
        {
            if (_shell == null)
                return;
            _shell.position = position + Vector3.up * ShellHeight;
            _shell.localScale = Vector3.one * (radius * 2f);
            if (_shellMaterial != null)
            {
                var color = _shellColor;
                color.a = alpha;
                _shellMaterial.SetColor(BaseColor, color);
            }
        }

        private void Play(EventReference sound)
        {
            if (!sound.IsNull)
                RuntimeManager.PlayOneShot(sound, _context.Owner.position);
        }
    }
}
