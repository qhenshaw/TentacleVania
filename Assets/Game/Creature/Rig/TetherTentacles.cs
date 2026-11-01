using System;
using TentacleVania.Core.Characters;
using UnityEngine;

namespace TentacleVania.Creature.Rig
{
    public sealed class TetherTentacles : MonoBehaviour, IChainSource, INestTentacles
    {
        private const int PhaseSeed = 4127;
        private const float ProbeHeight = 0.6f;

        [SerializeField, Range(1, CreatureRigSettings.MaxMawChains)] private int _count = 12;
        [SerializeField, Range(Chains.MinJoints, Chains.MaxJoints)] private int _joints = 10;
        [Tooltip("Rays go out this far looking for a wall or prop side to anchor to; a tentacle with no anchor stays hidden.")]
        [SerializeField, Min(1f)] private float _maxReach = 9f;
        [SerializeField, Min(0f)] private float _rootRadius = 0.45f;
        [SerializeField, Min(0f)] private float _rootHeight = 0.3f;
        [Tooltip("Anchors must be on a near-vertical surface at least this high above the nest's floor.")]
        [SerializeField, Min(0f)] private float _minAnchorHeight = 1f;
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
        private static readonly float[] ProbePitches = { 6f, 12f, 18f, 26f, 35f, 45f };

        private Vector3[] _anchors = Array.Empty<Vector3>();
        private bool[] _anchored = Array.Empty<bool>();
        private float[] _phase = Array.Empty<float>();
        private Vector3[] _pose = Array.Empty<Vector3>();
        private bool _visible;
        private float _shownAt = -100f;
        private float _flaredAt = -100f;
        private float _emergeFrom;

        public int ChainOrder => 0;
        public int ChainCount => _chains.Length;
        public int ChainJoints => _joints;
        public bool Visible => _visible;

        private void Awake()
        {
            _chains = new SpringChain[_count];
            _anchors = new Vector3[_count];
            _anchored = new bool[_count];
            _phase = new float[_count];
            _pose = new Vector3[_joints];
            var random = new System.Random(PhaseSeed + _count);
            for (var i = 0; i < _count; i++)
            {
                _phase[i] = (float)random.NextDouble() * 100f;
                _chains[i] = new SpringChain(_joints);
                _chains[i].Collapse(Root(i));
            }
        }

        public void Show(bool emerge)
        {
            FindAnchors();
            _visible = true;
            _shownAt = Time.time;
            _emergeFrom = emerge ? 0f : 1f;
            for (var i = 0; i < _count; i++)
            {
                BuildPose(i, Time.time, Emerge());
                _chains[i].Reset(_pose);
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
            var emerge = Emerge();
            for (var i = 0; i < _count; i++)
            {
                if (!_anchored[i])
                    continue;
                BuildPose(i, now, emerge);
                var length = 0f;
                for (var j = 1; j < _joints; j++)
                    length += Vector3.Distance(_pose[j - 1], _pose[j]);
                _chains[i].Step(_spring, dt, _pose, SpringChain.BoneLengthFor(_joints, length), true);
            }
        }

        public void WriteChain(int chain, Span<Vector4> joints)
        {
            var flare = 1f + FlareAmount() * 0.6f;
            if (_visible && _anchored[chain])
                Chains.Write(joints, _chains[chain].Positions, _rootWidth * flare, _tipWidth * flare);
            else
                Chains.Collapse(joints, Root(chain));
        }

        private Vector3 Root(int index)
        {
            var heading = Quaternion.Euler(0f, index * 360f / Mathf.Max(1, _count), 0f) * Vector3.forward;
            return transform.position + heading * _rootRadius + Vector3.up * _rootHeight;
        }

        private void BuildPose(int index, float time, float emerge)
        {
            var root = Root(index);
            var anchor = Vector3.Lerp(root, _anchors[index], emerge);
            var side = Vector3.Cross(Vector3.up, anchor - root).normalized;
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
                _pose[j] = Vector3.Lerp(root, anchor, along) + side * sway + Vector3.up * lift;
            }
        }

        private void FindAnchors()
        {
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
                    break;
                }
            }
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
