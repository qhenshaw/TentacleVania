using System.Collections;
using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Core;
using TentacleVania.Core.Characters;
using TentacleVania.Core.Progression;
using TentacleVania.Creature.Movement;
using TentacleVania.Creature.Rig;
using UnityEngine;

namespace TentacleVania.Creature
{
    public sealed class CreatureSizeStage : MonoBehaviour
    {
        public const int StageCount = 4;

        [Tooltip("Rig per stage: Hatchling (empty keeps the prefab's rig), Stalker, Brute, Apex.")]
        [SerializeField] private CreatureRigSettings[] _rigs = new CreatureRigSettings[StageCount];
        [Tooltip("Grab reach multiplier per stage (design: size stages raise reach; placeholder numbers).")]
        [SerializeField] private float[] _reachScale = { 1f, 1.1f, 1.2f, 1.35f };
        [Tooltip("Body hurtbox and controller size multiplier per stage.")]
        [SerializeField] private float[] _bodyScale = { 1f, 1.15f, 1.3f, 1.5f };
        [Tooltip("Gameplay camera distance multiplier per stage.")]
        [SerializeField] private float[] _cameraScale = { 1f, 1.08f, 1.18f, 1.32f };
        [Header("Growth moment (design: 3-5 s at the nest)")]
        [SerializeField, Min(0.5f)] private float _growDuration = 3.5f;
        [SerializeField, Min(0f)] private float _rebuildAt = 1.2f;
        [SerializeField] private EventReference _growSound;

        private CreatureLegs _legs;
        private CreatureTentacles _tentacles;
        private CreatureConsume _consume;
        private CreatureMotor _motor;
        private CapsuleCollider _hurtbox;
        private CharacterController _controller;
        private CreatureRigSettings _hatchlingRig;
        private float _hurtboxRadius;
        private float _hurtboxHeight;
        private float _controllerRadius;
        private float _controllerHeight;
        private Vector3 _hurtboxCenter;
        private Vector3 _controllerCenter;
        private int _applied = -1;
        private bool _growing;

        public int Applied => _applied;
        public bool Growing => _growing;

        private void Awake()
        {
            _legs = GetComponent<CreatureLegs>();
            _tentacles = GetComponent<CreatureTentacles>();
            _consume = GetComponent<CreatureConsume>();
            _motor = GetComponent<CreatureMotor>();
            _controller = GetComponent<CharacterController>();
            var hurtbox = GetComponentInChildren<Hurtbox>(true);
            _hurtbox = hurtbox != null ? hurtbox.Collider as CapsuleCollider : null;
            if (_legs != null)
                _hatchlingRig = _legs.Settings;
            if (_hurtbox != null)
            {
                _hurtboxRadius = _hurtbox.radius;
                _hurtboxHeight = _hurtbox.height;
                _hurtboxCenter = _hurtbox.center;
            }
            if (_controller != null)
            {
                _controllerRadius = _controller.radius;
                _controllerHeight = _controller.height;
                _controllerCenter = _controller.center;
            }
        }

        private void OnEnable() => Stats.Changed += OnStatsChanged;

        private void OnDisable()
        {
            Stats.Changed -= OnStatsChanged;
            if (_growing)
            {
                Cutscene.End(this);
                _growing = false;
            }
        }

        private void Start() => Apply(Stats.SizeStage, false);

        private void OnStatsChanged()
        {
            var stage = Stats.SizeStage;
            if (stage == _applied || _growing)
                return;
            if (stage > _applied && _applied >= 0 && isActiveAndEnabled)
                StartCoroutine(Grow(stage));
            else
                Apply(stage, false);
        }

        private IEnumerator Grow(int stage)
        {
            _growing = true;
            Cutscene.Begin(this);
            _motor.LockControl();
            if (!_growSound.IsNull)
                RuntimeManager.PlayOneShot(_growSound, transform.position);
            yield return new WaitForSeconds(_rebuildAt);
            Apply(stage, true);
            yield return new WaitForSeconds(Mathf.Max(0f, _growDuration - _rebuildAt));
            _motor.UnlockControl();
            Cutscene.End(this);
            _growing = false;
        }

        public void Apply(int stage, bool grew)
        {
            stage = Mathf.Clamp(stage, 0, StageCount - 1);
            _applied = stage;
            var rig = _rigs != null && stage < _rigs.Length && _rigs[stage] != null ? _rigs[stage] : _hatchlingRig;
            if (rig != null && _legs != null && _legs.Settings != rig)
            {
                _legs.Build(rig);
                if (_tentacles != null)
                    _tentacles.Build(rig);
            }
            if (_consume != null)
                _consume.ReachScale = Scale(_reachScale, stage);
            var body = Scale(_bodyScale, stage);
            if (_hurtbox != null)
            {
                _hurtbox.radius = _hurtboxRadius * body;
                _hurtbox.height = _hurtboxHeight * body;
                _hurtbox.center = new Vector3(_hurtboxCenter.x, _hurtboxCenter.y * body, _hurtboxCenter.z);
            }
            if (_controller != null)
            {
                _controller.radius = _controllerRadius * body;
                _controller.height = Mathf.Max(_controllerHeight * body, _controller.radius * 2f);
                _controller.center = new Vector3(_controllerCenter.x, _controllerCenter.y * body, _controllerCenter.z);
            }
            CreatureSize.Set(stage, Scale(_cameraScale, stage), grew);
        }

        private static float Scale(float[] table, int stage) =>
            table != null && table.Length > 0 ? table[Mathf.Clamp(stage, 0, table.Length - 1)] : 1f;
    }
}
