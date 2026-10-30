using System.Collections.Generic;
using TentacleVania.Core.Debugging;
using TentacleVania.Core.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TentacleVania.UI.Debug
{
    public sealed class DebugMenuHost : MonoBehaviour
    {
        private const string PanelSettingsPath = "DebugMenu/DebugPanelSettings";
        private const string StyleSheetPath = "DebugMenu/DebugMenu";
        private const float RefreshInterval = 0.2f;

        private readonly List<(Label label, DebugEntry entry)> _values = new();
        private readonly List<(Toggle toggle, DebugEntry entry)> _toggles = new();
        private readonly List<(Slider slider, DebugEntry entry)> _sliders = new();

        private UIDocument _document;
        private VisualElement _panel;
        private TabView _tabs;
        private int _builtVersion = -1;
        private float _nextRefresh;

        public static DebugMenuHost Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (!UnityEngine.Debug.isDebugBuild || Instance != null)
                return;
            var settings = Resources.Load<PanelSettings>(PanelSettingsPath);
            if (settings == null)
                return;
            var go = new GameObject(nameof(DebugMenuHost));
            DontDestroyOnLoad(go);
            var document = go.AddComponent<UIDocument>();
            document.panelSettings = settings;
            document.sortingOrder = 1000;
            go.AddComponent<DebugMenuHost>();
        }

        public static void Toggle() => DebugMenu.IsOpen = !DebugMenu.IsOpen;

        private void Awake()
        {
            Instance = this;
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            DebugMenu.OpenChanged += OnOpenChanged;
            var root = _document.rootVisualElement;
            var style = Resources.Load<StyleSheet>(StyleSheetPath);
            if (style != null)
                root.styleSheets.Add(style);
            _panel = new VisualElement { name = "debug-menu" };
            _panel.AddToClassList("debug-menu");
            var title = new Label("SITE BETA // DEBUG   [F5]");
            title.AddToClassList("debug-menu__title");
            _panel.Add(title);
            _tabs = new TabView();
            _tabs.AddToClassList("debug-menu__tabs");
            _panel.Add(_tabs);
            root.Add(_panel);
            ApplyOpen(DebugMenu.IsOpen);
        }

        private void OnDisable() => DebugMenu.OpenChanged -= OnOpenChanged;

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f5Key.wasPressedThisFrame)
                Toggle();
            if (!DebugMenu.IsOpen)
                return;
            if (_builtVersion != DebugMenu.Version)
                Rebuild();
            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + RefreshInterval;
                Refresh();
            }
        }

        private void OnOpenChanged(bool open) => ApplyOpen(open);

        private void ApplyOpen(bool open)
        {
            _panel.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            GameplayInput.Default.Enabled = !open;
            if (open)
            {
                Rebuild();
                Refresh();
            }
        }

        private void Rebuild()
        {
            _builtVersion = DebugMenu.Version;
            var selected = _tabs.selectedTabIndex;
            _tabs.Clear();
            _values.Clear();
            _toggles.Clear();
            _sliders.Clear();
            foreach (var tabName in DebugMenu.Tabs)
            {
                var tab = new Tab(tabName);
                var scroll = new ScrollView(ScrollViewMode.Vertical);
                scroll.AddToClassList("debug-menu__page");
                foreach (var entry in DebugMenu.EntriesFor(tabName))
                    scroll.Add(BuildRow(entry));
                tab.Add(scroll);
                _tabs.Add(tab);
            }
            if (selected >= 0 && selected < DebugMenu.Tabs.Count)
                _tabs.selectedTabIndex = selected;
        }

        private VisualElement BuildRow(DebugEntry entry)
        {
            VisualElement row;
            switch (entry.Kind)
            {
                case DebugEntryKind.Button:
                    row = new Button(() => entry.OnClick?.Invoke()) { text = entry.Label };
                    break;
                case DebugEntryKind.Toggle:
                    var toggle = new Toggle(entry.Label);
                    toggle.RegisterValueChangedCallback(e => entry.SetBool?.Invoke(e.newValue));
                    _toggles.Add((toggle, entry));
                    row = toggle;
                    break;
                case DebugEntryKind.Slider:
                    var slider = new Slider(entry.Label, entry.Min, entry.Max) { showInputField = true };
                    slider.RegisterValueChangedCallback(e => entry.SetFloat?.Invoke(e.newValue));
                    _sliders.Add((slider, entry));
                    row = slider;
                    break;
                case DebugEntryKind.Choice:
                    var choice = new VisualElement();
                    choice.AddToClassList("debug-menu__choice");
                    var dropdown = new DropdownField(entry.Label, new List<string>(entry.Choices), 0);
                    choice.Add(dropdown);
                    choice.Add(new Button(() => entry.OnChoice?.Invoke(dropdown.index)) { text = "Go" });
                    row = choice;
                    break;
                default:
                    var value = new VisualElement();
                    value.AddToClassList("debug-menu__value");
                    value.Add(new Label(entry.Label));
                    var text = new Label();
                    text.AddToClassList("debug-menu__value-text");
                    value.Add(text);
                    _values.Add((text, entry));
                    row = value;
                    break;
            }
            row.AddToClassList("debug-menu__row");
            return row;
        }

        private void Refresh()
        {
            foreach (var (label, entry) in _values)
                label.text = entry.GetText?.Invoke() ?? string.Empty;
            foreach (var (toggle, entry) in _toggles)
                toggle.SetValueWithoutNotify(entry.GetBool?.Invoke() ?? false);
            foreach (var (slider, entry) in _sliders)
                slider.SetValueWithoutNotify(entry.GetFloat?.Invoke() ?? 0f);
        }
    }
}
