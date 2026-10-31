using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Enemies;
using TentacleVania.Enemies.AI;
using TentacleVania.Enemies.Data;
using TentacleVania.Enemies.Telegraphs;
using UnityEngine;

namespace TentacleVania.Bosses.Automaton
{
    public sealed class SurgicalAutomaton : BossEnemy
    {
        private const int Anchors = 8;
        private const int Sectors = 2;

        [Header("Clamp Lunge (Surgical Automaton page)")]
        [SerializeField] private string _lungeAttack = "Clamp Lunge";
        [SerializeField, Min(1f)] private float _lungeDistance = 12f;
        [SerializeField] private LineRenderer _cable;
        [SerializeField] private Transform _clampOrigin;

        [Header("Phase 2 blade floor")]
        [SerializeField] private GroundIndicator[] _bladeIndicators = new GroundIndicator[Sectors];
        [SerializeField, Range(0f, 180f)] private float _bladeArc = 45f;
        [SerializeField] private float _bladeSpin = 10f;
        [SerializeField, Min(0f)] private float _bladeWarning = 1.5f;
        [SerializeField, Min(0f)] private float _bladeDamagePerSecond = 15f;
        [SerializeField, Min(0.05f)] private float _bladeTick = 0.25f;
        [SerializeField] private EventReference _bladeOpenSound;

        [Header("Theatre (fallback when the scene has no AutomatonTheatre)")]
        [SerializeField, Min(1f)] private float _theatreRadius = 11f;
        [SerializeField, Min(0f)] private float _tableRadius = 2.5f;

        private Vector3 _center;
        private float _radius;
        private float _table;
        private float _lastAttackStart = -1f;
        private Vector3 _anchor;
        private Vector3 _lungeEnd;
        private float _lungeSpeed;
        private bool _bladesOn;
        private float _bladesStarted;
        private float _bladeAngle;
        private float _nextBladeTick;

        public bool BladesOpen => _bladesOn && Time.time - _bladesStarted >= _bladeWarning;
        public float BladeAngle => _bladeAngle;
        public Vector3 TheatreCenter => _center;

        protected override void OnEnable()
        {
            base.OnEnable();
            var theatre = FindAnyObjectByType<AutomatonTheatre>();
            _center = theatre != null ? theatre.transform.position : transform.position;
            _radius = theatre != null ? theatre.Radius : _theatreRadius;
            _table = theatre != null ? theatre.TableRadius : _tableRadius;
            _bladesOn = false;
            _bladeAngle = Random.Range(0f, 360f);
            HideCable();
            HideBlades();
            Health.Bleeds = false;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            HideCable();
            HideBlades();
        }

        protected override void OnTick(float now, float dt)
        {
            base.OnTick(now, dt);
            if (!Health.IsAlive || IsFinishing || State == EnemyState.Dead || State == EnemyState.Consumed)
            {
                HideCable();
                HideBlades();
                return;
            }
            TickLunge(dt);
            if (!_bladesOn && Phase >= 1)
            {
                _bladesOn = true;
                _bladesStarted = now;
                _nextBladeTick = now + _bladeWarning;
            }
            if (_bladesOn)
                TickBlades(now, dt);
        }

        private void TickLunge(float dt)
        {
            var attack = CurrentAttack;
            if (State != EnemyState.Attacking || attack == null || attack.Name != _lungeAttack)
            {
                HideCable();
                return;
            }
            if (!Mathf.Approximately(AttackStartedAt, _lastAttackStart))
            {
                _lastAttackStart = AttackStartedAt;
                _anchor = Vector3.zero;
            }
            var phase = CurrentPhase;
            if (phase == AttackPhase.WindUp || _anchor == Vector3.zero)
                PickAnchor();
            ShowCable();
            if (phase != AttackPhase.Active || !Agent.enabled || !Agent.isOnNavMesh)
                return;
            var step = Vector3.MoveTowards(transform.position, _lungeEnd, _lungeSpeed * dt) - transform.position;
            step.y = 0f;
            Agent.Move(step);
        }

        private void PickAnchor()
        {
            var forward = transform.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 1e-4f ? forward.normalized : Vector3.forward;
            _anchor = AutomatonRules.AnchorAlong(_center, _radius, Anchors, transform.position, forward);
            var toAnchor = _anchor - transform.position;
            toAnchor.y = 0f;
            var distance = Mathf.Min(_lungeDistance, Mathf.Max(0f, toAnchor.magnitude - 1.2f));
            _lungeEnd = transform.position + (toAnchor.sqrMagnitude > 1e-4f ? toAnchor.normalized : forward) * distance;
            var active = CurrentAttack != null ? CurrentAttack.Active : 0.4f;
            _lungeSpeed = distance / Mathf.Max(0.05f, active);
        }

        private void ShowCable()
        {
            if (_cable == null)
                return;
            _cable.enabled = true;
            _cable.SetPosition(0, _clampOrigin != null ? _clampOrigin.position : transform.position + Vector3.up * 1.5f);
            _cable.SetPosition(1, _anchor + Vector3.up * 1.5f);
        }

        private void HideCable()
        {
            if (_cable != null)
                _cable.enabled = false;
        }

        private void TickBlades(float now, float dt)
        {
            var open = now - _bladesStarted >= _bladeWarning;
            if (open)
            {
                if (now - dt - _bladesStarted < _bladeWarning && !_bladeOpenSound.IsNull)
                    RuntimeManager.PlayOneShot(_bladeOpenSound, _center);
                _bladeAngle += _bladeSpin * dt;
            }
            var progress = Mathf.Clamp01((now - _bladesStarted) / Mathf.Max(0.01f, _bladeWarning));
            for (var i = 0; i < _bladeIndicators.Length && i < Sectors; i++)
            {
                if (_bladeIndicators[i] == null)
                    continue;
                var direction = Quaternion.Euler(0f, _bladeAngle + i * 180f, 0f) * Vector3.forward;
                _bladeIndicators[i].ShowSector(_center, direction, _radius, _bladeArc, Telegraph.Undodgeable, open ? 1f : progress, open);
            }
            if (!open || now < _nextBladeTick)
                return;
            _nextBladeTick = now + _bladeTick;
            var creature = CombatTargets.Creature;
            if (creature == null || !creature.IsAlive)
                return;
            if (!AutomatonRules.InBlade(creature.transform.position - _center, _bladeAngle, _bladeArc, _table, _radius, Sectors))
                return;
            var info = DamageInfo.Create(_bladeDamagePerSecond * _bladeTick, Team.Site);
            info.Flags = HitFlags.Hazard;
            info.Attacker = Health;
            info.Point = creature.transform.position;
            creature.TakeDamage(info);
        }

        private void HideBlades()
        {
            foreach (var indicator in _bladeIndicators)
                if (indicator != null)
                    indicator.Hide();
        }
    }
}
