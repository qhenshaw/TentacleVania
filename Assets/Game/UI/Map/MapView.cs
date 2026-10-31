using System.Collections.Generic;
using TentacleVania.Combat;
using TentacleVania.Core.Progression;
using TentacleVania.World.Streaming;
using UnityEngine;
using UnityEngine.UIElements;

namespace TentacleVania.UI.Map
{
    public sealed class MapView : VisualElement
    {
        private const float FitMargin = 0.9f;

        private readonly VisualElement _content = new() { name = "map-content", pickingMode = PickingMode.Ignore };
        private readonly VisualElement _creature = new() { name = "map-creature", pickingMode = PickingMode.Ignore };
        private readonly List<VisualElement> _pool = new();
        private readonly bool _follow;
        private readonly float _followScale;
        private RoomRegistry _registry;
        private float _scale = 1f;
        private float _minX;
        private float _maxZ;
        private int _used;

        public MapView(bool follow, float followScale = 5f)
        {
            _follow = follow;
            _followScale = followScale;
            AddToClassList("map-view");
            pickingMode = PickingMode.Ignore;
            style.overflow = Overflow.Hidden;
            _content.style.position = Position.Absolute;
            Add(_content);
            _creature.AddToClassList("map-creature");
            _content.Add(_creature);
            RegisterCallback<GeometryChangedEvent>(_ => Rebuild());
        }

        public RoomRegistry Registry => _registry;
        public int ExploredShown { get; private set; }

        public void SetRegistry(RoomRegistry registry)
        {
            _registry = registry;
            Rebuild();
        }

        public void Rebuild()
        {
            _used = 0;
            ExploredShown = 0;
            if (_registry == null || float.IsNaN(resolvedStyle.width) || resolvedStyle.width <= 0f)
            {
                HideUnused();
                return;
            }
            var any = false;
            var min = Vector2.zero;
            var max = Vector2.zero;
            foreach (var room in _registry.Rooms)
            {
                if (!MapProgress.IsExplored(room.Scene))
                    continue;
                var lo = new Vector2(room.Center.x - room.Size.x * 0.5f, room.Center.z - room.Size.z * 0.5f);
                var hi = new Vector2(room.Center.x + room.Size.x * 0.5f, room.Center.z + room.Size.z * 0.5f);
                min = any ? Vector2.Min(min, lo) : lo;
                max = any ? Vector2.Max(max, hi) : hi;
                any = true;
            }
            if (!any)
            {
                min = Vector2.zero;
                max = Vector2.one;
            }
            var size = Vector2.Max(max - min, Vector2.one);
            _scale = _follow ? _followScale : Mathf.Min(resolvedStyle.width / size.x, resolvedStyle.height / size.y) * FitMargin;
            _minX = min.x;
            _maxZ = max.y;
            if (!_follow)
            {
                _content.style.left = (resolvedStyle.width - size.x * _scale) * 0.5f;
                _content.style.top = (resolvedStyle.height - size.y * _scale) * 0.5f;
            }
            var color = _registry.Color;
            foreach (var room in _registry.Rooms)
            {
                if (!MapProgress.IsExplored(room.Scene))
                    continue;
                ExploredShown++;
                if (room.MapShapes.Count == 0)
                {
                    Shape(color, true, room.Center.x - room.Size.x * 0.5f, room.Center.z + room.Size.z * 0.5f, room.Size.x, room.Size.z);
                    continue;
                }
                foreach (var shape in room.MapShapes)
                    Shape(color, shape.width * shape.height > 10f, shape.xMin, shape.yMax, shape.width, shape.height);
            }
            foreach (var marker in _registry.Markers)
            {
                if (!MapProgress.IsExplored(marker.Room))
                    continue;
                var element = Next(MarkerClass(marker));
                Place(element, marker.Position.x, marker.Position.z, 0f, 0f);
            }
            if (Biomass.HasDrop && Biomass.DropScene == _registry.RootScene)
                Place(Next("map-marker--biomass"), Biomass.DropPosition.x, Biomass.DropPosition.z, 0f, 0f);
            HideUnused();
            _creature.BringToFront();
            Tick();
        }

        public void Tick()
        {
            var creature = CombatTargets.Creature;
            var here = creature != null && RoomStreamer.Active != null && RoomStreamer.Active.Registry == _registry;
            _creature.style.display = here ? DisplayStyle.Flex : DisplayStyle.None;
            if (!here)
                return;
            var position = creature.transform.position;
            var x = (position.x - _minX) * _scale;
            var y = (_maxZ - position.z) * _scale;
            _creature.style.left = x;
            _creature.style.top = y;
            if (_follow)
            {
                _content.style.left = resolvedStyle.width * 0.5f - x;
                _content.style.top = resolvedStyle.height * 0.5f - y;
            }
        }

        private void Shape(Color color, bool bordered, float left, float top, float width, float height)
        {
            var element = Next("map-room");
            element.style.backgroundColor = new Color(color.r, color.g, color.b, 0.45f);
            element.EnableInClassList("map-room--cell", !bordered);
            element.style.borderTopColor = element.style.borderBottomColor = element.style.borderLeftColor = element.style.borderRightColor = color;
            Place(element, left, top, width * _scale, height * _scale);
        }

        private static string MarkerClass(MapMarker marker) => marker.Kind switch
        {
            MapMarkerKind.Nest => Nests.IsGrown(marker.Id) ? "map-marker--nest" : "map-marker--nest-site",
            MapMarkerKind.Relay => RelayDestroyed(marker.Id) ? "map-marker--relay-off" : "map-marker--relay",
            MapMarkerKind.SecurityDoor => "map-marker--door",
            MapMarkerKind.Gate => "map-marker--gate",
            _ => "map-marker--hoarder",
        };

        private static bool RelayDestroyed(string id)
        {
            var slash = id.IndexOf('/');
            return slash > 0 && SecurityRelays.IsDestroyed(id.Substring(0, slash), id.Substring(slash + 1));
        }

        private void Place(VisualElement element, float worldX, float worldZ, float width, float height)
        {
            element.style.left = (worldX - _minX) * _scale;
            element.style.top = (_maxZ - worldZ) * _scale;
            if (width > 0f)
            {
                element.style.width = width;
                element.style.height = height;
            }
        }

        private VisualElement Next(string className)
        {
            VisualElement element;
            if (_used < _pool.Count)
            {
                element = _pool[_used];
                element.ClearClassList();
                element.style.width = StyleKeyword.Null;
                element.style.height = StyleKeyword.Null;
                element.style.backgroundColor = StyleKeyword.Null;
            }
            else
            {
                element = new VisualElement { pickingMode = PickingMode.Ignore };
                element.style.position = Position.Absolute;
                _pool.Add(element);
                _content.Add(element);
            }
            element.AddToClassList(className.StartsWith("map-marker") ? "map-marker" : "map-room");
            element.AddToClassList(className);
            element.style.display = DisplayStyle.Flex;
            _used++;
            return element;
        }

        private void HideUnused()
        {
            for (var i = _used; i < _pool.Count; i++)
                _pool[i].style.display = DisplayStyle.None;
        }
    }
}
