using TentacleVania.Abilities;
using TentacleVania.Combat;
using TentacleVania.Core.Input;
using TentacleVania.Core.Progression;
using TentacleVania.Creature;
using TentacleVania.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TentacleVania.UI.Hud
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class LoadoutHudView : MonoBehaviour
    {
        private const string EmptyClass = "hud__slot--empty";
        private const string ReadyClass = "hud__slot--ready";
        private const string ActiveClass = "hud__slot--active";
        private const string DeltaShownClass = "hud__biomass-delta--shown";
        private const string GainClass = "hud__biomass-delta--gain";
        private const string LossClass = "hud__biomass-delta--loss";

        [SerializeField, Min(0.1f)] private float _deltaSeconds = 1.6f;

        private readonly Label[] _weaponLabels = new Label[WeaponController.Slots];
        private readonly VisualElement[] _weaponSlots = new VisualElement[WeaponController.Slots];
        private readonly Object[] _shownWeapons = new Object[WeaponController.Slots];
        private VisualElement _abilitySlot;
        private Label _abilityLabel;
        private Object _shownAbility;
        private bool _dirty;
        private Label _biomassCount;
        private Label _biomassDelta;
        private Health _creature;
        private WeaponController _weapons;
        private AbilityController _ability;
        private CreatureCharge _charge;
        private int _lastBiomass = -1;
        private int _pendingDelta;
        private float _deltaUntil;
        private bool _deltaShown;
        private bool _ready;
        private bool _active;
        private readonly Label[] _keys = new Label[3];
        private InputDeviceKind _keysDevice = (InputDeviceKind)(-1);
        private static readonly string[] KeyActions = { "Weapon1", "Weapon2", "Ability" };

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            for (var i = 0; i < WeaponController.Slots; i++)
            {
                _weaponSlots[i] = root.Q("slot-weapon" + (i + 1));
                _weaponLabels[i] = root.Q<Label>("slot-weapon" + (i + 1) + "-label");
            }
            _abilitySlot = root.Q("slot-ability");
            _abilityLabel = root.Q<Label>("slot-ability-label");
            _keys[0] = root.Q<Label>("slot-weapon1-key");
            _keys[1] = root.Q<Label>("slot-weapon2-key");
            _keys[2] = root.Q<Label>("slot-ability-key");
            _keysDevice = (InputDeviceKind)(-1);
            Core.Settings.SettingsStore.Applied += OnSettingsApplied;
            _dirty = true;
            _biomassCount = root.Q<Label>("biomass-count");
            _biomassDelta = root.Q<Label>("biomass-delta");
            _lastBiomass = -1;
            _pendingDelta = 0;
            Biomass.Changed += OnBiomassChanged;
            OnBiomassChanged(Biomass.Carried);
        }

        private void OnDisable()
        {
            Biomass.Changed -= OnBiomassChanged;
            Core.Settings.SettingsStore.Applied -= OnSettingsApplied;
        }

        private void OnSettingsApplied(Core.Settings.SettingsData data) => _keysDevice = (InputDeviceKind)(-1);

        private void RefreshKeys()
        {
            var device = GameplayInput.Default.LastDevice;
            if (device == _keysDevice || InputSystem.actions == null)
                return;
            _keysDevice = device;
            var group = device == InputDeviceKind.Gamepad ? "Gamepad" : "Keyboard&Mouse";
            for (var i = 0; i < _keys.Length; i++)
            {
                if (_keys[i] == null)
                    continue;
                var action = InputSystem.actions.FindAction(GameplayInput.MapName + "/" + KeyActions[i]);
                var index = action != null ? action.GetBindingIndex(group: group) : -1;
                _keys[i].text = index >= 0 ? action.GetBindingDisplayString(index, InputBinding.DisplayStringOptions.DontIncludeInteractions) : "";
            }
        }

        private void LateUpdate()
        {
            if (_abilitySlot == null)
                return;
            RefreshKeys();
            var creature = CombatTargets.Creature;
            if (creature != _creature)
            {
                _creature = creature;
                _weapons = creature != null ? creature.GetComponent<WeaponController>() : null;
                _ability = creature != null ? creature.GetComponent<AbilityController>() : null;
                _charge = creature != null ? creature.GetComponent<CreatureCharge>() : null;
                _dirty = true;
            }
            for (var i = 0; i < WeaponController.Slots; i++)
            {
                var weapon = _weapons != null ? _weapons.Weapon(i) : null;
                if (!_dirty && _shownWeapons[i] == (Object)weapon)
                    continue;
                _shownWeapons[i] = weapon;
                _weaponLabels[i].text = weapon != null ? Initials(weapon.DisplayName) : "";
                _weaponSlots[i].EnableInClassList(EmptyClass, weapon == null);
            }
            var ability = _ability != null ? _ability.Equipped : null;
            if (_dirty || _shownAbility != (Object)ability)
            {
                _shownAbility = ability;
                _abilityLabel.text = ability != null ? Initials(ability.DisplayName) : "";
                _abilitySlot.EnableInClassList(EmptyClass, ability == null);
            }
            _dirty = false;
            var ready = ability != null && _charge != null && _charge.Current >= ability.Cost;
            if (ready != _ready)
            {
                _ready = ready;
                _abilitySlot.EnableInClassList(ReadyClass, ready);
            }
            var active = _ability != null && _ability.IsActive;
            if (active != _active)
            {
                _active = active;
                _abilitySlot.EnableInClassList(ActiveClass, active);
            }
            var showDelta = Time.unscaledTime < _deltaUntil;
            if (showDelta != _deltaShown && _biomassDelta != null)
            {
                _deltaShown = showDelta;
                _biomassDelta.EnableInClassList(DeltaShownClass, showDelta);
                if (!showDelta)
                    _pendingDelta = 0;
            }
        }

        private void OnBiomassChanged(int carried)
        {
            if (_biomassCount == null)
                return;
            _biomassCount.text = carried.ToString("N0");
            if (_lastBiomass >= 0 && carried != _lastBiomass && _biomassDelta != null)
            {
                _pendingDelta += carried - _lastBiomass;
                _biomassDelta.text = (_pendingDelta > 0 ? "+" : "") + _pendingDelta.ToString("N0");
                _biomassDelta.EnableInClassList(GainClass, _pendingDelta > 0);
                _biomassDelta.EnableInClassList(LossClass, _pendingDelta < 0);
                _deltaUntil = Time.unscaledTime + _deltaSeconds;
            }
            _lastBiomass = carried;
        }

        public static string Initials(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "";
            var parts = name.Split(' ', '-');
            if (parts.Length == 1)
                return name.Length <= 2 ? name.ToUpperInvariant() : name.Substring(0, 2).ToUpperInvariant();
            var text = "";
            for (var i = 0; i < parts.Length && text.Length < 2; i++)
                if (parts[i].Length > 0)
                    text += char.ToUpperInvariant(parts[i][0]);
            return text;
        }
    }
}
