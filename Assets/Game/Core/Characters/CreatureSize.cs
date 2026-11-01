using System;
using UnityEngine;

namespace TentacleVania.Core.Characters
{
    public static class CreatureSize
    {
        public static int Stage { get; private set; }
        public static float CameraDistanceScale { get; private set; } = 1f;
        public static event Action<int, bool> Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Stage = 0;
            CameraDistanceScale = 1f;
            Changed = null;
        }

        public static void Set(int stage, float cameraDistanceScale, bool grew)
        {
            Stage = stage;
            CameraDistanceScale = cameraDistanceScale;
            Changed?.Invoke(stage, grew);
        }
    }
}
