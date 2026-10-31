using TentacleVania.Enemies.Data;
using UnityEngine;

namespace TentacleVania.Enemies
{
    [RequireComponent(typeof(Enemy))]
    public sealed class EnemyAnimator : MonoBehaviour
    {
        public const string LocomotionState = "Locomotion";
        public const string StunState = "Stun";
        public const string CowerState = "Cower";
        public const string DeathState = "Death";
        public const string SpeedParameter = "Speed";
        public const string ActionSpeedParameter = "ActionSpeed";

        private const float CrossFade = 0.12f;
        private const float AttackCrossFade = 0.06f;
        private const float SpeedDamping = 0.1f;
        private const float MinActionSpeed = 0.25f;
        private const float MaxActionSpeed = 4f;
        private const float FlinchAttack = 0.2f;

        private static readonly int Locomotion = Animator.StringToHash(LocomotionState);
        private static readonly int Stun = Animator.StringToHash(StunState);
        private static readonly int Cower = Animator.StringToHash(CowerState);
        private static readonly int Death = Animator.StringToHash(DeathState);
        private static readonly int Speed = Animator.StringToHash(SpeedParameter);
        private static readonly int ActionSpeed = Animator.StringToHash(ActionSpeedParameter);
        private static readonly int[] Attacks =
        {
            Animator.StringToHash(nameof(AttackAnimation.Swing)),
            Animator.StringToHash(nameof(AttackAnimation.Punch)),
            Animator.StringToHash(nameof(AttackAnimation.Kick)),
            Animator.StringToHash(nameof(AttackAnimation.Shoot)),
        };

        [SerializeField] private Animator _animator;

        private Enemy _enemy;
        private int _current;
        private float _attackStartedAt = -1f;
        private Transform _model;
        private Quaternion _modelRest;
        private Vector3 _flinchAxis;
        private float _flinchDegrees;
        private float _flinchDuration;
        private float _flinchElapsed;

        public Animator Animator => _animator;
        public int CurrentState => _current;

        public string CurrentStateName
        {
            get
            {
                if (_current == Locomotion) return LocomotionState;
                if (_current == Stun) return StunState;
                if (_current == Cower) return CowerState;
                if (_current == Death) return DeathState;
                for (var i = 0; i < Attacks.Length; i++)
                {
                    if (_current == Attacks[i])
                        return ((AttackAnimation)i).ToString();
                }
                return "-";
            }
        }

        private void Awake()
        {
            _enemy = GetComponent<Enemy>();
            _animator.applyRootMotion = false;
            _model = _animator.transform;
            _modelRest = _model.localRotation;
        }

        private void OnEnable()
        {
            _current = Locomotion;
            _attackStartedAt = -1f;
            _flinchDuration = 0f;
            if (_model != null)
                _model.localRotation = _modelRest;
        }

        public void Flinch(Vector3 hitDirection, float degrees, float duration)
        {
            if (degrees <= 0f || duration <= 0f)
                return;
            hitDirection.y = 0f;
            if (hitDirection.sqrMagnitude < 1e-4f)
                hitDirection = -transform.forward;
            _flinchAxis = transform.InverseTransformDirection(Vector3.Cross(Vector3.up, hitDirection.normalized));
            _flinchDegrees = degrees;
            _flinchDuration = duration;
            _flinchElapsed = 0f;
        }

        private void TickFlinch(float dt)
        {
            if (_flinchDuration <= 0f)
                return;
            _flinchElapsed += dt;
            var t = _flinchElapsed / _flinchDuration;
            if (t >= 1f)
            {
                _flinchDuration = 0f;
                _model.localRotation = _modelRest;
                return;
            }
            var amount = t < FlinchAttack ? t / FlinchAttack : 1f - (t - FlinchAttack) / (1f - FlinchAttack);
            amount *= amount;
            _model.localRotation = Quaternion.AngleAxis(_flinchDegrees * amount, _flinchAxis) * _modelRest;
        }

        public void Tick(float dt)
        {
            TickFlinch(dt);
            _animator.SetFloat(Speed, _enemy.Velocity.magnitude, SpeedDamping, dt);

            switch (_enemy.State)
            {
                case EnemyState.Dead:
                    Play(Death, CrossFade);
                    break;
                case EnemyState.Stunned:
                case EnemyState.Consumed:
                    Play(Stun, CrossFade);
                    break;
                case EnemyState.Attacking:
                    PlayAttack(_enemy.CurrentAttack);
                    break;
                default:
                    Play(_enemy.IsCowering ? Cower : Locomotion, CrossFade);
                    break;
            }
        }

        private void PlayAttack(EnemyAttack attack)
        {
            if (attack == null || Mathf.Approximately(_attackStartedAt, _enemy.AttackStartedAt))
                return;
            _attackStartedAt = _enemy.AttackStartedAt;
            var index = (int)attack.Animation;
            var speed = attack.WindUp > 0f ? attack.AnimationHitTime / attack.WindUp : MaxActionSpeed;
            _animator.SetFloat(ActionSpeed, Mathf.Clamp(speed, MinActionSpeed, MaxActionSpeed));
            _current = Attacks[index];
            _animator.CrossFadeInFixedTime(_current, AttackCrossFade, 0, 0f);
        }

        private void Play(int state, float fade)
        {
            _attackStartedAt = -1f;
            if (_current == state)
                return;
            _current = state;
            _animator.CrossFadeInFixedTime(state, fade);
        }
    }
}
