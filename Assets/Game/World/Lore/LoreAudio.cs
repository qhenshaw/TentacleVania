using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace TentacleVania.World.Lore
{
    public static class LoreAudio
    {
        private static EventInstance _playing;

        public static bool IsPlaying => _playing.isValid();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _playing = default;

        public static void Play(LoreLog log)
        {
            Stop();
            if (log == null || log.Format != LoreFormat.Audio || log.Audio.IsNull)
                return;
            _playing = RuntimeManager.CreateInstance(log.Audio);
            _playing.start();
        }

        public static void Stop()
        {
            if (!_playing.isValid())
                return;
            _playing.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            _playing.release();
            _playing = default;
        }
    }
}
