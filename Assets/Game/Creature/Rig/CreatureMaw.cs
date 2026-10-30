using System;
using TentacleVania.Core.Characters;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace TentacleVania.Creature.Rig
{
    [DefaultExecutionOrder(106)]
    [RequireComponent(typeof(CreatureLegs))]
    public sealed class CreatureMaw : MonoBehaviour, IChainSource
    {
        private const int Joints = Chains.Joints;
        private const int MaxChains = CreatureRigSettings.MaxMawChains;
        private const float MinBone = 0.02f;
        private const float Slack = 1.05f;
        private const float OpenBulge = 0.3f;
        private const float SeekSpeed = 0.6f;
        private const float PreviewDistance = 1.3f;
        private const float PreviewHeight = 0.9f;
        private const float PreviewRadius = 0.35f;

        [SerializeField] private Material _material;

        private readonly Vector3[] _pose = new Vector3[Joints];
        private readonly SpringChain[] _chains = new SpringChain[MaxChains];
        private readonly LineRenderer[] _lines = new LineRenderer[MaxChains];
        private readonly Vector3[] _retractFrom = new Vector3[MaxChains];
        private readonly float[] _angle = new float[MaxChains];
        private readonly float[] _rootScale = new float[MaxChains];
        private readonly float[] _lengthScale = new float[MaxChains];
        private readonly float[] _widthScale = new float[MaxChains];
        private readonly float[] _openDelay = new float[MaxChains];
        private readonly float[] _closeDelay = new float[MaxChains];
        private readonly float[] _seed = new float[MaxChains];
        private readonly bool[] _leader = new bool[MaxChains];

        private CreatureLegs _legs;
        private CreatureRigSettings _settings;
        private MawTimeline _timeline;
        private bool _active;
        private bool _shown;
        private bool _previewRolled;
        private float _elapsed;
        private float _clock;
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
            Roll();
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
            Roll();
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

        private void Roll()
        {
            var count = ChainCount;
            var s = _settings;
            for (var i = 0; i < count; i++)
            {
                _angle[i] = MawPose.Angle(i, count) + Random.Range(-s.MawAngleJitter, s.MawAngleJitter);
                _rootScale[i] = 1f + Random.Range(-s.MawRootJitter, s.MawRootJitter);
                _lengthScale[i] = 1f + Random.Range(-s.MawLengthJitter, s.MawLengthJitter);
                _widthScale[i] = 1f + Random.Range(-s.MawWidthJitter, s.MawWidthJitter);
                _openDelay[i] = Random.Range(0f, s.MawTimingJitter);
                _closeDelay[i] = Random.Range(0f, s.MawTimingJitter);
                _seed[i] = Random.Range(0f, 100f);
                _leader[i] = false;
            }
            var leaders = Mathf.Min(count, Random.Range(s.MawLeaders.x, s.MawLeaders.y + 1));
            for (var placed = 0; placed < leaders;)
            {
                var pick = Random.Range(0, count);
                if (_leader[pick])
                    continue;
                _leader[pick] = true;
                placed++;
            }
        }

        private void Update()
        {
            var dt = Time.deltaTime * TimeScale;
            _clock += dt;
            if (Preview && !_active)
            {
                if (!_previewRolled)
                    Roll();
                _previewRolled = true;
                var mouth = Mouth;
                _target = new Vector3(mouth.x, transform.position.y + PreviewHeight, mouth.z) + transform.forward * PreviewDistance;
                _targetRadius = PreviewRadius;
                _openRadius = OpenRadiusFor(PreviewRadius);
                _phase = _timeline.Evaluate(Mathf.Clamp01(PreviewProgress) * _timeline.Total * 0.9999f, out _phaseT);
                return;
            }
            _previewRolled = false;
            if (!_active)
            {
                _phase = MawPhase.Hidden;
                return;
            }
            _elapsed += dt;
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
            var frame = MawPose.Frame(_target - mouth, Vector3.up);
            var axis = frame * Vector3.forward;
            for (var i = 0; i < count; i++)
            {
                var root = MawPose.Root(_angle[i], mouth, facing, _settings.MawRootRadius * _rootScale[i]);
                var up = MawPose.Radial(_angle[i], frame);
                var tip = Tip(i, mouth, root, up, out var arch, out var pinned, out var writhe);
                var span = Vector3.Distance(root, tip);
                var bone = Mathf.Max(MawPose.ArcLength(span, arch) * Slack / (Joints - 1), MinBone);
                ChainSolver.SeedArch(_pose, root, tip, up, arch);
                Writhe(i, up, Vector3.Cross(axis, up), writhe);
                _chains[i].Step(pinned ? _settings.MawSpring : _settings.MawWhipSpring, dt, _pose, bone, pinned);
                _lines[i].SetPositions(_chains[i].Positions);
                _lines[i].widthMultiplier = _widthScale[i];
                _lines[i].enabled = Chains.ShowPlaceholderLines;
            }
        }

        private Vector3 Tip(int i, Vector3 mouth, Vector3 root, Vector3 up, out float arch, out bool pinned, out float writhe)
        {
            var s = _settings;
            var angle = _angle[i];
            var length = _lengthScale[i];
            var open = mouth + (MawPose.OpenTip(angle, mouth, _target, _openRadius, s.MawOpenReach) - mouth) * length;
            var lean = s.MawSeekLean * (0.5f + 0.5f * Mathf.Sin((_clock * SeekSpeed + _seed[i]) * Mathf.PI * 2f));
            var seek = Vector3.Lerp(open, _target, lean);
            var close = MawPose.CloseTip(angle, mouth, _target, _targetRadius * length, s.MawPinch * _rootScale[i] * length);
            var pin = MawPose.PinTip(angle, mouth, _target, _targetRadius, s.MawLeaderHug);
            var rear = mouth - transform.forward * s.MawRearBack + up * (s.MawRootRadius * 0.5f);
            var openArch = _openRadius * OpenBulge * length;
            var wrapArch = _targetRadius * s.MawWrap * length;
            var overshoot = s.MawOvershoot;
            switch (_phase)
            {
                case MawPhase.Open:
                {
                    var seconds = _phaseT * _timeline.Open;
                    var anticipation = Mathf.Min(s.MawAnticipation, _timeline.Open * 0.5f);
                    if (seconds < anticipation)
                    {
                        arch = 0f;
                        pinned = false;
                        writhe = 0f;
                        return Vector3.Lerp(root, rear, MawPose.SmoothStep(seconds / Mathf.Max(anticipation, 1e-4f)));
                    }
                    var t = (seconds - anticipation) / Mathf.Max(_timeline.Open - anticipation, 1e-4f);
                    if (_leader[i])
                    {
                        var strike = Mathf.Clamp01(t / s.MawLeaderStrike);
                        arch = wrapArch * 0.5f * strike;
                        pinned = strike >= 1f;
                        writhe = pinned ? s.MawWrithe : 0f;
                        return Vector3.LerpUnclamped(rear, pin, MawPose.EaseOutBack(strike, overshoot));
                    }
                    var lash = MawPose.Stagger(t, _openDelay[i]);
                    arch = openArch * lash;
                    pinned = false;
                    writhe = s.MawSeekWrithe * lash;
                    return Vector3.LerpUnclamped(rear, seek, MawPose.EaseOutBack(lash, overshoot));
                }
                case MawPhase.Close:
                {
                    if (_leader[i])
                    {
                        var slide = MawPose.SmoothStep(_phaseT);
                        arch = wrapArch;
                        pinned = true;
                        writhe = s.MawWrithe;
                        return Vector3.Lerp(pin, close, slide);
                    }
                    var snap = MawPose.Stagger(_phaseT, _closeDelay[i]);
                    arch = Mathf.Lerp(openArch, wrapArch, snap);
                    pinned = snap >= 1f;
                    writhe = Mathf.Lerp(s.MawSeekWrithe, s.MawWrithe, snap);
                    return Vector3.LerpUnclamped(seek, close, MawPose.EaseOutBack(snap, overshoot));
                }
                case MawPhase.Swallow:
                    arch = wrapArch;
                    pinned = true;
                    writhe = s.MawWrithe;
                    return close;
                default:
                {
                    var back = _phaseT * _phaseT;
                    arch = wrapArch * (1f - back);
                    pinned = true;
                    writhe = s.MawWrithe * (1f - back);
                    return Vector3.Lerp(_retractFrom[i], root, back);
                }
            }
        }

        private void Writhe(int chain, Vector3 up, Vector3 side, float amplitude)
        {
            if (amplitude <= 0f)
                return;
            var last = Joints - 1;
            var time = _clock * _settings.MawWritheSpeed + _seed[chain];
            for (var j = 1; j <= last; j++)
            {
                var along = (float)j / last;
                var a = (Mathf.PerlinNoise(time, j * 0.37f) - 0.5f) * 2f;
                var b = (Mathf.PerlinNoise(j * 0.37f + 17f, time) - 0.5f) * 2f;
                _pose[j] += (up * a + side * b) * (amplitude * along);
            }
        }

        public int ChainOrder => 30;
        public int ChainCount => _settings != null ? Mathf.Min(_settings.MawCount, MaxChains) : 0;

        public void WriteChain(int chain, Span<Vector4> joints)
        {
            if (_shown)
            {
                var width = _widthScale[chain];
                Chains.Write(joints, _chains[chain].Positions, _settings.MawRootWidth * width, _settings.MawTipWidth * width);
            }
            else
            {
                Chains.Collapse(joints, Mouth);
            }
        }
    }
}
