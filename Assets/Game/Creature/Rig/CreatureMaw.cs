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
        private const int MaxChains = CreatureRigSettings.MaxMawChains;
        private const float MinBone = 0.02f;
        private const float Slack = 1.05f;
        private const float CoilSlack = 1.02f;
        private const float OpenBulge = 0.3f;
        private const float SeekSpeed = 0.6f;
        private const float PreviewDistance = 1.3f;
        private const float PreviewHeight = 0.9f;
        private const float PreviewRadius = 0.3f;
        private const float PreviewHalfHeight = 0.9f;

        [SerializeField] private Material _material;

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
        private readonly float[] _coilHeight = new float[MaxChains];
        private readonly float[] _coilWrap = new float[MaxChains];
        private readonly float[] _coilSpin = new float[MaxChains];
        private readonly float[] _coilStart = new float[MaxChains];
        private readonly float[] _coilRise = new float[MaxChains];
        private readonly int[] _slots = new int[MaxChains];

        private Vector3[] _pose;
        private int _joints;
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
        private float _targetHalfHeight;
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
            _joints = _settings.MawJoints;
            _pose = new Vector3[_joints];
            var root = new GameObject("MawLines").transform;
            root.SetParent(transform, false);
            var width = new AnimationCurve(new Keyframe(0f, _settings.MawRootWidth), new Keyframe(1f, _settings.MawTipWidth));
            for (var i = 0; i < _chains.Length; i++)
            {
                _chains[i] = new SpringChain(_joints);
                var line = new GameObject($"MawTentacle {i + 1}").AddComponent<LineRenderer>();
                line.transform.SetParent(root, false);
                line.useWorldSpace = true;
                line.positionCount = _joints;
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

        public float OpenRadiusFor(float radius, float halfHeight)
        {
            var range = _settings.MawOpenRadius;
            return Mathf.Clamp(Mathf.Max(radius, halfHeight) + _settings.MawOpenMargin, range.x, range.y);
        }

        public void Engulf(Vector3 centre, float radius, float halfHeight)
        {
            _active = true;
            _elapsed = 0f;
            Roll();
            SetTarget(centre, radius, halfHeight);
            _openRadius = OpenRadiusFor(radius, halfHeight);
            _phase = _timeline.Evaluate(0f, out _phaseT);
        }

        public void SetTarget(Vector3 centre, float radius, float halfHeight)
        {
            _target = centre;
            _targetRadius = radius;
            _targetHalfHeight = halfHeight;
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
                _slots[i] = i;
            for (var i = count - 1; i > 0; i--)
            {
                var j = Random.Range(0, i + 1);
                (_slots[i], _slots[j]) = (_slots[j], _slots[i]);
            }
            var spread = s.MawCoilHeightSpread;
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

                var lateral = Mathf.Cos(_angle[i] * Mathf.Deg2Rad);
                _coilSpin[i] = lateral >= 0f ? -1f : 1f;
                _coilStart[i] = -lateral * s.MawCoilStartSpread;
                var slot = (_slots[i] + 0.5f + Random.Range(-0.4f, 0.4f)) / count;
                _coilHeight[i] = Mathf.Lerp(-spread, spread, slot);
                _coilWrap[i] = Mathf.Max(0f, s.MawCoilAngle + Random.Range(-s.MawCoilAngleJitter, s.MawCoilAngleJitter));
                _coilRise[i] = Random.value < 0.5f ? -1f : 1f;
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
                _targetHalfHeight = PreviewHalfHeight;
                _openRadius = OpenRadiusFor(PreviewRadius, PreviewHalfHeight);
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
            var near = mouth - _target;
            near.y = 0f;
            near = near.sqrMagnitude > 1e-6f ? near.normalized : -transform.forward;
            for (var i = 0; i < count; i++)
            {
                var root = MawPose.Root(_angle[i], mouth, facing, _settings.MawRootRadius * _rootScale[i]);
                var up = MawPose.Radial(_angle[i], frame);
                var length = Pose(i, mouth, root, up, near, out var pinned, out var writhe);
                var bone = Mathf.Max(length / (_joints - 1), MinBone);
                Writhe(i, up, Vector3.Cross(axis, up), writhe);
                _chains[i].Step(pinned ? _settings.MawSpring : _settings.MawWhipSpring, dt, _pose, bone, pinned);
                _lines[i].SetPositions(_chains[i].Positions);
                _lines[i].widthMultiplier = _widthScale[i];
                _lines[i].enabled = Chains.ShowPlaceholderLines;
            }
        }

        private float Pose(int i, Vector3 mouth, Vector3 root, Vector3 up, Vector3 near, out bool pinned, out float writhe)
        {
            var s = _settings;
            var length = _lengthScale[i];
            var open = mouth + (MawPose.OpenTip(_angle[i], mouth, _target, _openRadius, s.MawOpenReach) - mouth) * length;
            var lean = s.MawSeekLean * (0.5f + 0.5f * Mathf.Sin((_clock * SeekSpeed + _seed[i]) * Mathf.PI * 2f));
            var seek = Vector3.Lerp(open, _target, lean);
            var startDirection = Quaternion.AngleAxis(_coilStart[i], Vector3.up) * near;
            var coilRadius = _targetRadius + s.MawCoilClearance;
            var coilHeight = _coilHeight[i] * _targetHalfHeight;
            var coilStart = MawPose.CoilPoint(_target, startDirection, coilRadius, coilHeight, 0f);
            var rear = mouth - transform.forward * s.MawRearBack + up * (s.MawRootRadius * 0.5f);
            var openArch = _openRadius * OpenBulge * length;
            var approachArch = _targetRadius * s.MawWrap * length;
            var coiledWrithe = s.MawWrithe * s.MawCoilWrithe;
            var overshoot = s.MawOvershoot;
            switch (_phase)
            {
                case MawPhase.Open:
                {
                    var seconds = _phaseT * _timeline.Open;
                    var anticipation = Mathf.Min(s.MawAnticipation, _timeline.Open * 0.5f);
                    if (seconds < anticipation)
                    {
                        pinned = false;
                        writhe = 0f;
                        return Arch(root, Vector3.Lerp(root, rear, MawPose.SmoothStep(seconds / Mathf.Max(anticipation, 1e-4f))), up, 0f, Slack);
                    }
                    var t = (seconds - anticipation) / Mathf.Max(_timeline.Open - anticipation, 1e-4f);
                    if (_leader[i])
                    {
                        var strike = Mathf.Clamp01(t / s.MawLeaderStrike);
                        pinned = strike >= 1f;
                        writhe = pinned ? coiledWrithe : 0f;
                        return Arch(root, Vector3.LerpUnclamped(rear, coilStart, MawPose.EaseOutBack(strike, overshoot)), up, approachArch * strike, Slack);
                    }
                    var lash = MawPose.Stagger(t, _openDelay[i]);
                    pinned = false;
                    writhe = s.MawSeekWrithe * lash;
                    return Arch(root, Vector3.LerpUnclamped(rear, seek, MawPose.EaseOutBack(lash, overshoot)), up, openArch * lash, Slack);
                }
                case MawPhase.Close:
                {
                    if (_leader[i])
                    {
                        pinned = true;
                        writhe = coiledWrithe;
                        return Coil(i, root, up, startDirection, coilRadius, coilHeight, MawPose.SmoothStep(_phaseT), approachArch);
                    }
                    var snap = MawPose.Stagger(_phaseT, _closeDelay[i]);
                    var share = s.MawCoilStrike;
                    if (snap < share)
                    {
                        var strike = snap / share;
                        pinned = false;
                        writhe = Mathf.Lerp(s.MawSeekWrithe, coiledWrithe, strike);
                        var tip = Vector3.LerpUnclamped(seek, coilStart, MawPose.EaseOutBack(strike, overshoot));
                        return Arch(root, tip, up, Mathf.Lerp(openArch, approachArch, strike), Slack);
                    }
                    pinned = true;
                    writhe = coiledWrithe;
                    return Coil(i, root, up, startDirection, coilRadius, coilHeight, MawPose.SmoothStep((snap - share) / (1f - share)), approachArch);
                }
                case MawPhase.Swallow:
                    pinned = true;
                    writhe = coiledWrithe;
                    return Coil(i, root, up, startDirection, coilRadius, coilHeight, 1f, approachArch);
                default:
                {
                    var back = _phaseT * _phaseT;
                    pinned = true;
                    writhe = coiledWrithe * (1f - back);
                    return Arch(root, Vector3.Lerp(_retractFrom[i], root, back), up, approachArch * (1f - back), Slack);
                }
            }
        }

        private float Arch(Vector3 root, Vector3 tip, Vector3 up, float arch, float slack)
        {
            ChainSolver.SeedArch(_pose, root, tip, up, arch);
            return MawPose.ArcLength(Vector3.Distance(root, tip), arch) * slack;
        }

        private float Coil(int i, Vector3 root, Vector3 up, Vector3 startDirection, float radius, float height, float wind, float arch)
        {
            var s = _settings;
            var risePerDegree = s.MawCoilPitch * 2f * _targetHalfHeight * _coilRise[i] / Mathf.Max(1f, s.MawCoilAngle);
            return MawPose.Coil(_pose, root, _target, startDirection, radius, height, _coilWrap[i] * wind, _coilSpin[i], risePerDegree, up, arch) * CoilSlack;
        }

        private void Writhe(int chain, Vector3 up, Vector3 side, float amplitude)
        {
            if (amplitude <= 0f)
                return;
            var last = _joints - 1;
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
        public int ChainJoints => _joints;

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
