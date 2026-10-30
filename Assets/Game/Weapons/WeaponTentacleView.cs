using System;
using TentacleVania.Core.Characters;
using TentacleVania.Creature.Rig;
using TentacleVania.Weapons.Combo;
using UnityEngine;
using UnityEngine.Rendering;

namespace TentacleVania.Weapons
{
    [DefaultExecutionOrder(110)]
    [RequireComponent(typeof(WeaponController))]
    public sealed class WeaponTentacleView : MonoBehaviour, IChainSource
    {
        private const int Joints = Chains.Joints;
        private const float MinBone = 0.02f;
        private const float Slack = 1.08f;

        [SerializeField] private Material _material;
        [SerializeField, Min(0.1f)] private float _length = 2.4f;
        [SerializeField, Min(0f)] private float _archHeight = 0.25f;
        [SerializeField] private Vector3 _rootOffset = new(0.18f, 0.5f, 0.15f);
        [SerializeField, Min(0.001f)] private float _rootWidth = 0.14f;
        [SerializeField, Min(0.001f)] private float _tipWidth = 0.03f;
        [Tooltip("Seconds of the wind-up the tentacle takes to grow out of the body.")]
        [SerializeField, Min(0.01f)] private float _emergeTime = 0.08f;
        [Tooltip("Seconds it takes to pull back into the body after the move ends.")]
        [SerializeField, Min(0.01f)] private float _retractTime = 0.12f;
        [SerializeField] private SpringChainSettings _spring = new()
        {
            PoseStiffness = 30f,
            TipLooseness = 0.6f,
            Damping = 0.15f,
            Gravity = 2f,
            Iterations = 3,
        };

        private readonly LineRenderer[] _lines = new LineRenderer[WeaponController.Slots];
        private readonly SpringChain[] _chains = new SpringChain[WeaponController.Slots];
        private readonly bool[] _shown = new bool[WeaponController.Slots];
        private readonly float[] _retractStarted = new float[WeaponController.Slots];
        private readonly Vector3[] _retractFrom = new Vector3[WeaponController.Slots];
        private readonly Vector3[] _pose = new Vector3[Joints];

        private WeaponController _controller;

        public bool IsShown(int slot) => _shown[slot];
        public Vector3 TipPosition(int slot) => _chains[slot].Tip;

        private void Awake()
        {
            _controller = GetComponent<WeaponController>();
            var width = new AnimationCurve(new Keyframe(0f, _rootWidth), new Keyframe(1f, _tipWidth));
            for (var slot = 0; slot < WeaponController.Slots; slot++)
            {
                _chains[slot] = new SpringChain(Joints);
                _chains[slot].Collapse(Root(slot));
                _retractStarted[slot] = float.NegativeInfinity;
                var line = new GameObject($"WeaponTentacle {slot + 1}").AddComponent<LineRenderer>();
                line.transform.SetParent(transform, false);
                line.useWorldSpace = true;
                line.positionCount = Joints;
                line.widthCurve = width;
                line.numCornerVertices = 3;
                line.numCapVertices = 3;
                line.sharedMaterial = _material;
                line.shadowCastingMode = ShadowCastingMode.On;
                line.generateLightingData = true;
                line.enabled = false;
                _lines[slot] = line;
                if (TryGetComponent<CharacterMaterials>(out var materials))
                    materials.Register(line);
            }
        }

        private void LateUpdate()
        {
            var dt = Time.deltaTime;
            var now = Time.time;
            for (var slot = 0; slot < WeaponController.Slots; slot++)
            {
                var root = Root(slot);
                var chain = _chains[slot];
                Vector3 tip;
                if (IsActive(slot))
                {
                    if (!_shown[slot])
                        chain.Collapse(root);
                    _shown[slot] = true;
                    _retractStarted[slot] = float.NegativeInfinity;
                    tip = ActiveTip(root);
                }
                else if (_shown[slot])
                {
                    if (float.IsNegativeInfinity(_retractStarted[slot]))
                    {
                        _retractStarted[slot] = now;
                        _retractFrom[slot] = chain.Tip;
                    }
                    var t = (now - _retractStarted[slot]) / _retractTime;
                    if (t >= 1f)
                    {
                        _shown[slot] = false;
                        chain.Collapse(root);
                        _lines[slot].enabled = false;
                        continue;
                    }
                    tip = Vector3.Lerp(_retractFrom[slot], root, t * t);
                }
                else
                {
                    _lines[slot].enabled = false;
                    continue;
                }

                var bone = Mathf.Clamp(Vector3.Distance(root, tip) * Slack / (Joints - 1), MinBone, _length / (Joints - 1));
                ChainSolver.SeedArch(_pose, root, tip, Vector3.up, _archHeight);
                chain.Step(_spring, dt, _pose, bone, true);
                _lines[slot].SetPositions(chain.Positions);
                _lines[slot].enabled = Chains.ShowPlaceholderLines;
            }
        }

        private Vector3 ActiveTip(Vector3 root)
        {
            var move = _controller.ActiveMove;
            var elapsed = _controller.ActiveElapsed;
            var origin = transform.position + Vector3.up * move.Height;
            var tip = MoveShapes.Tip(origin, _controller.ActiveForward, move, elapsed);
            if (_controller.Phase != MovePhase.WindUp)
                return tip;
            var grow = Mathf.Clamp01(elapsed / Mathf.Min(_emergeTime, move.WindUp));
            grow = grow * grow * (3f - 2f * grow);
            return Vector3.Lerp(root, tip, grow);
        }

        public int ChainOrder => 10;
        public int ChainCount => WeaponController.Slots;

        public void WriteChain(int chain, Span<Vector4> joints)
        {
            if (_shown[chain])
                Chains.Write(joints, _chains[chain].Positions, _rootWidth, _tipWidth);
            else
                Chains.Collapse(joints, Root(chain));
        }

        private bool IsActive(int slot) => _controller.IsAttacking && _controller.ActiveSlot == slot;

        private Vector3 Root(int slot)
        {
            var side = slot == 0 ? -1f : 1f;
            return transform.TransformPoint(new Vector3(_rootOffset.x * side, _rootOffset.y, _rootOffset.z));
        }
    }
}
