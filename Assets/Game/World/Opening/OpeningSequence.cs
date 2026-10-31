using System;
using System.Collections.Generic;
using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Core.Characters;
using TentacleVania.Core.Feedback;
using TentacleVania.Core.Saves;
using TentacleVania.Enemies;
using TentacleVania.Enemies.Types;
using TentacleVania.World.Arenas;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.VFX;

namespace TentacleVania.World.Opening
{
    public sealed class OpeningSequence : MonoBehaviour
    {
        public const string SeenId = "opening";

        [SerializeField] private Transform _pod;
        [SerializeField] private Renderer[] _glass = Array.Empty<Renderer>();
        [SerializeField] private Transform[] _workSpots = Array.Empty<Transform>();
        [SerializeField] private Enemy _scientistPrefab;
        [SerializeField] private ArenaEncounter _encounter;
        [SerializeField] private VisualEffect _burstEffect;
        [SerializeField] private EventReference _flickerEvent;
        [SerializeField] private EventReference _alarmEvent;
        [SerializeField] private EventReference _crackEvent;
        [SerializeField] private EventReference _burstEvent;
        [Tooltip("PA sequence the cold open plays through its PaZone; skipping marks it played.")]
        [SerializeField] private string _paSequence = "sh.entry";
        [SerializeField, Min(0f)] private float _flickerAt = 3f;
        [SerializeField, Min(0f)] private float _flickerTime = 1.6f;
        [SerializeField, Min(0f)] private float _alarmAt = 5f;
        [SerializeField, Min(0f)] private float _crackAt = 15f;
        [SerializeField, Min(0f)] private float _burstAt = 20f;
        [SerializeField, Min(0.1f)] private float _skipHold = 1f;
        [SerializeField, Range(0.05f, 0.25f)] private float _feedableHealth = 0.15f;
        [SerializeField, Min(0f)] private float _startRadius = 4f;
        [Tooltip("The creature starts the game at this fraction of its health, so the first consume visibly heals.")]
        [SerializeField, Range(0.1f, 1f)] private float _creatureStartHealth = 0.8f;

        private readonly List<Enemy> _scientists = new(4);

        private bool _running;
        private float _started;
        private float _skip;
        private bool _flickered;
        private bool _alarmed;
        private bool _cracked;
        private bool _pausedBefore;
        private float _sunIntensity;
        private Vector3 _podHome;

        public static OpeningSequence Current { get; private set; }
        public static event Action<string> Skipped;

        public static bool Playing => Current != null && Current._running;
        public static float SkipProgress => Current != null ? Current._skip : 0f;
        public static bool Seen => SaveSystem.Data.World.Has(WorldFlag.PlayedLine, SeenId);

        public float Elapsed => _running ? Time.time - _started : 0f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Current = null;
            Skipped = null;
        }

        private void OnEnable() => Current = this;

        private void OnDisable()
        {
            if (_running)
                Finish(false);
            if (Current == this)
                Current = null;
        }

        private void Start()
        {
            if (_pod != null)
                _podHome = _pod.localPosition;
            if (_encounter == null)
                foreach (var root in gameObject.scene.GetRootGameObjects())
                    if (_encounter == null)
                        _encounter = root.GetComponentInChildren<ArenaEncounter>();
            if (Seen)
            {
                ShowGlass(false);
                enabled = false;
                return;
            }
            if (_encounter != null)
                _encounter.Held = true;
        }

        private void Update()
        {
            if (!_running)
            {
                TryBegin();
                return;
            }
            var elapsed = Time.time - _started;
            TickSkip();
            if (!_running)
                return;
            if (!_flickered && elapsed >= _flickerAt)
            {
                _flickered = true;
                Play(_flickerEvent);
            }
            Flicker(elapsed);
            if (!_alarmed && elapsed >= _alarmAt)
            {
                _alarmed = true;
                Play(_alarmEvent);
            }
            if (!_cracked && elapsed >= _crackAt)
            {
                _cracked = true;
                Play(_crackEvent);
                ScreenShake.Request(_pod != null ? _pod.position : transform.position, 0.3f);
                Rumble.Play(0.3f, 0.2f, 0.3f);
            }
            if (_cracked && _pod != null)
                _pod.localPosition = _podHome + UnityEngine.Random.insideUnitSphere * (0.03f * Mathf.InverseLerp(_crackAt, _burstAt, elapsed));
            if (elapsed >= _burstAt)
                Finish(true);
        }

        public void Begin()
        {
            if (_running)
                return;
            var creature = CombatTargets.Creature;
            if (creature == null)
                return;
            _running = true;
            _started = Time.time;
            _skip = 0f;
            _flickered = _alarmed = _cracked = false;
            ShowGlass(true);
            if (creature.TryGetComponent(out ICreatureTransit transit))
                transit.BeginTransit();
            if (_pod != null)
                creature.transform.SetPositionAndRotation(_pod.position, _pod.rotation);
            creature.SetFraction(_creatureStartHealth);
            var manager = EnemyManager.Instance;
            _pausedBefore = manager.Paused;
            manager.Paused = true;
            if (_encounter != null)
                _encounter.Held = true;
            var sun = RenderSettings.sun;
            _sunIntensity = sun != null ? sun.intensity : 0f;
            _scientists.Clear();
            if (_scientistPrefab != null)
                foreach (var spot in _workSpots)
                    _scientists.Add(Instantiate(_scientistPrefab, spot.position, spot.rotation));
        }

