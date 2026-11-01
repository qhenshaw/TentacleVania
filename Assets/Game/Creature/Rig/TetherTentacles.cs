using System;
using TentacleVania.Core.Characters;
using UnityEngine;

namespace TentacleVania.Creature.Rig
{
    public sealed class TetherTentacles : MonoBehaviour, IChainSource, INestTentacles
    {
        public const int MaxBranchesPerStrand = 2;
        private const int PhaseSeed = 4127;
        private const float ProbeHeight = 0.6f;
        private const int BranchAttempts = 6;

        private static readonly float[] ProbePitches = { 6f, 12f, 18f, 26f, 35f, 45f };

        [SerializeField, Range(1, 24)] private int _count = 12;
        [SerializeField, Range(Chains.MinJoints, Chains.MaxJoints)] private int _joints = 10;
        [Tooltip("Rays go out this far looking for an anchor; a strand with no anchor stays hidden.")]
        [SerializeField, Min(1f)] private float _maxReach = 9f;
        [SerializeField, Min(0f)] private float _rootRadius = 0.45f;
        [SerializeField, Min(0f)] private float _rootHeight = 0.3f;
        [Tooltip("Main strands anchor only on a near-vertical surface at least this high above the nest's floor.")]
        [SerializeField, Min(0f)] private float _minAnchorHeight = 1f;
        [Tooltip("Each main strand gets 0 to this many branches (seeded per nest), each branching off between these fractions of its length and anchoring on any surface.")]
        [SerializeField, Range(0, MaxBranchesPerStrand)] private int _maxBranches = 2;
        [SerializeField] private Vector2 _branchAlong = new(0.3f, 0.7f);
        [Tooltip("Branch width at its root, as a fraction of the parent's width at the branch point.")]
        [SerializeField, Range(0.1f, 1f)] private float _branchWidth = 0.7f;
        [Tooltip("Strands per nest: mains first, then branches (trimmed to fit), then links between neighbouring mains fill the rest.")]
        [SerializeField, Range(1, 64)] private int _targetTotal = 32;
        [Tooltip("Where links attach along each of the two mains they join.")]
        [SerializeField] private Vector2 _linkAlong = new(0.25f, 0.85f);
        [Tooltip("Link width as a fraction of the thinner parent width at its ends.")]
        [SerializeField, Range(0.1f, 1f)] private float _linkWidth = 0.5f;
        [Tooltip("Sag at the middle of every strand, as a fraction of its length.")]
        [SerializeField, Range(0f, 0.2f)] private float _slack = 0.04f;
        [Tooltip("Metres the taut strand vibrates sideways at its middle.")]
        [SerializeField, Min(0f)] private float _vibration = 0.04f;
        [SerializeField, Min(0f)] private float _vibrationSpeed = 3f;
        [SerializeField, Min(0f)] private float _rootWidth = 0.18f;
        [SerializeField, Min(0f)] private float _tipWidth = 0.05f;
        [SerializeField, Min(0.1f)] private float _emergeTime = 1.6f;
        [SerializeField, Min(0.1f)] private float _flareTime = 0.9f;
        [SerializeField] private LayerMask _anchorLayers = 1;
        [SerializeField] private SpringChainSettings _spring = SpringChainSettings.Default;

        private readonly RaycastHit[] _hits = new RaycastHit[8];

        private SpringChain[] _chains = Array.Empty<SpringChain>();
        private Vector3[] _anchors = Array.Empty<Vector3>();
        private bool[] _anchored = Array.Empty<bool>();
        private float[] _phase = Array.Empty<float>();
        private int[] _branchParent = Array.Empty<int>();
        private float[] _branchAt = Array.Empty<float>();
        private int[] _linkTo = Array.Empty<int>();
        private float[] _linkAt = Array.Empty<float>();
        private int[] _rows = Array.Empty<int>();
        private Vector3[] _pose = Array.Empty<Vector3>();
        private int _rowCount;
        private bool _visible;
        private float _shownAt = -100f;
        private float _flaredAt = -100f;
        private float _emergeFrom;

        public int ChainOrder => 0;
        public int ChainCount => _rowCount;
        public int ChainJoints => _joints;
        public bool Visible => _visible;
        public int MaxChains => Mathf.Max(_targetTotal, _count) + _count * MaxBranchesPerStrand;

        private void Awake()
        {
            var total = MaxChains;
            _chains = new SpringChain[total];
            _anchors = new Vector3[total];
            _anchored = new bool[total];
            _phase = new float[total];
            _branchParent = new int[total];
            _branchAt = new float[total];
            _linkTo = new int[total];
            _linkAt = new float[total];
            _rows = new int[total];
            _pose = new Vector3[_joints];
            var random = new System.Random(PhaseSeed + _count);
            for (var i = 0; i < total; i++)
            {
                _phase[i] = (float)random.NextDouble() * 100f;
                _branchParent[i] = -1;
                _linkTo[i] = -1;
                _chains[i] = new SpringChain(_joints);
                _chains[i].Collapse(transform.position);
            }
        }

