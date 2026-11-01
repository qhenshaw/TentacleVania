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
        private const int KindCount = 8;
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
        [SerializeField] private Visual _corrode = new() { Color = new Color(0.5f, 1.8f, 0.2f), Amount = 0.6f, Pulse = 1f };
        [SerializeField] private Visual _parasite = new() { Color = new Color(1.2f, 0.3f, 1.4f), Amount = 0.5f, Pulse = 0.8f };
        [SerializeField, Min(0f)] private float _effectHeight = 1f;

        private readonly StatusTimers _timers = new();
        private readonly DamageOverTime _bleedDot = new();
        private readonly DamageOverTime _burnDot = new();
        private readonly DamageOverTime _parasiteDot = new();
        private readonly bool[] _shown = new bool[KindCount];
        private readonly float[] _appliedAt = InitAppliedAt();

        private Health _health;
        private CharacterMaterials _materials;
        private StatusEffectSettings _settings;
        private VisualEffect _bleedEffect;
        private VisualEffect _burnEffect;
        private VisualEffect _corrodeEffect;
        private VisualEffect _parasiteEffect;
        private Team _parasiteSource = Team.Environment;
        private ScaleModifier _corrodeModifier;
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
        public static bool ParasitesBurstOnDeath { get; set; } = true;
        public static float ParasiteBurstScale { get; set; } = 1f;
        public static int ParasiteBursts { get; private set; }
        public static event Action<StatusEffects, int> ParasiteBurst;

        private static float[] InitAppliedAt()
        {
            var times = new float[KindCount];
            for (var i = 0; i < KindCount; i++)
                times[i] = float.NegativeInfinity;
            return times;
        }

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
            ParasitesBurstOnDeath = true;
            ParasiteBurstScale = 1f;
            ParasiteBursts = 0;
            ParasiteBurst = null;
            _cascade = 0;
        }

        public float MoveScale => _timers.MoveScale(Time.time);
        public bool IsRooted => _timers.IsActive(StatusKind.Root, Time.time);
        public bool IsSlowed => _timers.IsActive(StatusKind.Slow, Time.time);
        public int BleedStacks => _bleedDot.Stacks;
        public int BleedMaxStacks => _settings != null ? _settings.Bleed.MaxStacks : 1;
        public float StaticNormalized => _static != null ? _static.Normalized : 0f;
        public float BleedDamagePerSecond => _bleedDot.DamagePerSecond;
        public float BurnDamagePerSecond => _burnDot.DamagePerSecond;
        public bool Glow { get; set; } = true;
        public bool ControlImmune { get; set; }
        public Health Health => _health;
        public StatusAffinity Affinity { get; } = new();

        public float SinceApplied(StatusKind kind) => (int)kind < KindCount ? Time.time - _appliedAt[(int)kind] : float.PositiveInfinity;

        public bool IsActive(StatusKind kind) => kind switch
        {
            StatusKind.Bleed => _bleedDot.IsActive,
            StatusKind.Burn => _burnDot.IsActive,
            StatusKind.Parasite => _parasiteDot.IsActive,
            StatusKind.Static => _static != null && _static.IsCharged,
            _ => _timers.IsActive(kind, Time.time),
        };

        public float Remaining(StatusKind kind) => kind switch
        {
            StatusKind.Bleed => _bleedDot.Remaining(Time.time),
            StatusKind.Burn => _burnDot.Remaining(Time.time),
            StatusKind.Parasite => _parasiteDot.Remaining(Time.time),
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
            _corrodeEffect = SpawnEffect(_settings.CorrodeEffect);
            _parasiteEffect = SpawnEffect(_settings.Parasite.Effect);
            ParasiteSwarm.Prewarm(_settings);
            _corrodeModifier = new() { HealthScale = _settings.CorrodeDamageTaken, ArmorScale = _settings.CorrodeArmorDamage };
            AcidPools.Prewarm(_settings);
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
            if (kind != StatusKind.Static)
            {
                potency *= Affinity.Scale(kind);
                _appliedAt[(int)kind] = Time.time;
            }
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
                case StatusKind.Parasite:
                    ApplyDot(_parasiteDot, _settings.Parasite, magnitude, duration, potency);
                    _parasiteSource = source;
                    break;
                case StatusKind.Corrode:
                    _timers.Apply(kind, 1f, (duration > 0f ? duration : _settings.CorrodeDuration) * Mathf.Max(0f, potency), Time.time);
                    if (!_settings.CorrodeSound.IsNull && !_shown[(int)StatusKind.Corrode])
                        RuntimeManager.PlayOneShot(_settings.CorrodeSound, transform.position);
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
            _appliedAt[(int)StatusKind.Static] = Time.time;
            if (_static.Add(amount * StaticBuildScale * Affinity.Scale(StatusKind.Static)))
                Discharge(source, 1f);
            Refresh();
        }

        public void FillStatic(Team source) => AddStatic((_static.Capacity - _static.Value) / Mathf.Max(0.01f, StaticBuildScale * Affinity.Scale(StatusKind.Static)), source);

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
                var count = PickChainTargets(origin);
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

        private int PickChainTargets(Vector3 origin) => Gather(origin, _settings.ChainRadius, _settings.ChainTargets + ExtraChainTargets);

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
            _timers.Clear(StatusKind.Corrode);
            _bleedDot.Clear();
            _burnDot.Clear();
            _parasiteDot.Clear();
            _static.Clear();
            Refresh();
        }

        private void Update()
        {
            var now = Time.time;
            var interval = _settings.TickInterval;
            var bleed = _bleedDot.Tick(now, interval);
            var burn = _burnDot.Tick(now, interval);
            var parasite = _parasiteDot.Tick(now, interval);
            if (bleed > 0f)
                Deal(bleed, DamageType.Physical, _bleedSource);
            if (burn > 0f)
                Deal(burn, DamageType.Fire, _burnSource);
            if (parasite > 0f)
                Deal(parasite, DamageType.Physical, _parasiteSource);
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

        private void OnDied(Health health)
        {
            var infected = _parasiteDot.IsActive;
            var source = _parasiteSource;
            ClearAll();
            if (infected && ParasitesBurstOnDeath)
                BurstParasites(source);
        }

        private void BurstParasites(Team source)
        {
            if (_cascade >= MaxCascade)
                return;
            _cascade++;
            try
            {
                ParasiteBursts++;
                var origin = transform.position + Vector3.up * _effectHeight;
                ParasiteSwarm.PlayBurst(origin);
                if (!_settings.ParasiteBurstSound.IsNull)
                    RuntimeManager.PlayOneShot(_settings.ParasiteBurstSound, origin);
                var count = Gather(origin, _settings.ParasiteBurstRadius, Picks.Length);
                var targets = TargetBuffers[_cascade - 1];
                for (var i = 0; i < count; i++)
                    targets[i] = Candidates[Picks[i]];
                var damage = _settings.ParasiteBurstDamage * ParasiteBurstScale;
                for (var i = 0; i < count; i++)
                {
                    var target = targets[i];
                    targets[i] = null;
                    target.Infect(source, damage);
                }
                ParasiteBurst?.Invoke(this, count);
            }
            finally
            {
                _cascade--;
            }
        }

        public int SpreadParasites(float radius, Team source)
        {
            var origin = transform.position + Vector3.up * _effectHeight;
            if (_cascade >= MaxCascade || _health == null)
                return 0;
            var count = Gather(origin, radius, Picks.Length);
            var targets = TargetBuffers[_cascade];
            for (var i = 0; i < count; i++)
                targets[i] = Candidates[Picks[i]];
            for (var i = 0; i < count; i++)
            {
                var target = targets[i];
                targets[i] = null;
                target.Infect(source, 0f);
            }
            return count;
        }

        private void Infect(Team source, float damage)
        {
            if (_health == null || !_health.IsAlive)
                return;
            Apply(StatusKind.Parasite, 0f, 0f, source, 1f);
            if (damage > 0f)
                Deal(damage, DamageType.Physical, source);
        }

        private int Gather(Vector3 origin, float radius, int max)
        {
            Candidates.Clear();
            CandidatePositions.Clear();
            foreach (var other in Registry)
            {
                if (other == this || other._health == null || !other._health.IsAlive || other._health.Team != _health.Team)
                    continue;
                Candidates.Add(other);
                CandidatePositions.Add(other.transform.position + Vector3.up * other._effectHeight);
            }
            return ChainRules.PickNearest(origin, CandidatePositions, radius, max, Picks);
        }

        public static StatusEffects NearestHostile(Vector3 position, Team attacker, float radius)
        {
            StatusEffects best = null;
            var bestDistance = radius * radius;
            foreach (var other in Registry)
            {
                if (other._health == null || !other._health.IsAlive || !TeamRules.CanDamage(attacker, other._health.Team) || other._health.Team == Team.Environment)
                    continue;
                var offset = other.transform.position - position;
                offset.y = 0f;
                var distance = offset.sqrMagnitude;
                if (distance > bestDistance)
                    continue;
                bestDistance = distance;
                best = other;
            }
            return best;
        }

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
                else if (kind == StatusKind.Parasite)
                    Toggle(_parasiteEffect, active);
                else if (kind == StatusKind.Corrode)
                {
                    Toggle(_corrodeEffect, active);
                    if (active)
                        _health.Incoming.Add(_corrodeModifier);
                    else
                        _health.Incoming.Remove(_corrodeModifier);
                }
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
                : _shown[(int)StatusKind.Corrode] ? _corrode
                : _shown[(int)StatusKind.Bleed] ? _bleed
                : _shown[(int)StatusKind.Parasite] ? _parasite
                : null;
            if (visual != null)
                _materials.SetStatus(visual.Color, visual.Amount, visual.Pulse);
            else
                _materials.ClearStatus();
        }
    }
}
