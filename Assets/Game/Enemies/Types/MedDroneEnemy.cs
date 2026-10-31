using FMOD.Studio;
using FMODUnity;
using TentacleVania.Combat;
using UnityEngine;
using UnityEngine.VFX;

namespace TentacleVania.Enemies.Types
{
    public sealed class MedDroneEnemy : Enemy
    {
        private const float AllyStandOff = 3f;
        private const float BeamTargetHeight = 1.1f;

        [SerializeField] private Transform _model;
        [SerializeField] private Transform _emitter;
        [SerializeField] private LineRenderer _beam;
        [SerializeField] private VisualEffect _healEffect;
        [SerializeField] private EventReference _hoverSound;
        [SerializeField] private EventReference _beamSound;
        [SerializeField, Min(0f)] private float _hoverHeight = 1.3f;
        [SerializeField, Min(0f)] private float _bobHeight = 0.12f;
        [SerializeField, Min(0.01f)] private float _bobSpeed = 0.6f;

        private EventInstance _hover;
        private EventInstance _beamLoop;
        private Enemy _healing;
        private Enemy _ally;
        private bool _beamOn;
        private bool _hoverOn;
        private float _bobPhase;

        public Enemy HealTarget => _healing;

        protected override void Awake()
        {
            base.Awake();
            _bobPhase = Random.value * Mathf.PI * 2f;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetBeam(null);
            if (!_hoverSound.IsNull)
            {
                _hover = RuntimeManager.CreateInstance(_hoverSound);
                _hover.set3DAttributes(transform.position.To3DAttributes());
                _hover.start();
                _hoverOn = true;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SetBeam(null);
            StopHover();
        }

        protected override void OnDeath()
        {
            SetBeam(null);
            StopHover();
        }

        protected override void OnTick(float now, float dt)
        {
            Bob(now);
            var active = State == EnemyState.Chase || State == EnemyState.Circle || State == EnemyState.Attacking;
            if (!active || !Health.IsAlive)
            {
                SetBeam(null);
                if (State == EnemyState.Consumed)
                    StopHover();
                return;
            }
            FindAllies(out _ally, out var wounded);
            SetBeam(wounded);
            if (_healing != null)
            {
                _healing.Health.Heal(Definition.HealPerSecond * dt);
                Face(_healing.transform.position - Position, dt);
            }
            if (_hoverOn)
                _hover.set3DAttributes(transform.position.To3DAttributes());
            if (_beamOn)
                _beamLoop.set3DAttributes(transform.position.To3DAttributes());
        }

        private void LateUpdate()
        {
            if (_beam == null || _healing == null)
                return;
            var origin = _emitter != null ? _emitter.position : transform.position + Vector3.up * _hoverHeight;
            var target = _healing.transform.position + Vector3.up * BeamTargetHeight;
            _beam.SetPosition(0, origin);
            _beam.SetPosition(1, target);
            if (_healEffect != null)
                _healEffect.transform.position = target;
        }

        protected override Vector3 ChaseDestination(Vector3 targetPosition)
        {
            var away = Position - targetPosition;
            away.y = 0f;
            var distance = away.magnitude;
            if (distance <= Definition.KeepAwayRange)
                return Flee(targetPosition, distance > 1e-3f ? away / distance : transform.forward);
            if (_ally == null)
                return Position;
            var allyPosition = _ally.transform.position;
            var side = allyPosition - targetPosition;
            side.y = 0f;
            var standOff = allyPosition + (side.sqrMagnitude > 1e-4f ? side.normalized : -transform.forward) * AllyStandOff;
            return TrySampleNavMesh(standOff, 2f, out var spot) ? spot : Position;
        }

        protected override Vector3 CircleDestination(Vector3 targetPosition, float dt) => ChaseDestination(targetPosition);

        protected override bool FaceTarget(float distance) => false;

        private Vector3 Flee(Vector3 targetPosition, Vector3 direction)
        {
            var reach = Definition.KeepAwayRange;
            for (var turn = 0; turn < 5; turn++)
            {
                var angle = (turn + 1) / 2 * 35f * (turn % 2 == 0 ? 1f : -1f);
                var candidate = Position + Quaternion.Euler(0f, angle, 0f) * direction * reach;
                if (TrySampleNavMesh(candidate, 1.5f, out var flee) && (flee - targetPosition).sqrMagnitude > (Position - targetPosition).sqrMagnitude)
                    return flee;
            }
            return Position;
        }

        private void FindAllies(out Enemy nearest, out Enemy wounded)
        {
            nearest = null;
            wounded = null;
            var nearestDistance = float.MaxValue;
            var lowest = 1f;
            var range = Definition.HealRange * Definition.HealRange;
            var enemies = EnemyManager.Instance.Enemies;
            for (var i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == this || enemy.Definition.Machine || enemy.Health == null || !enemy.Health.IsAlive
                    || enemy.State == EnemyState.Consumed || enemy.State == EnemyState.Dead)
                    continue;
                var offset = enemy.transform.position - Position;
                offset.y = 0f;
                var distance = offset.sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = enemy;
                }
                var normalized = enemy.Health.Normalized;
                if (distance <= range && normalized < lowest)
                {
                    lowest = normalized;
                    wounded = enemy;
                }
            }
        }

        private void SetBeam(Enemy target)
        {
            _healing = target;
            var on = target != null;
            if (_beam != null)
                _beam.enabled = on;
            if (on == _beamOn)
                return;
            _beamOn = on;
            if (_healEffect != null)
            {
                if (on)
                {
                    _healEffect.Reinit();
                    _healEffect.Play();
                }
                else
                {
                    _healEffect.Stop();
                }
            }
            if (on && !_beamSound.IsNull)
            {
                _beamLoop = RuntimeManager.CreateInstance(_beamSound);
                _beamLoop.set3DAttributes(transform.position.To3DAttributes());
                _beamLoop.start();
            }
            else if (!on && _beamLoop.isValid())
            {
                _beamLoop.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _beamLoop.release();
                _beamLoop.clearHandle();
            }
        }

        private void StopHover()
        {
            if (!_hoverOn)
                return;
            _hoverOn = false;
            _hover.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            _hover.release();
            _hover.clearHandle();
        }

        private void Bob(float now)
        {
            if (_model == null)
                return;
            var y = State == EnemyState.Dead ? 0.15f : _hoverHeight + Mathf.Sin(now * _bobSpeed * Mathf.PI * 2f + _bobPhase) * _bobHeight;
            var local = _model.localPosition;
            _model.localPosition = new(local.x, y, local.z);
        }
    }
}