        public void Show(bool emerge)
        {
            FindAnchors();
            _visible = true;
            _shownAt = Time.time;
            _emergeFrom = emerge ? 0f : 1f;
            var now = Time.time;
            for (var r = 0; r < _rowCount; r++)
            {
                BuildPose(_rows[r], now);
                _chains[_rows[r]].Reset(_pose);
            }
        }

        public void Hide() => _visible = false;

        public void Flare() => _flaredAt = Time.time;

        private float Emerge()
        {
            var t = Mathf.Clamp01((Time.time - _shownAt) / _emergeTime);
            t = Mathf.Lerp(_emergeFrom, 1f, t);
            return 1f - (1f - t) * (1f - t) * (1f - t);
        }

        private float FlareAmount()
        {
            var t = (Time.time - _flaredAt) / _flareTime;
            return t is < 0f or > 1f ? 0f : Mathf.Sin(t * Mathf.PI);
        }

        private void LateUpdate()
        {
            if (!_visible)
                return;
            var now = Time.time;
            var dt = Time.deltaTime;
            for (var r = 0; r < _rowCount; r++)
            {
                var index = _rows[r];
                BuildPose(index, now);
                var length = 0f;
                for (var j = 1; j < _joints; j++)
                    length += Vector3.Distance(_pose[j - 1], _pose[j]);
                _chains[index].Step(_spring, dt, _pose, SpringChain.BoneLengthFor(_joints, length), true);
            }
        }

        public void WriteChain(int chain, Span<Vector4> joints)
        {
            var index = _rows[chain];
            if (!_visible)
            {
                Chains.Collapse(joints, transform.position);
                return;
            }
            var flare = 1f + FlareAmount() * 0.6f;
            var parent = _branchParent[index];
            float root, tip;
            if (parent < 0)
            {
                root = _rootWidth;
                tip = _tipWidth;
            }
            else if (_linkTo[index] >= 0)
            {
                var a = Mathf.Lerp(_rootWidth, _tipWidth, _branchAt[index]);
                var b = Mathf.Lerp(_rootWidth, _tipWidth, _linkAt[index]);
                root = tip = Mathf.Min(a, b) * _linkWidth;
            }
            else
            {
                root = Mathf.Lerp(_rootWidth, _tipWidth, _branchAt[index]) * _branchWidth;
                tip = _tipWidth * _branchWidth;
            }
            Chains.Write(joints, _chains[index].Positions, root * flare, tip * flare);
        }

        private Vector3 MainRoot(int index)
        {
            var heading = Quaternion.Euler(0f, index * 360f / Mathf.Max(1, _count), 0f) * Vector3.forward;
            return transform.position + heading * _rootRadius + Vector3.up * _rootHeight;
        }

        private Vector3 PointAlong(int index, float along)
        {
            var positions = _chains[index].Positions;
            var scaled = along * (positions.Length - 1);
            var i = Mathf.Min((int)scaled, positions.Length - 2);
            return Vector3.Lerp(positions[i], positions[i + 1], scaled - i);
        }

        private void BuildPose(int index, float time)
        {
            var parent = _branchParent[index];
            var emerge = Emerge();
            Vector3 root;
            if (parent < 0)
            {
                root = MainRoot(index);
            }
            else
            {
                root = PointAlong(parent, _branchAt[index]);
                var start = _linkTo[index] >= 0 ? Mathf.Max(_branchAt[index], _linkAt[index]) : _branchAt[index];
                emerge = Mathf.Clamp01((emerge - start) / (1f - start));
            }
            var target = _linkTo[index] >= 0 ? PointAlong(_linkTo[index], _linkAt[index]) : _anchors[index];
            var anchor = Vector3.Lerp(root, target, emerge);
            var sag = Vector3.Distance(root, anchor) * _slack;
            var side = Vector3.Cross(Vector3.up, anchor - root);
            side = side.sqrMagnitude > 1e-6f ? side.normalized : Vector3.right;
            var amplitude = _vibration * (1f + FlareAmount() * 6f);
            var phase = _phase[index];
            var last = _joints - 1;
            for (var j = 0; j <= last; j++)
            {
                var along = (float)j / last;
                var bend = Mathf.Sin(along * Mathf.PI);
                var wave = time * _vibrationSpeed + phase;
                var sway = (Mathf.PerlinNoise(wave, phase) * 2f - 1f) * amplitude * bend;
                var lift = (Mathf.PerlinNoise(phase, wave) * 2f - 1f) * amplitude * bend;
                _pose[j] = Vector3.Lerp(root, anchor, along) + side * sway + Vector3.up * (lift - sag * bend);
            }
        }

