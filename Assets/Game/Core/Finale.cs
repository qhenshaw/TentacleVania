using System;
using TentacleVania.Core.Saves;
using UnityEngine;

namespace TentacleVania.Core
{
    public static class Finale
    {
        public static event Action CreditsRequested;
        public static event Action CreditsFinished;

        public static bool RollingCredits { get; private set; }
        public static bool Completed => SaveSystem.Data.Progress.Completed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            RollingCredits = false;
            CreditsRequested = null;
            CreditsFinished = null;
        }

        public static void RollCredits()
        {
            if (RollingCredits)
                return;
            RollingCredits = true;
            CreditsRequested?.Invoke();
        }

        public static void FinishCredits()
        {
            if (!RollingCredits)
                return;
            RollingCredits = false;
            CreditsFinished?.Invoke();
        }

        public static void Complete() => SaveSystem.Data.Progress.Completed = true;
    }
}
