using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using TentacleVania.Combat;
using TentacleVania.Core;
using TentacleVania.Core.Saves;
using TentacleVania.Core.Scenes;
using TentacleVania.Creature;
using TentacleVania.Creature.Rig;
using TentacleVania.Enemies;
using TentacleVania.World.Arenas;
using TentacleVania.World.Streaming;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace TentacleVania.Benchmark
{
    [AddComponentMenu("")]
    public sealed class BenchmarkRunner : MonoBehaviour
    {
        public const string Argument = "-benchmark";
        private const string OpeningSeenId = "opening";
        private const float SetupTimeout = 30f;
        private const int MaxSamples = 60 * 600;

        private enum Phase
        {
            Boot,
            Enter,
            Setup,
            Run,
            Done,
        }

        private readonly List<Enemy> _spawned = new(16);
        private readonly float[] _frameMs = new float[MaxSamples];
        private readonly FrameTiming[] _timing = new FrameTiming[1];

        private BenchmarkSettings _settings;
        private float _seconds;
        private string _reportPath;
        private Phase _phase;
        private float _phaseStarted;
        private float _recordStarted = -1f;
        private int _samples;
        private double _gpuSum;
        private double _cpuSum;
        private int _timingSamples;
        private long _gcBytes;
        private int _gcFrames;
        private ProfilerRecorder _gcRecorder;
        private Gamepad _pad;
        private bool _padDown;
        private float _nextTap;
        private float _nextKill;
        private float _nextConsume;
        private float _nextHeal;
        private int _kills;
        private int _consumes;
        private int _chains;
        private Consumable _pendingGrab;
        private Vector3 _centre;

        public static BenchmarkRunner Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Detect()
        {
            if (!Application.isPlaying || Instance != null)
                return;
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, Argument);
            if (index < 0)
                return;
            var seconds = 60f;
            if (index + 1 < args.Length && float.TryParse(args[index + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                seconds = parsed;
            var report = index + 2 < args.Length && !args[index + 2].StartsWith("-") ? args[index + 2] : null;
            Begin(seconds, report);
        }

        public static BenchmarkRunner Begin(float seconds, string reportPath)
        {
            var host = new GameObject("BenchmarkRunner");
            DontDestroyOnLoad(host);
            var runner = host.AddComponent<BenchmarkRunner>();
            runner._seconds = Mathf.Clamp(seconds, 1f, MaxSamples / 60f);
            runner._reportPath = string.IsNullOrEmpty(reportPath) ? Path.Combine(Application.persistentDataPath, "benchmark.txt") : reportPath;
            return runner;
        }

        private void Awake()
        {
            Instance = this;
            _settings = Resources.Load<BenchmarkSettings>(BenchmarkSettings.ResourcePath);
            _gcRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            SetPhase(Phase.Boot);
        }

        private void OnDestroy()
        {
            _gcRecorder.Dispose();
            if (_pad != null)
                InputSystem.RemoveDevice(_pad);
            if (Instance == this)
                Instance = null;
        }

        private void SetPhase(Phase phase)
        {
            _phase = phase;
            _phaseStarted = Time.realtimeSinceStartup;
        }

        private float PhaseTime => Time.realtimeSinceStartup - _phaseStarted;

        private void Update()
        {
            switch (_phase)
            {
                case Phase.Boot:
                    TickBoot();
                    break;
                case Phase.Enter:
                    TickEnter();
                    break;
                case Phase.Setup:
                    TickSetup();
                    break;
                case Phase.Run:
                    TickRun();
                    break;
            }
        }

        private void TickBoot()
        {
            if (SceneFlow.IsLoading || SceneManager.GetActiveScene().name != SceneFlow.MainMenu && SceneFlow.HasBootstrap)
            {
                if (PhaseTime > SetupTimeout)
                    Fail("main menu never loaded");
                return;
            }
            SaveSystem.Storage = new LocalFileStorage(Path.Combine(Application.persistentDataPath, "BenchmarkSaves"));
            SaveSystem.NewGame(0);
            SaveSystem.Data.World.Set(WorldFlag.PlayedLine, OpeningSeenId);
            SceneFlow.Load(SceneFlow.FirstGameplay);
            SetPhase(Phase.Enter);
        }

        private void TickEnter()
        {
            var creature = CombatTargets.Creature;
            var streamer = RoomStreamer.Active;
            if (SceneFlow.IsLoading || creature == null || streamer == null)
            {
                if (PhaseTime > SetupTimeout)
                    Fail("gameplay never started");
                return;
            }
            var index = streamer.Registry.IndexOf(_settings != null ? _settings.Room : "");
            if (index < 0)
            {
                Fail("benchmark room not in the sector registry");
                return;
            }
            _centre = streamer.Registry.Rooms[index].Center;
            if (NavMesh.SamplePosition(_centre, out var centre, 6f, NavMesh.AllAreas))
                _centre = centre.position;
            Teleport(creature.transform, _centre);
            SetPhase(Phase.Setup);
        }

        private void TickSetup()
        {
            var arena = ArenaIn(_settings.Room);
            if (arena == null)
            {
                if (PhaseTime > SetupTimeout)
                    Fail("benchmark room never loaded");
                return;
            }
            arena.Held = true;
            Core.Rendering.DynamicResolution.Enabled = false;
            var creature = CombatTargets.Creature;
            creature.Floor = 1f;
            var legs = creature.GetComponent<CreatureLegs>();
            if (legs != null && _settings.ApexRig != null)
                legs.Build(_settings.ApexRig);
            _chains = legs != null ? legs.LegCount : 0;
            GameSettings.Gore = true;
            if (_settings.Boss != null)
                Spawn(_settings.Boss, 0);
            for (var i = _spawned.Count; i < _settings.EnemyCount; i++)
                Spawn(_settings.Enemies[i % _settings.Enemies.Length], i);
            _pad = InputSystem.AddDevice<Gamepad>("BenchmarkPad");
            var now = Time.time;
            _nextTap = now;
            _nextKill = now + _settings.KillInterval;
            _nextConsume = now + _settings.ConsumeInterval;
            SetPhase(Phase.Run);
        }

        private void TickRun()
        {
            var now = Time.time;
            DriveGameplay(now);
            if (PhaseTime < _settings.WarmUp)
                return;
            if (_recordStarted < 0f)
                _recordStarted = Time.realtimeSinceStartup;
            Record();
            if (Time.realtimeSinceStartup - _recordStarted >= _seconds || _samples >= MaxSamples)
                Finish();
        }

        private void DriveGameplay(float now)
        {
            var creature = CombatTargets.Creature;
            if (creature == null)
                return;
            if (now >= _nextHeal)
            {
                _nextHeal = now + 1f;
                creature.Heal(creature.Max);
                var drift = creature.transform.position - _centre;
                drift.y = 0f;
                if (drift.sqrMagnitude > 9f && _pendingGrab == null)
                    Teleport(creature.transform, _centre);
            }
            if (_pad != null && now >= _nextTap)
            {
                _padDown = !_padDown;
                _nextTap = now + (_padDown ? 0.05f : _settings.TapInterval);
                InputSystem.QueueStateEvent(_pad, _padDown ? new GamepadState().WithButton(GamepadButton.West) : new GamepadState());
            }
            Prune();
            if (now >= _nextKill)
            {
                _nextKill = now + _settings.KillInterval;
                var victim = PickNonBoss();
                if (victim != null)
                {
                    victim.Health.Kill();
                    _kills++;
                }
            }
            if (_pendingGrab != null)
            {
                if (_pendingGrab.IsGrabbable && creature.GetComponent<CreatureConsume>().TryGrab())
                    _consumes++;
                _pendingGrab = null;
            }
            if (now >= _nextConsume)
            {
                _nextConsume = now + _settings.ConsumeInterval;
                TryConsume(creature);
            }
            for (var i = _spawned.Count; i < _settings.EnemyCount; i++)
                Spawn(_settings.Enemies[(i + _kills) % _settings.Enemies.Length], i + _kills);
        }

        private void TryConsume(Health creature)
        {
            var victim = PickNonBoss();
            if (victim == null || !victim.TryGetComponent(out Consumable consumable))
                return;
            var forward = creature.transform.forward;
            if (!NavMesh.SamplePosition(creature.transform.position + forward * 1.4f, out var hit, 0.6f, NavMesh.AllAreas))
                return;
            var spot = hit.position;
            if (victim.TryGetComponent(out NavMeshAgent agent) && agent.enabled)
                agent.Warp(spot);
            else
                victim.transform.position = spot;
            victim.Health.SetFraction(0.1f);
            _pendingGrab = consumable;
        }

        private Enemy PickNonBoss()
        {
            for (var i = 0; i < _spawned.Count; i++)
            {
                var enemy = _spawned[(i + _kills) % _spawned.Count];
                if (enemy != null && !enemy.IsBoss && enemy.Health.IsAlive && enemy.State != EnemyState.Consumed)
                    return enemy;
            }
            return null;
        }

        private void Prune()
        {
            for (var i = _spawned.Count - 1; i >= 0; i--)
                if (_spawned[i] == null || !_spawned[i].Health.IsAlive)
                    _spawned.RemoveAt(i);
        }

        private void Spawn(Enemy prefab, int slot)
        {
            var creature = CombatTargets.Creature;
            if (prefab == null || creature == null)
                return;
            var point = Vector3.zero;
            var found = false;
            for (var attempt = 0; attempt < 8 && !found; attempt++)
            {
                var angle = (slot + attempt * 3) * 137.5f;
                var probe = _centre + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * _settings.RingRadius;
                if (NavMesh.SamplePosition(probe, out var hit, 1.5f, NavMesh.AllAreas))
                {
                    point = hit.position;
                    found = true;
                }
            }
            if (!found)
                return;
            var rotation = Quaternion.LookRotation(creature.transform.position - point);
            _spawned.Add(Instantiate(prefab, point, rotation));
        }

        private void Record()
        {
            _frameMs[_samples++] = Time.unscaledDeltaTime * 1000f;
            FrameTimingManager.CaptureFrameTimings();
            if (FrameTimingManager.GetLatestTimings(1, _timing) > 0 && _timing[0].gpuFrameTime > 0d)
            {
                _gpuSum += _timing[0].gpuFrameTime;
                _cpuSum += _timing[0].cpuMainThreadFrameTime;
                _timingSamples++;
            }
            if (_gcRecorder.Valid)
            {
                var bytes = _gcRecorder.LastValue;
                _gcBytes += bytes;
                if (bytes > 0)
                    _gcFrames++;
            }
        }

        private void Finish()
        {
            SetPhase(Phase.Done);
            var stats = BenchmarkStats.Compute(_frameMs, _samples);
            var report = new StringBuilder();
            report.AppendLine("TentacleVania benchmark");
            report.AppendLine($"Version: {Application.version}{(Debug.isDebugBuild ? " (development)" : "")}, {SystemInfo.graphicsDeviceName}, {SystemInfo.graphicsDeviceType}");
            report.AppendLine($"Resolution: {Screen.width}x{Screen.height}, quality {QualitySettings.names[QualitySettings.GetQualityLevel()]}");
            report.AppendLine($"Scene: {_settings.Room}, {_settings.EnemyCount} enemies (boss {(_settings.Boss != null ? _settings.Boss.name : "none")}), creature {_chains} leg chains, gore on");
            report.AppendLine($"Warm-up {_settings.WarmUp:F0} s, recorded {_seconds:F0} s, {stats.Frames} frames");
            report.AppendLine($"Frame time: avg {stats.AverageMs:F2} ms ({stats.AverageFps:F1} fps), p50 {stats.P50Ms:F2}, p95 {stats.P95Ms:F2}, p99 {stats.P99Ms:F2}, max {stats.MaxMs:F2}");
            report.AppendLine($"Over {BenchmarkStats.FrameBudgetMs:F1} ms: {stats.OverBudget} frames ({stats.OverBudgetPercent:F1}%)");
            report.AppendLine(_timingSamples > 0
                ? $"CPU main thread avg {_cpuSum / _timingSamples:F2} ms, GPU avg {_gpuSum / _timingSamples:F2} ms"
                : "CPU/GPU split: n/a (Frame Timing Stats off or unsupported)");
            report.AppendLine(_gcRecorder.Valid
                ? $"GC allocated: {_gcBytes / 1024f:F1} KB in {_gcFrames} frames"
                : "GC allocated: n/a (needs a development build)");
            report.AppendLine($"Kills {_kills}, consumes {_consumes}; dynamic resolution off (native render scale)");
            Write(report.ToString());
        }

        private void Fail(string reason)
        {
            SetPhase(Phase.Done);
            Write("TentacleVania benchmark FAILED: " + reason + Environment.NewLine);
        }

        private void Write(string text)
        {
            try
            {
                var folder = Path.GetDirectoryName(_reportPath);
                if (!string.IsNullOrEmpty(folder))
                    Directory.CreateDirectory(folder);
                File.WriteAllText(_reportPath, text);
                Debug.Log(text);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            if (!Application.isEditor)
                Application.Quit();
        }

        public string Status => $"{_phase}, {(_recordStarted > 0f ? _samples + " frames" : PhaseTime.ToString("F0") + " s")}";

        private static ArenaEncounter ArenaIn(string scene)
        {
            foreach (var arena in FindObjectsByType<ArenaEncounter>())
                if (arena.gameObject.scene.name == scene)
                    return arena;
            return null;
        }

        private static void Teleport(Transform creature, Vector3 position)
        {
            var controller = creature.GetComponent<CharacterController>();
            if (controller != null)
                controller.enabled = false;
            creature.position = new Vector3(position.x, 0.1f, position.z);
            if (controller != null)
                controller.enabled = true;
        }
    }
}
