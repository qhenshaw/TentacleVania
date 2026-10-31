using System;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Saves;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace TentacleVania.Audio
{
    public sealed class PaDirector : MonoBehaviour
    {
        private enum Phase
        {
            Idle,
            Chime,
            Speaking,
            Gap,
        }

        private struct Queued
        {
            public PaLine Line;
            public string Sector;
        }

        private readonly Queue<Queued> _queue = new(32);
        private readonly object[] _arguments = new object[1];
        private PaScript _script;
        private EventInstance _voice;
        private Phase _phase;
        private Queued _next;
        private float _until;
        private float _duck = 1f;
        private PaChannel _lastChannel = PaChannel.Radio;
        private float _lastEnded = -100f;

        public static PaDirector Instance { get; private set; }
        public static event Action<string, PaChannel> LineStarted;
        public static event Action LineEnded;

        public bool Speaking => _phase == Phase.Speaking;
        public string CurrentText { get; private set; }
        public PaChannel CurrentChannel { get; private set; }
        public int QueuedCount => _queue.Count;
        public PaScript Script => _script;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            LineStarted = null;
            LineEnded = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (!Application.isPlaying || Instance != null)
                return;
            var script = Resources.Load<PaScript>(PaScript.ResourcePath);
            if (script == null)
                return;
            var go = new GameObject("PaDirector");
            DontDestroyOnLoad(go);
            go.AddComponent<PaDirector>()._script = script;
        }

        private void Awake() => Instance = this;

        private void OnEnable()
        {
            BossProgress.Defeated += OnBossDefeated;
            SecurityRelays.RelayDestroyed += OnRelayDestroyed;
        }

        private void OnDisable()
        {
            BossProgress.Defeated -= OnBossDefeated;
            SecurityRelays.RelayDestroyed -= OnRelayDestroyed;
            StopVoice();
            AudioMix.SetDuck(AudioMix.Music, 1f);
            AudioMix.SetDuck(AudioMix.Sfx, 1f);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public bool Play(string sequenceId, string sector = null)
        {
            var sequence = _script != null ? _script.Find(sequenceId) : null;
            if (sequence == null)
                return false;
            if (!SaveSystem.Data.World.Set(WorldFlag.PlayedLine, PaRules.SaveKey(sequence.Id, sequence.PerSector, sector)))
                return false;
            foreach (var line in sequence.Lines)
                _queue.Enqueue(new Queued { Line = line, Sector = sector });
            return true;
        }

        public void SkipAll()
        {
            _queue.Clear();
            StopVoice();
            EndLine();
            _phase = Phase.Idle;
        }

        private void Update()
        {
            if (_script == null)
                return;
            var now = Time.unscaledTime;
            switch (_phase)
            {
                case Phase.Idle:
                    if (_queue.Count == 0)
                        break;
                    _next = _queue.Dequeue();
                    if (_next.Line.Channel == PaChannel.PA && (_lastChannel != PaChannel.PA || now - _lastEnded > 2f) && !_script.Chime.IsNull)
                    {
                        RuntimeManager.PlayOneShot(_script.Chime);
                        _phase = Phase.Chime;
                        _until = now + _script.ChimeSeconds;
                    }
                    else
                    {
                        StartLine(now);
                    }
                    break;
                case Phase.Chime:
                    if (now >= _until)
                        StartLine(now);
                    break;
                case Phase.Speaking:
                    if (now >= _until)
                    {
                        StopVoice();
                        EndLine();
                        _phase = Phase.Gap;
                        _until = now + _script.GapSeconds;
                    }
                    break;
                case Phase.Gap:
                    if (now >= _until)
                        _phase = Phase.Idle;
                    break;
            }
            var ducked = _phase != Phase.Idle || _queue.Count > 0;
            _duck = Mathf.MoveTowards(_duck, ducked ? 0f : 1f, Time.unscaledDeltaTime * 3f);
            AudioMix.SetDuck(AudioMix.Music, Mathf.Lerp(_script.MusicDuck, 1f, _duck));
            AudioMix.SetDuck(AudioMix.Sfx, Mathf.Lerp(_script.SfxDuck, 1f, _duck));
        }

        private void StartLine(float now)
        {
            var line = _next.Line;
            CurrentText = Text(line.Key, _next.Sector);
            CurrentChannel = line.Channel;
            var voiceSeconds = 0f;
            var voice = _script.VoiceFor(line);
            if (!voice.IsNull)
            {
                _voice = RuntimeManager.CreateInstance(voice);
                _voice.start();
                if (_voice.getDescription(out var description) == FMOD.RESULT.OK && description.getLength(out var ms) == FMOD.RESULT.OK)
                    voiceSeconds = ms / 1000f;
            }
            _until = now + PaRules.LineSeconds(CurrentText.Length, voiceSeconds, _script.SecondsPerCharacter, _script.MinLineSeconds);
            _phase = Phase.Speaking;
            LineStarted?.Invoke(CurrentText, CurrentChannel);
        }

        private void EndLine()
        {
            if (CurrentText == null)
                return;
            _lastChannel = CurrentChannel;
            _lastEnded = Time.unscaledTime;
            CurrentText = null;
            LineEnded?.Invoke();
        }

        private string Text(string key, string sector)
        {
            var database = LocalizationSettings.StringDatabase;
            if (string.IsNullOrEmpty(sector))
                return database.GetLocalizedString(PaScript.Table, key);
            _arguments[0] = database.GetLocalizedString(PaScript.Table, "sector." + sector);
            return database.GetLocalizedString(PaScript.Table, key, _arguments);
        }

        private void StopVoice()
        {
            if (!_voice.isValid())
                return;
            _voice.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            _voice.release();
            _voice.clearHandle();
        }

        private void OnBossDefeated(string id)
        {
            var sequence = _script != null ? _script.Find(PaTrigger.BossDefeated, id) : null;
            if (sequence != null)
                Play(sequence.Id);
        }

        private void OnRelayDestroyed(string system, int count)
        {
            var sequence = _script != null ? _script.Find(PaTrigger.RelayCount, count.ToString()) : null;
            if (sequence != null)
                Play(sequence.Id, system);
        }
    }
}
