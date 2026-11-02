using System.Collections.Generic;
using TentacleVania.Abilities.Mutations;
using TentacleVania.Core.Progression;
using TentacleVania.Creature;
using TentacleVania.Creature.Movement;
using TentacleVania.UI.Fonts;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace TentacleVania.UI.Run
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class MutationPickScreen : MonoBehaviour
    {
        private const string Table = "UI";
        private const string OpenClass = "pick--open";
        private const string MarkedClass = "pick__card--marked";

        private readonly List<MutationDefinition> _offers = new(4);
        private readonly List<string> _marked = new(8);
        private VisualElement _pick;
        private Label _notches;
        private Label _hint;
        private VisualElement _cards;
        private Button _confirm;
        private Button _back;
        private MutationController _controller;
        private MutationDefinition _pending;
        private CreatureMotor _lockedMotor;

        public static MutationPickScreen Instance { get; private set; }

        public bool IsOpen { get; private set; }
        public bool Swapping => _pending != null;
        public IReadOnlyList<MutationDefinition> Offers => _offers;

        private void OnEnable()
        {
            Instance = this;
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.StretchToParentSize();
            root.pickingMode = PickingMode.Ignore;
            _pick = root.Q("pick");
            _notches = root.Q<Label>("notches");
            _hint = root.Q<Label>("hint");
            _cards = root.Q("cards");
            _confirm = root.Q<Button>("confirm");
            _back = root.Q<Button>("back");
            UiText.Bind(root.Q<Label>("title"), "run.pick.title");
            UiText.Bind(_confirm, "run.pick.replace");
            _confirm.clicked += Confirm;
            _back.clicked += Back;
            _pick.RegisterCallback<NavigationCancelEvent>(_ => Back());
        }

        private void OnDisable()
        {
            if (IsOpen)
                Close();
            if (Instance == this)
                Instance = null;
        }

        public void Open(MutationController controller, IReadOnlyList<MutationDefinition> offers)
        {
            _controller = controller;
            _offers.Clear();
            foreach (var offer in offers)
                _offers.Add(offer);
            _pending = null;
            _marked.Clear();
            IsOpen = true;
            _pick.AddToClassList(OpenClass);
            var spawned = CreatureSpawner.Spawned;
            if (_lockedMotor == null && spawned != null && spawned.TryGetComponent(out CreatureMotor motor))
            {
                _lockedMotor = motor;
                motor.LockControl();
            }
            Refresh();
        }

        public void Close()
        {
            if (!IsOpen)
                return;
            IsOpen = false;
            _pending = null;
            _pick.RemoveFromClassList(OpenClass);
            if (_lockedMotor != null)
                _lockedMotor.UnlockControl();
            _lockedMotor = null;
        }

        public void Choose(int index)
        {
            if (!IsOpen || Swapping || index < 0 || index >= _offers.Count)
                return;
            var offer = _offers[index];
            MutationLoadout.Grant(offer.name);
            if (_controller.Equip(offer.name))
            {
                Close();
                return;
            }
            _pending = offer;
            _marked.Clear();
            Refresh();
        }

        public void Toggle(string id)
        {
            if (!Swapping)
                return;
            if (!_marked.Remove(id))
                _marked.Add(id);
            Refresh();
        }

        public void Confirm()
        {
            if (!Swapping || !Fits())
                return;
            foreach (var id in _marked)
                MutationLoadout.Unequip(id);
            _controller.Equip(_pending.name);
            Close();
        }

        public void Back()
        {
            if (!IsOpen)
                return;
            if (!Swapping)
            {
                Close();
                return;
            }
            _pending = null;
            _marked.Clear();
            Refresh();
        }

        private bool Fits() => _pending != null && MutationLoadout.Fits(UsedAfterShedding(), _pending.Notches, MutationLoadout.NotchPool);

        private int UsedAfterShedding()
        {
            var used = _controller.UsedNotches();
            foreach (var id in _marked)
            {
                var definition = _controller.Find(id);
                if (definition != null)
                    used -= definition.Notches;
            }
            return used;
        }

        private void Refresh()
        {
            _cards.Clear();
            _notches.text = string.Format(T("run.pick.notches"), UsedAfterShedding(), MutationLoadout.NotchPool);
            if (Swapping)
            {
                _hint.text = string.Format(T("run.pick.swap"), T(_pending.TextKey + ".name"));
                foreach (var id in MutationLoadout.Equipped)
                {
                    var definition = _controller.Find(id);
                    if (definition == null)
                        continue;
                    var card = Card(definition, () => Toggle(id));
                    card.EnableInClassList(MarkedClass, _marked.Contains(id));
                    _cards.Add(card);
                }
                _confirm.style.display = DisplayStyle.Flex;
                _confirm.SetEnabled(Fits());
                _back.text = T("run.pick.back");
            }
            else
            {
                _hint.text = T("run.pick.hint");
                for (var i = 0; i < _offers.Count; i++)
                {
                    var index = i;
                    _cards.Add(Card(_offers[i], () => Choose(index)));
                }
                _confirm.style.display = DisplayStyle.None;
                _back.text = T("run.pick.skip");
            }
            _cards.schedule.Execute(FocusFirst);
        }

        private void FocusFirst()
        {
            var first = _cards.Q<Button>();
            if (first != null)
                first.Focus();
            else
                _back.Focus();
        }

        private static Button Card(MutationDefinition definition, System.Action clicked)
        {
            var card = new Button(clicked) { name = "card-" + definition.name };
            card.AddToClassList("pick__card");
            card.Add(Label(T(definition.TextKey + ".name"), "pick__name"));
            card.Add(Label(new string('●', definition.Notches), "pick__cost"));
            card.Add(Label(Builds(definition.Builds), "pick__builds"));
            card.Add(Label(T(definition.TextKey + ".effect"), "pick__effect"));
            return card;
        }

        private static Label Label(string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList(className);
            return label;
        }

        private static string Builds(MutationBuild builds)
        {
            if (builds == MutationBuild.None)
                return "";
            var text = "";
            for (var bit = 0; bit < 12; bit++)
            {
                var build = (MutationBuild)(1 << bit);
                if ((builds & build) == 0)
                    continue;
                text += (text.Length > 0 ? " · " : "") + T("mutation.build." + build.ToString().ToLowerInvariant());
            }
            return text;
        }

        private static string T(string key) => LocalizationSettings.StringDatabase.GetLocalizedString(Table, key);
    }
}
