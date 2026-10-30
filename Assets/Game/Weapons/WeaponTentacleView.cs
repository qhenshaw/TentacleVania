using System;
using System.Collections.Generic;
using TentacleVania.Core.Characters;
using TentacleVania.Creature.Rig;
using TentacleVania.Weapons.Combo;
using TentacleVania.Weapons.Data;
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
        private const float MinTipReach = 0.15f;
        private const float RollSharpness = 20f;
        private const float RollMinSpeed = 0.5f;

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
        private readonly List<TipMesh>[] _tipMeshes = { new(2), new(2) };
        private readonly TipMesh[] _activeMesh = new TipMesh[WeaponController.Slots];
        private readonly float[] _grow = new float[WeaponController.Slots];
        private readonly Vector3[] _lastTip = new Vector3[WeaponController.Slots];
        private readonly Vector3[] _edge = new Vector3[WeaponController.Slots];

        private WeaponController _controller;
        private CharacterMaterials _materials;

        private sealed class TipMesh
        {
            public WeaponDefinition Weapon;
            public Transform Transform;
            public GameObject GameObject;
            public float Length;
        }

        public static bool ShowTipMeshes { get; set; } = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => ShowTipMeshes = true;

        public bool IsShown(int slot) => _shown[slot];
        public Vector3 TipPosition(int slot) => _chains[slot].Tip;

        private void Awake()
        {
            _controller = GetComponent<WeaponController>();
            _materials = GetComponent<CharacterMaterials>();
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

        private void Start()
        {
            for (var slot = 0; slot < WeaponController.Slots; slot++)
                TipMeshFor(slot, _controller.Weapon(slot));
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
                    {
                        chain.Collapse(root);
                        _activeMesh[slot] = TipMeshFor(slot, _controller.Weapon(slot));
                    }
                    _shown[slot] = true;
                    _retractStarted[slot] = float.NegativeInfinity;
                    tip = ActiveTip(root, out var grow);
                    _grow[slot] = grow;
                    tip = PullIn(root, tip, _activeMesh[slot], grow);
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
                        HideTipMesh(slot);
                        continue;
                    }
                    tip = Vector3.Lerp(_retractFrom[slot], root, t * t);
                    _grow[slot] = 1f - t * t;
                }
                else
                {
                    _lines[slot].enabled = false;
                    HideTipMesh(slot);
                    continue;
                }

                var bone = Mathf.Clamp(Vector3.Distance(root, tip) * Slack / (Joints - 1), MinBone, _length / (Joints - 1));
                ChainSolver.SeedArch(_pose, root, tip, Vector3.up, _archHeight);
                chain.Step(_spring, dt, _pose, bone, true);
                _lines[slot].SetPositions(chain.Positions);
                _lines[slot].enabled = Chains.ShowPlaceholderLines;
                PlaceTipMesh(slot, chain.Positions, dt);
            }
        }

        private Vector3 ActiveTip(Vector3 root, out float grow)
        {
            var move = _controller.ActiveMove;
            var elapsed = _controller.ActiveElapsed;
            var origin = transform.position + Vector3.up * move.Height;
            var tip = MoveShapes.Tip(origin, _controller.ActiveForward, move, elapsed);
            grow = 1f;
            if (_controller.Phase != MovePhase.WindUp)
                return tip;
            grow = Mathf.Clamp01(elapsed / Mathf.Min(_emergeTime, move.WindUp));
            grow = grow * grow * (3f - 2f * grow);
            return Vector3.Lerp(root, tip, grow);
        }

        private static Vector3 PullIn(Vector3 root, Vector3 tip, TipMesh mesh, float grow)
        {
            if (mesh == null)
                return tip;
            var span = tip - root;
            var distance = span.magnitude;
            if (distance < 1e-4f)
                return tip;
            var reach = (mesh.Length - mesh.Weapon.TipOverlap) * mesh.Weapon.TipScale * grow;
            var kept = Mathf.Max(distance * MinTipReach, distance - reach);
            return root + span * (kept / distance);
        }

        private TipMesh TipMeshFor(int slot, WeaponDefinition weapon)
        {
            if (weapon == null || weapon.TipMesh == null)
                return null;
            var cache = _tipMeshes[slot];
            foreach (var cached in cache)
                if (cached.Weapon == weapon)
                    return cached;
            var instance = Instantiate(weapon.TipMesh, transform);
            instance.name = $"{weapon.TipMesh.name} {slot + 1}";
            instance.SetActive(false);
            var filter = instance.GetComponentInChildren<MeshFilter>();
            var mesh = new TipMesh
            {
                Weapon = weapon,
                Transform = instance.transform,
                GameObject = instance,
                Length = filter != null && filter.sharedMesh != null ? filter.sharedMesh.bounds.max.y : 0f,
            };
            if (_materials != null)
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                    _materials.Register(renderer);
            cache.Add(mesh);
            return mesh;
        }

        private void PlaceTipMesh(int slot, ReadOnlySpan<Vector3> positions, float dt)
        {
            var mesh = _activeMesh[slot];
            var grow = _grow[slot];
            if (mesh == null || !ShowTipMeshes || grow <= 0f)
            {
                HideTipMesh(slot);
                return;
            }
            var tip = positions[positions.Length - 1];
            var forward = tip - positions[positions.Length - 2];
            if (forward.sqrMagnitude < 1e-8f)
                forward = tip - positions[0];
            if (forward.sqrMagnitude < 1e-8f)
                forward = transform.forward;
            forward.Normalize();

            var target = Vector3.Cross(Vector3.up, forward);
            if (dt > 0f && mesh.GameObject.activeSelf)
            {
                var velocity = (tip - _lastTip[slot]) / dt;
                var lateral = velocity - forward * Vector3.Dot(velocity, forward);
                if (lateral.sqrMagnitude > RollMinSpeed * RollMinSpeed)
                    target = lateral;
            }
            _lastTip[slot] = tip;
            target -= forward * Vector3.Dot(target, forward);
            if (target.sqrMagnitude < 1e-8f)
                target = Vector3.Cross(forward, Vector3.right);
            target.Normalize();

            var edge = _edge[slot] - forward * Vector3.Dot(_edge[slot], forward);
            if (!mesh.GameObject.activeSelf || edge.sqrMagnitude < 1e-6f)
                edge = target;
            else if (Vector3.Dot(edge, target) < 0f)
                target = -target;
            edge = Vector3.Slerp(edge.normalized, target, 1f - Mathf.Exp(-RollSharpness * dt)).normalized;
            _edge[slot] = edge;

            var scale = mesh.Weapon.TipScale * grow;
            var rotation = Quaternion.LookRotation(Vector3.Cross(edge, forward), forward);
            mesh.Transform.SetPositionAndRotation(tip - forward * (mesh.Weapon.TipOverlap * scale), rotation);
            mesh.Transform.localScale = Vector3.one * scale;
            if (!mesh.GameObject.activeSelf)
                mesh.GameObject.SetActive(true);
        }

        private void HideTipMesh(int slot)
        {
            _grow[slot] = 0f;
            var mesh = _activeMesh[slot];
            if (mesh != null && mesh.GameObject.activeSelf)
                mesh.GameObject.SetActive(false);
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
