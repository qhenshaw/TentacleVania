using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Core.Characters;
using TentacleVania.Creature.Movement;
using UnityEngine;

namespace TentacleVania.Creature
{
    [RequireComponent(typeof(Health))]
    public sealed class CreatureVoice : MonoBehaviour
    {
        [SerializeField] private EventReference _hurt;
        [SerializeField] private EventReference _death;
        [Tooltip("Heavy attacks and combo finishers.")]
        [SerializeField] private EventReference _grunt;
        [Tooltip("When a consume finishes.")]
        [SerializeField] private EventReference _feed;
        [Tooltip("Now and then while standing still out of combat.")]
        [SerializeField] private EventReference _idle;
        [Tooltip("When an arena locks down.")]
        [SerializeField] private EventReference _hiss;
        [SerializeField, Min(0f)] private float _hurtCooldown = 1.5f;
        [SerializeField] private Vector2 _idleInterval = new(8f, 15f);
        [Tooltip("Seconds without hitting or being hit before idle sounds start.")]
        [SerializeField, Min(0f)] private float _calmAfter = 6f;
        [SerializeField, Min(0f)] private float _stillSpeed = 0.2f;

        private Health _health;
        private CreatureMotor _motor;
        private float _hurtReady;
        private float _lastCombat;
        private float _nextIdle;

        public static bool Enabled { get; set; } = true;
        public int Played { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Enabled = true;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _motor = GetComponent<CreatureMotor>();
        }

        private void OnEnable()
        {
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
            CombatEvents.HitLanded += OnHit;
            CombatEvents.Consumed += OnConsumed;
            CreatureSignals.HeavyAttack += OnHeavyAttack;
            CreatureSignals.Lockdown += OnLockdown;
            _lastCombat = Time.time;
            ScheduleIdle();
        }

        private void OnDisable()
        {
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
            CombatEvents.HitLanded -= OnHit;
            CombatEvents.Consumed -= OnConsumed;
            CreatureSignals.HeavyAttack -= OnHeavyAttack;
            CreatureSignals.Lockdown -= OnLockdown;
        }

        private void Update()
        {
            var now = Time.time;
            if (now < _nextIdle)
                return;
            var still = _motor == null || _motor.Velocity.sqrMagnitude < _stillSpeed * _stillSpeed;
            if (_health.IsAlive && still && now - _lastCombat >= _calmAfter)
                Play(_idle);
            ScheduleIdle();
        }

        private void ScheduleIdle() => _nextIdle = Time.time + Random.Range(_idleInterval.x, _idleInterval.y);

        private void OnDamaged(Health health, in DamageInfo info, in DamageResult result)
        {
            _lastCombat = Time.time;
            if (result.HealthDamage <= 0f || !health.IsAlive || Time.time < _hurtReady)
                return;
            _hurtReady = Time.time + _hurtCooldown;
            Play(_hurt);
        }

        private void OnDied(Health health) => Play(_death);

        private void OnHit(Health target, in DamageInfo info, in DamageResult result)
        {
            if (info.Attacker == _health)
                _lastCombat = Time.time;
        }

        private void OnConsumed(Consumable consumable) => Play(_feed);

        private void OnHeavyAttack()
        {
            _lastCombat = Time.time;
            Play(_grunt);
        }

        private void OnLockdown()
        {
            _lastCombat = Time.time;
            Play(_hiss);
        }

        private void Play(EventReference sound)
        {
            if (!Enabled || sound.IsNull)
                return;
            Played++;
            RuntimeManager.PlayOneShot(sound, transform.position);
        }
    }
}
