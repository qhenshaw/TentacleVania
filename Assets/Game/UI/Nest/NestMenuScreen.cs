using System.Collections.Generic;
using TentacleVania.Abilities.Mutations;
using TentacleVania.Core.Input;
using TentacleVania.Core.Progression;
using TentacleVania.Creature;
using TentacleVania.Creature.Movement;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace TentacleVania.UI.Nest
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class NestMenuScreen : MonoBehaviour
    {
        private const string Table = "UI";
        private const string OpenClass = "nest--open";
        private const string ShownClass = "nest--shown";
        private const string HiddenClass = "nest__hidden";
        private const string ActiveTabClass = "nest__tab--active";

        private static readonly StatKind[] Kinds = { StatKind.Brutality, StatKind.Ferocity, StatKind.Tenacity, StatKind.Cunning };
        private static readonly string[] StatKeys = { "nest.stat.brutality", "nest.stat.ferocity", "nest.stat.tenacity", "nest.stat.cunning" };
        private static readonly string[] EffectKeys = { "nest.effect.brutality", "nest.effect.ferocity", "nest.effect.tenacity", "nest.effect.cunning" };
        private const string PointsKey = "nest.points";

        private readonly List<(LocalizedString text, LocalizedString.ChangeHandler handler)> _bindings = new();
        private readonly Dictionary<string, string> _formats = new();
        private readonly Label[] _values = new Label[4];
        private readonly Label[] _effects = new Label[4];
        private readonly Button[] _plus = new Button[4];
        private readonly Button[] _minus = new Button[4];
        private readonly RespecDraft _draft = new();

        private VisualElement _menu;
        private Label _biomass;
        private Label _next;
        private Label _level;
        private Label _points;
        private Button _respec;
        private Button _confirm;
        private Button _cancel;
        private Button _leave;
        private CreatureMotor _lockedMotor;
        private bool _respecMode;
        private bool _waitForRelease;
        private bool _mutationMode;
        private Button _tabStats;
        private Button _tabMutations;
        private VisualElement _statRows;
        private VisualElement _mutationRows;
        private Label _notches;

        public static NestMenuScreen Instance { get; private set; }

        public bool IsOpen { get; private set; }
        public bool InRespec => _respecMode;

        private void OnEnable()
        {
            Instance = this;
            var root = GetComponent<UIDocument>().rootVisualElement;
            _menu = root.Q<VisualElement>("nest-menu");
            _tabStats = root.Q<Button>("tab-stats");
            _tabMutations = root.Q<Button>("tab-mutations");
            Bind(_tabStats, "nest.tab.stats");
            Bind(_tabMutations, "nest.tab.mutations");
            _tabStats.clicked += () => ShowTab(false);
            _tabMutations.clicked += () => ShowTab(true);
            _statRows = root.Q<VisualElement>("rows");
            _mutationRows = root.Q<VisualElement>("mutation-rows");
            _notches = root.Q<Label>("notches");
            Bind(root.Q<Label>("biomass-label"), "nest.biomass");
            Bind(root.Q<Label>("next-label"), "nest.next");
            Bind(root.Q<Label>("level-label"), "nest.level");
            _biomass = root.Q<Label>("biomass-value");
            _next = root.Q<Label>("next-value");
            _level = root.Q<Label>("level-value");
            _points = root.Q<Label>("points");
            _respec = root.Q<Button>("respec");
            _confirm = root.Q<Button>("confirm");
            _cancel = root.Q<Button>("cancel");
            _leave = root.Q<Button>("leave");
            Bind(_respec, "nest.respec");
            Bind(_confirm, "nest.confirm");
            Bind(_cancel, "nest.cancel");
            Bind(_leave, "nest.leave");
            _respec.clicked += BeginRespec;
            _confirm.clicked += ConfirmRespec;
            _cancel.clicked += CancelRespec;
            _leave.clicked += Close;
            BuildRows(root.Q<VisualElement>("rows"));
            foreach (var key in EffectKeys)
                CacheFormat(key);
            CacheFormat(PointsKey);
            _menu.RegisterCallback<NavigationCancelEvent>(_ => Back());
            Nests.MenuRequested += OnMenuRequested;
            Stats.Changed += RefreshIfOpen;
            Biomass.Changed += OnBiomassChanged;
        }

        private void OnDisable()
        {
            Nests.MenuRequested -= OnMenuRequested;
            Stats.Changed -= RefreshIfOpen;
            Biomass.Changed -= OnBiomassChanged;
            foreach (var (text, handler) in _bindings)
                text.StringChanged -= handler;
            _bindings.Clear();
            if (IsOpen || _waitForRelease)
                Unlock();
            IsOpen = false;
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (_waitForRelease && !GameplayInput.Default.Grab.Held)
            {
                _waitForRelease = false;
                Unlock();
            }
        }

        public void Open()
        {
            if (IsOpen)
                return;
            IsOpen = true;
            _respecMode = false;
            _mutationMode = false;
            _menu.AddToClassList(OpenClass);
            _menu.schedule.Execute(() => _menu.AddToClassList(ShownClass));
            var spawned = CreatureSpawner.Spawned;
            if (_lockedMotor == null && spawned != null && spawned.TryGetComponent(out CreatureMotor motor))
            {
                _lockedMotor = motor;
                motor.LockControl();
            }
            Refresh();
            _plus[0].schedule.Execute(() => _plus[0].Focus());
        }

        public void Close()
        {
            if (!IsOpen)
                return;
            IsOpen = false;
            _respecMode = false;
            _menu.RemoveFromClassList(ShownClass);
            _menu.RemoveFromClassList(OpenClass);
            _waitForRelease = true;
        }

        public void ShowTab(bool mutations)
        {
            if (_respecMode)
                CancelRespec();
            _mutationMode = mutations;
            _tabStats.EnableInClassList(ActiveTabClass, !mutations);
            _tabMutations.EnableInClassList(ActiveTabClass, mutations);
            Refresh();
        }

        public void ToggleMutation(string id)
        {
            var controller = Mutations;
            if (controller == null)
                return;
            if (MutationLoadout.IsEquipped(id))
                MutationLoadout.Unequip(id);
            else
                controller.Equip(id);
            Refresh();
        }

        private static MutationController Mutations =>
            CreatureSpawner.Spawned != null ? CreatureSpawner.Spawned.GetComponent<MutationController>() : null;

        public void Back()
        {
            if (_respecMode)
                CancelRespec();
            else
                Close();
        }

        public void Plus(int index)
        {
            if (_respecMode)
                _draft.Add(Kinds[index]);
            else
                Stats.TryLevelUp(Kinds[index]);
            Refresh();
        }

        public void Minus(int index)
        {
            if (_respecMode)
                _draft.Remove(Kinds[index]);
            Refresh();
        }

        public void BeginRespec()
        {
            _respecMode = true;
            _draft.Begin(Stats.TotalLevel);
            Refresh();
            _plus[0].Focus();
        }

        public void ConfirmRespec()
        {
            if (!_draft.Apply())
                return;
            _respecMode = false;
            Refresh();
            _respec.Focus();
        }

        public void CancelRespec()
        {
            _respecMode = false;
            Refresh();
            _respec.Focus();
        }

        private void OnMenuRequested(string nest) => Open();

        private void OnBiomassChanged(int carried) => RefreshIfOpen();

        private void RefreshIfOpen()
        {
            if (IsOpen)
                Refresh();
        }

        private void Unlock()
        {
            if (_lockedMotor != null)
                _lockedMotor.UnlockControl();
            _lockedMotor = null;
        }

        private void Refresh()
        {
            _statRows.EnableInClassList(HiddenClass, _mutationMode);
            _mutationRows.EnableInClassList(HiddenClass, !_mutationMode);
            _notches.EnableInClassList(HiddenClass, !_mutationMode);
            if (_mutationMode)
            {
                RefreshMutations();
                return;
            }
            _biomass.text = Biomass.Carried.ToString("N0");
            _next.text = Stats.NextLevelCost.ToString("N0");
            _level.text = Stats.TotalLevel.ToString();
            var canAfford = Biomass.Carried >= Stats.NextLevelCost;
            for (var i = 0; i < Kinds.Length; i++)
            {
                var value = _respecMode ? _draft.Get(Kinds[i]) : Stats.Get(Kinds[i]);
                _values[i].text = value.ToString();
                _effects[i].text = Effect(i, value);
                _plus[i].SetEnabled(_respecMode ? _draft.Unassigned > 0 && value < StatRules.MaxValue : canAfford && value < StatRules.MaxValue);
                _minus[i].EnableInClassList(HiddenClass, !_respecMode);
                _minus[i].SetEnabled(value > StatRules.StartValue);
            }
            _points.EnableInClassList(HiddenClass, !_respecMode);
            if (_respecMode)
                _points.text = string.Format(Format(PointsKey), _draft.Unassigned);
            _respec.EnableInClassList(HiddenClass, _respecMode);
            _leave.EnableInClassList(HiddenClass, _respecMode);
            _confirm.EnableInClassList(HiddenClass, !_respecMode);
            _cancel.EnableInClassList(HiddenClass, !_respecMode);
            _confirm.SetEnabled(_draft.IsComplete);
        }

        private void RefreshMutations()
        {
            _points.AddToClassList(HiddenClass);
            _respec.AddToClassList(HiddenClass);
            _confirm.AddToClassList(HiddenClass);
            _cancel.AddToClassList(HiddenClass);
            _leave.RemoveFromClassList(HiddenClass);
            _mutationRows.Clear();
            var controller = Mutations;
            var used = controller != null ? controller.UsedNotches() : 0;
            _notches.text = string.Format(Text("nest.notches"), used, MutationLoadout.NotchPool);
            if (controller == null || MutationLoadout.Owned.Count == 0)
            {
                var none = new Label(Text("nest.mutations.none"));
                none.AddToClassList("nest__effect");
                _mutationRows.Add(none);
                return;
            }
            foreach (var id in MutationLoadout.Owned)
            {
                var definition = controller.Find(id);
                if (definition == null)
                    continue;
                var equipped = MutationLoadout.IsEquipped(id);
                var row = new VisualElement();
                row.AddToClassList("nest__row");
                var name = new Label(Text(definition.TextKey + ".name"));
                name.AddToClassList("nest__stat");
                var cost = new Label(new string('●', definition.Notches));
                cost.AddToClassList("nest__cost");
                var effect = new Label(Text(definition.TextKey + ".effect"));
                effect.AddToClassList("nest__effect");
                var button = new Button(() => ToggleMutation(id)) { text = Text(equipped ? "nest.unequip" : "nest.equip"), name = "mutation-" + id };
                button.AddToClassList("nest__action");
                button.SetEnabled(equipped || MutationLoadout.Fits(used, definition.Notches, MutationLoadout.NotchPool));
                row.Add(name);
                row.Add(cost);
                row.Add(effect);
                row.Add(button);
                _mutationRows.Add(row);
            }
        }

        private static string Text(string key) => LocalizationSettings.StringDatabase.GetLocalizedString(Table, key);

        private string Effect(int index, int value) => index switch
        {
            0 => string.Format(Format(EffectKeys[0]), StatRules.WeaponDamage(value).ToString("0.00")),
            1 => string.Format(Format(EffectKeys[1]), StatRules.AttackSpeed(value).ToString("0.00"), StatRules.DashRecovery(value).ToString("0.00")),
            2 => string.Format(Format(EffectKeys[2]), StatRules.MaxHealthBonus(value).ToString("0"), StatRules.DamageTaken(value).ToString("0.000")),
            _ => string.Format(Format(EffectKeys[3]), StatRules.ChargeGain(value).ToString("0.00"), StatRules.AbilityDamage(value).ToString("0.00")),
        };

        private void BuildRows(VisualElement rows)
        {
            rows.Clear();
            for (var i = 0; i < Kinds.Length; i++)
            {
                var index = i;
                var row = new VisualElement();
                row.AddToClassList("nest__row");
                var name = new Label();
                name.AddToClassList("nest__stat");
                Bind(name, StatKeys[i]);
                _values[i] = new Label();
                _values[i].AddToClassList("nest__value");
                _effects[i] = new Label();
                _effects[i].AddToClassList("nest__effect");
                _minus[i] = new Button(() => Minus(index)) { text = "−", name = $"minus-{i}" };
                _minus[i].AddToClassList("nest__step");
                _plus[i] = new Button(() => Plus(index)) { text = "+", name = $"plus-{i}" };
                _plus[i].AddToClassList("nest__step");
                row.Add(name);
                row.Add(_values[i]);
                row.Add(_effects[i]);
                row.Add(_minus[i]);
                row.Add(_plus[i]);
                rows.Add(row);
            }
        }

        private void Bind(TextElement element, string key)
        {
            var text = new LocalizedString(Table, key);
            LocalizedString.ChangeHandler handler = value => element.text = value;
            text.StringChanged += handler;
            _bindings.Add((text, handler));
        }

        private void CacheFormat(string key)
        {
            var text = new LocalizedString(Table, key);
            LocalizedString.ChangeHandler handler = value =>
            {
                _formats[key] = value;
                RefreshIfOpen();
            };
            text.StringChanged += handler;
            _bindings.Add((text, handler));
        }

        private string Format(string key) => _formats.TryGetValue(key, out var format) ? format : "{0}";
    }
}