        public void Replay()
        {
            if (_running)
                return;
            SaveSystem.Data.World.Clear(WorldFlag.PlayedLine, SeenId);
            enabled = true;
            Begin();
        }

        public void Skip()
        {
            if (!_running)
                return;
            Skipped?.Invoke(_paSequence);
            Finish(true);
        }

        private void TryBegin()
        {
            if (Seen)
            {
                enabled = false;
                return;
            }
            if (_encounter != null && _encounter.State == ArenaState.Cleared)
            {
                SaveSystem.Data.World.Set(WorldFlag.PlayedLine, SeenId);
                ShowGlass(false);
                enabled = false;
                return;
            }
            var creature = CombatTargets.Creature;
            if (creature == null || _pod == null)
                return;
            var offset = creature.transform.position - _pod.position;
            offset.y = 0f;
            if (offset.sqrMagnitude <= _startRadius * _startRadius)
                Begin();
        }

        private void TickSkip()
        {
            if (AnyButtonHeld())
            {
                _skip += Time.unscaledDeltaTime / _skipHold;
                if (_skip >= 1f)
                    Skip();
            }
            else
            {
                _skip = 0f;
            }
        }

        private static bool AnyButtonHeld()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.anyKey.isPressed)
                return true;
            var mouse = Mouse.current;
            if (mouse != null && (mouse.leftButton.isPressed || mouse.rightButton.isPressed))
                return true;
            var pad = Gamepad.current;
            return pad != null && (pad.buttonSouth.isPressed || pad.buttonEast.isPressed || pad.buttonWest.isPressed || pad.buttonNorth.isPressed
                || pad.startButton.isPressed || pad.selectButton.isPressed || pad.leftShoulder.isPressed || pad.rightShoulder.isPressed
                || pad.leftTrigger.isPressed || pad.rightTrigger.isPressed);
        }

        private void Flicker(float elapsed)
        {
            var sun = RenderSettings.sun;
            if (sun == null || elapsed < _flickerAt)
                return;
            var t = elapsed - _flickerAt;
            if (t < _flickerTime)
                sun.intensity = _sunIntensity * (Mathf.PerlinNoise(elapsed * 14f, 0.3f) > 0.45f ? 1f : 0.08f);
            else
                sun.intensity = _sunIntensity * 0.55f;
        }

        private void Finish(bool burst)
        {
            _running = false;
            _skip = 0f;
            var sun = RenderSettings.sun;
            if (sun != null && _sunIntensity > 0f)
                sun.intensity = _sunIntensity;
            if (_pod != null)
                _pod.localPosition = _podHome;
            var manager = EnemyManager.Current;
            if (manager != null)
                manager.Paused = _pausedBefore;
            var creature = CombatTargets.Creature;
            if (creature != null && creature.TryGetComponent(out ICreatureTransit transit))
                transit.EndTransit();
            SaveSystem.Data.World.Set(WorldFlag.PlayedLine, SeenId);
            if (!burst)
                return;
            ShowGlass(false);
            if (_burstEffect != null && _pod != null)
            {
                _burstEffect.transform.position = _pod.position + Vector3.up;
                _burstEffect.Reinit();
                _burstEffect.Play();
            }
            Play(_burstEvent);
            ScreenShake.Request(_pod != null ? _pod.position : transform.position, 0.8f);
            Rumble.Play(0.8f, 0.6f, 0.4f);
            foreach (var scientist in _scientists)
                Terrify(scientist);
            if (_encounter != null)
            {
                _encounter.Held = false;
                _encounter.Adopt(_scientists, Time.time);
            }
            enabled = false;
        }

        private void Terrify(Enemy enemy)
        {
            if (enemy == null)
                return;
            if (enemy is ScientistEnemy scientist)
                scientist.Terrify();
            var health = enemy.Health;
            for (var i = 0; i < 4 && health.IsAlive && health.Normalized > _feedableHealth; i++)
            {
                var info = DamageInfo.Create(health.Current - health.Max * _feedableHealth, Team.Environment);
                info.Point = enemy.transform.position + Vector3.up;
                if (_pod != null)
                    info.Direction = (enemy.transform.position - _pod.position).normalized;
                health.TakeDamage(info);
            }
        }

        private void ShowGlass(bool shown)
        {
            foreach (var glass in _glass)
                if (glass != null)
                    glass.enabled = shown;
        }

        private void Play(EventReference sound)
        {
            if (!sound.IsNull)
                RuntimeManager.PlayOneShot(sound, _pod != null ? _pod.position : transform.position);
        }
    }
}
