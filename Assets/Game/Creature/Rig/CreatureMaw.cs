using System;
using TentacleVania.Core.Characters;
using UnityEngine;
using UnityEngine.Rendering;

namespace TentacleVania.Creature.Rig
{
    [DefaultExecutionOrder(106)]
    [RequireComponent(typeof(CreatureLegs))]
    public sealed class CreatureMaw : MonoBehaviour, IChainSource
    {
        private const int Joints = Chains.Joints;
        private const float MinBone = 0.02f;
        private const float Slack = 1.05f;
        private const float OpenBulge = 0.3f;
        private const float PreviewDistance = 1.3f;
        private const float PreviewHeight = 0.9f;
        private const float PreviewRadius = 0.35f;

        [SerializeField] private Material _material;

        private readonly Vector3[] _pose = new Vector3[Joints];
        private readonly SpringChain[] _chains = new SpringChain[CreatureRigSettings.MaxMawChains];
        private readonly LineRenderer[] _lines = new LineRenderer[CreatureRigSettings.MaxMawChains];
        private readonly Vector3[] _retractFrom = new Vector3[CreatureRigSettings.MaxMawChains];

        private CreatureLegs _legs;
        private CreatureRigSettings _settings;
        private MawTimeline _timeline;
        private bool _active;
        private bool _shown;
        private float _elapsed;
        private float _phaseT;
        private MawPhase _phase;
        private MawPhase _drawnPhase;
        private Vector3 _target;
        private float _targetRadius;
        private float _openRadius;

        public static float TimeScale { get; set; } = 1f;
        public static bool Preview { get; set; }
        public static float PreviewProgress { get; set; }

        public CreatureRigSettings Settings => _settings;
        public MawPhase Phase => _phase;
        public float PhaseT => _phaseT;
        public bool IsActive => _active;
        public float OpenTime => _timeline.Open;
        public Vector3 Mouth => transform.TransformPoint(_settings.MawOffset) + _legs.BodyOffset;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            TimeScale = 1f;
            Preview = false;
            PreviewProgress = 0f;
        }

        private void Awake()
        {
            _legs = GetComponent<CreatureLegs>();
            _settings = _legs.Settings;
            _timeline = _settings.MawTimeline;
            var root = new GameObject("MawLines").transform;
            root.SetParent(transform, false);
            var width = new AnimationCurve(new Keyframe(0f, _settings.MawRootWidth), new Keyframe(1f, _settings.MawTipWidth));
            for (var i = 0; i < _chains.Length; i++)
            {
                _chains[i] = new SpringChain(Joints);
                var line = new GameObject($"MawTentacle {i + 1}").AddComponent<LineRenderer>();
                line.transform.SetParent(root, false);
                line.useWorldSpace = true;
                line.positionCount = Joints;
                line.widthCurve = width;
                line.numCornerVertices = 3;
                line.numCapVertices = 3;
                line.sharedMaterial = _material;
                line.shadowCastingMode = ShadowCastingMode.On;
                line.generateLightingData = true;
                line.enabled = false;
                _lines[i] = line;
                if (TryGetComponent<CharacterMaterials>(out var materials))
                    materials.Register(line);
            }
        }

        private void OnDisable() => Cancel();

        public float OpenRadiusFor(float targetRadius)
        {
            var range = _settings.MawOpenRadius;
            return Mathf.Clamp(targetRadius + _settings.MawOpenMargin, range.x, range.y);
        }

        public void Engulf(Vector3 target, float targetRadius)
        {
            _active = true;
            _elapsed = 0f;
            SetTarget(target, targetRadius);
            _openRadius = OpenRadiusFor(targetRadius);
            _phase = _timeline.Evaluate(0f, out _phaseT);
        }

        public void SetTarget(Vector3 target, float targetRadius)
        {
            _target = target;
            _targetRadius = targetRadius;
        }

        public void Cancel()
        {
            _active = false;
            _phase = MawPhase.Hidden;
        }

