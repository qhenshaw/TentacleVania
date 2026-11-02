using System.Collections.Generic;
using TentacleVania.Abilities.Mutations;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Runs;
using TentacleVania.Core.Saves;
using TentacleVania.Creature;
using TentacleVania.Creature.Movement;
using TentacleVania.UI.Run;
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
        private const string HealId = "run-heal";
        private const string OfferPrefix = "run-offer-";

        private readonly List<MutationDefinition> _runOffers = new(8);
        private readonly List<MutationDefinition> _pool = new(64);
        private readonly List<MutationBuild> _poolBuilds = new(64);
        private readonly List<int> _poolNotches = new(64);
        private readonly List<MutationBuild> _pickedBuilds = new(16);
        private readonly List<int> _picks = new(8);
        private int _runVisit = int.MinValue;

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
            if (RunSession.Active)
            {
                BuyRun(id);
                return;
            }
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
            if (RunSession.Active)
            {
                RefreshRun();
                return;
            }
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

        private void RefreshRun()
        {
            _rows.Add(Row(HealId, T(Table, "hoarder.run.heal"), T(Table, "hoarder.run.healEffect"), "", RunShop.HealPrice));
            StockRun();
            for (var i = 0; i < _runOffers.Count; i++)
            {
                if (RunShop.IsSold(i))
                    continue;
                var mutation = _runOffers[i];
                _rows.Add(Row(OfferPrefix + i, T(Table, mutation.TextKey + ".name"), T(Table, mutation.TextKey + ".effect"), new string('\u25CF', mutation.Notches), RunShop.MutationPrice(mutation.Notches)));
            }
        }

        private void StockRun()
        {
            if (_runVisit == RunShop.Visit)
                return;
            _runVisit = RunShop.Visit;
            _runOffers.Clear();
            var controller = Controller();
            if (controller == null || controller.Catalog == null)
                return;
            _pool.Clear();
            _poolBuilds.Clear();
            _poolNotches.Clear();
            foreach (var entry in controller.Catalog.Entries)
                if (entry is MutationDefinition definition && !MutationLoadout.IsEquipped(entry.name))
                {
                    _pool.Add(definition);
                    _poolBuilds.Add(definition.Builds);
                    _poolNotches.Add(definition.Notches);
                }
            _pickedBuilds.Clear();
            foreach (var id in MutationLoadout.Equipped)
            {
                var equipped = controller.Find(id);
                if (equipped != null)
                    _pickedBuilds.Add(equipped.Builds);
            }
            MutationOffers.Pick(RunShop.StockRng(), _poolBuilds, _poolNotches, false, _pickedBuilds, RunShop.Offers(SaveSystem.Data.Meta), _picks);
            foreach (var index in _picks)
                _runOffers.Add(_pool[index]);
        }

        private void BuyRun(string id)
        {
            if (id == HealId)
            {
                var creature = Combat.CombatTargets.Creature;
                if (creature != null && creature.Normalized < 1f && RunShop.TryBuyHeal())
                {
                    creature.Heal(creature.Max * RunShop.HealFraction);
                    Chitter(true);
                }
                else
                {
                    Chitter(false);
                }
                Refresh();
                FocusFirst();
                return;
            }
            if (!id.StartsWith(OfferPrefix) || !int.TryParse(id.Substring(OfferPrefix.Length), out var offer) || offer < 0 || offer >= _runOffers.Count)
                return;
            var mutation = _runOffers[offer];
            var controller = Controller();
            if (controller == null || !RunShop.TryBuy(offer, RunShop.MutationPrice(mutation.Notches)))
            {
                Chitter(false);
                Refresh();
                FocusFirst();
                return;
            }
            Chitter(true);
            Close();
            var pick = MutationPickScreen.Instance;
            if (pick != null)
            {
                pick.Open(controller, new[] { mutation });
                pick.Choose(0);
            }
            else
            {
                MutationLoadout.Grant(mutation.name);
                controller.Equip(mutation.name);
            }
        }

        private void Chitter(bool bought)
        {
            _line.text = T(LineTable, bought ? "hoarder.buy" : "hoarder.poor");
            if (bought && _merchant != null)
                _merchant.Chitter();
        }

        private static MutationController Controller()
        {
            var spawned = CreatureSpawner.Spawned;
            return spawned != null ? spawned.GetComponent<MutationController>() : null;
        }

        private bool AnyStock()
        {
            if (RunSession.Active)
                return true;
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
