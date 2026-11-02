using System.Collections.Generic;
using TentacleVania.Abilities;
using TentacleVania.Abilities.Mutations;
using TentacleVania.Core.Runs;
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

        private readonly List<WeaponDefinition> _weapons = new(16);
        private readonly List<AbilityDefinition> _abilities = new(16);
        private readonly int[] _picks = new int[Rows];
        private readonly Label[] _values = new Label[Rows];
        private VisualElement _pick;
        private VisualElement _rows;
        private Button _breakout;
        private CreatureMotor _lockedMotor;

        public static PodScreen Instance { get; private set; }

        public bool IsOpen { get; private set; }
        public WeaponDefinition Weapon1 => Pick(_weapons, _picks[0]);
        public WeaponDefinition Weapon2 => Pick(_weapons, _picks[1]);
        public AbilityDefinition Ability => Pick(_abilities, _picks[2]);

        private void OnEnable()
        {
            Instance = this;
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.StretchToParentSize();
            root.pickingMode = PickingMode.Ignore;
            _pick = root.Q("pick");
            _rows = root.Q("rows");
            _breakout = root.Q<Button>("breakout");
            UiText.Bind(root.Q<Label>("title"), "run.pod.title");
            UiText.Bind(root.Q<Label>("hint"), "run.pod.hint");
            UiText.Bind(_breakout, "run.pod.breakout");
            _breakout.clicked += BreakOut;
            BuildRows();
            RunPod.Requested += Open;
        }

        private void OnDisable()
        {
            RunPod.Requested -= Open;
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
            Gather(spawned);
            var weapons = spawned.GetComponent<WeaponController>();
            var abilities = spawned.GetComponent<AbilityController>();
            _picks[0] = IndexOr(_weapons, weapons != null ? weapons.Weapon(0) : null, 0);
            _picks[1] = IndexOr(_weapons, weapons != null ? weapons.Weapon(1) : null, -1);
            if (_picks[1] == _picks[0])
                _picks[1] = -1;
            _picks[2] = IndexOr(_abilities, abilities != null ? abilities.Equipped : null, _abilities.Count > 0 ? 0 : -1);
            IsOpen = true;
            _pick.AddToClassList(OpenClass);
            if (_lockedMotor == null && spawned.TryGetComponent(out CreatureMotor motor))
            {
                _lockedMotor = motor;
                motor.LockControl();
            }
            Refresh();
            _breakout.schedule.Execute(() => _breakout.Focus());
        }

        public void Cycle(int row, int direction)
        {
            if (!IsOpen || row < 0 || row >= Rows)
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
            Refresh();
        }

        public void BreakOut()
        {
            if (!IsOpen)
                return;
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
            RunPod.BreakOut();
        }

        private void Close()
        {
            IsOpen = false;
            _pick.RemoveFromClassList(OpenClass);
            if (_lockedMotor != null)
                _lockedMotor.UnlockControl();
            _lockedMotor = null;
        }

        private void Gather(GameObject spawned)
        {
            _weapons.Clear();
            _abilities.Clear();
            var controller = spawned.GetComponent<MutationController>();
            if (controller == null || controller.Catalog == null)
                return;
            foreach (var entry in controller.Catalog.Entries)
            {
                if (entry is WeaponDefinition weapon)
                    _weapons.Add(weapon);
                else if (entry is AbilityDefinition ability && ability.SlotAbility)
                    _abilities.Add(ability);
            }
        }

        private void BuildRows()
        {
            _rows.Clear();
            string[] keys = { "run.pod.weapon1", "run.pod.weapon2", "run.pod.ability" };
            for (var i = 0; i < Rows; i++)
            {
                var row = i;
                var line = new VisualElement();
                line.AddToClassList("pod__row");
                var label = new Label();
                label.AddToClassList("pod__label");
                UiText.Bind(label, keys[i]);
                var previous = new Button(() => Cycle(row, -1)) { text = "<", name = "prev-" + i };
                previous.AddToClassList("pod__arrow");
                var value = new Label();
                value.AddToClassList("pod__value");
                var next = new Button(() => Cycle(row, 1)) { text = ">", name = "next-" + i };
                next.AddToClassList("pod__arrow");
                line.Add(label);
                line.Add(previous);
                line.Add(value);
                line.Add(next);
                _rows.Add(line);
                _values[i] = value;
            }
        }

        private void Refresh()
        {
            var none = Text("run.pod.none");
            _values[0].text = Weapon1 != null ? Weapon1.DisplayName : none;
            _values[1].text = Weapon2 != null ? Weapon2.DisplayName : none;
            _values[2].text = Ability != null ? Ability.DisplayName : none;
        }

        private static int IndexOr<T>(List<T> list, T item, int fallback) where T : Object
        {
            if (item != null)
                for (var i = 0; i < list.Count; i++)
                    if (list[i] == item)
                        return i;
            return Mathf.Min(fallback, list.Count - 1);
        }

        private static T Pick<T>(List<T> list, int index) where T : class => index >= 0 && index < list.Count ? list[index] : null;

        private static string Text(string key) => LocalizationSettings.StringDatabase.GetLocalizedString(Table, key);
    }
}