        private void Update()
        {
            if (Preview && !_active)
            {
                var mouth = Mouth;
                var forward = transform.forward;
                _target = new Vector3(mouth.x, transform.position.y + PreviewHeight, mouth.z) + forward * PreviewDistance;
                _targetRadius = PreviewRadius;
                _openRadius = OpenRadiusFor(PreviewRadius);
                _phase = _timeline.Evaluate(Mathf.Clamp01(PreviewProgress) * _timeline.Total * 0.9999f, out _phaseT);
                return;
            }
            if (!_active)
            {
                _phase = MawPhase.Hidden;
                return;
            }
            _elapsed += Time.deltaTime * TimeScale;
            _phase = _timeline.Evaluate(_elapsed, out _phaseT);
            if (_phase == MawPhase.Hidden)
                _active = false;
        }

        private void LateUpdate()
        {
            var count = ChainCount;
            if (_phase == MawPhase.Hidden)
            {
                if (_shown)
                    for (var i = 0; i < count; i++)
                        _lines[i].enabled = false;
                _shown = false;
                _drawnPhase = MawPhase.Hidden;
                return;
            }

            var mouth = Mouth;
            var facing = transform.rotation;
            if (!_shown)
                for (var i = 0; i < count; i++)
                    _chains[i].Collapse(mouth);
            if (_phase == MawPhase.Retract && _drawnPhase != MawPhase.Retract)
                for (var i = 0; i < count; i++)
                    _retractFrom[i] = _chains[i].Tip;
            _shown = true;
            _drawnPhase = _phase;

            var dt = Time.deltaTime * TimeScale;
            var spring = _settings.MawSpring;
            for (var i = 0; i < count; i++)
            {
                var root = MawPose.Root(i, count, mouth, facing, _settings.MawRootRadius);
                var tip = Tip(i, count, mouth, root, out var arch, out var up);
                var span = Vector3.Distance(root, tip);
                var bone = Mathf.Max(MawPose.ArcLength(span, arch) * Slack / (Joints - 1), MinBone);
                ChainSolver.SeedArch(_pose, root, tip, up, arch);
                _chains[i].Step(spring, dt, _pose, bone, true);
                _lines[i].SetPositions(_chains[i].Positions);
                _lines[i].enabled = Chains.ShowPlaceholderLines;
            }
        }

        private Vector3 Tip(int index, int count, Vector3 mouth, Vector3 root, out float arch, out Vector3 up)
        {
            var frame = MawPose.Frame(_target - mouth, Vector3.up);
            up = MawPose.Radial(index, count, frame);
            var open = MawPose.OpenTip(index, count, mouth, _target, _openRadius, _settings.MawOpenReach);
            var close = MawPose.CloseTip(index, count, mouth, _target, _targetRadius, _settings.MawPinch);
            var openArch = _openRadius * OpenBulge;
            var wrapArch = _targetRadius * _settings.MawWrap;
            switch (_phase)
            {
                case MawPhase.Open:
                {
                    var s = MawPose.SmoothStep(_phaseT);
                    arch = openArch * s;
                    return Vector3.Lerp(root, open, s);
                }
                case MawPhase.Close:
                {
                    var s = _phaseT * _phaseT;
                    arch = Mathf.Lerp(openArch, wrapArch, s);
                    return Vector3.Lerp(open, close, s);
                }
                case MawPhase.Swallow:
                    arch = wrapArch;
                    return close;
                default:
                {
                    var s = _phaseT * _phaseT;
                    arch = wrapArch * (1f - s);
                    return Vector3.Lerp(_retractFrom[index], root, s);
                }
            }
        }

        public int ChainOrder => 30;
        public int ChainCount => _settings != null ? _settings.MawCount : 0;

        public void WriteChain(int chain, Span<Vector4> joints)
        {
            if (_shown)
                Chains.Write(joints, _chains[chain].Positions, _settings.MawRootWidth, _settings.MawTipWidth);
            else
                Chains.Collapse(joints, Mouth);
        }
    }
}
