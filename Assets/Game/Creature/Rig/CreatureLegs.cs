using TentacleVania.Creature.Movement;
using UnityEngine;
using UnityEngine.Rendering;

namespace TentacleVania.Creature.Rig
{
    [DefaultExecutionOrder(100)]
    public sealed class CreatureLegs : MonoBehaviour
    {
        private const float GroundProbeHeight = 1f;
        private const float GroundProbeDepth = 3f;

        [SerializeField] private CreatureRigSettings _settings;
        [SerializeField] private Material _legMaterial;
        [SerializeField] private CreatureMotor _motor;

        private readonly RaycastHit[] _hits = new RaycastHit[8];

        private Collider _self;
        private Transform _legRoot;
        private Vector3[][] _joints;
        private LineRenderer[] _lines;
        private Vector3[] _feet;
        private Vector3[] _stepFrom;
        private float[] _stepProgress;
        private bool[] _stepping;
        private bool[] _start;
        private float[] _error;
        private Vector3 _lastPosition;

        public CreatureRigSettings Settings => _settings;
        public int LegCount => _feet?.Length ?? 0;
        public int SteppingCount { get; private set; }

        public Vector3 Foot(int leg) => _feet[leg];
        public bool IsStepping(int leg) => _stepping[leg];

        private void Awake()
        {
            _self = GetComponent<Collider>();
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
            _stepProgress = new float[count];
            _stepping = new bool[count];
            _start = new bool[count];
            _error = new float[count];

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
            }
            _lastPosition = transform.position;
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

            var count = _feet.Length;
            for (var i = 0; i < count; i++)
            {
                var offset = _feet[i] - RestPoint(i);
                offset.y = 0f;
                _error[i] = offset.magnitude;
            }

            LegGait.SelectSteps(_error, _stepping, _start, _settings.StepThreshold, _settings.EmergencyDistance);

            var up = transform.up;
            SteppingCount = 0;
            for (var i = 0; i < count; i++)
            {
                if (_start[i])
                {
                    _stepping[i] = true;
                    _stepProgress[i] = 0f;
                    _stepFrom[i] = _feet[i];
                }

                if (_stepping[i])
                {
                    var target = Ground(LandingPoint(i, velocity));
                    _stepProgress[i] = Mathf.Min(1f, _stepProgress[i] + dt / _settings.StepDuration);
                    var t = _stepProgress[i];
                    var eased = t * t * (3f - 2f * t);
                    _feet[i] = Vector3.Lerp(_stepFrom[i], target, eased) + up * (Mathf.Sin(t * Mathf.PI) * _settings.StepHeight);
                    if (t >= 1f)
                    {
                        _feet[i] = target;
                        _stepping[i] = false;
                    }
                    else
                    {
                        SteppingCount++;
                    }
                }

                var root = transform.TransformPoint(RootLocal(i));
                var joints = _joints[i];
                ChainSolver.SeedArch(joints, root, _feet[i], up, _settings.ArchHeight);
                ChainSolver.Solve(joints, _settings.BoneLength, root, _feet[i], up, _settings.SolverIterations);
                _lines[i].SetPositions(joints);
            }
        }

        private Vector3 RootLocal(int leg)
        {
            var direction = Quaternion.Euler(0f, _settings.LegAngle(leg), 0f) * Vector3.forward;
            return direction * _settings.RootRadius + Vector3.up * _settings.RootHeight;
        }

        private Vector3 RestPoint(int leg)
        {
            var direction = Quaternion.Euler(0f, _settings.LegAngle(leg), 0f) * Vector3.forward;
            return transform.TransformPoint(direction * _settings.RestRadius);
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
            var hitCount = Physics.RaycastNonAlloc(origin, Vector3.down, _hits, GroundProbeHeight + GroundProbeDepth, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
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
