using System;
using TentacleVania.Core.Characters;
using TentacleVania.Creature.Movement;
using UnityEngine;
using UnityEngine.Rendering;

namespace TentacleVania.Creature.Rig
{
    [DefaultExecutionOrder(100)]
    public sealed class CreatureLegs : MonoBehaviour, IChainSource
    {
        private const float GroundProbeHeight = 1f;
        private const float GroundProbeDepth = 3f;
        private const int VariationSeed = 7151;
        private const float BobDecay = 9f;
        private const float BreathRate = 1.7f;
        private const float StrideDuty = 0.7f;
        private const float MaxSpringStep = 1f / 120f;

        [SerializeField] private CreatureRigSettings _settings;
        [SerializeField] private Material _legMaterial;
        [SerializeField] private CreatureMotor _motor;
        [SerializeField] private LayerMask _groundLayers = 1;
        [SerializeField] private Transform[] _bodyParts;

        private readonly RaycastHit[] _hits = new RaycastHit[8];
        private readonly DashTrail _trail = new();

        private Collider _self;
        private Transform _legRoot;
        private Vector3[][] _joints;
        private LineRenderer[] _lines;
        private Vector3[] _feet;
        private Vector3[] _stepFrom;
        private Vector3[] _stepScatter;
        private float[] _stepProgress;
        private float[] _stepDuration;
        private float[] _lift;
        private bool[] _stepping;
        private bool[] _start;
        private float[] _error;
        private float[] _angleSpread;
        private float[] _radiusSpread;
        private float[] _paceSpread;
        private float[] _heightSpread;
        private float[] _thresholdSpread;
        private float[] _phase;
        private Vector3[] _bodyRest;
        private Vector3 _bodyOffset;
        private Vector3 _bodyVelocity;
        private float _bob;
        private Vector3 _lastPosition;
        private Vector3 _lastVelocity;
        private System.Random _random;
        private Vector3 _trailDirection = Vector3.back;

        public static bool Organic { get; set; } = true;
        public static bool DashStretch { get; set; } = true;

        public CreatureRigSettings Settings => _settings;
        public int LegCount => _feet?.Length ?? 0;
        public int SteppingCount { get; private set; }
        public Vector3 BodyOffset => _bodyOffset;
        public DashTrail Trail => _trail;
        public Vector3 TrailDirection => _trailDirection;

        public Vector3 Foot(int leg) => _feet[leg];
        public bool IsStepping(int leg) => _stepping[leg];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Organic = true;
            DashStretch = true;
        }

        private void Awake()
        {
            _self = GetComponent<Collider>();
            var parts = _bodyParts?.Length ?? 0;
            _bodyRest = new Vector3[parts];
            for (var i = 0; i < parts; i++)
                _bodyRest[i] = _bodyParts[i].localPosition;
            Build(_settings);
        }

        public void Build(CreatureRigSettings settings)
        {
            _settings = settings;
            if (_legRoot != null)
                Destroy(_legRoot.gameObject);
            _legRoot = new GameObject("Legs").transform;
            _legRoot.SetParent(transform, false);

            var count = settings.LegCount;
            _joints = new Vector3[count][];
            _lines = new LineRenderer[count];
            _feet = new Vector3[count];
            _stepFrom = new Vector3[count];
            _stepScatter = new Vector3[count];
            _stepProgress = new float[count];
            _stepDuration = new float[count];
            _lift = new float[count];
            _stepping = new bool[count];
            _start = new bool[count];
            _error = new float[count];
            _angleSpread = new float[count];
            _radiusSpread = new float[count];
            _paceSpread = new float[count];
            _heightSpread = new float[count];
            _thresholdSpread = new float[count];
            _phase = new float[count];

            _random = new(VariationSeed + count);
            for (var i = 0; i < count; i++)
            {
                _angleSpread[i] = Spread();
                _radiusSpread[i] = Spread();
                _paceSpread[i] = Spread();
                _heightSpread[i] = Spread();
                _thresholdSpread[i] = Spread();
                _phase[i] = (float)_random.NextDouble() * 100f;
            }

            var width = new AnimationCurve(new Keyframe(0f, settings.RootWidth), new Keyframe(1f, settings.TipWidth));
            for (var i = 0; i < count; i++)
            {
                _joints[i] = new Vector3[settings.JointsPerLeg];
                _feet[i] = Ground(RestPoint(i));
                var line = new GameObject($"Leg {i}").AddComponent<LineRenderer>();
                line.transform.SetParent(_legRoot, false);
                line.useWorldSpace = true;
                line.positionCount = settings.JointsPerLeg;
                line.widthCurve = width;
                line.numCornerVertices = 3;
                line.numCapVertices = 3;
                line.sharedMaterial = _legMaterial;
                line.shadowCastingMode = ShadowCastingMode.On;
                line.generateLightingData = true;
                _lines[i] = line;
                if (TryGetComponent<CharacterMaterials>(out var materials))
                    materials.Register(line);
            }
            _lastPosition = transform.position;
            _lastVelocity = Vector3.zero;
            _bodyOffset = Vector3.zero;
            _bodyVelocity = Vector3.zero;
            _bob = 0f;
            _trail.Reset();
            SteppingCount = 0;
        }

