using TentacleVania.Core.Debugging;
using TentacleVania.Core.Progression;
using TentacleVania.World.Streaming;
using UnityEngine;

namespace TentacleVania.World.Map
{
    public static class MapDebugCommands
    {
        private const string Tab = WorldDebugCommands.Tab;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            DebugMenu.Value(Tab, "Map", () =>
            {
                var registry = RoomStreamer.Active != null ? RoomStreamer.Active.Registry : null;
                if (registry == null)
                    return "no streamed sector";
                var explored = 0;
                foreach (var room in registry.Rooms)
                    if (MapProgress.IsExplored(room.Scene))
                        explored++;
                return $"{registry.Sector}: {explored}/{registry.Rooms.Count} rooms, {registry.Markers.Count} markers, travel {FastTravel.Phase}";
            });
            DebugMenu.Button(Tab, "Reveal sector map", () => ForEachRoom(room => MapProgress.Explore(room)));
            DebugMenu.Button(Tab, "Forget sector map", () => ForEachRoom(MapProgress.Forget));
            DebugMenu.Button(Tab, "Grow every atlas nest", () =>
            {
                var atlas = MapAtlas.Instance;
                if (atlas == null)
                    return;
                foreach (var registry in atlas.Sectors)
                    if (registry != null)
                        foreach (var marker in registry.Markers)
                            if (marker.Kind == MapMarkerKind.Nest)
                                Nests.Grow(marker.Id);
            });
        }

        private static void ForEachRoom(System.Action<string> action)
        {
            var registry = RoomStreamer.Active != null ? RoomStreamer.Active.Registry : null;
            if (registry == null)
                return;
            foreach (var room in registry.Rooms)
                action(room.Scene);
        }
    }
}
