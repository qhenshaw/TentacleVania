using System;
using TentacleVania.Core.Characters;
using UnityEngine;
using UnityEngine.Rendering;

namespace TentacleVania.Creature.Rig
{
    [DefaultExecutionOrder(105)]
    [RequireComponent(typeof(CreatureLegs))]
    public sealed class CreatureTentacles : MonoBehaviour, IChainSource
    {
        private const float RootRadius = 0.08f;
        private const int PhaseSeed = 3301;
        private const float TrailPitch = 12f;

        [SerializeField] private Material _material;

        private Vector3[] _pose = Array.Empty<Vector3>();
        private int _joints;

        private CreatureLegs _legs;
        private CreatureRigSettings _settings;
        private Transform _root;
        private SpringChain[] _chains = Array.Empty<SpringChain>();
        private LineRenderer[] _lines = Array.Empty<LineRenderer>();
        private float[] _phase = Array.Empty<float>();

        public static bool Visible { get; set; } = true;

        public int Count => _chains.Length;
        public SpringChain Chain(int index) => _chains[index];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Visible = true;

        private void Awake()
        {
            _legs = GetComponent<CreatureLegs>();
            Build(_legs.Settings);
        }

        public void Build(CreatureRigSettings settings)
        {
            _settings = settings;
            if (_root != null)
                Destroy(_root.gameObject);
            _root = new GameObject("FreeTentacles").transform;
            _root.SetParent(transform, false);

            var count = settings.FreeCount;
            _joints = settings.FreeJoints;
            _pose = new Vector3[_joints];
            _chains = new SpringChain[count];
            _lines = new LineRenderer[count];
            _phase = new float[count];
            var random = new System.Random(PhaseSeed + count);
            var width = new AnimationCurve(new Keyframe(0f, settings.FreeRootWidth), new Keyframe(1f, settings.FreeTipWidth));
            for (var i = 0; i < count; i++)
            {
                _phase[i] = (float)random.NextDouble() * 100f;
                _chains[i] = new SpringChain(_joints);
                BuildPose(i, Time.time);
                _chains[i].Reset(_pose);

                var line = new GameObject($"FreeTentacle {i + 1}").AddComponent<LineRenderer>();
                line.transform.SetParent(_root, false);
                line.useWorldSpace = true;
                line.positionCount = _joints;
                line.widthCurve = width;
                line.numCornerVertices = 3;
                line.numCapVertices = 3;
                line.sharedMaterial = _material;
                line.shadowCastingMode = ShadowCastingMode.On;
                line.generateLightingData = true;
                _lines[i] = line;
                if (TryGetComponent<CharacterMaterials>(out var materials))
                    materials.Register(line);
            }
        }

        private void LateUpdate()
        {
            var dt = Time.deltaTime;
            var bone = SpringChain.BoneLengthFor(_joints, _settings.FreeLength * _legs.Trail.LengthScale(_settings.DashStretch));
            var spring = _settings.FreeSpring;
            for (var i = 0; i < _chains.Length; i++)
            {
                BuildPose(i, Time.time);
                _chains[i].Step(spring, dt, _pose, bone, false);
                _lines[i].SetPositions(_chains[i].Positions);
                _lines[i].enabled = Visible && Chains.ShowPlaceholderLines;
            }
        }

        public int ChainOrder => 5;
        public int ChainCount => _chains.Length;
        public int ChainJoints => _joints;

        public void WriteChain(int chain, Span<Vector4> joints)
        {
            var thin = _legs.Trail.WidthScale(_settings.DashThinning);
            if (Visible)
                Chains.Write(joints, _chains[chain].Positions, _settings.FreeRootWidth * thin, _settings.FreeTipWidth * thin);
            else
                Chains.Collapse(joints, _chains[chain].Positions[0]);
        }

        private void BuildPose(int index, float time)
        {
            var count = _chains.Length;
            var spread = count > 1 ? Mathf.Lerp(-0.5f, 0.5f, (float)index / (count - 1)) * _settings.FreeSpread : 0f;
            var yaw = 180f + spread;
            var heading = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            var rootLocal = heading * RootRadius + Vector3.up * _settings.FreeRootHeight;
            var offset = _legs != null ? _legs.BodyOffset : Vector3.zero;
            _pose[0] = transform.TransformPoint(rootLocal) + offset;

            var bone = SpringChain.BoneLengthFor(_joints, _settings.FreeLength * _legs.Trail.LengthScale(_settings.DashStretch));
            var rotation = transform.rotation;
            var weight = _legs.Trail.Weight;
            var trail = Quaternion.AngleAxis(spread / Mathf.Max(1f, _settings.FreeSpread) * 2f * _settings.DashFan, Vector3.up)
                * Quaternion.AngleAxis(-TrailPitch, Vector3.Cross(Vector3.up, -_legs.TrailDirection)) * -_legs.TrailDirection;
            var phase = _phase[index];
            var swayTime = time * _settings.FreeSwaySpeed;
            for (var j = 1; j < _joints; j++)
            {
                var along = (float)(j - 1) / (_joints - 2);
                var wave = swayTime + phase - along * 0.8f;
                var swayYaw = (Mathf.PerlinNoise(wave, phase) * 2f - 1f) * _settings.FreeSway;
                var swayPitch = (Mathf.PerlinNoise(phase, wave) * 2f - 1f) * _settings.FreeSway;
                var pitch = _settings.FreePitch - _settings.FreeCurl * along + swayPitch;
                var direction = rotation * (Quaternion.Euler(-pitch, yaw + swayYaw, 0f) * Vector3.forward);
                if (weight > 0f)
                    direction = Vector3.Slerp(direction, trail, weight);
                _pose[j] = _pose[j - 1] + direction * bone;
            }
        }
    }
}
