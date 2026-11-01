using System;
using System.Collections.Generic;
using FMODUnity;
using TentacleVania.Combat.Damage;
using TentacleVania.Core.Characters;
using TentacleVania.Core.Progression;
using UnityEngine;
using UnityEngine.VFX;

namespace TentacleVania.Combat.Status
{
    [RequireComponent(typeof(Health))]
    public sealed class StatusEffects : MonoBehaviour
    {
        private const int KindCount = 6;
        private const int MaxCascade = 8;

        private static readonly List<StatusEffects> Registry = new();
        private static readonly List<Vector3> CandidatePositions = new();
        private static readonly List<StatusEffects> Candidates = new();
        private static readonly int[] Picks = new int[16];
        private static readonly StatusEffects[][] TargetBuffers = BuildTargetBuffers();
        private static int _cascade;

        [Serializable]
        public sealed class Visual
        {
            [ColorUsage(false, true)] public Color Color = Color.white;
            [Min(0f)] public float Amount = 0.8f;
            [Min(0f)] public float Pulse = 1.5f;
        }

        [SerializeField] private Visual _slow = new() { Color = new Color(0.55f, 0.35f, 1.6f) };
        [SerializeField] private Visual _root = new() { Color = new Color(1.4f, 1.1f, 0.2f), Pulse = 3f };
        [SerializeField] private Visual _bleed = new() { Color = new Color(1.6f, 0.08f, 0.1f), Amount = 0.6f, Pulse = 2f };
        [SerializeField] private Visual _burn = new() { Color = new Color(2f, 0.7f, 0.1f), Amount = 0.7f, Pulse = 4f };
        [SerializeField, Min(0f)] private float _effectHeight = 1f;

        private readonly StatusTimers _timers = new();
        private readonly DamageOverTime _bleedDot = new();
        private readonly DamageOverTime _burnDot = new();
        private readonly bool[] _shown = new bool[KindCount];

        private Health _health;
        private CharacterMaterials _materials;
        private StatusEffectSettings _settings;
        private VisualEffect _bleedEffect;
        private VisualEffect _burnEffect;
        private Team _bleedSource = Team.Environment;
        private Team _burnSource = Team.Environment;
        private StaticMeter _static;

        public event Action<StatusEffects, StatusKind> Started;
        public event Action<StatusEffects, StatusKind> Ended;
        public event Action<StatusEffects, float> BleedBurst;
        public static event Action<StatusEffects, int> StaticDischarged;

        public static float StaticBuildScale { get; set; } = 1f;
        public static int ExtraChainTargets { get; set; }
        public static float ChainDamageScale { get; set; } = 1f;
        public static float ChainAddsStatic { get; set; }
        public static int Discharges { get; private set; }

        private static StatusEffects[][] BuildTargetBuffers()
        {
            var buffers = new StatusEffects[MaxCascade][];
            for (var i = 0; i < MaxCascade; i++)
                buffers[i] = new StatusEffects[16];
            return buffers;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Registry.Clear();
            StaticDischarged = null;
            StaticBuildScale = 1f;
            ExtraChainTargets = 0;
            ChainDamageScale = 1f;
            ChainAddsStatic = 0f;
            Discharges = 0;
            _cascade = 0;
        }

        public float MoveScale => _timers.MoveScale(Time.time);
        public bool IsRooted => _timers.IsActive(StatusKind.Root, Time.time);
        public bool IsSlowed => _timers.IsActive(StatusKind.Slow, Time.time);
        public int BleedStacks => _bleedDot.Stacks;
        public float StaticNormalized => _static != null ? _static.Normalized : 0f;
        public float BleedDamagePerSecond => _bleedDot.DamagePerSecond;
        public float BurnDamagePerSecond => _burnDot.DamagePerSecond;
        public bool Glow { get; set; } = true;
        public bool ControlImmune { get; set; }
        public Health Health => _health;

        public bool IsActive(StatusKind kind) => kind switch
        {
            StatusKind.Bleed => _bleedDot.IsActive,
            StatusKind.Burn => _burnDot.IsActive,
            StatusKind.Static => _static != null && _static.IsCharged,
            _ => _timers.IsActive(kind, Time.time),
        };

        public float Remaining(StatusKind kind) => kind switch
        {
            StatusKind.Bleed => _bleedDot.Remaining(Time.time),
            StatusKind.Burn => _burnDot.Remaining(Time.time),
            _ => _timers.Remaining(kind, Time.time),
        };

