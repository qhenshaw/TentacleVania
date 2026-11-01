using System;
using System.Collections.Generic;
using TentacleVania.Abilities;
using TentacleVania.Abilities.Mutations;
using TentacleVania.Audio;
using TentacleVania.Bosses;
using TentacleVania.Core.Feedback;
using TentacleVania.Core.Input;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Saves;
using TentacleVania.Core.Scenes;
using TentacleVania.Creature;
using TentacleVania.Creature.Movement;
using TentacleVania.UI.Lore;
using TentacleVania.UI.Nest;
using TentacleVania.Weapons;
using TentacleVania.World.Arenas;
using TentacleVania.World.Lore;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace TentacleVania.UI.Pause
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class PauseMenuScreen : MonoBehaviour
    {
        private const string Table = "UI";
        private const string OpenClass = "pause--open";
        private const string HiddenClass = "pause--hidden";
        private const string ActiveTabClass = "pause__tab--active";
        private static readonly string[] Tabs = { "map", "abilities", "mutations", "logs" };

        [SerializeField] private StyleSheet _mapStyle;

        private readonly List<Button> _tabButtons = new();
        private VisualElement _pause;
        private ScrollView _content;
        private VisualElement _confirm;
        private Label _confirmText;
        private Button _confirmYes;
        private Button _resume;
        private Button _settings;
        private Button _quitMenu;
        private Button _quitGame;
        private Action _onConfirm;
        private Focusable _focusAfterConfirm;
        private CreatureMotor _lockedMotor;
        private float _timeScale = 1f;
        private int _tab;
        private int _changedFrame = -10;
        private int _blockedFrame = -10;

        public static PauseMenuScreen Instance { get; private set; }

        public bool IsOpen { get; private set; }
        public bool ConfirmOpen => _confirm != null && !_confirm.ClassListContains(HiddenClass);

        public static bool InFight
        {
            get
            {
                if (BossEnemy.Active != null)
                    return true;
                var arena = ArenaEncounter.Active;
                return arena != null && arena.State is ArenaState.Spawning or ArenaState.Fighting or ArenaState.BetweenWaves;
            }
        }

        private void OnEnable()
        {
            Instance = this;
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.StretchToParentSize();
            root.pickingMode = PickingMode.Ignore;
            if (_mapStyle != null && !root.styleSheets.Contains(_mapStyle))
                root.styleSheets.Add(_mapStyle);
            _pause = root.Q("pause");
            _content = root.Q<ScrollView>("content");
            _confirm = root.Q("confirm");
            _confirmText = root.Q<Label>("confirm-text");
            Fonts.UiText.Bind(root.Q<Label>("title"), "pause.title");
            _resume = Button(root, "resume", "pause.resume", Close);
            _settings = Button(root, "settings", "pause.settings", OpenSettings);
            _quitMenu = Button(root, "quit-menu", "pause.quitMenu", () => AskQuit(false));
            _quitGame = Button(root, "quit-game", "pause.quitGame", () => AskQuit(true));
            _confirmYes = Button(root, "confirm-yes", "confirm.yes", Confirm);
            Button(root, "confirm-no", "confirm.no", CancelConfirm);
            var tabs = root.Q("tabs");
            tabs.Clear();
            _tabButtons.Clear();
            for (var i = 0; i < Tabs.Length; i++)
            {
                var index = i;
                var button = new Button(() => ShowTab(index)) { name = "tab-" + Tabs[i] };
                Fonts.UiText.Bind(button, "pause.tab." + Tabs[i]);
                button.AddToClassList("pause__tab");
                tabs.Add(button);
                _tabButtons.Add(button);
            }
            _pause.RegisterCallback<NavigationCancelEvent>(_ => Back());
        }

        private void OnDisable()
        {
            if (IsOpen)
                Close();
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            var frame = Time.frameCount;
            if (OtherMenuOpen())
                _blockedFrame = frame;
            if (!GameplayInput.Default.Pause.Pressed || frame - _blockedFrame <= 1 || frame == _changedFrame || SceneFlow.IsLoading)
                return;
            if (!IsOpen)
                Open();
            else
                Back();
        }

        public void Open()
        {
            if (IsOpen || SceneFlow.IsLoading)
                return;
            IsOpen = true;
            _changedFrame = Time.frameCount;
            HitStop.Cancel();
            Rumble.Stop();
            _timeScale = Time.timeScale;
            Time.timeScale = 0f;
            AudioMix.SetPaused(AudioMix.Sfx, true);
            var spawned = CreatureSpawner.Spawned;
            if (_lockedMotor == null && spawned != null && spawned.TryGetComponent(out CreatureMotor motor))
            {
                _lockedMotor = motor;
                motor.LockControl();
            }
            HideConfirm();
            _pause.AddToClassList(OpenClass);
            ShowTab(_tab);
            _resume.schedule.Execute(() => _resume.Focus());
        }

        public void Close()
        {
            if (!IsOpen)
                return;
            IsOpen = false;
            _changedFrame = Time.frameCount;
            HideConfirm();
            _pause.RemoveFromClassList(OpenClass);
            Time.timeScale = _timeScale;
            AudioMix.SetPaused(AudioMix.Sfx, false);
            if (_lockedMotor != null)
                _lockedMotor.UnlockControl();
            _lockedMotor = null;
        }

        public void Back()
        {
            if (Time.frameCount == _changedFrame)
                return;
            if (ConfirmOpen)
            {
                _changedFrame = Time.frameCount;
                CancelConfirm();
            }
            else
            {
                Close();
            }
        }

        public void ShowTab(int index)
        {
            _tab = Mathf.Clamp(index, 0, Tabs.Length - 1);
            for (var i = 0; i < _tabButtons.Count; i++)
                _tabButtons[i].EnableInClassList(ActiveTabClass, i == _tab);
            _content.Clear();
            switch (Tabs[_tab])
            {
                case "map":
                    FillMap();
                    break;
                case "abilities":
                    FillAbilities();
                    break;
                case "mutations":
                    FillMutations();
                    break;
                case "logs":
                    FillLogs();
                    break;
            }
        }

        public void OpenSettings()
        {
            var screen = Settings.SettingsScreen.Instance;
            if (screen == null)
                return;
            screen.Open(() =>
            {
                _changedFrame = Time.frameCount;
                _blockedFrame = Time.frameCount;
                _settings.Focus();
            });
        }

        public void AskQuit(bool toDesktop)
        {
            _focusAfterConfirm = toDesktop ? _quitGame : _quitMenu;
            _onConfirm = () => Quit(toDesktop);
            _confirmText.text = T(InFight ? "pause.quit.fight" : toDesktop ? "pause.quit.confirmGame" : "pause.quit.confirmMenu");
            _confirm.RemoveFromClassList(HiddenClass);
            _confirmYes.schedule.Execute(() => _confirmYes.Focus());
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
            if (_focusAfterConfirm != null)
                _focusAfterConfirm.Focus();
        }

        public void Quit(bool toDesktop)
        {
            if (!InFight && SaveSystem.Active)
                SaveSystem.Save();
            Close();
            Time.timeScale = 1f;
            if (!toDesktop)
            {
                SceneFlow.Load(SceneFlow.MainMenu);
                return;
            }
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void FillMap()
        {
            var title = new Label();
            title.AddToClassList("map__title");
            var info = new Label();
            info.AddToClassList("map__info");
            var registry = Map.MapScreen.CurrentRegistry();
            Map.MapScreen.FillHeader(title, info, registry);
            _content.Add(title);
            _content.Add(info);
            if (registry == null)
                return;
            var view = new Map.MapView(false);
            view.style.height = 470f;
            _content.Add(view);
            view.SetRegistry(registry);
        }

        private void FillLogs()
        {
            var library = LoreLibrary.Instance;
            var shown = 0;
            if (library != null)
            {
                foreach (var log in library.Logs)
                {
                    if (log == null || !LoreLogs.Has(log.Id))
                        continue;
                    var id = log.Id;
                    var button = new Button(() => LoreLogs.Read(id))
                    {
                        text = LocalizationSettings.StringDatabase.GetLocalizedString(LoreLog.Table, log.TitleKey),
                        name = "log-" + id,
                    };
                    button.AddToClassList("pause__button");
                    button.AddToClassList("pause__log");
                    _content.Add(button);
                    shown++;
                }
            }
            if (shown == 0)
                Note("pause.logs.empty");
        }

        private void FillAbilities()
        {
            var creature = CreatureSpawner.Spawned;
            var weapons = creature != null ? creature.GetComponent<WeaponController>() : null;
            var ability = creature != null ? creature.GetComponent<AbilityController>() : null;
            Heading("pause.loadout");
            if (weapons != null)
            {
                for (var slot = 0; slot < WeaponController.Slots; slot++)
                {
                    var weapon = weapons.Weapon(slot);
                    var modifier = weapons.Modifier(slot);
                    var name = weapon != null ? weapon.DisplayName : T("nest.empty");
                    if (modifier != null)
                        name += "  ·  " + string.Format(T("pause.modifier"), T(modifier.TextKey + ".name"));
                    Row(T(slot == 0 ? "nest.weapon1" : "nest.weapon2"), name);
                }
            }
            if (ability != null)
                Row(T("nest.ability"), ability.Equipped != null ? ability.Equipped.DisplayName : T("nest.empty"));
            Heading("pause.abilities");
            var unlocked = SaveSystem.Data.Build.Abilities;
            var shown = 0;
            foreach (var id in unlocked)
            {
                var definition = ability != null ? ability.Find(id) : null;
                if (definition == null)
                    continue;
                var kind = T(definition.SlotAbility ? "pause.ability.active" : "pause.ability.traversal");
                if (definition.MaxTier > 1)
                    kind += "  ·  " + string.Format(T("nest.upgrades.tier"), Upgrades.AbilityTier(id), definition.MaxTier);
                Row(definition.DisplayName, kind);
                shown++;
            }
            if (shown == 0)
                Note("pause.abilities.none");
        }

        private void FillMutations()
        {
            var creature = CreatureSpawner.Spawned;
            var controller = creature != null ? creature.GetComponent<MutationController>() : null;
            var used = controller != null ? controller.UsedNotches() : 0;
            Note(string.Format(T("nest.notches"), used, MutationLoadout.NotchPool), false);
            var shown = 0;
            if (controller != null)
            {
                for (var pass = 0; pass < 2; pass++)
                {
                    var equipped = pass == 0;
                    var headed = false;
                    foreach (var id in MutationLoadout.Owned)
                    {
                        if (MutationLoadout.IsEquipped(id) != equipped)
                            continue;
                        var definition = controller.Find(id);
                        if (definition == null)
                            continue;
                        if (!headed)
                        {
                            Heading(equipped ? "pause.mutations.equipped" : "pause.mutations.owned");
                            headed = true;
                        }
                        MutationRow(definition);
                        shown++;
                    }
                }
            }
            if (shown == 0)
                Note("pause.mutations.none");
        }

        private void MutationRow(MutationDefinition definition)
        {
            var row = new VisualElement();
            row.AddToClassList("pause__row");
            var name = new Label(T(definition.TextKey + ".name"));
            name.AddToClassList("pause__label");
            var cost = new Label(new string('●', definition.Notches));
            cost.AddToClassList("pause__cost");
            var effect = new Label(T(definition.TextKey + ".effect"));
            effect.AddToClassList("pause__value");
            row.Add(name);
            row.Add(cost);
            row.Add(effect);
            _content.Add(row);
        }

        private void Heading(string key)
        {
            var label = new Label(T(key));
            label.AddToClassList("pause__heading");
            _content.Add(label);
        }

        private void Row(string label, string value)
        {
            var row = new VisualElement();
            row.AddToClassList("pause__row");
            var left = new Label(label);
            left.AddToClassList("pause__label");
            var right = new Label(value);
            right.AddToClassList("pause__value");
            row.Add(left);
            row.Add(right);
            _content.Add(row);
        }

        private void Note(string keyOrText, bool isKey = true)
        {
            var label = new Label(isKey ? T(keyOrText) : keyOrText);
            label.AddToClassList("pause__note");
            _content.Add(label);
        }

        private void HideConfirm()
        {
            _onConfirm = null;
            _confirm.AddToClassList(HiddenClass);
        }

        private static bool OtherMenuOpen()
        {
            var settings = Settings.SettingsScreen.Instance;
            var nest = NestMenuScreen.Instance;
            var reader = LoreReaderScreen.Instance;
            var hoarder = Hoarder.HoarderScreen.Instance;
            return (settings != null && settings.IsOpen) || (nest != null && nest.IsOpen) || (reader != null && reader.IsOpen) || (hoarder != null && hoarder.IsOpen) || !GameplayInput.Default.Enabled;
        }

        private static Button Button(VisualElement root, string name, string key, Action onClick)
        {
            var button = root.Q<Button>(name);
            Fonts.UiText.Bind(button, key);
            button.clicked += onClick;
            return button;
        }

        private static string T(string key) => LocalizationSettings.StringDatabase.GetLocalizedString(Table, key);
    }
}
