using TentacleVania.World.Streaming;
using UnityEngine;

namespace TentacleVania.World.Map
{
    [CreateAssetMenu(menuName = "TentacleVania/World/Map Atlas")]
    public sealed class MapAtlas : ScriptableObject
    {
        private const string ResourcePath = "MapAtlas";

        [Tooltip("Every sector's room registry, in travel-list order.")]
        [SerializeField] private RoomRegistry[] _sectors = System.Array.Empty<RoomRegistry>();

        private static MapAtlas _instance;

        public static MapAtlas Instance => _instance != null ? _instance : _instance = Resources.Load<MapAtlas>(ResourcePath);

        public RoomRegistry[] Sectors => _sectors;

        public RoomRegistry ForRootScene(string scene)
        {
            foreach (var registry in _sectors)
                if (registry != null && registry.RootScene == scene)
                    return registry;
            return null;
        }

        public bool TryFindNest(string id, out RoomRegistry registry, out MapMarker marker)
        {
            foreach (var sector in _sectors)
            {
                if (sector == null)
                    continue;
                var found = sector.FindMarker(MapMarkerKind.Nest, id);
                if (found == null)
                    continue;
                registry = sector;
                marker = found;
                return true;
            }
            registry = null;
            marker = null;
            return false;
        }
    }
}