        private void LateUpdate()
        {
            var dt = Time.deltaTime;
            if (dt <= 0f)
                return;

            var position = transform.position;
            var velocity = _motor != null ? _motor.Velocity : (position - _lastPosition) / dt;
            velocity.y = 0f;
            _lastPosition = position;
            var acceleration = (velocity - _lastVelocity) / dt;
            _lastVelocity = velocity;

            var variation = Variation;
            var count = _feet.Length;
            for (var i = 0; i < count; i++)
            {
                var offset = _feet[i] - RestPoint(i);
                offset.y = 0f;
                _error[i] = offset.magnitude / (1f + _thresholdSpread[i] * variation);
            }

            LegGait.SelectSteps(_error, _stepping, _start, _settings.StepThreshold, _settings.EmergencyDistance);

            var up = transform.up;
            var liftCurve = Organic ? _settings.LiftCurve : 1f;
            var outward = Organic ? _settings.SwingOutward : 0f;
            var sway = Vector3.zero;
            var stride = (Mathf.Max(0.01f, _settings.LegLength - _settings.RestRadius) + _settings.StepThreshold) * StrideDuty;
            var duration = Mathf.Min(_settings.StepDuration, stride / Mathf.Max(0.01f, velocity.magnitude));
            SteppingCount = 0;
            for (var i = 0; i < count; i++)
            {
                if (_start[i])
                {
                    _stepping[i] = true;
                    _stepProgress[i] = 0f;
                    _stepFrom[i] = _feet[i];
                    _stepDuration[i] = duration * (1f + (_paceSpread[i] + Spread() * 0.5f) * variation);
                    var scatter = new Vector3(Spread(), 0f, Spread()) * (_settings.StepThreshold * variation);
                    _stepScatter[i] = scatter;
                }

                _lift[i] = 0f;
                if (_stepping[i])
                {
                    var target = Ground(LandingPoint(i, velocity) + _stepScatter[i]);
                    _stepProgress[i] = Mathf.Min(1f, _stepProgress[i] + dt / _stepDuration[i]);
                    var t = _stepProgress[i];
                    var eased = t * t * (3f - 2f * t);
                    var lift = Mathf.Sin(Mathf.PI * Mathf.Pow(t, liftCurve));
                    var height = _settings.StepHeight * (1f + _heightSpread[i] * variation * 1.5f);
                    var radial = LegDirection(i);
                    _feet[i] = Vector3.Lerp(_stepFrom[i], target, eased) + (up + radial * outward) * (lift * height);
                    _lift[i] = lift;
                    sway -= radial * lift;
                    if (t >= 1f)
                    {
                        _feet[i] = target;
                        _stepping[i] = false;
                        _lift[i] = 0f;
                        _bob = Mathf.Min(_bob + _settings.BodyBob * 0.6f, _settings.BodyBob * 1.5f);
                    }
                    else
                    {
                        SteppingCount++;
                    }
                }
            }

            UpdateBody(sway / count, acceleration, up, dt);

            var dashing = DashStretch && _motor != null && _motor.IsDashing;
            if (dashing)
                _trailDirection = _motor.DashDirection;
            _trail.Update(dashing, dt, _settings.DashRise, _settings.DashRecoverFrequency, _settings.DashRecoverDamping);
            var weight = _trail.Weight;
            var lengthScale = _trail.LengthScale(_settings.DashStretch);
            var bone = _settings.BoneLength * lengthScale;
            var arch = _settings.ArchHeight * (1f - weight * 0.8f);

            var time = Time.time * _settings.WritheSpeed;
            var writhe = Organic ? _settings.WritheAmplitude : 0f;
            for (var i = 0; i < count; i++)
            {
                var root = transform.TransformPoint(RootLocal(i)) + _bodyOffset;
                var joints = _joints[i];
                var foot = weight > 0f ? Vector3.Lerp(_feet[i], TrailPoint(i, root, up, lengthScale), weight) : _feet[i];
                ChainSolver.SeedArch(joints, root, foot, up, arch);
                ChainSolver.Solve(joints, bone, root, foot, up, _settings.SolverIterations);
                if (writhe > 0f)
                    Writhe(joints, root, foot, up, time, _phase[i], writhe * (0.6f + _lift[i] + weight));
                _lines[i].SetPositions(joints);
                _lines[i].enabled = Chains.ShowPlaceholderLines;
            }
        }

        public int ChainOrder => 0;
        public int ChainCount => LegCount;
        public int ChainJoints => _settings.JointsPerLeg;

        public void WriteChain(int chain, Span<Vector4> joints)
        {
            var thin = _trail.WidthScale(_settings.DashThinning);
            Chains.Write(joints, _joints[chain], _settings.RootWidth * thin, _settings.TipWidth * thin);
        }

