using FMOD.Studio;
using FMODUnity;
using TentacleVania.Bosses;
using TentacleVania.Combat;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Scenes;
using TentacleVania.Enemies;
using TentacleVania.World.Arenas;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TentacleVania.Audio
{
    public sealed class MusicDirector : MonoBehaviour
    {
        private sealed class Track
        {
            public EventInstance Instance;
            public FMOD.GUID Guid;
            public float Volume;
            public bool Playing;
        }

        private readonly Track _arena = new();
        private readonly Track _boss = new();
        private readonly Track _rest = new();
        private readonly Track _menu = new();
        private MusicSettings _settings;
        private ArenaState _lastArenaState;
        private ArenaEncounter _lastArena;
        private Vector3 _restPosition;
        private bool _restActive;
        private bool _menuScene;
        private bool _globalIntensity = true;

        public static MusicDirector Instance { get; private set; }
        public static bool Muted { get; set; }
        public MusicState State { get; private set; }
        public float Intensity { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            Muted = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (!Application.isPlaying || Instance != null)
                return;
            var settings = Resources.Load<MusicSettings>(MusicSettings.ResourcePath);
            if (settings == null)
                return;
            var go = new GameObject("MusicDirector");
            DontDestroyOnLoad(go);
            go.AddComponent<MusicDirector>()._settings = settings;
        }

        private void Awake() => Instance = this;

        private void OnEnable()
        {
            Nests.Rested += OnRested;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            _menuScene = SceneManager.GetActiveScene().name == SceneFlow.MainMenu;
        }

        private void OnActiveSceneChanged(Scene previous, Scene next) => _menuScene = next.name == SceneFlow.MainMenu;

        private void OnDisable()
        {
            Nests.Rested -= OnRested;
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            Release(_arena);
            Release(_boss);
            Release(_rest);
            Release(_menu);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (_settings == null)
                return;
            var creature = CombatTargets.Creature;
            var alive = creature != null && creature.IsAlive && !Muted;
            var arena = ArenaEncounter.Active;
            var arenaState = arena != null ? arena.State : ArenaState.Waiting;
            if (arena == _lastArena && arenaState == ArenaState.Cleared && _lastArenaState != ArenaState.Cleared && alive)
                PlayOneShot(_settings.RoomClearSting);
            _lastArena = arena;
            _lastArenaState = arenaState;
            var arenaFighting = arena != null && (arenaState == ArenaState.Spawning || arenaState == ArenaState.Fighting || arenaState == ArenaState.BetweenWaves);
            var boss = BossEnemy.Active;
            var bossFighting = boss != null && boss.Health.IsAlive && boss.State != EnemyState.Dead;
            if (_restActive && (creature == null || (creature.transform.position - _restPosition).sqrMagnitude > _settings.RestLeaveDistance * _settings.RestLeaveDistance))
                _restActive = false;
            var inMenu = _menuScene && !Muted;
            State = inMenu ? MusicState.Menu : alive ? MusicRules.Pick(bossFighting, arenaFighting, _restActive || Core.Runs.RunPod.Open) : MusicState.Silent;

            var targetIntensity = State switch
            {
                MusicState.Arena => MusicRules.ArenaIntensity(arena.WaveNumber, arena.WaveCount),
                MusicState.Boss => MusicRules.BossIntensity(boss.Phase, boss.Boss.Phases.Count),
                MusicState.Menu => _settings.MenuIntensity,
                _ => _arena.Playing || _boss.Playing ? Intensity : 0f,
            };
            var dt = Time.unscaledDeltaTime;
            Intensity = Mathf.MoveTowards(Intensity, targetIntensity, _settings.IntensitySpeed * dt);

            var bossMusic = boss != null && !boss.Boss.Music.IsNull ? boss.Boss.Music : _settings.BossTheme;
            Drive(_arena, _settings.ArenaFor(Core.Progression.DifficultyCurve.CurrentSector), State == MusicState.Arena, dt);
            Drive(_boss, State == MusicState.Boss ? bossMusic : default, State == MusicState.Boss, dt);
            Drive(_rest, _settings.NestRest, State == MusicState.NestRest, dt);
            Drive(_menu, _settings.Menu, State == MusicState.Menu, dt);
            ApplyIntensity();
        }

        private void Drive(Track track, EventReference music, bool wanted, float dt)
        {
            if (wanted && track.Playing && !music.IsNull && !track.Guid.Equals(music.Guid))
                Release(track);
            if (wanted && !track.Playing && !music.IsNull)
            {
                track.Instance = RuntimeManager.CreateInstance(music);
                track.Guid = music.Guid;
                track.Volume = 0f;
                track.Instance.setVolume(0f);
                track.Instance.start();
                track.Playing = true;
            }
            if (!track.Playing)
                return;
            var rate = wanted ? 1f / _settings.FadeIn : 1f / _settings.FadeOut;
            track.Volume = Mathf.MoveTowards(track.Volume, wanted ? 1f : 0f, rate * dt);
            track.Instance.setVolume(track.Volume);
            if (!wanted && track.Volume <= 0f)
                Release(track);
        }

        private void ApplyIntensity()
        {
            if (_globalIntensity)
                _globalIntensity = RuntimeManager.StudioSystem.setParameterByName(_settings.IntensityParameter, Intensity) == FMOD.RESULT.OK;
            if (_globalIntensity)
                return;
            if (_arena.Playing)
                _arena.Instance.setParameterByName(_settings.IntensityParameter, Intensity);
            if (_boss.Playing)
                _boss.Instance.setParameterByName(_settings.IntensityParameter, Intensity);
            if (_menu.Playing)
                _menu.Instance.setParameterByName(_settings.IntensityParameter, Intensity);
        }

        private static void Release(Track track)
        {
            if (!track.Playing)
                return;
            track.Instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            track.Instance.release();
            track.Instance.clearHandle();
            track.Playing = false;
            track.Volume = 0f;
        }

        private void OnRested(string nest)
        {
            var creature = CombatTargets.Creature;
            if (creature == null)
                return;
            _restActive = true;
            _restPosition = creature.transform.position;
        }

        private static void PlayOneShot(EventReference sound)
        {
            if (!sound.IsNull)
                RuntimeManager.PlayOneShot(sound);
        }
    }
}
