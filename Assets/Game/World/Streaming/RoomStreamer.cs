using System;
using TentacleVania.Core;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Scenes;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace TentacleVania.World.Streaming
{
    public sealed class RoomStreamer : MonoBehaviour
    {
        public enum Slot
        {
            Unloaded,
            Loading,
            Loaded,
            Unloading,
        }

        [SerializeField] private RoomRegistry _registry;
        [Tooltip("Where to stream from before the creature spawns, when there is no nest respawn in this scene.")]
        [SerializeField] private Transform _spawnPoint;
        [SerializeField, Min(0.02f)] private float _interval = 0.2f;
        [SerializeField] private int _loadPriority = 50;

        private Slot[] _slots;
        private AsyncOperationHandle<SceneInstance>[] _handles;
        private Action<AsyncOperationHandle<SceneInstance>>[] _onLoaded;
        private Action<AsyncOperationHandle<SceneInstance>>[] _onUnloaded;
        private int _current = -1;
        private float _next;
        private bool _holding;

        public static RoomStreamer Active { get; private set; }
        public static string CurrentScene { get; private set; }
        public static bool Paused { get; set; }
        public static event Action<string> CurrentRoomChanged;

        public RoomRegistry Registry => _registry;
        public int CurrentIndex => _current;
        public int InFlight { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Active = null;
            CurrentScene = null;
            Paused = false;
            CurrentRoomChanged = null;
        }

        private void Awake()
        {
            var count = _registry.Rooms.Count;
            _slots = new Slot[count];
            _handles = new AsyncOperationHandle<SceneInstance>[count];
            _onLoaded = new Action<AsyncOperationHandle<SceneInstance>>[count];
            _onUnloaded = new Action<AsyncOperationHandle<SceneInstance>>[count];
            for (var i = 0; i < count; i++)
            {
                var index = i;
                _onLoaded[i] = handle => Loaded(index, handle);
                _onUnloaded[i] = _ => Unloaded(index);
            }
            Active = this;
            CurrentScene = null;
            _holding = true;
            LoadGate.Hold(this);
        }

        private void Start() => Tick();

        private void OnDestroy()
        {
            LoadGate.Release(this);
            if (Active == this)
            {
                Active = null;
                CurrentScene = null;
            }
            for (var i = 0; i < _slots.Length; i++)
                if (_slots[i] == Slot.Loaded && _handles[i].IsValid() && _handles[i].Result.Scene.isLoaded)
                    Addressables.UnloadSceneAsync(_handles[i]);
        }

        private void Update()
        {
            if (Time.unscaledTime >= _next)
            {
                _next = Time.unscaledTime + _interval;
                Tick();
            }
            if (_holding && InFlight == 0 && (_current < 0 || _slots[_current] == Slot.Loaded))
            {
                _holding = false;
                LoadGate.Release(this);
            }
        }

        public Slot State(int index) => _slots[index];

        public void LoadAll()
        {
            for (var i = 0; i < _slots.Length; i++)
                Load(i);
        }

        public void UnloadDistant()
        {
            var focus = Focus();
            var rooms = _registry.Rooms;
            for (var i = 0; i < _slots.Length; i++)
                if (!RoomStreamingRules.Wanted(focus, rooms, i, _current, _registry.LoadRadius))
                    Unload(i);
        }

        private void Tick()
        {
            var focus = Focus();
            var rooms = _registry.Rooms;
            var current = RoomStreamingRules.CurrentRoom(focus, rooms, _current);
            if (current != _current)
            {
                _current = current;
                CurrentScene = current >= 0 ? rooms[current].Scene : null;
                if (CurrentScene != null)
                    MapProgress.Explore(CurrentScene);
                CurrentRoomChanged?.Invoke(CurrentScene);
            }
            if (Paused)
                return;
            for (var i = 0; i < rooms.Count; i++)
            {
                if (_slots[i] == Slot.Unloaded && RoomStreamingRules.Wanted(focus, rooms, i, current, _registry.LoadRadius))
                    Load(i);
                else if (_slots[i] == Slot.Loaded && RoomStreamingRules.Unwanted(focus, rooms, i, current, _registry.UnloadRadius))
                    Unload(i);
            }
        }

        private Vector3 Focus()
        {
            var target = CameraFocus.Target;
            if (target != null)
                return target.position;
            if (Nests.TryGetRespawn(SceneManager.GetActiveScene().name, out var nest, out _))
                return nest;
            return _spawnPoint != null ? _spawnPoint.position : transform.position;
        }

        private void Load(int index)
        {
            if (_slots[index] != Slot.Unloaded)
                return;
            _slots[index] = Slot.Loading;
            InFlight++;
            _handles[index] = Addressables.LoadSceneAsync(_registry.Rooms[index].Scene, LoadSceneMode.Additive, true, _loadPriority);
            _handles[index].Completed += _onLoaded[index];
        }

        private void Unload(int index)
        {
            if (_slots[index] != Slot.Loaded)
                return;
            _slots[index] = Slot.Unloading;
            InFlight++;
            Addressables.UnloadSceneAsync(_handles[index]).Completed += _onUnloaded[index];
        }

        private void Loaded(int index, AsyncOperationHandle<SceneInstance> handle)
        {
            InFlight--;
            if (this == null)
                return;
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"Room {_registry.Rooms[index].Scene} failed to load: {handle.OperationException}");
                _slots[index] = Slot.Unloaded;
                return;
            }
            _slots[index] = Slot.Loaded;
        }

        private void Unloaded(int index)
        {
            InFlight--;
            if (this != null)
                _slots[index] = Slot.Unloaded;
        }
    }
}