        public static bool Blocked(StatusKind kind, bool controlImmune) => controlImmune && kind is StatusKind.Root or StatusKind.Slow;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _materials = GetComponent<CharacterMaterials>();
            _settings = StatusEffectSettings.Instance;
            _bleedEffect = SpawnEffect(_settings.Bleed.Effect);
            _burnEffect = SpawnEffect(_settings.Burn.Effect);
            _static = new(_settings.StaticCapacity);
            ChainLightningLines.Prewarm(_settings);
            Registry.Add(this);
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
            enabled = false;
        }

        private void OnDestroy()
        {
            Registry.Remove(this);
            if (_health == null)
                return;
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
        }

        private VisualEffect SpawnEffect(VisualEffect prefab)
        {
            if (prefab == null)
                return null;
            var effect = Instantiate(prefab, transform);
            effect.transform.localPosition = Vector3.up * _effectHeight;
            effect.Stop();
            return effect;
        }

        public void Apply(StatusKind kind, float magnitude, float duration) => Apply(kind, magnitude, duration, Team.Environment, 1f);

        public void Apply(StatusKind kind, float magnitude, float duration, Team source, float potency)
        {
            if (kind == StatusKind.None || _health == null || !_health.IsAlive || Blocked(kind, ControlImmune))
                return;
            switch (kind)
            {
                case StatusKind.Bleed:
                    ApplyDot(_bleedDot, _settings.Bleed, magnitude, duration, potency);
                    _bleedSource = source;
                    break;
                case StatusKind.Burn:
                    ApplyDot(_burnDot, _settings.Burn, magnitude, duration, potency);
                    _burnSource = source;
                    break;
                case StatusKind.Static:
                    AddStatic((magnitude > 0f ? magnitude : _settings.StaticPerHit) * Mathf.Max(0f, potency), source);
                    return;
                default:
                    _timers.Apply(kind, magnitude, duration, Time.time);
                    break;
            }
            enabled = true;
            Refresh();
        }

        private void ApplyDot(DamageOverTime dot, StatusEffectSettings.DamageStatus status, float magnitude, float duration, float potency)
        {
            var perStack = (magnitude > 0f ? magnitude : status.DamagePerSecond) * Mathf.Max(0f, potency);
            dot.Apply(perStack, duration > 0f ? duration : status.Duration, status.MaxStacks, Time.time, _settings.TickInterval);
            if (!status.ApplySound.IsNull)
                RuntimeManager.PlayOneShot(status.ApplySound, transform.position);
        }

        public float BurstBleed(float scale = 1f)
        {
            if (!_bleedDot.IsActive)
                return 0f;
            var damage = _bleedDot.Burst(Time.time) * scale;
            Refresh();
            if (damage > 0f)
                Deal(damage, DamageType.Physical, _bleedSource);
            BleedBurst?.Invoke(this, damage);
            return damage;
        }

        public void AddStatic(float amount, Team source)
        {
            if (_health == null || !_health.IsAlive || source == _health.Team || amount <= 0f)
                return;
            if (_static.Add(amount * StaticBuildScale))
                Discharge(source, 1f);
            Refresh();
        }

        public void FillStatic(Team source) => AddStatic(_static.Capacity - _static.Value, source);

        public float ReleaseStatic(Team source, float scale = 1f)
        {
            if (!_static.IsCharged)
                return 0f;
            var fraction = _static.Release();
            Refresh();
            Discharge(source, fraction * scale);
            return fraction;
        }

        private void Discharge(Team source, float scale)
        {
            if (_cascade >= MaxCascade)
                return;
            _cascade++;
            try
            {
                Discharges++;
                var origin = transform.position + Vector3.up * _effectHeight;
                var count = PickChainTargets(origin, _health.Team);
                var targets = TargetBuffers[_cascade - 1];
                for (var i = 0; i < count; i++)
                    targets[i] = Candidates[Picks[i]];
                var damage = _settings.ChainDamage * ChainDamageScale * scale;
                if (!_settings.ChainSound.IsNull)
                    RuntimeManager.PlayOneShot(_settings.ChainSound, origin);
                for (var i = 0; i < count; i++)
                {
                    var target = targets[i];
                    targets[i] = null;
                    var point = target.transform.position + Vector3.up * target._effectHeight;
                    ChainLightningLines.Draw(origin, point);
                    target.TakeChain(damage, source, point, point - origin);
                }
                if (_settings.ChainHitsSource)
                    TakeChain(damage, source, origin, Vector3.up);
                StaticDischarged?.Invoke(this, count);
            }
            finally
            {
                _cascade--;
            }
        }

