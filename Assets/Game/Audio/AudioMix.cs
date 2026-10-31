using FMODUnity;
using UnityEngine;

namespace TentacleVania.Audio
{
    public static class AudioMix
    {
        public const string Master = "bus:/";
        public const string Music = "bus:/Music";
        public const string Sfx = "bus:/SFX";
        public const string Voice = "bus:/VO";

        private static readonly string[] Paths = { Master, Music, Sfx, Voice };
        private static readonly float[] User = { 1f, 1f, 1f, 1f };
        private static readonly float[] Duck = { 1f, 1f, 1f, 1f };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            for (var i = 0; i < Paths.Length; i++)
            {
                User[i] = 1f;
                Duck[i] = 1f;
            }
        }

        public static float UserVolume(string bus) => User[Index(bus)];

        public static void SetUserVolume(string bus, float volume)
        {
            var i = Index(bus);
            User[i] = Mathf.Clamp01(volume);
            Apply(i);
        }

        public static void SetDuck(string bus, float factor)
        {
            var i = Index(bus);
            if (Mathf.Approximately(Duck[i], factor))
                return;
            Duck[i] = Mathf.Clamp01(factor);
            Apply(i);
        }

        private static int Index(string bus)
        {
            for (var i = 0; i < Paths.Length; i++)
                if (Paths[i] == bus)
                    return i;
            return 0;
        }

        private static void Apply(int i)
        {
            if (!Application.isPlaying)
                return;
            var bus = RuntimeManager.GetBus(Paths[i]);
            if (bus.isValid())
                bus.setVolume(User[i] * Duck[i]);
        }
    }
}