        private void FindAnchors()
        {
            _rowCount = 0;
            var origin = transform.position + Vector3.up * ProbeHeight;
            var minHeight = transform.position.y + _minAnchorHeight;
            for (var i = 0; i < _count; i++)
            {
                var yaw = i * 360f / _count + (_phase[i] % 1f) * 12f;
                _anchored[i] = false;
                foreach (var pitch in ProbePitches)
                {
                    var direction = Quaternion.Euler(-pitch, yaw, 0f) * Vector3.forward;
                    if (!Probe(origin, direction, out var hit) || Mathf.Abs(hit.normal.y) > 0.5f || hit.point.y < minHeight)
                        continue;
                    _anchors[i] = hit.point;
                    _anchored[i] = true;
                    _rows[_rowCount++] = i;
                    _chains[i].Collapse(MainRoot(i));
                    break;
                }
            }
            var random = new System.Random(Seed());
            var next = _count;
            for (var i = 0; i < _count; i++)
            {
                if (!_anchored[i])
                    continue;
                var branches = random.Next(0, _maxBranches + 1);
                for (var b = 0; b < branches && _rowCount < _targetTotal && next < MaxChains; b++)
                {
                    var at = Mathf.Lerp(_branchAlong.x, _branchAlong.y, (float)random.NextDouble());
                    var root = Vector3.Lerp(MainRoot(i), _anchors[i], at);
                    var along = _anchors[i] - MainRoot(i);
                    along.y = 0f;
                    var baseYaw = along.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(along).eulerAngles.y : 0f;
                    for (var attempt = 0; attempt < BranchAttempts; attempt++)
                    {
                        var turn = (float)(random.NextDouble() * 2.0 - 1.0) * 100f;
                        var pitch = Mathf.Lerp(-50f, 60f, (float)random.NextDouble());
                        var direction = Quaternion.Euler(-pitch, baseYaw + turn, 0f) * Vector3.forward;
                        if (!Probe(root, direction, out var hit) || hit.distance < 0.8f)
                            continue;
                        _branchParent[next] = i;
                        _linkTo[next] = -1;
                        _branchAt[next] = at;
                        _anchors[next] = hit.point;
                        _anchored[next] = true;
                        _chains[next].Collapse(root);
                        _rows[_rowCount++] = next;
                        next++;
                        break;
                    }
                }
            }
            var mains = 0;
            for (var i = 0; i < _count; i++)
                if (_anchored[i])
                    mains++;
            for (var attempt = 0; mains >= 2 && _rowCount < _targetTotal && next < MaxChains && attempt < _targetTotal * 4; attempt++)
            {
                var from = random.Next(0, _count);
                if (!_anchored[from])
                    continue;
                var step = random.Next(0, 2) == 0 ? 1 : _count - 1;
                var to = (from + step) % _count;
                while (!_anchored[to])
                    to = (to + step) % _count;
                if (to == from)
                    continue;
                _branchParent[next] = from;
                _linkTo[next] = to;
                _branchAt[next] = Mathf.Lerp(_linkAlong.x, _linkAlong.y, (float)random.NextDouble());
                _linkAt[next] = Mathf.Lerp(_linkAlong.x, _linkAlong.y, (float)random.NextDouble());
                _anchored[next] = true;
                _chains[next].Collapse(Vector3.Lerp(MainRoot(from), _anchors[from], _branchAt[next]));
                _rows[_rowCount++] = next;
                next++;
            }
            for (var i = next; i < MaxChains; i++)
            {
                _anchored[i] = false;
                _branchParent[i] = -1;
                _linkTo[i] = -1;
            }
        }

        private int Seed()
        {
            var p = transform.position;
            return PhaseSeed ^ Mathf.RoundToInt(p.x * 73f) ^ (Mathf.RoundToInt(p.z * 151f) << 8) ^ (Mathf.RoundToInt(p.y * 31f) << 16);
        }

        private bool Probe(Vector3 origin, Vector3 direction, out RaycastHit hit)
        {
            var count = Physics.RaycastNonAlloc(origin, direction, _hits, _maxReach, _anchorLayers, QueryTriggerInteraction.Ignore);
            var owner = transform.parent != null ? transform.parent : transform;
            var best = -1;
            for (var i = 0; i < count; i++)
            {
                if (_hits[i].transform.IsChildOf(owner))
                    continue;
                if (best < 0 || _hits[i].distance < _hits[best].distance)
                    best = i;
            }
            hit = best >= 0 ? _hits[best] : default;
            return best >= 0;
        }
    }
}
