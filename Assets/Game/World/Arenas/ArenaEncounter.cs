using System;
using System.Collections.Generic;
using TentacleVania.Combat;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Saves;
using TentacleVania.Enemies;
using UnityEngine;
using UnityEngine.AI;

namespace TentacleVania.World.Arenas
{
    public enum ArenaState
    {
        Waiting,
        Spawning,
        Fighting,
        BetweenWaves,
        Cleared,
    }

    public sealed class ArenaEncounter : MonoBehaviour
    {
        [SerializeField] private ArenaWaveSet _waves;
        [SerializeField] private ArenaDoor[] _doors;
        [SerializeField] private Transform[] _spawnPoints;
        [Tooltip("Spawn points for enemies whose definition spawns on walls (turrets).")]
        [SerializeField] private Transform[] _wallMounts = System.Array.Empty<Transform>();
        [SerializeField] private Vector3 _triggerCenter;
        [SerializeField] private Vector3 _triggerSize = new(18f, 4f, 16f);
        [Tooltip("Boss arenas stay empty and open for good once cleared (saved); other arenas refill when the creature rests.")]
        [SerializeField] private bool _bossArena;
        [Tooltip("Clears once and never refills on rest, like a boss arena (the opening pod chamber).")]
        [SerializeField] private bool _oneTime;
        [Tooltip("Unique save id; when empty, the scene name and object name are used.")]
        [SerializeField] private string _id;

        private readonly List<Enemy> _alive = new(16);
        private readonly List<ArenaWaveSet.Entry> _queue = new(16);

        private int _waveIndex = -1;
        private float _nextAction;
        private int _spawnCursor;
        private int _wallCursor;

        public event Action<ArenaEncounter> StateChanged;
        public static event Action<ArenaEncounter> AnyCleared;

        public bool HadHumans { get; private set; }
        public bool Held { get; set; }

        public static ArenaEncounter Active { get; private set; }

        public ArenaState State { get; private set; }
        public int WaveNumber => _waveIndex + 1;
        public int WaveCount => _waves.Waves.Count;
        public int AliveCount => _alive.Count;
        public float StartedAt { get; private set; }
        public float ClearedAt { get; private set; }
        public bool IsBossArena => _bossArena;
        public string SaveId => string.IsNullOrEmpty(_id) ? gameObject.scene.name + "/" + name : _id;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Active = null;
            AnyCleared = null;
        }

        private void Awake()
        {
            if (_doors != null && _doors.Length > 0)
                return;
            var found = new List<ArenaDoor>();
            foreach (var root in gameObject.scene.GetRootGameObjects())
                found.AddRange(root.GetComponentsInChildren<ArenaDoor>(true));
            _doors = found.ToArray();
        }

        private void OnEnable()
        {
            if (Active == null)
                Active = this;
            Nests.Rested += OnRested;
        }

        private void OnDisable()
        {
            Nests.Rested -= OnRested;
            if (Active == this)
                Active = null;
        }

        private void Start()
        {
            var saved = Nests.StaysCleared(_bossArena || _oneTime) && SaveSystem.Data.World.Has(WorldFlag.ClearedArena, SaveId);
            if (!saved && !ArenaSession.IsCleared(SaveId))
                return;
            foreach (var door in _doors)
                door.Open();
            SetState(ArenaState.Cleared);
        }

        public void ResetForRest()
        {
            if (Nests.StaysCleared(_bossArena || _oneTime) && State == ArenaState.Cleared)
                return;
            foreach (var enemy in _alive)
                if (enemy != null)
                    Destroy(enemy.gameObject);
            _alive.Clear();
            _queue.Clear();
            _waveIndex = -1;
            foreach (var door in _doors)
                door.ResetToStart();
            SetState(ArenaState.Waiting);
        }

        private void OnRested(string nest)
        {
            if (State != ArenaState.Waiting)
                ResetForRest();
        }

        private void Update()
        {
            var now = Time.time;
            switch (State)
            {
                case ArenaState.Waiting:
                    if (!Held && CreatureInside())
                        Begin(now);
                    break;
                case ArenaState.Spawning:
                    TickSpawning(now);
                    break;
                case ArenaState.Fighting:
                    PruneDead();
                    if (_alive.Count == 0)
                        WaveDefeated(now);
                    break;
                case ArenaState.BetweenWaves:
                    if (now >= _nextAction)
                        StartWave(_waveIndex + 1, now);
                    break;
            }
        }

        public void SetWaves(ArenaWaveSet waves)
        {
            if (waves != null)
                _waves = waves;
        }

        public void Begin(float now) => BeginAt(0, now);

