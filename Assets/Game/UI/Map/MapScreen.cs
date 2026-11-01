using TentacleVania.Core.Input;
using TentacleVania.Core.Progression;
using TentacleVania.UI.Hoarder;
using TentacleVania.UI.Lore;
using TentacleVania.UI.Nest;
using TentacleVania.World.Map;
using TentacleVania.World.Streaming;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace TentacleVania.UI.Map
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class MapScreen : MonoBehaviour
    {
        public const int RelaysRequired = 3;
        private const string Table = "UI";
        private const string SectorTable = "Dialogue";

        private VisualElement _full;
        private VisualElement _overlayFrame;
        private Label _title;
        private Label _info;
        private MapView _fullView;
        private MapView _overlay;
        private bool _overlayWanted;

        public static MapScreen Instance { get; private set; }

        public bool FullOpen { get; private set; }
        public bool OverlayShown { get; private set; }
        public MapView FullView => _fullView;

        private void OnEnable()
        {
            Instance = this;
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.StretchToParentSize();
            root.pickingMode = PickingMode.Ignore;
            _full = root.Q("full");
            _overlayFrame = root.Q("overlay");
            _title = root.Q<Label>("title");
            _info = root.Q<Label>("info");
            BuildLegend(root.Q<Label>("legend"));
            _fullView = new MapView(false);
            root.Q("frame").Add(_fullView);
            _fullView.style.flexGrow = 1f;
            _overlay = new MapView(true, 6f);
            _overlay.style.flexGrow = 1f;
            _overlayFrame.Add(_overlay);
            _full.RegisterCallback<NavigationCancelEvent>(_ => CloseFull());
            MapProgress.Explored += OnExplored;
        }

        private void OnDisable()
        {
            MapProgress.Explored -= OnExplored;
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            var input = GameplayInput.Default;
            if (Blocked())
            {
                ShowOverlay(false);
                return;
            }
            if (input.Map.Pressed)
                ToggleFull();
            if (input.MapOverlay.Pressed && !FullOpen)
                _overlayWanted = !_overlayWanted;
            ShowOverlay(_overlayWanted && !FullOpen);
            if (FullOpen)
                _fullView.Tick();
            if (OverlayShown)
                _overlay.Tick();
        }

        public void ToggleFull()
        {
            if (FullOpen)
                CloseFull();
            else
                OpenFull();
        }

        public void OpenFull()
        {
            FullOpen = true;
            _full.AddToClassList("map__full--open");
            var registry = CurrentRegistry();
            FillHeader(_title, _info, registry);
            _fullView.SetRegistry(registry);
        }

        public void CloseFull()
        {
            FullOpen = false;
            _full.RemoveFromClassList("map__full--open");
        }

        public void SetOverlay(bool wanted) => _overlayWanted = wanted;

        private void ShowOverlay(bool shown)
        {
            if (shown == OverlayShown)
                return;
            OverlayShown = shown;
            _overlayFrame.EnableInClassList("map__overlay--shown", shown);
            if (shown)
                _overlay.SetRegistry(CurrentRegistry());
        }

        public static RoomRegistry CurrentRegistry()
        {
            if (RoomStreamer.Active != null)
                return RoomStreamer.Active.Registry;
            var atlas = MapAtlas.Instance;
            return atlas != null ? atlas.ForRootScene(SceneManager.GetActiveScene().name) : null;
        }

        public static void FillHeader(Label title, Label info, RoomRegistry registry)
        {
            if (registry == null)
            {
                title.text = T("map.title");
                title.style.color = new Color(0.6f, 0.9f, 0.7f);
                info.text = T("map.noData");
                return;
            }
            title.text = SectorName(registry.Sector);
            title.style.color = registry.Color;
            var hasRelays = false;
            foreach (var marker in registry.Markers)
                hasRelays |= marker.Kind == MapMarkerKind.Relay;
            info.text = hasRelays ? string.Format(T("map.relays"), SecurityRelays.Count(registry.Sector), RelaysRequired) : "";
        }

        private static readonly (string Icon, string Key)[] LegendItems =
        {
            ("map-marker--nest", "map.legend.nest"),
            ("map-marker--nest-site", "map.legend.nestSite"),
            ("map-marker--relay", "map.legend.relay"),
            ("map-marker--door", "map.legend.door"),
            ("map-marker--gate", "map.legend.gate"),
            ("map-marker--hoarder", "map.legend.hoarder"),
            ("map-marker--pickup", "map.legend.pickup"),
            ("map-marker--lore", "map.legend.lore"),
            ("map-marker--tunnel", "map.legend.tunnel"),
            ("map-marker--biomass", "map.legend.biomass"),
            ("map-creature", "map.legend.you"),
        };

        private static void BuildLegend(Label placeholder)
        {
            var legend = new VisualElement { name = "legend-items", pickingMode = PickingMode.Ignore };
            legend.AddToClassList("map__legend");
            foreach (var (icon, key) in LegendItems)
            {
                var item = new VisualElement { pickingMode = PickingMode.Ignore };
                item.AddToClassList("map-legend__item");
                var marker = new VisualElement { pickingMode = PickingMode.Ignore };
                if (icon != "map-creature")
                    marker.AddToClassList("map-marker");
                marker.AddToClassList(icon);
                marker.AddToClassList("map-legend__icon");
                var label = new Label { pickingMode = PickingMode.Ignore };
                label.AddToClassList("map-legend__label");
                Fonts.UiText.Bind(label, key);
                item.Add(marker);
                item.Add(label);
                legend.Add(item);
            }
            placeholder.parent.Insert(placeholder.parent.IndexOf(placeholder), legend);
            placeholder.RemoveFromHierarchy();
        }

        public static string SectorName(string sector)
        {
            var name = LocalizationSettings.StringDatabase.GetLocalizedString(SectorTable, "sector." + sector);
            return string.IsNullOrEmpty(name) || name.StartsWith("No translation", System.StringComparison.Ordinal) ? sector : name;
        }

        private void OnExplored(string room)
        {
            if (FullOpen)
                _fullView.Rebuild();
            if (OverlayShown)
                _overlay.Rebuild();
        }

        private static bool Blocked()
        {
            if (!GameplayInput.Default.Enabled || FastTravel.Phase != TravelPhase.Idle)
                return true;
            var pause = Pause.PauseMenuScreen.Instance;
            var nest = NestMenuScreen.Instance;
            var reader = LoreReaderScreen.Instance;
            var hoarder = HoarderScreen.Instance;
            var settings = Settings.SettingsScreen.Instance;
            return (pause != null && pause.IsOpen) || (nest != null && nest.IsOpen) || (reader != null && reader.IsOpen)
                || (hoarder != null && hoarder.IsOpen) || (settings != null && settings.IsOpen);
        }

        private static string T(string key) => LocalizationSettings.StringDatabase.GetLocalizedString(Table, key);
    }
}
