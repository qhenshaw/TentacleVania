using System.Collections.Generic;
using TentacleVania.Weapons.Combo;
using TentacleVania.Weapons.Data;
using UnityEngine;
using UnityEngine.VFX;

namespace TentacleVania.Weapons
{
    [RequireComponent(typeof(WeaponController))]
    public sealed class WeaponVfx : MonoBehaviour
    {
        private const int PoolSize = 3;

        private static readonly int StartAngleId = Shader.PropertyToID("StartAngle");
        private static readonly int EndAngleId = Shader.PropertyToID("EndAngle");
        private static readonly int ReachId = Shader.PropertyToID("Reach");
        private static readonly int RadiusId = Shader.PropertyToID("Radius");
        private static readonly int DurationId = Shader.PropertyToID("Duration");

        private readonly Dictionary<VisualEffect, VisualEffect[]> _pools = new();
        private readonly Dictionary<VisualEffect, int> _cursors = new();

        private WeaponController _controller;
        private Transform _root;
        private bool _played;
        private VisualEffect _following;
        private WeaponMove _followMove;

        public static bool Enabled { get; set; } = true;
        public int Played { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Enabled = true;

        private void Awake() => _controller = GetComponent<WeaponController>();

        private void OnEnable()
        {
            _controller.AttackStarted += OnAttackStarted;
            _controller.Equipped += OnEquipped;
        }

        private void OnDisable()
        {
            _controller.AttackStarted -= OnAttackStarted;
            _controller.Equipped -= OnEquipped;
        }

        private void OnEquipped(int slot, WeaponDefinition weapon) => Prewarm(weapon);

        private void OnDestroy()
        {
            if (_root != null)
                Destroy(_root.gameObject);
        }

        private void Start()
        {
            for (var slot = 0; slot < WeaponController.Slots; slot++)
                Prewarm(_controller.Weapon(slot));
        }

        public void Prewarm(WeaponDefinition weapon)
        {
            if (weapon == null)
                return;
            for (var i = 0; i < 3; i++)
                Pool(weapon.Step(i).Vfx);
            Pool(weapon.Heavy.Vfx);
            Pool(weapon.DashAttack.Vfx);
        }

        private void OnAttackStarted(WeaponController controller)
        {
            _played = false;
            _following = null;
        }

        private void LateUpdate()
        {
            if (!_controller.IsAttacking)
            {
                _following = null;
                return;
            }
            var move = _controller.ActiveMove;
            if (_following != null && _followMove == move)
                _following.transform.position = transform.position + Vector3.up * move.Height;
            if (_played || !Enabled || move.Vfx == null || _controller.Phase != MovePhase.Strike)
                return;
            _played = true;
            Play(move, _controller.ActiveForward);
        }

        private void Play(WeaponMove move, Vector3 forward)
        {
            var effect = Next(move.Vfx);
            if (effect == null)
                return;
            var origin = transform.position;
            var rotation = Quaternion.LookRotation(forward.sqrMagnitude > 1e-4f ? forward : transform.forward);
            Vector3 position;
            float start, end, reach;
            switch (move.Shape)
            {
                case MoveShape.Area:
                    position = MoveShapes.AreaCenter(origin, forward, move);
                    start = -move.SectorAngle * 0.5f;
                    end = move.SectorAngle * 0.5f;
                    reach = move.Radius;
                    break;
                case MoveShape.Body:
                    position = origin + Vector3.up * move.Height;
                    start = 0f;
                    end = 360f;
                    reach = move.Radius;
                    _following = effect;
                    _followMove = move;
                    break;
                default:
                    position = origin + Vector3.up * move.Height;
                    start = move.StartAngle;
                    end = move.EndAngle;
                    reach = move.Reach;
                    break;
            }
            effect.transform.SetPositionAndRotation(position, rotation);
            effect.transform.localScale = Vector3.one * move.VfxScale;
            SetFloat(effect, StartAngleId, start);
            SetFloat(effect, EndAngleId, end);
            SetFloat(effect, ReachId, reach);
            SetFloat(effect, RadiusId, move.Shape == MoveShape.Sweep ? move.Radius : reach);
            SetFloat(effect, DurationId, move.Strike);
            effect.Reinit();
            effect.Play();
            Played++;
        }

        private static void SetFloat(VisualEffect effect, int id, float value)
        {
            if (effect.HasFloat(id))
                effect.SetFloat(id, value);
        }

        private VisualEffect Next(VisualEffect prefab)
        {
            var pool = Pool(prefab);
            if (pool == null)
                return null;
            var cursor = _cursors[prefab];
            _cursors[prefab] = (cursor + 1) % pool.Length;
            return pool[cursor];
        }

        private VisualEffect[] Pool(VisualEffect prefab)
        {
            if (prefab == null)
                return null;
            if (_pools.TryGetValue(prefab, out var pool))
                return pool;
            if (_root == null)
                _root = new GameObject("WeaponVfx").transform;
            pool = new VisualEffect[PoolSize];
            for (var i = 0; i < PoolSize; i++)
            {
                pool[i] = Instantiate(prefab, _root);
                pool[i].name = $"{prefab.name} {i + 1}";
            }
            _pools[prefab] = pool;
            _cursors[prefab] = 0;
            return pool;
        }
    }
}
