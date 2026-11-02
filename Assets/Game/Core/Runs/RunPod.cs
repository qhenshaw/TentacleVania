using System;
using UnityEngine;

namespace TentacleVania.Core.Runs
{
    public static class RunPod
    {
        public static bool Open { get; private set; }

        public static event Action Requested;
        public static event Action BrokeOut;

        public static void Request()
        {
            Open = true;
            if (Requested == null)
            {
                BreakOut();
                return;
            }
            Requested.Invoke();
        }

        public static void BreakOut()
        {
            if (!Open)
                return;
            Open = false;
            BrokeOut?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Open = false;
            Requested = null;
            BrokeOut = null;
        }
    }
}
