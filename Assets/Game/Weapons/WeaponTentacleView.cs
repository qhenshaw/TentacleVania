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

        [SerializeField] private Material _material;
        [SerializeField, Min(0.1f)] private float _length = 2.4f;
        [SerializeField, Min(0f)] private float _archHeight = 0.25f;
        [SerializeField] private Vector3 _rootOffset = new(0.18f, 0.5f, 0.15f);
        [SerializeField, Min(0.001f)] private float _rootWidth = 0.14f;
        [SerializeField, Min(0.001f)] private float _tipWidth = 0.03f;

        private readonly LineRenderer[] _lines = new LineRenderer[WeaponController.Slots];
        private readonly Vector3[][] _joints = new Vector3[WeaponController.Slots][];

        private WeaponController _controller;

        private void Awake()
        {
            _controller = GetComponent<WeaponController>();
            var width = new AnimationCurve(new Keyframe(0f, _rootWidth), new Keyframe(1f, _tipWidth));
            for (var slot = 0; slot < WeaponController.Slots; slot++)
            {
                _joints[slot] = new Vector3[Joints];
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
            for (var slot = 0; slot < WeaponController.Slots; slot++)
            {
                var line = _lines[slot];
                var active = IsActive(slot);
                line.enabled = active && Chains.ShowPlaceholderLines;
                if (!active)
                    continue;
                var root = Root(slot);
                var move = _controller.ActiveMove;
                var origin = transform.position + Vector3.up * move.Height;
                var tip = MoveShapes.Tip(origin, _controller.ActiveForward, move, _controller.ActiveElapsed);
                var joints = _joints[slot];
                var bone = Mathf.Max(Vector3.Distance(root, tip), 0.2f) * 1.08f / (Joints - 1);
                bone = Mathf.Min(bone, _length / (Joints - 1));
                ChainSolver.SeedArch(joints, root, tip, Vector3.up, _archHeight);
                ChainSolver.Solve(joints, bone, root, tip, Vector3.up, 4);
                line.SetPositions(joints);
            }
        }

        public int ChainOrder => 10;
        public int ChainCount => WeaponController.Slots;

        public void WriteChain(int chain, Span<Vector4> joints)
        {
            if (IsActive(chain))
                Chains.Write(joints, _joints[chain], _rootWidth, _tipWidth);
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
