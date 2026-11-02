using System.Collections.Generic;
using TentacleVania.Core.Runs;
using TentacleVania.Creature;
using TentacleVania.Creature.Movement;
using TentacleVania.UI.Fonts;
using TentacleVania.Weapons;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace TentacleVania.UI.Run
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class EvolutionPickScreen : MonoBehaviour
    {
        private const string Table = "UI";
        private const string OpenClass = "pick--open";

        private readonly List<EvolutionOffer> _offers = new(4);
        private VisualElement _pick;
        private VisualElement _cards;
        private Button _back;
        private WeaponController _weapons;
        private CreatureMotor _lockedMotor;

        public static EvolutionPickScreen Instance { get; private set; }

        public bool IsOpen { get; private set; }
        public IReadOnlyList<EvolutionOffer> Offers => _offers;

        private void OnEnable()
        {
            Instance = this;
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.StretchToParentSize();
            root.pickingMode = PickingMode.Ignore;
            _pick = root.Q("pick");
            _cards = root.Q("cards");
            _back = root.Q<Button>("back");
            UiText.Bind(root.Q<Label>("title"), "run.evolve.title");
            UiText.Bind(root.Q<Label>("hint"), "run.evolve.hint");
            UiText.Bind(_back, "run.pick.skip");
            root.Q("notches").style.display = DisplayStyle.None;
            root.Q("confirm").style.display = DisplayStyle.None;
            _back.clicked += Close;
            _pick.RegisterCallback<NavigationCancelEvent>(_ => Close());
        }

        private void OnDisable()
        {
            if (IsOpen)
                Close();
            if (Instance == this)
                Instance = null;
        }

        public void Open(WeaponController weapons, IReadOnlyList<EvolutionOffer> offers)
        {
            _weapons = weapons;
            _offers.Clear();
            foreach (var offer in offers)
                _offers.Add(offer);
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
            _pick.RemoveFromClassList(OpenClass);
            if (_lockedMotor != null)
                _lockedMotor.UnlockControl();
            _lockedMotor = null;
        }

        public void Choose(int index)
        {
            if (!IsOpen || index < 0 || index >= _offers.Count)
                return;
            RunRewardHandler.ApplyEvolution(_weapons, _offers[index]);
            Close();
        }

        private void Refresh()
        {
            _cards.Clear();
            for (var i = 0; i < _offers.Count; i++)
            {
                var index = i;
                var weapon = _weapons.Weapon(_offers[i].Slot);
                var modifier = weapon.Modifiers[_offers[i].Option];
                var card = new Button(() => Choose(index)) { name = "card-" + modifier.name };
                card.AddToClassList("pick__card");
                card.Add(Label(T(modifier.TextKey + ".name"), "pick__name"));
                card.Add(Label(weapon.DisplayName, "pick__builds"));
                card.Add(Label(T(modifier.TextKey + ".effect"), "pick__effect"));
                _cards.Add(card);
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

        private static Label Label(string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList(className);
            return label;
        }

        private static string T(string key) => LocalizationSettings.StringDatabase.GetLocalizedString(Table, key);
    }
}
