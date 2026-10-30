using System.Collections;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Creature.Movement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TentacleVania.Creature
{
    [RequireComponent(typeof(Health), typeof(CreatureMotor))]
    public sealed class CreatureVitals : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _restartDelay = 1.5f;

        private Health _health;
        private CreatureMotor _motor;
        private DashInvulnerability _dashInvulnerability;

        public Health Health => _health;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _motor = GetComponent<CreatureMotor>();
            _dashInvulnerability = new(_motor);
        }

        private void OnEnable()
        {
            _health.Incoming.Add(_dashInvulnerability);
            _health.Died += OnDied;
            CombatTargets.RegisterCreature(_health, GetComponent<CharacterController>().center.y);
        }

        private void OnDisable()
        {
            _health.Incoming.Remove(_dashInvulnerability);
            _health.Died -= OnDied;
            CombatTargets.UnregisterCreature(_health);
        }

        private void OnDied(Health health)
        {
            _motor.InputEnabled = false;
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
                if (_motor.IsInvulnerable && (context.Info.Flags & HitFlags.Undodgeable) == 0)
                    context.Cancelled = true;
            }
        }
    }
}
