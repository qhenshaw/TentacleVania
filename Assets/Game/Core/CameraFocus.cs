using UnityEngine;

namespace TentacleVania.Core
{
    public static class CameraFocus
    {
        public static Transform Target { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Target = null;
    }
}
