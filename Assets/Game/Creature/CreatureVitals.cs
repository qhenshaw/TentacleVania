using System.Collections;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Core.Characters;
using TentacleVania.Creature.Movement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TentacleVania.Creature
{
    [RequireComponent(typeof(Health), typeof(CreatureMotor))]
    public sealed class CreatureVitals : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _restartDelay = 1.5f;
        [SerializeField, ColorUsage(false, true)] private Color _stunGlow = new(0.4f, 0.7f, 2.2f);
        [SerializeField, Min(0f)] private float _stunGlowPulse = 14f;

        private Health _health;
        private CreatureMotor _motor;
        private CharacterMaterials _materials;
        private CreatureCharge _charge;
        private DashInvulnerability _dashInvulnerability;
        private float _stunnedUntil = float.NegativeInfinity;
        private bool _stunned;

        public static bool GodMode { get; set; }

        public Health Health => _health;
        public bool IsStunned => _stunned;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _motor = GetComponent<CreatureMotor>();
            _materials = GetComponent<CharacterMaterials>();
            _charge = GetComponent<CreatureCharge>();
            _dashInvulnerability = new(_motor);
        }

        private void OnEnable()
        {
            _health.Incoming.Add(_dashInvulnerability);
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
            CombatTargets.RegisterCreature(_health, GetComponent<CharacterController>().center.y);
        }

        private void OnDisable()
        {
            _health.Incoming.Remove(_dashInvulnerability);
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
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
            if (duration <= 0f || !_health.IsAlive)
                return;
            _stunnedUntil = Mathf.Max(_stunnedUntil, Time.time + duration);
            if (_stunned)
                return;
            _stunned = true;
            _motor.LockControl();
            if (_materials != null)
                _materials.SetStatus(_stunGlow, 1f, _stunGlowPulse);
        }

        private void EndStun()
        {
            if (!_stunned)
                return;
            _stunned = false;
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

        private IEnumerator RestartArena()
        {
            yield return new WaitForSeconds(_restartDelay);
            SceneManager.LoadScene(gameObject.scene.name);
        }

        private sealed class DashInvulnerability : IDamageModifier
        {
            private readonly CreatureMotor _motor;

            public DashInvulnerability(CreatureMotor motor) => _motor = motor;

            public int Order => int.MinValue;

            public void Modify(ref DamageContext context)
            {
                if (GodMode || (_motor.IsInvulnerable && (context.Info.Flags & HitFlags.Undodgeable) == 0))
                    context.Cancelled = true;
            }
        }
    }
}
