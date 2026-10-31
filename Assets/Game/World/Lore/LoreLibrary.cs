using UnityEngine;

namespace TentacleVania.World.Lore
{
    [CreateAssetMenu(menuName = "TentacleVania/World/Lore Library")]
    public sealed class LoreLibrary : ScriptableObject
    {
        private const string ResourcePath = "LoreLibrary";

        [Tooltip("Every lore log, in the order the pause menu lists them.")]
        [SerializeField] private LoreLog[] _logs = System.Array.Empty<LoreLog>();

        private static LoreLibrary _instance;

        public static LoreLibrary Instance => _instance != null ? _instance : _instance = Resources.Load<LoreLibrary>(ResourcePath);

        public LoreLog[] Logs => _logs;

        public LoreLog Find(string id)
        {
            foreach (var log in _logs)
                if (log != null && log.name == id)
                    return log;
            return null;
        }
    }
}
