using System.Collections;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Core.Characters;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Scenes;
using TentacleVania.Creature.Movement;
using UnityEngine;

namespace TentacleVania.Creature
{
    [RequireComponent(typeof(Health), typeof(CreatureMotor))]
    public sealed class CreatureVitals : MonoBehaviour, IStatsSource
    {
        [SerializeField] private CreatureVitalsSettings _settings;

        private Health _health;
        private CreatureMotor _motor;
        private CreatureConsume _consume;
        private CharacterMaterials _materials;
        private CreatureCharge _charge;
        private DashInvulnerability _dashInvulnerability;
        private readonly OutgoingStatModifier _outgoingStats = new();
        private readonly IncomingStatModifier _incomingStats = new();
        private float _configuredMax;
        private float _stunnedUntil = float.NegativeInfinity;
        private float _stunImmuneUntil = float.NegativeInfinity;
        private bool _stunned;

        public static bool GodMode { get; set; }

        public Health Health => _health;
        public Object StatsAsset => _settings;
        public bool IsStunned => _stunned;
        public float StunGraceRemaining => _stunned ? 0f : Mathf.Max(0f, _stunImmuneUntil - Time.time);

        private void Awake()
        {
            _health = GetComponent<Health>();
            _motor = GetComponent<CreatureMotor>();
            _materials = GetComponent<CharacterMaterials>();
            _charge = GetComponent<CreatureCharge>();
            _consume = GetComponent<CreatureConsume>();
            _dashInvulnerability = new(_motor);
            ConfigureHealth();
        }

        private void OnEnable()
        {
            _health.Incoming.Add(_dashInvulnerability);
            _health.Incoming.Add(_incomingStats);
            _health.Outgoing.Add(_outgoingStats);
            Stats.Changed += ConfigureHealth;
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
            Nests.Rested += OnRested;
            CombatTargets.RegisterCreature(_health, GetComponent<CharacterController>().center.y);
        }

        private void OnDisable()
        {
            _health.Incoming.Remove(_dashInvulnerability);
            _health.Incoming.Remove(_incomingStats);
            _health.Outgoing.Remove(_outgoingStats);
            Stats.Changed -= ConfigureHealth;
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
            Nests.Rested -= OnRested;
            CombatTargets.UnregisterCreature(_health);
            EndStun();
        }

        private void Update()
        {
            if (_stunned && Time.time >= _stunnedUntil)
                EndStun();
        }

        public void Stun(float duration)
        {
            if (duration <= 0f || !_health.IsAlive || _stunned || Time.time < _stunImmuneUntil || (_consume != null && _consume.IsRooted))
                return;
            _stunnedUntil = Time.time + duration;
            _stunned = true;
            _motor.LockControl();
            if (_materials != null)
                _materials.SetStatus(_settings.StunGlow, 1f, _settings.StunGlowPulse, _settings.StunGlowFresnel, _settings.StunGlowFill);
        }

        private void EndStun()
        {
            if (!_stunned)
                return;
            _stunned = false;
            _stunImmuneUntil = Time.time + _settings.StunGrace;
            _motor.UnlockControl();
            if (_materials != null)
                _materials.ClearStatus();
            if (_charge != null)
                _charge.RefreshGlow();
        }

        private void OnDamaged(Health health, in DamageInfo info, in DamageResult result)
        {
            if (info.StunDuration > 0f)
                Stun(info.StunDuration);
        }

        private void OnDied(Health health)
        {
            EndStun();
            _motor.LockControl();
            StartCoroutine(RestartArena());
        }

        private void ConfigureHealth()
        {
            var max = (_settings.MaxHealth + Stats.MaxHealthBonus) * StatRules.SizeHealthScale(Stats.SizeStage);
            if (Mathf.Approximately(max, _configuredMax))
                return;
            _configuredMax = max;
            _health.Configure(Team.Creature, max, _settings.MaxArmor);
        }

        private void OnRested(string nest)
        {
            if (!_health.IsAlive)
                return;
            EndStun();
            _health.ResetToFull();
        }

        private IEnumerator RestartArena()
        {
            yield return new WaitForSeconds(_settings.RestartDelay);
            var scene = string.IsNullOrEmpty(Nests.RespawnScene) ? gameObject.scene.name : Nests.RespawnScene;
            SceneFlow.Load(scene);
        }

        private sealed class DashInvulnerability : IDamageModifier
        {
            private readonly CreatureMotor _motor;

            public DashInvulnerability(CreatureMotor motor) => _motor = motor;

            public int Order => int.MinValue;

            public void Modify(ref DamageContext context)
            {
                if (GodMode || (_motor.IsInvulnerable && (context.Info.Flags & (HitFlags.Undodgeable | HitFlags.DamageOverTime)) == 0))
                    context.Cancelled = true;
            }
        }
    }
}
