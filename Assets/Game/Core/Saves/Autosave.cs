using System;
using TentacleVania.Core.Progression;
using UnityEngine;

namespace TentacleVania.Core.Saves
{
    public enum AutosaveReason
    {
        RoomCleared,
        Pickup,
        DoorOpened,
        ShortcutOpened,
        Purchase,
        Death,
        NestRest,
        NestGrown,
        Reclaim,
        BossDefeated,
        RelayDestroyed,
        WallBreached,
        NodeOverloaded,
        ArenaStarted,
    }

    public static class Autosave
    {
        private static bool _requested;
        private static Runner _runner;

        public static event Action<AutosaveReason> Requested;

        public static bool Enabled { get; set; } = true;
        public static float LastSaveTime { get; private set; } = float.NegativeInfinity;
        public static AutosaveReason LastReason { get; private set; }
        public static int Count { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _requested = false;
            _runner = null;
            Requested = null;
            Enabled = true;
            LastSaveTime = float.NegativeInfinity;
            Count = 0;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Hook()
        {
            Nests.Rested += _ => Request(AutosaveReason.NestRest);
            Nests.Grown += _ => Request(AutosaveReason.NestGrown);
            Biomass.Reclaimed += _ => Request(AutosaveReason.Reclaim);
            Application.quitting += SaveSystem.Flush;
        }

        public static void Request(AutosaveReason reason)
        {
            if (!Enabled || !SaveSystem.Active || !Application.isPlaying)
                return;
            _requested = true;
            LastReason = reason;
            Requested?.Invoke(reason);
            if (_runner == null)
            {
                var host = new GameObject("Autosave") { hideFlags = HideFlags.HideAndDontSave };
                UnityEngine.Object.DontDestroyOnLoad(host);
                _runner = host.AddComponent<Runner>();
            }
        }

        private static void Tick()
        {
            if (!_requested)
                return;
            _requested = false;
            if (!SaveSystem.Active)
                return;
            SaveSystem.SaveAsync();
            LastSaveTime = Time.unscaledTime;
            Count++;
        }

        private sealed class Runner : MonoBehaviour
        {
            private void LateUpdate() => Tick();
        }
    }
}
