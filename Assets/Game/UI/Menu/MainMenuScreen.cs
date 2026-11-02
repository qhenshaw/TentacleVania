using System;
using System.Collections.Generic;
using TentacleVania.Core.Saves;
using TentacleVania.Core.Scenes;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace TentacleVania.UI.Menu
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class MainMenuScreen : MonoBehaviour
    {
        private const string Table = "UI";
        private const string SectorTable = "Dialogue";
        private const string HiddenClass = "main-menu--hidden";
        private const string NewGameArgument = "-newGame";

        private static bool _commandLineHandled;

        private readonly List<(LocalizedString text, LocalizedString.ChangeHandler handler)> _bindings = new();
        private readonly List<Button> _slotButtons = new();
        private VisualElement _buttons;
        private VisualElement _slots;
        private VisualElement _slotList;
        private Label _slotsTitle;
        private VisualElement _confirm;
        private Label _confirmText;
        private Button _confirmYes;
        private Button _continue;
        private Button _newGame;
        private Button _loadGame;
        private SaveSummary[] _summaries = Array.Empty<SaveSummary>();
        private bool _newGameMode;
        private bool _starting;
        private Action _onConfirm;
        private Focusable _focusAfterConfirm;

        public static MainMenuScreen Instance { get; private set; }

        public bool SlotsOpen => !_slots.ClassListContains(HiddenClass);
        public bool ConfirmOpen => !_confirm.ClassListContains(HiddenClass);

        private void OnEnable()
        {
            Instance = this;
            _starting = false;
            var root = GetComponent<UIDocument>().rootVisualElement;
            Bind(root.Q<Label>("title"), "menu.title");
            _buttons = root.Q("buttons");
            _continue = Button(root, "continue", "menu.continue", Continue);
            _newGame = Button(root, "new-game", "menu.newGame", () => OpenSlots(true));
            _loadGame = Button(root, "load-game", "menu.loadGame", () => OpenSlots(false));
            Button(root, "settings", "menu.settings", OpenSettings);
            Button(root, "quit", "menu.quit", Quit);
            _slots = root.Q("slots");
            _slotList = root.Q("slot-list");
            _slotsTitle = root.Q<Label>("slots-title");
            Button(root, "slots-back", "slot.back", CloseSlots);
            _confirm = root.Q("confirm");
            _confirmText = root.Q<Label>("confirm-text");
            _confirmYes = Button(root, "confirm-yes", "confirm.yes", Confirm);
            Button(root, "confirm-no", "confirm.no", CancelConfirm);
            root.Q("main-menu").RegisterCallback<NavigationCancelEvent>(_ => Back());
            Refresh();
            FocusMain();
        }

        private void Update()
        {
            if (_commandLineHandled || SceneFlow.IsLoading)
                return;
            _commandLineHandled = true;
            if (Array.IndexOf(Environment.GetCommandLineArgs(), NewGameArgument) < 0)
                return;
            SaveSystem.Storage = new LocalFileStorage(System.IO.Path.Combine(Application.persistentDataPath, "CommandLineSaves"));
            StartNew(0);
        }

        private void OnDisable()
        {
            foreach (var (text, handler) in _bindings)
                text.StringChanged -= handler;
            _bindings.Clear();
            if (Instance == this)
                Instance = null;
        }

        public void Refresh()
        {
            _summaries = SaveSlots.All();
            var any = SaveSlots.MostRecent(_summaries) >= 0;
            _continue.style.display = any ? DisplayStyle.Flex : DisplayStyle.None;
            _loadGame.SetEnabled(any);
        }

        public void Play()
        {
            Refresh();
            if (SaveSlots.MostRecent(_summaries) >= 0)
                Continue();
            else
                StartNew(Math.Max(0, SaveSlots.FirstEmpty(_summaries)));
        }

        public void Continue()
        {
            var slot = SaveSlots.MostRecent(_summaries);
            if (slot >= 0)
                LoadSlot(slot);
        }

        public void OpenSlots(bool newGame)
        {
            _newGameMode = newGame;
            Refresh();
            _slotsTitle.text = T(newGame ? "slot.title.new" : "slot.title.load");
            _slotList.Clear();
            _slotButtons.Clear();
            foreach (var summary in _summaries)
                _slotList.Add(SlotRow(summary));
            _buttons.AddToClassList(HiddenClass);
            _slots.RemoveFromClassList(HiddenClass);
            var first = _slotButtons.Find(button => button.enabledSelf);
            if (first != null)
                first.schedule.Execute(() => first.Focus());
        }

        public void CloseSlots()
        {
            _slots.AddToClassList(HiddenClass);
            _buttons.RemoveFromClassList(HiddenClass);
            Refresh();
            FocusMain();
        }

        public void ChooseSlot(int slot)
        {
            var summary = _summaries[slot];
            if (!_newGameMode)
            {
                if (summary.Exists)
                    LoadSlot(slot);
                return;
            }
            if (!summary.Exists)
            {
                StartNew(slot);
                return;
            }
            Ask(string.Format(T("confirm.overwrite"), slot + 1), () =>
            {
                SaveSystem.Delete(slot);
                StartNew(slot);
            });
        }

        public void DeleteSlot(int slot)
        {
            Ask(string.Format(T("confirm.delete"), slot + 1), () =>
            {
                SaveSystem.Delete(slot);
                OpenSlots(_newGameMode);
            });
        }

        public void Confirm()
        {
            var action = _onConfirm;
            HideConfirm();
            action?.Invoke();
        }

        public void CancelConfirm()
        {
            HideConfirm();
            var focus = _focusAfterConfirm;
            if (focus != null)
                focus.Focus();
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void OpenSettings()
        {
            var screen = Settings.SettingsScreen.Instance;
            if (screen != null)
                screen.Open(FocusMain);
        }

        private void Back()
        {
            if (ConfirmOpen)
                CancelConfirm();
            else if (SlotsOpen)
                CloseSlots();
        }

        private void LoadSlot(int slot)
        {
            if (_starting || !SaveSystem.Load(slot))
                return;
            _starting = true;
            SceneFlow.Load(SaveSystem.RunScene);
        }

        private void StartNew(int slot)
        {
            if (_starting)
                return;
            _starting = true;
            SaveSystem.NewGame(slot);
            SaveSystem.Save();
            SceneFlow.Load(SaveSystem.RunScene);
        }

        private VisualElement SlotRow(SaveSummary summary)
        {
            var row = new VisualElement();
            row.AddToClassList("main-menu__slot-row");
            var slot = summary.Slot;
            var button = new Button(() => ChooseSlot(slot));
            button.AddToClassList("main-menu__slot");
            var name = new Label(string.Format(T("slot.name"), slot + 1));
            name.AddToClassList("main-menu__slot-name");
            var details = new Label(summary.Exists
                ? string.Format(T("slot.details"), SectorName(summary.Sector), summary.Level, SaveSlots.Playtime(summary.PlaytimeSeconds))
                : T("slot.empty"));
            details.AddToClassList("main-menu__slot-details");
            button.Add(name);
            button.Add(details);
            button.SetEnabled(_newGameMode || summary.Exists);
            row.Add(button);
            _slotButtons.Add(button);
            if (summary.Exists)
            {
                var delete = new Button(() => DeleteSlot(slot)) { text = T("slot.delete") };
                delete.AddToClassList("main-menu__button");
                delete.AddToClassList("main-menu__slot-delete");
                row.Add(delete);
            }
            return row;
        }

        private void Ask(string text, Action onConfirm)
        {
            _focusAfterConfirm = _slots.focusController?.focusedElement as Focusable;
            _onConfirm = onConfirm;
            _confirmText.text = text;
            _confirm.RemoveFromClassList(HiddenClass);
            _confirmYes.schedule.Execute(() => _confirmYes.Focus());
        }

        private void HideConfirm()
        {
            _onConfirm = null;
            _confirm.AddToClassList(HiddenClass);
        }

        private void FocusMain()
        {
            var target = _continue.style.display == DisplayStyle.None ? _newGame : _continue;
            target.schedule.Execute(() => target.Focus());
        }

        private static string SectorName(string sector)
        {
            if (string.IsNullOrEmpty(sector))
                return T("slot.sector.unknown");
            var name = LocalizationSettings.StringDatabase.GetLocalizedString(SectorTable, "sector." + sector);
            return string.IsNullOrEmpty(name) || name.StartsWith("No translation", StringComparison.Ordinal) ? T("slot.sector.unknown") : name;
        }

        private Button Button(VisualElement root, string name, string key, Action onClick)
        {
            var button = root.Q<Button>(name);
            Bind(button, key);
            button.clicked += onClick;
            return button;
        }

        private void Bind(TextElement element, string key)
        {
            var text = new LocalizedString(Table, key);
            LocalizedString.ChangeHandler handler = value => element.text = value;
            text.StringChanged += handler;
            _bindings.Add((text, handler));
        }

        private static string T(string key) => LocalizationSettings.StringDatabase.GetLocalizedString(Table, key);
    }
}
