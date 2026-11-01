using TentacleVania.Core.Progression;
using TentacleVania.World.Map;
using TentacleVania.World.Streaming;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace TentacleVania.UI.Map
{
    public sealed class SiteMapView : VisualElement
    {
        private const string Table = "UI";
        private const int Floors = 4;
        private const int Columns = 3;

        private readonly Label _title = new();
        private readonly VisualElement[,] _cells = new VisualElement[Floors, Columns];
        private readonly Label[,] _names = new Label[Floors, Columns];
        private readonly VisualElement _routes = new();

        public SiteMapView()
        {
            pickingMode = PickingMode.Ignore;
            AddToClassList("site");
            _title.AddToClassList("site__title");
            Add(_title);
            for (var floor = 0; floor < Floors; floor++)
            {
                var row = new VisualElement { pickingMode = PickingMode.Ignore };
                row.AddToClassList("site__floor");
                var level = new Label("L" + floor);
                level.AddToClassList("site__level");
                row.Add(level);
                for (var column = 0; column < Columns; column++)
                {
                    var cell = new VisualElement { pickingMode = PickingMode.Ignore };
                    cell.AddToClassList("site__sector");
                    var name = new Label();
                    name.AddToClassList("site__name");
                    cell.Add(name);
                    row.Add(cell);
                    _cells[floor, column] = cell;
                    _names[floor, column] = name;
                }
                Add(row);
            }
            _routes.AddToClassList("site__routes");
            Add(_routes);
        }

        public void Refresh(RoomRegistry current)
        {
            var graph = WorldGraph.Instance;
            var atlas = MapAtlas.Instance;
            _title.text = T("map.site.title");
            _routes.Clear();
            if (graph == null)
                return;
            foreach (var sector in graph.Sectors)
            {
                var cell = _cells[sector.Floor, sector.Column];
                var registry = Registry(atlas, sector.Id);
                var known = registry != null && Visited(registry);
                var here = current != null && current.Sector == sector.Id;
                _names[sector.Floor, sector.Column].text = known ? MapScreen.SectorName(sector.Id) : T("map.site.unknown");
                cell.style.backgroundColor = known ? new Color(registry.Color.r, registry.Color.g, registry.Color.b, 0.55f) : new Color(0.2f, 0.22f, 0.21f, 0.6f);
                cell.EnableInClassList("site__sector--here", here);
            }
            if (current == null)
                return;
            var heading = new Label(T("map.site.routes"));
            heading.AddToClassList("site__heading");
            _routes.Add(heading);
            foreach (var connection in graph.Connections)
            {
                if (connection.From != current.Sector && connection.To != current.Sector)
                    continue;
                var other = connection.From == current.Sector ? connection.To : connection.From;
                var otherRegistry = Registry(atlas, other);
                var otherName = otherRegistry != null && Visited(otherRegistry) ? MapScreen.SectorName(other) : T("map.site.unknown");
                var open = WorldGraph.IsOpen(connection, BossProgress.IsAbilityUnlocked, BossProgress.IsDefeated);
                var line = new Label($"{T("map.route." + connection.Route.ToString().ToLowerInvariant())} → {otherName}: {(open ? T("map.site.open") : T(connection.GateKey))}");
                line.AddToClassList("site__route");
                line.EnableInClassList("site__route--locked", !open);
                _routes.Add(line);
            }
        }

        private static RoomRegistry Registry(MapAtlas atlas, string sector)
        {
            if (atlas == null)
                return null;
            foreach (var registry in atlas.Sectors)
                if (registry != null && registry.Sector == sector)
                    return registry;
            return null;
        }

        private static bool Visited(RoomRegistry registry)
        {
            foreach (var room in registry.Rooms)
                if (MapProgress.IsExplored(room.Scene))
                    return true;
            return false;
        }

        private static string T(string key)
        {
            var text = LocalizationSettings.StringDatabase.GetLocalizedString(Table, key);
            return string.IsNullOrEmpty(text) || text.StartsWith("No translation", System.StringComparison.Ordinal) ? key : text;
        }
    }
}
