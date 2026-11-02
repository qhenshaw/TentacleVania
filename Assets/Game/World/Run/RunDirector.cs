using System.Collections;
using System.Collections.Generic;
using TentacleVania.Combat;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Runs;
using TentacleVania.Core.Scenes;
using TentacleVania.World.Arenas;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace TentacleVania.World.Run
{
    public sealed class RunDirector : MonoBehaviour
    {
        [SerializeField] private RunKit _kit;
        [Tooltip("0 picks a random seed.")]
        [SerializeField] private int _seed;

        private readonly List<DoorOffer> _offers = new(RunGenerator.MaxDoors);
        private readonly Dictionary<RoomRole, HashSet<int>> _used = new();
        private readonly List<RunExit> _exits = new(4);
        private FloorPlan _plan;
        private System.Random _rng;
        private AsyncOperationHandle<SceneInstance> _handle;
        private bool _hasRoom;
        private bool _loading;
        private DoorOffer _current;
        private ArenaEncounter _arena;
        private bool _cleared;

        public static RunDirector Active { get; private set; }

        public FloorPlan Plan => _plan;
        public DoorOffer Current => _current;
        public bool Loading => _loading;
        public bool RoomCleared => _cleared;
        public string CurrentScene => _hasRoom ? _handle.Result.Scene.name : "";

        private void Awake()
        {
            Active = this;
            LoadGate.Hold(this);
            RunExit.Chosen += OnChosen;
        }

        private void OnDestroy()
        {
            RunExit.Chosen -= OnChosen;
            LoadGate.Release(this);
            if (Active == this)
                Active = null;
            if (_hasRoom && _handle.IsValid())
                Addressables.UnloadSceneAsync(_handle);
            RunSession.End();
        }

        private void Start()
        {
            RunSession.Begin(_seed != 0 ? _seed : System.Environment.TickCount);
            StartFloor(0);
        }

        private void StartFloor(int floor)
        {
            RunSession.Floor = floor;
            RunSession.Step = 0;
            var pool = _kit != null ? _kit.Pool(floor) : null;
            if (pool != null)
                DifficultyCurve.CurrentSector = pool.Sector;
            _plan = RunGenerator.PlanFloor(RunSession.Seed, floor);
            _rng = new System.Random(RunGenerator.FloorSeed(RunSession.Seed, floor) + 1);
            foreach (var used in _used.Values)
                used.Clear();
            var entry = RunGenerator.Offers(_rng, RoomRole.Entry, RunSession.Context, _kit.Count(floor, RoomRole.Entry), Used(RoomRole.Entry), _offers);
            Go(entry[0]);
        }

        private HashSet<int> Used(RoomRole role)
        {
            if (!_used.TryGetValue(role, out var used))
                _used[role] = used = new HashSet<int>();
            return used;
        }

        private void OnChosen(RunExit exit)
        {
            if (_loading || !exit.HasOffer)
                return;
            RunSession.Step++;
            Go(exit.Offer);
        }

        public void Skip()
        {
            if (_loading)
                return;
            if (!_cleared)
            {
                OnCleared();
                return;
            }
            foreach (var exit in _exits)
                if (exit.CanInteract)
                {
                    OnChosen(exit);
                    return;
                }
        }

        private void Go(DoorOffer offer)
        {
            _current = offer;
            Used(offer.Role).Add(offer.Room);
            StartCoroutine(Load(offer));
        }

        private IEnumerator Load(DoorOffer offer)
        {
            _loading = true;
            _cleared = false;
            if (_arena != null)
                _arena.StateChanged -= OnArenaChanged;
            _arena = null;
            if (_hasRoom)
            {
                var unload = Addressables.UnloadSceneAsync(_handle);
                yield return unload;
                _hasRoom = false;
            }
            var scene = _kit.Scene(RunSession.Floor, offer.Role, offer.Room);
            if (string.IsNullOrEmpty(scene))
            {
                Debug.LogError($"Run kit has no {offer.Role} room {offer.Room} for floor {RunSession.Floor}");
                _loading = false;
                yield break;
            }
            _handle = Addressables.LoadSceneAsync(scene, LoadSceneMode.Additive);
            yield return _handle;
            if (_handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"Run room {scene} failed to load: {_handle.OperationException}");
                _loading = false;
                yield break;
            }
            _hasRoom = true;
            LoadGate.Release(this);
            while (CombatTargets.Creature == null)
                yield return null;
            Setup(_handle.Result.Scene);
            _loading = false;
        }

        private void Setup(Scene scene)
        {
            RunEntry entry = null;
            _exits.Clear();
            foreach (var root in scene.GetRootGameObjects())
            {
                if (entry == null)
                    entry = root.GetComponentInChildren<RunEntry>();
                _exits.AddRange(root.GetComponentsInChildren<RunExit>());
                if (_arena == null)
                    _arena = root.GetComponentInChildren<ArenaEncounter>();
            }
            if (entry != null)
                Place(CombatTargets.Creature.transform, entry.transform);
            var last = RunSession.Step >= _plan.Count - 1;
            _offers.Clear();
            if (!last)
            {
                var next = _plan.Steps[RunSession.Step + 1];
                RunGenerator.Offers(_rng, next, RunSession.Context, _kit.Count(RunSession.Floor, next), Used(next), _offers);
            }
            Shuffle(_exits);
            for (var i = 0; i < _exits.Count; i++)
            {
                if (i < _offers.Count)
                    _exits[i].Assign(_offers[i]);
                else
                    _exits[i].Clear();
            }
            var fights = _current.Role is RoomRole.Combat or RoomRole.Elite or RoomRole.Boss;
            if (_arena != null && fights)
            {
                if (_current.Role != RoomRole.Boss)
                    _arena.SetWaves(RunWaveComposer.Compose(_rng, _kit.Pool(RunSession.Floor), _current.Role, RunSession.Step));
                _arena.StateChanged += OnArenaChanged;
            }
            else
            {
                OnCleared();
            }
        }

        private void OnArenaChanged(ArenaEncounter arena)
        {
            if (arena.State == ArenaState.Cleared)
                OnCleared();
        }

        private void OnCleared()
        {
            if (_cleared)
                return;
            _cleared = true;
            if (_arena != null)
                _arena.StateChanged -= OnArenaChanged;
            RunSession.Grant(_current.Reward == RunReward.Challenge ? RunReward.Mutation : _current.Reward);
            if (_current.Role == RoomRole.Boss)
            {
                FinishFloor();
                return;
            }
            foreach (var exit in _exits)
                exit.Open();
        }

        private void FinishFloor()
        {
            var floor = RunSession.Floor;
            RunSession.ClearFloor(floor);
            if (floor + 1 >= _kit.FloorCount)
            {
                RunSession.End();
                return;
            }
            StartFloor(floor + 1);
        }

        private void Shuffle(List<RunExit> exits)
        {
            for (var i = exits.Count - 1; i > 0; i--)
            {
                var j = _rng.Next(i + 1);
                (exits[i], exits[j]) = (exits[j], exits[i]);
            }
        }

        private static void Place(Transform creature, Transform at)
        {
            var controller = creature.GetComponent<CharacterController>();
            if (controller != null)
                controller.enabled = false;
            creature.SetPositionAndRotation(at.position, at.rotation);
            if (controller != null)
                controller.enabled = true;
        }
    }
}