        private int PickChainTargets(Vector3 origin, Team team)
        {
            Candidates.Clear();
            CandidatePositions.Clear();
            foreach (var other in Registry)
            {
                if (other == this || other._health == null || !other._health.IsAlive || other._health.Team != team)
                    continue;
                Candidates.Add(other);
                CandidatePositions.Add(other.transform.position + Vector3.up * other._effectHeight);
            }
            return ChainRules.PickNearest(origin, CandidatePositions, _settings.ChainRadius, _settings.ChainTargets + ExtraChainTargets, Picks);
        }

        private void TakeChain(float damage, Team source, Vector3 point, Vector3 direction)
        {
            if (!_health.IsAlive)
                return;
            if (damage > 0f)
            {
                var info = DamageInfo.Create(damage, source, DamageType.Electric);
                info.Flags = HitFlags.ChainLightning;
                info.Point = point;
                info.Direction = direction;
                _health.TakeDamage(info);
            }
            if (ChainAddsStatic > 0f)
                AddStatic(ChainAddsStatic, source);
        }

        public void ClearAll()
        {
            _timers.Clear(StatusKind.Slow);
            _timers.Clear(StatusKind.Root);
            _bleedDot.Clear();
            _burnDot.Clear();
            _static.Clear();
            Refresh();
        }

        private void Update()
        {
            var now = Time.time;
            var interval = _settings.TickInterval;
            var bleed = _bleedDot.Tick(now, interval);
            var burn = _burnDot.Tick(now, interval);
            if (bleed > 0f)
                Deal(bleed, DamageType.Physical, _bleedSource);
            if (burn > 0f)
                Deal(burn, DamageType.Fire, _burnSource);
            if (Refresh())
                enabled = false;
        }

        private void Deal(float amount, DamageType type, Team source)
        {
            if (!_health.IsAlive)
                return;
            var info = DamageInfo.Create(amount, source, type);
            info.Flags = HitFlags.DamageOverTime;
            info.Point = transform.position + Vector3.up * _effectHeight;
            _health.TakeDamage(info);
        }

        private void OnDamaged(Health health, in DamageInfo info, in DamageResult result)
        {
            if ((info.Flags & (HitFlags.DamageOverTime | HitFlags.ChainLightning)) != 0)
                return;
            var potency = info.SourceTeam == Team.Creature ? Stats.StatusPotency : 1f;
            if (info.Status != StatusKind.None)
                Apply(info.Status, info.StatusMagnitude, info.StatusDuration, info.SourceTeam, potency);
            if (info.Type == DamageType.Electric && info.Status != StatusKind.Static && info.SourceTeam == Team.Creature)
                AddStatic(_settings.StaticPerHit * potency, info.SourceTeam);
        }

        private void OnDied(Health health) => ClearAll();

        private bool Refresh()
        {
            var changed = false;
            var idle = true;
            for (var i = 1; i < KindCount; i++)
            {
                var kind = (StatusKind)i;
                var active = IsActive(kind);
                if (kind != StatusKind.Static)
                    idle &= !active;
                if (active == _shown[i])
                    continue;
                _shown[i] = active;
                changed = true;
                if (kind == StatusKind.Bleed)
                    Toggle(_bleedEffect, active);
                else if (kind == StatusKind.Burn)
                    Toggle(_burnEffect, active);
                if (active)
                    Started?.Invoke(this, kind);
                else
                    Ended?.Invoke(this, kind);
            }
            if (changed)
                ApplyGlow();
            return idle;
        }

        private static void Toggle(VisualEffect effect, bool on)
        {
            if (effect == null)
                return;
            if (on)
                effect.Play();
            else
                effect.Stop();
        }

        private void ApplyGlow()
        {
            if (_materials == null || !Glow)
                return;
            var visual = _shown[(int)StatusKind.Root] ? _root
                : _shown[(int)StatusKind.Slow] ? _slow
                : _shown[(int)StatusKind.Burn] ? _burn
                : _shown[(int)StatusKind.Bleed] ? _bleed
                : null;
            if (visual != null)
                _materials.SetStatus(visual.Color, visual.Amount, visual.Pulse);
            else
                _materials.ClearStatus();
        }
    }
}