        public void BeginAt(int wave, float now)
        {
            StartedAt = now;
            HadHumans = false;
            foreach (var door in _doors)
                door.Seal();
            var progress = SaveSystem.Data.Progress;
            progress.ArenaInProgress = SaveId;
            progress.ArenaScene = gameObject.scene.name;
            progress.ArenaPosition = transform.position;
            Autosave.Request(AutosaveReason.ArenaStarted);
            StartWave(Mathf.Clamp(wave, 0, _waves.Waves.Count - 1), now);
        }

        public void Adopt(IReadOnlyList<Enemy> enemies, float now)
        {
            StartedAt = now;
            HadHumans = false;
            foreach (var door in _doors)
                door.Seal();
            _waveIndex = _waves.Waves.Count - 1;
            _queue.Clear();
            _alive.Clear();
            foreach (var enemy in enemies)
            {
                if (enemy == null)
                    continue;
                _alive.Add(enemy);
                if (enemy.Definition != null && !enemy.Definition.Machine)
                    HadHumans = true;
            }
            SetState(ArenaState.Fighting);
        }

        public void MarkCleared()
        {
            _queue.Clear();
            foreach (var door in _doors)
                door.Open();
            SetState(ArenaState.Cleared);
        }

        public void SkipWave()
        {
            foreach (var enemy in _alive)
                if (enemy != null)
                    enemy.Health.Kill();
            _queue.Clear();
            if (State is ArenaState.Spawning)
                SetState(ArenaState.Fighting);
        }

        private bool CreatureInside()
        {
            var creature = CombatTargets.Creature;
            if (creature == null)
                return false;
            var local = transform.InverseTransformPoint(creature.transform.position) - _triggerCenter;
            var half = _triggerSize * 0.5f;
            return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
        }

        private void StartWave(int index, float now)
        {
            _waveIndex = index;
            _queue.Clear();
            foreach (var entry in _waves.Waves[index].Enemies)
                for (var i = 0; i < entry.Count; i++)
                    _queue.Add(entry);
            Shuffle(_queue);
            _nextAction = now;
            SetState(ArenaState.Spawning);
        }

        private void TickSpawning(float now)
        {
            PruneDead();
            if (now < _nextAction)
                return;
            if (_queue.Count == 0)
            {
                SetState(ArenaState.Fighting);
                return;
            }
            var next = _queue[^1];
            _queue.RemoveAt(_queue.Count - 1);
            Spawn(next.Prefab, next.Elite, next.AnyHealth);
            _nextAction = now + _waves.SpawnInterval;
        }

        private void Spawn(Enemy prefab, bool elite, bool anyHealth)
        {
            var onWall = prefab.Definition != null && prefab.Definition.SpawnOnWall && _wallMounts.Length > 0;
            var point = onWall ? _wallMounts[_wallCursor++ % _wallMounts.Length] : _spawnPoints[_spawnCursor++ % _spawnPoints.Length];
            var position = point.position;
            if (NavMesh.SamplePosition(position, out var hit, 2f, NavMesh.AllAreas))
                position = hit.position;
            if (prefab.Definition != null && !prefab.Definition.Machine)
                HadHumans = true;
            var enemy = Instantiate(prefab, position, point.rotation);
            if (elite)
                enemy.MakeElite();
            if (anyHealth && enemy.TryGetComponent(out Consumable consumable))
                consumable.AnyHealth = true;
            _alive.Add(enemy);
        }

        private void WaveDefeated(float now)
        {
            if (_waveIndex + 1 >= _waves.Waves.Count)
            {
                ClearedAt = now;
                if (Nests.StaysCleared(_bossArena || _oneTime))
                    SaveSystem.Data.World.Set(WorldFlag.ClearedArena, SaveId);
                ArenaSession.MarkCleared(SaveId);
                if (SaveSystem.Data.Progress.ArenaInProgress == SaveId)
                    SaveSystem.Data.Progress.ArenaInProgress = "";
                foreach (var door in _doors)
                    door.Open();
                SetState(ArenaState.Cleared);
                AnyCleared?.Invoke(this);
                Autosave.Request(AutosaveReason.RoomCleared);
                return;
            }
            _nextAction = now + _waves.DelayBetweenWaves;
            SetState(ArenaState.BetweenWaves);
        }

        private void PruneDead()
        {
            for (var i = _alive.Count - 1; i >= 0; i--)
            {
                var enemy = _alive[i];
                if (enemy == null || enemy.State == EnemyState.Dead || !enemy.Health.IsAlive)
                    _alive.RemoveAt(i);
            }
        }

        private void SetState(ArenaState state)
        {
            State = state;
            if (state is ArenaState.Spawning or ArenaState.Fighting or ArenaState.BetweenWaves)
                Active = this;
            StateChanged?.Invoke(this);
        }

        private static void Shuffle<T>(List<T> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.6f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(_triggerCenter, _triggerSize);
        }
    }
}
