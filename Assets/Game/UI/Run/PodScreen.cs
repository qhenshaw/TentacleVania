using System.Collections.Generic;
using TentacleVania.Abilities;
using TentacleVania.Abilities.Mutations;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Runs;
using TentacleVania.Core.Saves;
using TentacleVania.Creature;
using TentacleVania.Creature.Movement;
using TentacleVania.UI.Fonts;
using TentacleVania.Weapons;
using TentacleVania.Weapons.Data;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace TentacleVania.UI.Run
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class PodScreen : MonoBehaviour
    {
        private const string Table = "UI";
        private const string OpenClass = "pick--open";
        private const int Rows = 3;

        private static readonly StatKind[] StatOrder = { StatKind.Brutality, StatKind.Ferocity, StatKind.Tenacity, StatKind.Cunning };

        private readonly List<WeaponDefinition> _weapons = new(16);
        private readonly List<AbilityDefinition> _abilities = new(16);
        private readonly int[] _picks = new int[Rows];
        private readonly Label[] _values = new Label[Rows];
        private VisualElement _pick;
        private VisualElement _rows;
        private Label _hint;
        private Button _breakout;
        private Button _call;
        private ContentCatalog _catalog;
        private CreatureMotor _lockedMotor;

        public static PodScreen Instance { get; private set; }

        public bool IsOpen { get; private set; }
        public bool Calling { get; private set; }
        public WeaponDefinition Weapon1 => Pick(_weapons, _picks[0]);
        public WeaponDefinition Weapon2 => Pick(_weapons, _picks[1]);
        public AbilityDefinition Ability => Pick(_abilities, _picks[2]);
        public IReadOnlyList<WeaponDefinition> Weapons => _weapons;
        public IReadOnlyList<AbilityDefinition> Abilities => _abilities;

        private void OnEnable()
        {
            Instance = this;
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.StretchToParentSize();
            root.pickingMode = PickingMode.Ignore;
            _pick = root.Q("pick");
            _rows = root.Q("rows");
            _hint = root.Q<Label>("hint");
            _breakout = root.Q<Button>("breakout");
            _call = root.Q<Button>("call");
            UiText.Bind(root.Q<Label>("title"), "run.pod.title");
            UiText.Bind(_breakout, "run.pod.breakout");
            _breakout.clicked += BreakOut;
            _call.clicked += ToggleCall;
            _pick.RegisterCallback<NavigationCancelEvent>(_ =>
            {
                if (Calling)
                    ToggleCall();
            });
            RunPod.Requested += Open;
            RunPod.BrokeOut += OnBrokeOut;
        }

        private void OnDisable()
        {
            RunPod.Requested -= Open;
            RunPod.BrokeOut -= OnBrokeOut;
            if (IsOpen)
                Close();
            if (Instance == this)
                Instance = null;
        }

        public void Open()
        {
            var spawned = CreatureSpawner.Spawned;
            if (spawned == null)
            {
                RunPod.BreakOut();
                return;
            }
            var mutations = spawned.GetComponent<MutationController>();
            _catalog = mutations != null ? mutations.Catalog : null;
            Gather();
            var weapons = spawned.GetComponent<WeaponController>();
            var abilities = spawned.GetComponent<AbilityController>();
            _picks[0] = IndexOr(_weapons, weapons != null ? weapons.Weapon(0) : null, 0);
            _picks[1] = IndexOr(_weapons, weapons != null ? weapons.Weapon(1) : null, -1);
            if (_picks[1] == _picks[0])
                _picks[1] = -1;
            _picks[2] = IndexOr(_abilities, abilities != null ? abilities.Equipped : null, _abilities.Count > 0 ? 0 : -1);
            IsOpen = true;
            Calling = false;
            _pick.AddToClassList(OpenClass);
            if (_lockedMotor == null && spawned.TryGetComponent(out CreatureMotor motor))
            {
                _lockedMotor = motor;
                motor.LockControl();
            }
            ShowLoadout();
        }

        public void Cycle(int row, int direction)
        {
            if (!IsOpen || Calling || row < 0 || row >= Rows)
                return;
            var count = row == 2 ? _abilities.Count : _weapons.Count;
            var other = row == 0 ? _picks[1] : row == 1 ? _picks[0] : int.MinValue;
            var pick = _picks[row];
            for (var tries = 0; tries <= count + 1; tries++)
            {
                pick += direction;
                if (pick >= count)
                    pick = -1;
                if (pick < -1)
                    pick = count - 1;
                if (row == 0 && pick < 0)
                    continue;
                if (pick >= 0 && pick == other)
                    continue;
                break;
            }
            _picks[row] = pick;
            RefreshLoadout();
        }

        public void ToggleCall()
        {
            if (!IsOpen)
                return;
            if (Calling)
            {
                var weapon1 = Weapon1;
                var weapon2 = Weapon2;
                var ability = Ability;
                Gather();
                _picks[0] = IndexOr(_weapons, weapon1, 0);
                _picks[1] = IndexOr(_weapons, weapon2, -1);
                _picks[2] = IndexOr(_abilities, ability, -1);
                ShowLoadout();
            }
            else
            {
                ShowCall();
            }
        }

        public bool BuyRank(StatKind kind)
        {
            var bought = MotherCall.BuyRank(kind);
            if (Calling)
                ShowCall();
            return bought;
        }

        public bool Unlock(UnlockKind kind, string item)
        {
            var stock = MotherCallStock.Loaded;
            var bought = stock != null && MotherCall.Unlock(stock.Find(kind, item));
            if (Calling)
                ShowCall();
            return bought;
        }

        public void BreakOut()
        {
            if (!IsOpen)
                return;
            Apply();
            RunPod.BreakOut();
        }

        private void OnBrokeOut()
        {
            if (IsOpen)
                Apply();
        }

        private void Apply()
        {
            var spawned = CreatureSpawner.Spawned;
            if (spawned != null)
            {
                if (spawned.TryGetComponent(out WeaponController weapons))
                {
                    weapons.Equip(0, Weapon1);
                    weapons.Equip(1, Weapon2);
                }
                if (spawned.TryGetComponent(out AbilityController abilities))
                    abilities.Equip(Ability);
            }
            Close();
        }

        private void Close()
        {
            IsOpen = false;
            Calling = false;
            _pick.RemoveFromClassList(OpenClass);
            if (_lockedMotor != null)
                _lockedMotor.UnlockControl();
            _lockedMotor = null;
        }

        private void Gather()
        {
            _weapons.Clear();
            _abilities.Clear();
            if (_catalog == null)
                return;
            var stock = MotherCallStock.Loaded;
            var meta = SaveSystem.Data.Meta;
            foreach (var entry in _catalog.Entries)
            {
                if (entry is WeaponDefinition weapon && MotherCall.IsUnlocked(meta, stock, UnlockKind.Weapon, weapon.name))
                    _weapons.Add(weapon);
                else if (entry is AbilityDefinition ability && ability.SlotAbility && MotherCall.IsUnlocked(meta, stock, UnlockKind.Ability, ability.name))
                    _abilities.Add(ability);
            }
        }

        private void ShowLoadout()
        {
            Calling = false;
            _hint.text = Text("run.pod.hint");
            _call.text = Text("run.call.open");
            _breakout.style.display = DisplayStyle.Flex;
            _rows.Clear();
            string[] keys = { "run.pod.weapon1", "run.pod.weapon2", "run.pod.ability" };
            for (var i = 0; i < Rows; i++)
            {
                var row = i;
                var line = Row();
                line.Add(Label(Text(keys[i]), "pod__label"));
                line.Add(Arrow("<", "prev-" + i, () => Cycle(row, -1)));
                var value = Label("", "pod__value");
                line.Add(value);
                line.Add(Arrow(">", "next-" + i, () => Cycle(row, 1)));
                _rows.Add(line);
                _values[i] = value;
            }
            RefreshLoadout();
            _breakout.schedule.Execute(() => _breakout.Focus());
        }

        private void RefreshLoadout()
        {
            var none = Text("run.pod.none");
            _values[0].text = Weapon1 != null ? Weapon1.DisplayName : none;
            _values[1].text = Weapon2 != null ? Weapon2.DisplayName : none;
            _values[2].text = Ability != null ? Ability.DisplayName : none;
        }

        private void ShowCall()
        {
            Calling = true;
            var meta = SaveSystem.Data.Meta;
            _hint.text = string.Format(Text("run.call.essence"), meta.Essence.ToString("N0"));
            _call.text = Text("run.call.back");
            _breakout.style.display = DisplayStyle.None;
            _rows.Clear();
            var scroll = new ScrollView();
            scroll.AddToClassList("pod__scroll");
            _rows.Add(scroll);
            foreach (var kind in StatOrder)
            {
                var stat = kind;
                var rank = MotherCall.Rank(meta, kind);
                var cost = MotherCall.RankCost(rank);
                var line = Row();
                line.Add(Label(Text("nest.stat." + kind.ToString().ToLowerInvariant()), "pod__label"));
                line.Add(Label(string.Format(Text("run.call.rank"), rank, MotherCall.MaxRank), "pod__value"));
                var buy = Buy(rank >= MotherCall.MaxRank ? Text("run.call.max") : string.Format(Text("run.call.buy"), cost), "rank-" + kind, () => BuyRank(stat));
                buy.SetEnabled(rank < MotherCall.MaxRank && meta.Essence >= cost);
                line.Add(buy);
                scroll.Add(line);
            }
            var stock = MotherCallStock.Loaded;
            if (stock != null)
                foreach (var entry in stock.Entries)
                {
                    if (entry.Cost <= 0)
                        continue;
                    var owned = MotherCall.IsUnlocked(meta, entry);
                    var kind = entry.Kind;
                    var item = entry.Item;
                    var line = Row();
                    line.Add(Label(Text("run.call.kind." + kind.ToString().ToLowerInvariant()), "pod__label"));
                    line.Add(Label(UnlockName(entry), "pod__value"));
                    var buy = Buy(owned ? Text("run.call.owned") : string.Format(Text("run.call.buy"), entry.Cost), "unlock-" + entry.Id, () => Unlock(kind, item));
                    buy.SetEnabled(!owned && meta.Essence >= entry.Cost);
                    line.Add(buy);
                    scroll.Add(line);
                }
            _call.schedule.Execute(() => _call.Focus());
        }

        private string UnlockName(MotherCallStock.Entry entry)
        {
            switch (entry.Kind)
            {
                case UnlockKind.Weapon:
                    var weapon = _catalog != null ? _catalog.Find<WeaponDefinition>(entry.Item) : null;
                    return weapon != null ? weapon.DisplayName : entry.Item;
                case UnlockKind.Ability:
                    var ability = _catalog != null ? _catalog.Find<AbilityDefinition>(entry.Item) : null;
                    return ability != null ? ability.DisplayName : entry.Item;
                case UnlockKind.StartingNotch:
                    return string.Format(Text("run.call.notch"), entry.Item);
                case UnlockKind.HoarderTier:
                    return string.Format(Text("run.call.hoarder"), entry.Item);
                default:
                    return Text("run.call.defiance");
            }
        }

        private static VisualElement Row()
        {
            var line = new VisualElement();
            line.AddToClassList("pod__row");
            return line;
        }

        private static Button Arrow(string text, string name, System.Action clicked)
        {
            var button = new Button(clicked) { text = text, name = name };
            button.AddToClassList("pod__arrow");
            return button;
        }

        private static Button Buy(string text, string name, System.Action clicked)
        {
            var button = new Button(clicked) { text = text, name = name };
            button.AddToClassList("pod__buy");
            return button;
        }

        private static Label Label(string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList(className);
            return label;
        }

        private static int IndexOr<TItem>(List<TItem> list, TItem item, int fallback) where TItem : Object
        {
            if (item != null)
                for (var i = 0; i < list.Count; i++)
                    if (list[i] == item)
                        return i;
            return Mathf.Min(fallback, list.Count - 1);
        }

        private static TItem Pick<TItem>(List<TItem> list, int index) where TItem : class => index >= 0 && index < list.Count ? list[index] : null;

        private static string Text(string key) => LocalizationSettings.StringDatabase.GetLocalizedString(Table, key);
    }
}
