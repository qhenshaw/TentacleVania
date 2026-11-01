using System.Collections.Generic;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Enemies;
using TentacleVania.Enemies.AI;
using TentacleVania.Enemies.Traps;
using UnityEngine;

namespace TentacleVania.Bosses.CryoLoader
{
    public sealed class CryoLoaderBoss : BossEnemy
    {
        private static readonly Vector3[] MazeSlots =
        {
            new(-5f, 0f, 4f), new(5f, 0f, 4f), new(-5f, 0f, -4f), new(5f, 0f, -4f),
            new(0f, 0f, 7f), new(0f, 0f, -7f), new(-8f, 0f, 0f), new(8f, 0f, 0f),
        };

        [Header("Ice maze (design: smashes ice walls, reshaping the arena)")]
        [SerializeField] private IceWall _wall;
        [Tooltip("Walls kept standing; a smashed one is re-raised in a free slot every interval.")]
        [SerializeField, Min(0)] private int _mazeWalls = 5;
        [SerializeField, Min(1f)] private float _mazeInterval = 12f;
        [SerializeField, Min(1f)] private float _wallWidth = 5f;
        [SerializeField, Min(1f)] private float _creatureClearance = 3f;

        [Header("Ram Charge")]
        [SerializeField] private string _chargeAttack = "Ram Charge";
        [SerializeField, Min(0f)] private float _chargeSpeed = 15f;
        [SerializeField, Min(0.1f)] private float _smashRadius = 2.2f;

        [Header("Icefall (phase 2)")]
        [SerializeField] private AcidBurst _icefall;
        [SerializeField, Min(0)] private int _icefallPhase = 1;
        [SerializeField, Min(1f)] private float _icefallInterval = 7f;
        [SerializeField, Min(1)] private int _icefallChunks = 3;
        [SerializeField, Min(0.1f)] private float _icefallRadius = 1.6f;
        [SerializeField, Min(0.4f)] private float _icefallFuse = 1.1f;
        [SerializeField, Min(0f)] private float _icefallDamage = 16f;
        [SerializeField, Min(0f)] private float _icefallSpread = 6f;

        private readonly IceWall[] _slots = new IceWall[MazeSlots.Length];
        private readonly int[] _generations = new int[MazeSlots.Length];
        private Vector3 _home;
        private Quaternion _homeRotation;
        private float _nextMaze;
        private float _nextIcefall;
        private int _smashed;

        public int Smashed => _smashed;
        public int Standing
        {
            get
            {
                var count = 0;
                for (var i = 0; i < _slots.Length; i++)
                    if (Owns(i))
                        count++;
                return count;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            if (_icefall != null)
                EnemyManager.PrewarmBursts(_icefall, _icefallChunks);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _home = Position;
            _homeRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            _smashed = 0;
            _nextIcefall = Time.time + _icefallInterval;
            for (var i = 0; i < _slots.Length; i++)
                _slots[i] = null;
            for (var i = 0; i < _mazeWalls; i++)
                Regrow();
            _nextMaze = Time.time + _mazeInterval;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ClearMaze();
        }

        protected override void OnDeath()
        {
            base.OnDeath();
            ClearMaze();
        }

        private bool Owns(int slot)
        {
            var wall = _slots[slot];
            return wall != null && wall.isActiveAndEnabled && wall.Generation == _generations[slot] && wall.State != IceWall.WallState.Falling;
        }

        private void ClearMaze()
        {
            for (var i = 0; i < _slots.Length; i++)
            {
                if (Owns(i))
                    _slots[i].Shatter();
                _slots[i] = null;
            }
        }

        private void RaiseSlot(int slot)
        {
            if (_wall == null)
                return;
            var offset = _homeRotation * MazeSlots[slot];
            var along = Mathf.Abs(MazeSlots[slot].x) > Mathf.Abs(MazeSlots[slot].z) ? _homeRotation * Vector3.forward : _homeRotation * Vector3.right;
            _slots[slot] = EnemyManager.RaiseWall(_wall, _home + offset, along, _wallWidth, float.MaxValue);
            _generations[slot] = _slots[slot].Generation;
        }

        protected override void OnTick(float now, float dt)
        {
            base.OnTick(now, dt);
            if (!Health.IsAlive || State == EnemyState.Dead || State == EnemyState.Consumed || IsFinishing)
                return;
            if (State == EnemyState.Attacking && CurrentAttack != null && CurrentAttack.Name == _chargeAttack && CurrentPhase == AttackPhase.Active)
                Charge(dt);
            if (State == EnemyState.Spawning)
                return;
            if (now >= _nextMaze)
            {
                _nextMaze = now + _mazeInterval;
                Regrow();
            }
            if (Phase >= _icefallPhase && now >= _nextIcefall)
            {
                _nextIcefall = now + _icefallInterval;
                Icefall();
            }
        }

        private void Charge(float dt)
        {
            var ahead = Position + transform.forward * (_smashRadius * 0.5f);
            for (var i = 0; i < _slots.Length; i++)
            {
                if (!Owns(i) || !_slots[i].Touches(ahead, _smashRadius))
                    continue;
                _slots[i].Shatter();
                _smashed++;
            }
            if (Agent.enabled && Agent.isOnNavMesh)
                Agent.Move(transform.forward * (_chargeSpeed * dt));
        }

        private void Regrow()
        {
            if (Standing >= _mazeWalls)
                return;
            var creature = CombatTargets.Creature;
            var start = Standing == 0 ? 0 : Random.Range(0, MazeSlots.Length);
            for (var i = 0; i < MazeSlots.Length; i++)
            {
                var slot = (start + i) % MazeSlots.Length;
                if (Owns(slot))
                    continue;
                var along = Mathf.Abs(MazeSlots[slot].x) > Mathf.Abs(MazeSlots[slot].z) ? _homeRotation * Vector3.forward : _homeRotation * Vector3.right;
                var center = _home + _homeRotation * MazeSlots[slot];
                if (creature != null && IceWall.Distance(center, along, _wallWidth, 1f, creature.transform.position) < _creatureClearance)
                    continue;
                if (IceWall.Distance(center, along, _wallWidth, 1f, Position) < _creatureClearance)
                    continue;
                RaiseSlot(slot);
                return;
            }
        }

        private void Icefall()
        {
            var creature = CombatTargets.Creature;
            if (_icefall == null || creature == null || !creature.IsAlive)
                return;
            var info = DamageInfo.Create(_icefallDamage, Team.Site, DamageType.Physical);
            info.Attacker = Health;
            var target = creature.transform.position;
            for (var i = 0; i < _icefallChunks; i++)
            {
                var spot = i == 0 ? target : target + Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(_icefallRadius * 2f, _icefallSpread);
                EnemyManager.PlaceBurst(_icefall, spot, _icefallRadius, _icefallFuse, info);
            }
        }
    }
}
