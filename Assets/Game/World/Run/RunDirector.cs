using System.Collections;
using System.Collections.Generic;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Core;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Runs;
using TentacleVania.Core.Saves;
using TentacleVania.Core.Scenes;
using TentacleVania.World.Arenas;
using TentacleVania.World.Lore;
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
        [Tooltip("Seconds after the boss room clears before the floor-end growth (the boss absorb plays first).")]
        [SerializeField, Min(0f)] private float _absorbWait = 4f;
        [Tooltip("Seconds after the growth before the next floor loads.")]
        [SerializeField, Min(0f)] private float _afterGrowthWait = 1f;
        [Tooltip("Health fraction the death defiance leaves after a lethal hit (placeholder).")]
        [SerializeField, Range(0.05f, 1f)] private float _defianceHealth = 0.3f;
        [Tooltip("Spawned in combat rooms by chance (Core.Runs.RunLore) with an uncollected log.")]
        [SerializeField] private LoreTerminal _loreTerminal;
        [Tooltip("Spawned inside the arena door of rooms that have no RunExits (legacy arenas used as rare elites).")]
        [SerializeField] private RunExit _exitPrefab;

        private const float DoorEntryDistance = 3f;
        private const float SpawnedExitSpacing = 3.2f;
        private const float SpawnedExitDepth = 1.2f;

        private readonly List<int> _weights = new(16);
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
        private bool _resumeCleared;
        private bool _podPending;
        private int _rerollsHere;
        private int _resumeRerolls;
        private Health _defiant;
        private readonly List<bool> _collected = new(64);

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
            RunExit.RerollRequested += OnRerollRequested;
            CombatEvents.Died += OnDied;
            RunPod.BrokeOut += ArmDefiance;
            RunSession.RespawnScene = gameObject.scene.name;
        }

        private void OnDestroy()
        {
            RunExit.Chosen -= OnChosen;
            RunExit.RerollRequested -= OnRerollRequested;
            CombatEvents.Died -= OnDied;
            RunPod.BrokeOut -= ArmDefiance;
            Disarm();
            LoadGate.Release(this);
            DifficultyCurve.RoomDepth = 0;
            RunSession.RespawnScene = null;
            if (Active == this)
                Active = null;
            if (_hasRoom && _handle.IsValid())
                Addressables.UnloadSceneAsync(_handle);
            RunSession.Suspend();
        }

        private void Start()
        {
            var saved = SaveSystem.Data.Run;
            if (SaveSystem.Active && saved.Active)
            {
                Resume(saved);
                return;
            }
            RunSession.Begin(_seed != 0 ? _seed : System.Environment.TickCount);
            _podPending = true;
            StartFloor(0);
        }

        private void Resume(RunState saved)
        {
            RunSession.Resume(saved);
            PrepareFloor(RunSession.Floor);
            foreach (var entry in saved.UsedRooms)
            {
                var split = entry.IndexOf(':');
                if (split > 0 && int.TryParse(entry.Substring(0, split), out var role) && int.TryParse(entry.Substring(split + 1), out var room))
                    Used((RoomRole)role).Add(room);
            }
            _resumeCleared = saved.Cleared;
            _resumeRerolls = saved.RerollsHere;
            Go(new DoorOffer((RoomRole)saved.Role, (RunReward)saved.Reward, saved.Room));
        }

        private void StartFloor(int floor)
        {
            RunSession.Floor = floor;
            RunSession.Step = 0;
            PrepareFloor(floor);
            var entry = RunGenerator.Offers(StepRng(), RoomRole.Entry, RunSession.Context, _kit.Count(floor, RoomRole.Entry), Used(RoomRole.Entry), _offers, _kit.Weights(floor, RoomRole.Entry, _weights));
            Go(entry[0]);
        }

        private void PrepareFloor(int floor)
        {
            var pool = _kit != null ? _kit.Pool(floor) : null;
            if (pool != null)
                DifficultyCurve.CurrentSector = pool.Sector;
            _plan = RunGenerator.PlanFloor(RunSession.Seed, floor);
            foreach (var used in _used.Values)
                used.Clear();
        }

        private System.Random StepRng() => new(unchecked(RunGenerator.FloorSeed(RunSession.Seed, RunSession.Floor) + 1 + RunSession.Step * 7919));

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
            if (RunPod.Open)
            {
                RunPod.BreakOut();
                return;
            }
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

        public void Restart(int seed)
        {
            if (_loading)
                return;
            if (RunPod.Open)
                RunPod.BreakOut();
            RunSession.Begin(seed);
            _podPending = true;
            StartFloor(0);
        }

        public void JumpToBoss()
        {
            if (_loading || _plan == null || _current.Role is RoomRole.Boss or RoomRole.Finale)
                return;
            if (RunPod.Open)
                RunPod.BreakOut();
            RunSession.Step = _plan.Count - 1;
            var offers = RunGenerator.Offers(StepRng(), RoomRole.Boss, RunSession.Context, _kit.Count(RunSession.Floor, RoomRole.Boss), Used(RoomRole.Boss), _offers, _kit.Weights(RunSession.Floor, RoomRole.Boss, _weights));
            Go(offers[0]);
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
            ArenaSession.Forget();
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
            if (_podPending)
            {
                _podPending = false;
                RunPod.Request();
            }
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
            else
                PlaceAtDoor(scene);
            ArmDefiance();
            _rng = StepRng();
            var last = RunSession.Step >= _plan.Count - 1;
            _offers.Clear();
            if (!last)
            {
                var next = _plan.Steps[RunSession.Step + 1];
                RunGenerator.Offers(_rng, next, RunSession.Context, _kit.Count(RunSession.Floor, next), Used(next), _offers, _kit.Weights(RunSession.Floor, next, _weights));
            }
            if (_exits.Count == 0 && !last && _current.Role is RoomRole.Combat or RoomRole.Elite)
                SpawnExits(scene);
            Shuffle(_exits, _rng);
            AssignExits();
            _rerollsHere = 0;
            for (var i = 0; i < _resumeRerolls; i++)
                Redraw();
            _resumeRerolls = 0;
            var fights = _current.Role is RoomRole.Combat or RoomRole.Elite or RoomRole.Boss or RoomRole.Finale;
            if (_resumeCleared)
            {
                _resumeCleared = false;
                _cleared = true;
                if (_arena != null)
                    _arena.MarkCleared();
                Store();
                if (_current.Role == RoomRole.Boss)
                    StartCoroutine(FinishFloor());
                else if (_current.Role == RoomRole.Finale)
                    StartCoroutine(FinishRun());
                else
                    foreach (var exit in _exits)
                        exit.Open();
                return;
            }
            if (_arena != null && fights)
            {
                DifficultyCurve.RoomDepth = _current.Role is RoomRole.Boss or RoomRole.Finale ? 0 : RunSession.Step;
                if (_current.Role == RoomRole.Combat)
                    SpawnLore(scene);
                if (_current.Role is RoomRole.Combat or RoomRole.Elite && !_kit.KeepsWaves(RunSession.Floor, _current.Role, _current.Room))
                    _arena.SetWaves(RunWaveComposer.Compose(_rng, _kit.Pool(RunSession.Floor), _current.Role, RunSession.Step));
                _arena.StateChanged += OnArenaChanged;
                Store();
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
            RunSession.Rooms++;
            RunSession.Grant(_current.Reward);
            Store();
            if (_current.Role == RoomRole.Boss)
            {
                StartCoroutine(FinishFloor());
                return;
            }
            if (_current.Role == RoomRole.Finale)
            {
                StartCoroutine(FinishRun());
                return;
            }
            foreach (var exit in _exits)
                exit.Open();
        }

        private IEnumerator FinishFloor()
        {
            _loading = true;
            var floor = RunSession.Floor;
            yield return new WaitForSeconds(_absorbWait);
            RunSession.ClearFloor(floor);
            yield return null;
            while (Cutscene.Active)
                yield return null;
            yield return new WaitForSeconds(_afterGrowthWait);
            _loading = false;
            if (floor + 1 < _kit.FloorCount)
            {
                StartFloor(floor + 1);
                yield break;
            }
            if (_kit.Count(floor, RoomRole.Finale) > 0)
            {
                RunSession.Step++;
                Go(new DoorOffer(RoomRole.Finale, RunReward.None, 0));
                yield break;
            }
            RunSession.End(true);
        }

        private IEnumerator FinishRun()
        {
            _loading = true;
            yield return new WaitForSeconds(_absorbWait);
            _loading = false;
            RunSession.End(true);
        }

        private void SpawnLore(Scene scene)
        {
            var library = LoreLibrary.Instance;
            if (_loreTerminal == null || library == null)
                return;
            _collected.Clear();
            foreach (var log in library.Logs)
                _collected.Add(log == null || LoreLogs.Has(log.Id));
            var pick = RunLore.Pick(new System.Random(unchecked(RunGenerator.FloorSeed(RunSession.Seed, RunSession.Floor) * 13 + RunSession.Step)), _collected);
            if (pick < 0)
                return;
            var center = Vector3.zero;
            var count = 0;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var renderer in root.GetComponentsInChildren<Renderer>())
                {
                    center += renderer.bounds.center;
                    count++;
                }
            if (count == 0 || !UnityEngine.AI.NavMesh.SamplePosition(center / count + new Vector3(2.5f, 0f, 2.5f), out var hit, 6f, UnityEngine.AI.NavMesh.AllAreas))
                return;
            var terminal = Instantiate(_loreTerminal, hit.position, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(terminal.gameObject, scene);
            terminal.SetLog(library.Logs[pick]);
        }

        private void SpawnExits(Scene scene)
        {
            if (_exitPrefab == null || !DoorFrame(scene, out var door, out var inward))
                return;
            var right = Vector3.Cross(Vector3.up, inward);
            var outward = Quaternion.LookRotation(-inward);
            for (var i = 0; i < _offers.Count; i++)
            {
                var offset = (i - (_offers.Count - 1) * 0.5f) * SpawnedExitSpacing;
                var exit = Instantiate(_exitPrefab, door + inward * SpawnedExitDepth + right * offset, outward);
                exit.name = "SpawnedExit" + i;
                SceneManager.MoveGameObjectToScene(exit.gameObject, scene);
                _exits.Add(exit);
            }
        }

        private static void PlaceAtDoor(Scene scene)
        {
            if (!DoorFrame(scene, out var door, out var inward))
                return;
            var marker = new GameObject("RunDoorEntry").transform;
            marker.SetPositionAndRotation(door + inward * DoorEntryDistance, Quaternion.LookRotation(inward));
            if (UnityEngine.AI.NavMesh.SamplePosition(marker.position, out var hit, 3f, UnityEngine.AI.NavMesh.AllAreas))
                marker.position = hit.position;
            Place(CombatTargets.Creature.transform, marker);
            Destroy(marker.gameObject);
        }

        private static bool DoorFrame(Scene scene, out Vector3 doorPosition, out Vector3 inward)
        {
            doorPosition = inward = Vector3.zero;
            ArenaDoor door = null;
            var bounds = new Bounds();
            var hasBounds = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (door == null)
                    door = root.GetComponentInChildren<ArenaDoor>(true);
                foreach (var renderer in root.GetComponentsInChildren<Renderer>())
                {
                    if (hasBounds)
                        bounds.Encapsulate(renderer.bounds);
                    else
                        bounds = renderer.bounds;
                    hasBounds = true;
                }
            }
            if (door == null || !hasBounds)
                return false;
            inward = bounds.center - door.transform.position;
            inward.y = 0f;
            if (inward.sqrMagnitude < 0.01f)
                return false;
            inward.Normalize();
            doorPosition = door.transform.position;
            return true;
        }

        private void Store()
        {
            if (!RunSession.Active)
                return;
            var state = SaveSystem.Data.Run;
            RunSession.Capture(state);
            state.Role = (int)_current.Role;
            state.Reward = (int)_current.Reward;
            state.Room = _current.Room;
            state.Cleared = _cleared;
            state.RerollsHere = _rerollsHere;
            state.UsedRooms.Clear();
            foreach (var pair in _used)
                foreach (var room in pair.Value)
                    state.UsedRooms.Add((int)pair.Key + ":" + room);
            Autosave.Request(AutosaveReason.RunRoom);
        }

        private void Update()
        {
            if (RunSession.Active && !RunPod.Open)
                RunSession.Seconds += Time.deltaTime;
        }

        private void ArmDefiance()
        {
            var creature = CombatTargets.Creature;
            if (!RunSession.DefianceReady || creature == null)
                return;
            if (_defiant != creature)
            {
                Disarm();
                _defiant = creature;
                _defiant.Damaged += OnCreatureDamaged;
            }
            _defiant.Floor = Mathf.Max(_defiant.Floor, 1f);
        }

        private void Disarm()
        {
            if (_defiant == null)
                return;
            _defiant.Damaged -= OnCreatureDamaged;
            _defiant = null;
        }

        private void OnCreatureDamaged(Health target, in DamageInfo info, in DamageResult result)
        {
            if (!RunSession.DefianceReady || !target.AtFloor)
                return;
            RunSession.DefianceUsed = true;
            target.Floor = 0f;
            target.SetFraction(_defianceHealth);
            Disarm();
            RunSession.Defied();
            Store();
        }

        private void OnDied(Health health)
        {
            if (RunSession.Active && health != null && health.TryGetComponent(out Enemies.Enemy enemy))
            {
                if (enemy.IsBoss)
                    RunSession.AddEssence(RunSession.BossEssence);
                else if (enemy.IsElite)
                    RunSession.AddEssence(RunSession.EliteEssence);
            }
            if (health != null && health == CombatTargets.Creature)
                RunSession.End(false);
        }

        public bool Reroll()
        {
            if (_loading || !_cleared || RunSession.Rerolls <= 0 || RunSession.Step >= _plan.Count - 1 || _plan.Steps[RunSession.Step + 1] != RoomRole.Combat)
                return false;
            RunSession.Rerolls--;
            Redraw();
            foreach (var exit in _exits)
                exit.Open();
            Store();
            return true;
        }

        private void OnRerollRequested(RunExit exit) => Reroll();

        private void Redraw()
        {
            _rerollsHere++;
            var next = _plan.Steps[RunSession.Step + 1];
            var rng = new System.Random(unchecked(RunGenerator.FloorSeed(RunSession.Seed, RunSession.Floor) * 31 + RunSession.Step * 7919 + _rerollsHere * 104729));
            RunGenerator.Offers(rng, next, RunSession.Context, _kit.Count(RunSession.Floor, next), Used(next), _offers, _kit.Weights(RunSession.Floor, next, _weights));
            Shuffle(_exits, rng);
            AssignExits();
        }

        private void AssignExits()
        {
            for (var i = 0; i < _exits.Count; i++)
            {
                if (i < _offers.Count)
                    _exits[i].Assign(_offers[i]);
                else
                    _exits[i].Clear();
            }
        }

        private static void Shuffle(List<RunExit> exits, System.Random rng)
        {
            for (var i = exits.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
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
