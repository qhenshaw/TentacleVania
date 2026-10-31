using FMODUnity;
using UnityEngine;

namespace TentacleVania.Enemies
{
    public static class EnemyVoices
    {
        public const int MaxPerWindow = 3;
        public const float Window = 0.6f;

        private static readonly VoiceLimiter Limiter = new(MaxPerWindow, Window);

        public static bool Enabled { get; set; } = true;
        public static int Played { get; private set; }
        public static int Dropped { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Enabled = true;
            Played = 0;
            Dropped = 0;
            Limiter.Clear();
        }

        public static bool Play(EventReference sound, Vector3 position, bool important = false)
        {
            if (!Enabled || sound.IsNull)
                return false;
            if (!Limiter.TryUse(Time.unscaledTime, important))
            {
                Dropped++;
                return false;
            }
            Played++;
            RuntimeManager.PlayOneShot(sound, position);
            return true;
        }
    }
}
