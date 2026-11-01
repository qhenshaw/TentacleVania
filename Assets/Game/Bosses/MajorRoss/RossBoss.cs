using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Enemies;
using TentacleVania.Enemies.Data;
using TentacleVania.Enemies.Traps;
using UnityEngine;

namespace TentacleVania.Bosses.MajorRoss
{
    public sealed class RossBoss : BossEnemy
    {
        [Header("Walker (design: destroyed at 30%, then Ross fights on foot)")]
        [SerializeField] private GameObject _walker;
        [SerializeField] private Transform _model;
        [Tooltip("Boss phase index at which the walker is destroyed.")]
        [SerializeField, Min(1)] private int _footPhase = 1;
        [Tooltip("Model scale multiplier on foot (the walker phase scales Ross up).")]
        [SerializeField, Min(0.1f)] private float _footScale = 0.65f;
        [SerializeField, Min(0f)] private float _wreckStun = 2f;
        [SerializeField] private EventReference _wreckSound;

        [Header("Missile Barrage")]
        [SerializeField] private string _barrageAttack = "Missile Barrage";
        [Tooltip("Extra rockets beside the one on the creature's marked spot.")]
        [SerializeField, Min(0)] private int _extraRockets = 3;
        [SerializeField, Min(0f)] private float _rocketSpread = 5f;

        private Vector3 _modelScale;
        private bool _onFoot;

        public bool OnFoot => _onFoot;

        protected override void Awake()
        {
            base.Awake();
            if (_model != null)
                _modelScale = _model.localScale;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _onFoot = false;
            if (_walker != null)
                _walker.SetActive(true);
            if (_model != null)
                _model.localScale = _modelScale;
        }

        protected override void OnTick(float now, float dt)
        {
            base.OnTick(now, dt);
            if (!_onFoot && Phase >= _footPhase && Health.IsAlive && !IsFinishing)
                Wreck();
        }

        private void Wreck()
        {
            _onFoot = true;
            if (_walker != null)
                _walker.SetActive(false);
            if (_model != null)
                _model.localScale = _modelScale * _footScale;
            if (!_wreckSound.IsNull)
                RuntimeManager.PlayOneShot(_wreckSound, Position);
            Stun(_wreckStun);
        }

        protected override Vector3 ChaseDestination(Vector3 targetPosition)
        {
            if (!_onFoot)
                return base.ChaseDestination(targetPosition);
            var away = Position - targetPosition;
            away.y = 0f;
            var distance = away.magnitude;
            var range = Definition.PreferredRange;
            if (distance >= range.x && distance <= range.y)
                return Position;
            var direction = distance > 1e-3f ? away / distance : -transform.forward;
            var ideal = (range.x + range.y) * 0.5f;
            for (var turn = 0; turn < 5; turn++)
            {
                var angle = (turn + 1) / 2 * 30f * (turn % 2 == 0 ? 1f : -1f);
                if (TrySampleNavMesh(targetPosition + Quaternion.Euler(0f, angle, 0f) * direction * ideal, 1.5f, out var point))
                    return point;
            }
            return distance > range.y ? targetPosition : Position;
        }

        protected override Vector3 CircleDestination(Vector3 targetPosition, float dt) => _onFoot ? ChaseDestination(targetPosition) : base.CircleDestination(targetPosition, dt);

        protected override void OnActiveStart(EnemyAttack attack)
        {
            base.OnActiveStart(attack);
            if (attack.Name != _barrageAttack || attack.BurstPrefab == null)
                return;
            var creature = CombatTargets.Creature;
            if (creature == null)
                return;
            var info = DamageInfo.Create(attack.Damage, Team.Site, attack.Type, attack.Stagger);
            info.Attacker = Health;
            var center = creature.transform.position;
            for (var i = 0; i < _extraRockets; i++)
            {
                var angle = (i + Random.value * 0.5f) * 360f / Mathf.Max(1, _extraRockets);
                var spot = center + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * Random.Range(attack.AreaRadius * 1.5f, _rocketSpread);
                EnemyManager.PlaceBurst(attack.BurstPrefab, spot, attack.AreaRadius, attack.BurstFuse + 0.2f * (i + 1), info);
            }
        }
    }
}
