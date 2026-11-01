using TentacleVania.Abilities.Mutations;
using TentacleVania.Core.Progression;
using TentacleVania.Creature;
using TentacleVania.Creature.Movement;
using TentacleVania.World.Hoarder;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace TentacleVania.UI.Hoarder
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class HoarderScreen : MonoBehaviour
    {
        private const string Table = "UI";
        private const string LineTable = "Dialogue";
        private const string OpenClass = "hoarder--open";
        private const int GreetingCount = 3;

        private VisualElement _hoarder;
        private Label _line;
        private Label _biomass;
        private ScrollView _rows;
        private Button _leave;
        private HoarderMerchant _merchant;
        private CreatureMotor _lockedMotor;
        private int _greeting;

        public static HoarderScreen Instance { get; private set; }

        public bool IsOpen { get; private set; }
        public string CurrentLine => _line.text;

        private void OnEnable()
        {
            Instance = this;
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.StretchToParentSize();
            root.pickingMode = PickingMode.Ignore;
            _hoarder = root.Q("hoarder");
            _line = root.Q<Label>("line");
            _biomass = root.Q<Label>("biomass");
            _rows = root.Q<ScrollView>("rows");
            _leave = root.Q<Button>("leave");
            root.Q<Label>("title").text = T(Table, "hoarder.title");
            root.Q<Label>("speaker").text = T(LineTable, "hoarder.speaker");
            _leave.text = T(Table, "hoarder.leave");
            _leave.clicked += Close;
            _hoarder.RegisterCallback<NavigationCancelEvent>(_ => Close());
            HoarderMerchant.TradeRequested += Open;
            Biomass.Changed += OnBiomassChanged;
        }

        private void OnDisable()
        {
            HoarderMerchant.TradeRequested -= Open;
            Biomass.Changed -= OnBiomassChanged;
            if (IsOpen)
                Close();
            if (Instance == this)
                Instance = null;
        }

        public void Open(HoarderMerchant merchant)
        {
            _merchant = merchant;
            IsOpen = true;
            _hoarder.AddToClassList(OpenClass);
            var spawned = CreatureSpawner.Spawned;
            if (_lockedMotor == null && spawned != null && spawned.TryGetComponent(out CreatureMotor motor))
            {
                _lockedMotor = motor;
                motor.LockControl();
            }
            Refresh();
            _line.text = T(LineTable, AnyStock() ? "hoarder.greet." + (_greeting++ % GreetingCount + 1) : "hoarder.empty");
            _leave.schedule.Execute(FocusFirst);
        }

        public void Close()
        {
            if (!IsOpen)
                return;
            IsOpen = false;
            _hoarder.RemoveFromClassList(OpenClass);
            if (_lockedMotor != null)
                _lockedMotor.UnlockControl();
            _lockedMotor = null;
        }

        public void Buy(string id)
        {
            var entry = Find(id);
            if (entry == null || !Describe(entry, out _, out _, out _, out var price))
                return;
            if (Shop.TryBuy(entry.Kind, entry.Mutation, entry.Trinket, price))
            {
                _line.text = T(LineTable, "hoarder.buy");
                if (_merchant != null)
                    _merchant.Chitter();
            }
            else
            {
                _line.text = T(LineTable, "hoarder.poor");
            }
            Refresh();
            FocusFirst();
        }

        private void Refresh()
        {
            _biomass.text = string.Format(T(Table, "hoarder.biomass"), Biomass.Carried.ToString("N0"));
            _rows.Clear();
            var shown = 0;
            if (_merchant != null && _merchant.Stock != null)
            {
                foreach (var entry in _merchant.Stock.Entries)
                {
                    if (!_merchant.Sells(entry) || !Describe(entry, out var name, out var effect, out var cost, out var price))
                        continue;
                    _rows.Add(Row(entry.Mutation, name, effect, cost, price));
                    shown++;
                }
            }
            if (shown == 0)
            {
                var note = new Label(T(Table, "hoarder.soldOut"));
                note.AddToClassList("hoarder__note");
                _rows.Add(note);
            }
        }

        private VisualElement Row(string id, string nameText, string effectText, string costText, int price)
        {
            var row = new VisualElement();
            row.AddToClassList("hoarder__row");
            var head = new VisualElement();
            head.AddToClassList("hoarder__row-head");
            var name = new Label(nameText);
            name.AddToClassList("hoarder__name");
            var cost = new Label(costText);
            cost.AddToClassList("hoarder__cost");
            var buy = new Button(() => Buy(id)) { text = string.Format(T(Table, "hoarder.buy"), price.ToString("N0")), name = "buy-" + id };
            buy.AddToClassList("hoarder__button");
            buy.SetEnabled(Biomass.Carried >= price);
            head.Add(name);
            head.Add(cost);
            head.Add(buy);
            var effect = new Label(effectText);
            effect.AddToClassList("hoarder__effect");
            row.Add(head);
            row.Add(effect);
            return row;
        }

        private bool AnyStock()
        {
            if (_merchant == null || _merchant.Stock == null)
                return false;
            foreach (var entry in _merchant.Stock.Entries)
                if (_merchant.Sells(entry) && Describe(entry, out _, out _, out _, out _))
                    return true;
            return false;
        }

        private void FocusFirst()
        {
            var first = _rows.Q<Button>();
            if (first != null && first.enabledSelf)
                first.Focus();
            else
                _leave.Focus();
        }

        private void OnBiomassChanged(int carried)
        {
            if (IsOpen)
                Refresh();
        }

        private StockEntry Find(string id)
        {
            if (_merchant == null || _merchant.Stock == null)
                return null;
            foreach (var entry in _merchant.Stock.Entries)
                if (entry.Mutation == id)
                    return entry;
            return null;
        }

        private static bool Describe(StockEntry entry, out string name, out string effect, out string cost, out int price)
        {
            name = effect = cost = "";
            price = entry.Price;
            var catalog = Catalog();
            if (catalog == null)
                return false;
            switch (entry.Kind)
            {
                case PickupKind.Weapon:
                    var weapon = catalog.Find<Weapons.Data.WeaponDefinition>(entry.Mutation);
                    if (weapon == null || price <= 0)
                        return false;
                    name = weapon.DisplayName;
                    effect = T(Table, "hoarder.kind.weapon");
                    return true;
                case PickupKind.Ability:
                    var ability = catalog.Find<Abilities.AbilityDefinition>(entry.Mutation);
                    if (ability == null || price <= 0)
                        return false;
                    name = ability.DisplayName;
                    effect = T(Table, "hoarder.kind.ability");
                    return true;
                default:
                    var mutation = catalog.Find<MutationDefinition>(entry.Mutation);
                    if (mutation == null)
                        return false;
                    name = T(Table, mutation.TextKey + ".name");
                    effect = T(Table, mutation.TextKey + ".effect");
                    cost = new string('\u25CF', mutation.Notches);
                    if (price <= 0)
                        price = Shop.MutationPrice(mutation.Notches);
                    return true;
            }
        }

        private static Core.Saves.ContentCatalog Catalog()
        {
            var spawned = CreatureSpawner.Spawned;
            var controller = spawned != null ? spawned.GetComponent<MutationController>() : null;
            return controller != null ? controller.Catalog : null;
        }

        private static string T(string table, string key) => LocalizationSettings.StringDatabase.GetLocalizedString(table, key);
    }
}
