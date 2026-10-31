using UnityEngine;

namespace TentacleVania.Core.Scenes
{
    public static class SpawnOverride
    {
        private static string _scene;
        private static Vector3 _position;
        private static float _yaw;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _scene = null;

        public static void Set(string scene, Vector3 position, float yaw)
        {
            _scene = scene;
            _position = position;
            _yaw = yaw;
        }

        public static bool TryGet(string scene, out Vector3 position, out float yaw)
        {
            position = _position;
            yaw = _yaw;
            return !string.IsNullOrEmpty(_scene) && _scene == scene;
        }

        public static void Clear() => _scene = null;
    }
}
