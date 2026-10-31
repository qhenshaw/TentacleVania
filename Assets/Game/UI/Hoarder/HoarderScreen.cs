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

        public void Buy(string mutationId)
        {
            var entry = Find(mutationId);
            var definition = Mutation(mutationId);
            if (entry == null || definition == null)
                return;
            if (Shop.TryBuyMutation(mutationId, entry.Trinket, Price(entry, definition)))
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
                    var definition = Mutation(entry.Mutation);
                    if (definition == null || !Shop.InStock(entry.Mutation, entry.Trinket))
                        continue;
                    _rows.Add(Row(entry, definition));
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

        private VisualElement Row(StockEntry entry, MutationDefinition definition)
        {
            var price = Price(entry, definition);
            var row = new VisualElement();
            row.AddToClassList("hoarder__row");
            var head = new VisualElement();
            head.AddToClassList("hoarder__row-head");
            var name = new Label(T(Table, definition.TextKey + ".name"));
            name.AddToClassList("hoarder__name");
            var cost = new Label(new string('●', definition.Notches));
            cost.AddToClassList("hoarder__cost");
            var id = entry.Mutation;
            var buy = new Button(() => Buy(id)) { text = string.Format(T(Table, "hoarder.buy"), price.ToString("N0")), name = "buy-" + id };
            buy.AddToClassList("hoarder__button");
            buy.SetEnabled(Biomass.Carried >= price);
            head.Add(name);
            head.Add(cost);
            head.Add(buy);
            var effect = new Label(T(Table, definition.TextKey + ".effect"));
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
                if (Mutation(entry.Mutation) != null && Shop.InStock(entry.Mutation, entry.Trinket))
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

        private StockEntry Find(string mutationId)
        {
            if (_merchant == null || _merchant.Stock == null)
                return null;
            foreach (var entry in _merchant.Stock.Entries)
                if (entry.Mutation == mutationId)
                    return entry;
            return null;
        }

        private static int Price(StockEntry entry, MutationDefinition definition) => entry.Price > 0 ? entry.Price : Shop.MutationPrice(definition.Notches);

        private static MutationDefinition Mutation(string id)
        {
            var spawned = CreatureSpawner.Spawned;
            var controller = spawned != null ? spawned.GetComponent<MutationController>() : null;
            return controller != null ? controller.Find(id) : null;
        }

        private static string T(string table, string key) => LocalizationSettings.StringDatabase.GetLocalizedString(table, key);
    }
}
