using System;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

namespace TentacleVania.Audio
{
    public enum PaChannel
    {
        PA,
        Radio,
        Crane,
    }

    public enum PaTrigger
    {
        Manual,
        BossDefeated,
        RelayCount,
    }

    [Serializable]
    public sealed class PaLine
    {
        [Tooltip("Key in the Dialogue string table; relay lines take the sector name as {0}.")]
        public string Key;
        public PaChannel Channel;
        [Tooltip("Voice-over event; empty uses the channel's placeholder voice.")]
        public EventReference Voice;
    }

    [Serializable]
    public sealed class PaSequence
    {
        public string Id;
        public PaTrigger Trigger;
        [Tooltip("Boss save id (BossDefeated) or relay count 1-3 (RelayCount).")]
        public string TriggerKey;
        [Tooltip("Played once per security system instead of once per game (relay lines).")]
        public bool PerSector;
        public List<PaLine> Lines = new();
    }

    [CreateAssetMenu(menuName = "TentacleVania/Audio/PA Script")]
    public sealed class PaScript : ScriptableObject
    {
        public const string ResourcePath = "PaScript";
        public const string Table = "Dialogue";

        [SerializeField] private EventReference _chime;
        [SerializeField] private EventReference _paVoice;
        [SerializeField] private EventReference _radioVoice;
        [SerializeField] private EventReference _craneVoice;
        [Tooltip("Subtitles stay up at least this long per character when the voice is shorter.")]
        [SerializeField, Min(0f)] private float _secondsPerCharacter = 0.055f;
        [SerializeField, Min(0f)] private float _minLineSeconds = 2f;
        [SerializeField, Min(0f)] private float _gapSeconds = 0.4f;
        [SerializeField, Min(0f)] private float _chimeSeconds = 0.9f;
        [SerializeField, Range(0f, 1f)] private float _musicDuck = 0.45f;
        [SerializeField, Range(0f, 1f)] private float _sfxDuck = 0.7f;
        [SerializeField] private List<PaSequence> _sequences = new();

        public EventReference Chime => _chime;
        public float SecondsPerCharacter => _secondsPerCharacter;
        public float MinLineSeconds => _minLineSeconds;
        public float GapSeconds => _gapSeconds;
        public float ChimeSeconds => _chimeSeconds;
        public float MusicDuck => _musicDuck;
        public float SfxDuck => _sfxDuck;
        public IReadOnlyList<PaSequence> Sequences => _sequences;

        public EventReference VoiceFor(PaLine line) => !line.Voice.IsNull ? line.Voice : line.Channel switch
        {
            PaChannel.PA => _paVoice,
            PaChannel.Radio => _radioVoice,
            _ => _craneVoice,
        };

        public PaSequence Find(string id)
        {
            foreach (var sequence in _sequences)
                if (sequence.Id == id)
                    return sequence;
            return null;
        }

        public PaSequence Find(PaTrigger trigger, string key)
        {
            foreach (var sequence in _sequences)
                if (sequence.Trigger == trigger && sequence.TriggerKey == key)
                    return sequence;
            return null;
        }
    }
}
