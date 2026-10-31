using System;
using System.Collections.Generic;
using TentacleVania.Core.Settings;
using TentacleVania.Creature;
using TentacleVania.Creature.Movement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace TentacleVania.UI.Settings
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class SettingsScreen : MonoBehaviour
    {
        private const string Table = "UI";
        private const string OpenClass = "settings--open";
        private const string ActiveTabClass = "settings__tab--active";
        private static readonly string[] Tabs = { "audio", "gameplay", "accessibility", "controls", "display", "language" };
        private static readonly string[] RebindActions = { "Weapon1", "Weapon2", "Dash", "Grab", "Ability" };
        private static readonly int[] FrameCaps = { 0, 30, 60, 120, 144 };

        private readonly List<Button> _tabButtons = new();
        private VisualElement _root;
        private ScrollView _content;
        private int _tab;
        private Action _onClose;
        private CreatureMotor _lockedMotor;
        private InputActionRebindingExtensions.RebindingOperation _rebind;

        public static SettingsScreen Instance { get; private set; }
        public bool IsOpen { get; private set; }

        private void OnEnable()
        {
            Instance = this;
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.StretchToParentSize();
            root.pickingMode = PickingMode.Ignore;
            _root = root.Q("settings");
            _content = root.Q<ScrollView>("content");
            root.Q<Label>("title").text = T("settings.title");
            var back = root.Q<Button>("back");
            back.text = T("settings.back");
            back.clicked += Close;
            var reset = root.Q<Button>("reset");
            reset.text = T("settings.reset");
            reset.clicked += ResetTab;
            var tabs = root.Q("tabs");
            tabs.Clear();
            _tabButtons.Clear();
            for (var i = 0; i < Tabs.Length; i++)
            {
                var index = i;
                var button = new Button(() => ShowTab(index)) { text = T("settings.tab." + Tabs[i]) };
                button.AddToClassList("settings__tab");
                tabs.Add(button);
                _tabButtons.Add(button);
            }
            _root.RegisterCallback<NavigationCancelEvent>(_ => Close());
        }

        private void OnDisable()
        {
            CancelRebind();
            if (IsOpen)
                Close();
            if (Instance == this)
                Instance = null;
        }

        public void Open(Action onClose = null)
        {
            _onClose = onClose;
            IsOpen = true;
            _root.AddToClassList(OpenClass);
            var spawned = CreatureSpawner.Spawned;
            if (_lockedMotor == null && spawned != null && spawned.TryGetComponent(out CreatureMotor motor))
            {
                _lockedMotor = motor;
                motor.LockControl();
            }
            ShowTab(_tab);
            _tabButtons[_tab].schedule.Execute(() => _tabButtons[_tab].Focus());
        }

        public void Close()
        {
            if (!IsOpen)
                return;
            CancelRebind();
            IsOpen = false;
            _root.RemoveFromClassList(OpenClass);
            SettingsStore.Save();
            if (_lockedMotor != null)
                _lockedMotor.UnlockControl();
            _lockedMotor = null;
            var onClose = _onClose;
            _onClose = null;
            onClose?.Invoke();
        }

        public void ShowTab(int index)
        {
            _tab = Mathf.Clamp(index, 0, Tabs.Length - 1);
            for (var i = 0; i < _tabButtons.Count; i++)
                _tabButtons[i].EnableInClassList(ActiveTabClass, i == _tab);
            _content.Clear();
            var data = SettingsStore.Data;
            switch (Tabs[_tab])
            {
                case "audio":
                    Slider("settings.master", data.MasterVolume, 0f, 1f, v => data.MasterVolume = v);
                    Slider("settings.music", data.MusicVolume, 0f, 1f, v => data.MusicVolume = v);
                    Slider("settings.sfx", data.SfxVolume, 0f, 1f, v => data.SfxVolume = v);
                    Slider("settings.voice", data.VoiceVolume, 0f, 1f, v => data.VoiceVolume = v);
                    break;
                case "gameplay":
                    Slider("settings.damageTaken", data.DamageTakenScale, 0.25f, 2f, v => data.DamageTakenScale = v);
                    Slider("settings.damageDealt", data.DamageDealtScale, 0.5f, 3f, v => data.DamageDealtScale = v);
                    Slider("settings.enemySpeed", data.EnemySpeedScale, 0.5f, 1f, v => data.EnemySpeedScale = v);
                    Toggle("settings.keepBiomass", data.KeepBiomassOnDeath, v => data.KeepBiomassOnDeath = v);
                    break;
                case "accessibility":
                    Toggle("settings.shake", data.ScreenShake, v => data.ScreenShake = v);
                    Toggle("settings.flashes", data.Flashes, v => data.Flashes = v);
                    Toggle("settings.gore", data.Gore, v => data.Gore = v);
                    Toggle("settings.rumble", data.Rumble, v => data.Rumble = v);
                    Toggle("settings.subtitles", data.Subtitles, v => data.Subtitles = v);
                    Choice("settings.colorblind", new[] { "settings.colorblind.off", "settings.colorblind.protanopia", "settings.colorblind.deuteranopia", "settings.colorblind.tritanopia" },
                        (int)data.Colorblind, i => data.Colorblind = (ColorblindMode)i);
                    break;
                case "controls":
                    foreach (var action in RebindActions)
                        BindingRow(action);
                    break;
                case "display":
                    Choice("settings.displayMode", new[] { "settings.mode.borderless", "settings.mode.exclusive", "settings.mode.windowed" },
                        (int)data.DisplayMode, i => data.DisplayMode = (DisplayModeSetting)i);
                    ResolutionRow(data);
                    Toggle("settings.vsync", data.VSync, v => data.VSync = v);
                    var caps = new string[FrameCaps.Length];
                    for (var i = 0; i < caps.Length; i++)
                        caps[i] = FrameCaps[i] == 0 ? T("settings.cap.unlimited") : FrameCaps[i].ToString();
                    ChoiceRaw("settings.frameCap", caps, Mathf.Max(0, Array.IndexOf(FrameCaps, data.FrameCap)), i => data.FrameCap = FrameCaps[i]);
                    break;
                case "language":
                    LanguageRow(data);
                    break;
            }
        }

        private void ResetTab()
        {
            if (Tabs[_tab] == "controls")
            {
                var actions = InputSystem.actions;
                if (actions != null)
                    actions.RemoveAllBindingOverrides();
                SettingsStore.Save();
            }
            else
            {
                var fresh = new SettingsData();
                var data = SettingsStore.Data;
                switch (Tabs[_tab])
                {
                    case "audio":
                        data.MasterVolume = fresh.MasterVolume;
                        data.MusicVolume = fresh.MusicVolume;
                        data.SfxVolume = fresh.SfxVolume;
                        data.VoiceVolume = fresh.VoiceVolume;
                        break;
                    case "gameplay":
                        data.DamageTakenScale = fresh.DamageTakenScale;
                        data.DamageDealtScale = fresh.DamageDealtScale;
                        data.EnemySpeedScale = fresh.EnemySpeedScale;
                        data.KeepBiomassOnDeath = fresh.KeepBiomassOnDeath;
                        break;
                    case "accessibility":
                        data.ScreenShake = fresh.ScreenShake;
                        data.Flashes = fresh.Flashes;
                        data.Gore = fresh.Gore;
                        data.Rumble = fresh.Rumble;
                        data.Subtitles = fresh.Subtitles;
                        data.Colorblind = fresh.Colorblind;
                        break;
                    case "display":
                        data.DisplayMode = fresh.DisplayMode;
                        data.Width = 0;
                        data.Height = 0;
                        data.VSync = fresh.VSync;
                        data.FrameCap = fresh.FrameCap;
                        break;
                }
                SettingsStore.Apply();
            }
            ShowTab(_tab);
        }

        private VisualElement Row(string key)
        {
            var row = new VisualElement();
            row.AddToClassList("settings__row");
            var label = new Label(T(key));
            label.AddToClassList("settings__label");
            row.Add(label);
            _content.Add(row);
            return row;
        }

        private void Slider(string key, float value, float min, float max, Action<float> set)
        {
            var slider = new Slider(min, max) { value = value, showInputField = false };
            slider.AddToClassList("settings__control");
            slider.RegisterValueChangedCallback(e =>
            {
                set(e.newValue);
                SettingsStore.Apply();
            });
            Row(key).Add(slider);
        }

        private void Toggle(string key, bool value, Action<bool> set)
        {
            var toggle = new Toggle { value = value };
            toggle.AddToClassList("settings__control");
            toggle.RegisterValueChangedCallback(e =>
            {
                set(e.newValue);
                SettingsStore.Apply();
            });
            Row(key).Add(toggle);
        }

        private void Choice(string key, string[] optionKeys, int index, Action<int> set)
        {
            var options = new string[optionKeys.Length];
            for (var i = 0; i < options.Length; i++)
                options[i] = T(optionKeys[i]);
            ChoiceRaw(key, options, index, set);
        }

        private void ChoiceRaw(string key, string[] options, int index, Action<int> set)
        {
            var dropdown = new DropdownField(new List<string>(options), Mathf.Clamp(index, 0, options.Length - 1));
            dropdown.AddToClassList("settings__control");
            dropdown.RegisterValueChangedCallback(_ =>
            {
                set(dropdown.index);
                SettingsStore.Apply();
            });
            Row(key).Add(dropdown);
        }

        private void ResolutionRow(SettingsData data)
        {
            var sizes = new List<Vector2Int>();
            foreach (var resolution in Screen.resolutions)
            {
                var size = new Vector2Int(resolution.width, resolution.height);
                if (!sizes.Contains(size))
                    sizes.Add(size);
            }
            if (sizes.Count == 0)
                sizes.Add(new Vector2Int(Screen.width, Screen.height));
            var options = new string[sizes.Count + 1];
            options[0] = T("settings.resolution.native");
            var current = 0;
            for (var i = 0; i < sizes.Count; i++)
            {
                options[i + 1] = $"{sizes[i].x} × {sizes[i].y}";
                if (sizes[i].x == data.Width && sizes[i].y == data.Height)
                    current = i + 1;
            }
            ChoiceRaw("settings.resolution", options, current, i =>
            {
                data.Width = i == 0 ? 0 : sizes[i - 1].x;
                data.Height = i == 0 ? 0 : sizes[i - 1].y;
            });
        }

        private void LanguageRow(SettingsData data)
        {
            var locales = LocalizationSettings.AvailableLocales.Locales;
            var options = new string[locales.Count];
            var current = 0;
            for (var i = 0; i < locales.Count; i++)
            {
                options[i] = locales[i].Identifier.CultureInfo != null ? locales[i].Identifier.CultureInfo.NativeName : locales[i].LocaleName;
                if (locales[i] == LocalizationSettings.SelectedLocale)
                    current = i;
            }
            ChoiceRaw("settings.language", options, current, i => data.Language = locales[i].Identifier.Code);
        }

        private void BindingRow(string actionName)
        {
            var action = InputSystem.actions != null ? InputSystem.actions.FindAction("Gameplay/" + actionName) : null;
            if (action == null)
                return;
            var row = Row("action." + actionName);
            foreach (var group in new[] { "Gamepad", "Keyboard&Mouse" })
            {
                var index = action.GetBindingIndex(group: group);
                if (index < 0)
                    continue;
                var button = new Button { text = action.GetBindingDisplayString(index) };
                button.AddToClassList("settings__binding");
                button.clicked += () => StartRebind(action, index, button, group);
                row.Add(button);
            }
        }

        private void StartRebind(InputAction action, int index, Button button, string group)
        {
            CancelRebind();
            button.text = T("settings.rebind.prompt");
            var wasEnabled = action.enabled;
            action.Disable();
            var operation = action.PerformInteractiveRebinding(index)
                .WithCancelingThrough("<Keyboard>/escape")
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .WithControlsExcluding("<Pointer>/position")
                .OnMatchWaitForAnother(0.1f);
            operation = group == "Gamepad" ? operation.WithControlsHavingToMatchPath("<Gamepad>") : operation.WithControlsExcluding("<Gamepad>");
            _rebind = operation
                .OnComplete(op => FinishRebind(op, action, index, button, wasEnabled))
                .OnCancel(op => FinishRebind(op, action, index, button, wasEnabled))
                .Start();
        }

        private void FinishRebind(InputActionRebindingExtensions.RebindingOperation operation, InputAction action, int index, Button button, bool wasEnabled)
        {
            operation.Dispose();
            if (_rebind == operation)
                _rebind = null;
            if (wasEnabled)
                action.Enable();
            button.text = action.GetBindingDisplayString(index);
            SettingsStore.Save();
        }

        private void CancelRebind()
        {
            if (_rebind == null)
                return;
            _rebind.Cancel();
            _rebind = null;
        }

        private static string T(string key) => LocalizationSettings.StringDatabase.GetLocalizedString(Table, key);
    }
}