        private float Variation => Organic ? _settings.Variation : 0f;

        private float Spread() => (float)_random.NextDouble() * 2f - 1f;

        private void UpdateBody(Vector3 sway, Vector3 acceleration, Vector3 up, float dt)
        {
            _bob *= Mathf.Exp(-BobDecay * dt);
            if (!Organic)
            {
                _bodyOffset = Vector3.zero;
                _bodyVelocity = Vector3.zero;
            }
            else
            {
                var breath = Mathf.Sin(Time.time * BreathRate) * _settings.BodyBob * 0.4f;
                var target = sway * _settings.BodySway + up * (breath - _bob);
                var omega = 2f * Mathf.PI * _settings.BodySpringFrequency;
                var damping = 2f * _settings.BodySpringDamping * omega;
                var drag = acceleration * _settings.BodyInertia;
                var steps = Mathf.CeilToInt(dt / MaxSpringStep);
                var step = dt / steps;
                for (var s = 0; s < steps; s++)
                {
                    _bodyVelocity += (omega * omega * (target - _bodyOffset) - damping * _bodyVelocity - drag) * step;
                    _bodyOffset += _bodyVelocity * step;
                }

                var limit = _settings.BodyMaxOffset;
                if (_bodyOffset.sqrMagnitude > limit * limit)
                {
                    _bodyOffset = _bodyOffset.normalized * limit;
                    var outward = Vector3.Dot(_bodyVelocity, _bodyOffset) / limit;
                    if (outward > 0f)
                        _bodyVelocity -= _bodyOffset / limit * outward;
                }
            }

            var local = transform.InverseTransformVector(_bodyOffset);
            for (var i = 0; i < _bodyRest.Length; i++)
                _bodyParts[i].localPosition = _bodyRest[i] + local;
        }

        private static void Writhe(Span<Vector3> joints, Vector3 root, Vector3 foot, Vector3 up, float time, float phase, float amplitude)
        {
            var span = foot - root;
            span -= up * Vector3.Dot(span, up);
            var side = Vector3.Cross(up, span);
            var sqr = side.sqrMagnitude;
            if (sqr < 1e-8f)
                return;
            side /= Mathf.Sqrt(sqr);

            var last = joints.Length - 1;
            for (var i = 1; i < last; i++)
            {
                var along = (float)i / last;
                var envelope = Mathf.Sin(Mathf.PI * along) * amplitude;
                var wave = time + phase - along * 1.5f;
                var lateral = Mathf.PerlinNoise(wave, phase) * 2f - 1f;
                var vertical = Mathf.PerlinNoise(phase, wave) * 2f - 1f;
                joints[i] += (side * lateral + up * (vertical * 0.5f)) * envelope;
            }
        }

        private float Angle(int leg) =>
            _settings.LegAngle(leg) + _angleSpread[leg] * Variation * (180f / _settings.LegCount);

        private Vector3 LegDirection(int leg) => transform.rotation * (Quaternion.Euler(0f, Angle(leg), 0f) * Vector3.forward);

        private Vector3 RootLocal(int leg)
        {
            var direction = Quaternion.Euler(0f, Angle(leg), 0f) * Vector3.forward;
            return direction * _settings.RootRadius + Vector3.up * _settings.RootHeight;
        }

        private Vector3 TrailPoint(int leg, Vector3 root, Vector3 up, float lengthScale)
        {
            var back = -_trailDirection;
            var side = Vector3.Dot(LegDirection(leg), Vector3.Cross(up, back));
            var direction = Quaternion.AngleAxis(side * _settings.DashFan, up) * back;
            var point = root + direction * (_settings.LegLength * lengthScale * 0.95f);
            return point + up * (Vector3.Dot(transform.position - point, up) + _settings.DashTrailHeight);
        }

        private Vector3 RestPoint(int leg)
        {
            var direction = Quaternion.Euler(0f, Angle(leg), 0f) * Vector3.forward;
            var radius = _settings.RestRadius * (1f + _radiusSpread[leg] * Variation * 0.5f);
            return transform.TransformPoint(direction * radius);
        }

        private Vector3 LandingPoint(int leg, Vector3 velocity)
        {
            var rest = RestPoint(leg);
            var lead = Vector3.ClampMagnitude(velocity * _settings.PredictTime, _settings.LegLength - _settings.RestRadius);
            return rest + lead;
        }

        private Vector3 Ground(Vector3 point)
        {
            var origin = point + Vector3.up * GroundProbeHeight;
            var hitCount = Physics.RaycastNonAlloc(origin, Vector3.down, _hits, GroundProbeHeight + GroundProbeDepth, _groundLayers, QueryTriggerInteraction.Ignore);
            var best = -1;
            for (var i = 0; i < hitCount; i++)
            {
                if (_hits[i].collider == _self)
                    continue;
                if (best < 0 || _hits[i].distance < _hits[best].distance)
                    best = i;
            }
            if (best >= 0)
                return _hits[best].point;
            point.y = transform.position.y;
            return point;
        }
    }
}
