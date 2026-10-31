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
        public const float HoldTime = 0.3f;
        public const int RelaysRequired = 3;
        private const string Table = "UI";
        private const string SectorTable = "Dialogue";

        private VisualElement _full;
        private VisualElement _minimapFrame;
        private Label _title;
        private Label _info;
        private MapView _fullView;
        private MapView _minimap;
        private float _pressedAt = -1f;
        private bool _holding;

        public static MapScreen Instance { get; private set; }

        public bool FullOpen { get; private set; }
        public bool MinimapShown { get; private set; }
        public MapView FullView => _fullView;

        private void OnEnable()
        {
            Instance = this;
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.StretchToParentSize();
            root.pickingMode = PickingMode.Ignore;
            _full = root.Q("full");
            _minimapFrame = root.Q("minimap");
            _title = root.Q<Label>("title");
            _info = root.Q<Label>("info");
            root.Q<Label>("legend").text = T("map.legend");
            _fullView = new MapView(false);
            root.Q("frame").Add(_fullView);
            _fullView.style.flexGrow = 1f;
            _minimap = new MapView(true, 4f);
            _minimap.style.flexGrow = 1f;
            _minimapFrame.Add(_minimap);
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
                _pressedAt = -1f;
                SetMinimap(false);
                return;
            }
            var map = input.Map;
            if (map.Pressed)
                _pressedAt = Time.unscaledTime;
            if (_pressedAt >= 0f && map.Held && Time.unscaledTime - _pressedAt >= HoldTime && !FullOpen)
            {
                _holding = true;
                SetMinimap(true);
            }
            if (map.Released && _pressedAt >= 0f)
            {
                if (!_holding)
                    ToggleFull();
                _pressedAt = -1f;
                _holding = false;
                SetMinimap(false);
            }
            if (FullOpen)
                _fullView.Tick();
            if (MinimapShown)
                _minimap.Tick();
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

        public void SetMinimap(bool shown)
        {
            if (shown == MinimapShown)
                return;
            MinimapShown = shown;
            _minimapFrame.EnableInClassList("map__minimap--shown", shown);
            if (shown)
                _minimap.SetRegistry(CurrentRegistry());
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

        public static string SectorName(string sector)
        {
            var name = LocalizationSettings.StringDatabase.GetLocalizedString(SectorTable, "sector." + sector);
            return string.IsNullOrEmpty(name) || name.StartsWith("No translation", System.StringComparison.Ordinal) ? sector : name;
        }

        private void OnExplored(string room)
        {
            if (FullOpen)
                _fullView.Rebuild();
            if (MinimapShown)
                _minimap.Rebuild();
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
