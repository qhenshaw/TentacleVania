using TentacleVania.Abilities;
using TentacleVania.Combat;
using TentacleVania.Core.Input;
using TentacleVania.Core.Interaction;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Settings;
using TentacleVania.Creature;
using TentacleVania.Creature.Movement;
using TentacleVania.Enemies;
using TentacleVania.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace TentacleVania.UI.Hud
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class TutorialPromptView : MonoBehaviour
    {
        private const string Table = "UI";
        private const string ShownClass = "tutorial--shown";
        private const float MoveNeeded = 0.6f;
        private const float ContextHold = 8f;
        private const float ConsumeShowTime = 4f;
        private const float DashTriggerRange = 12f;
        private const float GrabTriggerRange = 8f;

        private VisualElement _box;
        [SerializeField] private InputGlyphLibrary _glyphs;

        private Label _label;
        private VisualElement _icon;
        private GameObject _creature;
        private WeaponController _weapons;
        private CreatureMotor _motor;
        private AbilityController _ability;
        private CreatureCharge _charge;
        private string _active;
        private float _activeSince;
        private float _moveTime;
        private bool _consumePending;
        private bool _weapon2Pending;
        private InputDeviceKind _device;
        private bool _dirty;

        public string Active => _active;
        public string Text => _label != null ? _label.text : "";

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.StretchToParentSize();
            root.pickingMode = PickingMode.Ignore;
            _box = root.Q("tutorial");
            _label = root.Q<Label>("tutorial-text");
            _icon = root.Q("tutorial-icon");
            CombatEvents.ConsumeStarted += OnConsumeStarted;
            Collectibles.WeaponFound += OnWeaponFound;
            Nests.Grown += OnNestGrown;
            SettingsStore.Applied += OnSettingsApplied;
            Tutorials.Completed += OnCompleted;
        }

        private void OnDisable()
        {
            CombatEvents.ConsumeStarted -= OnConsumeStarted;
            Collectibles.WeaponFound -= OnWeaponFound;
            Nests.Grown -= OnNestGrown;
            SettingsStore.Applied -= OnSettingsApplied;
            Tutorials.Completed -= OnCompleted;
            Unhook();
        }

        private void Update()
        {
            Bind();
            var next = Tutorials.Enabled && _creature != null && _motor.InputEnabled ? Pick() : null;
            if (next != _active)
            {
                _active = next;
                _activeSince = Time.time;
                _dirty = true;
            }
            if (_active == Tutorials.Move)
            {
                _moveTime += GameplayInput.Default.Move.magnitude > 0.5f ? Time.deltaTime : 0f;
                if (_moveTime >= MoveNeeded)
                    Tutorials.Complete(Tutorials.Move);
            }
            if (_active == Tutorials.Consume && Time.time - _activeSince >= ConsumeShowTime)
            {
                _consumePending = false;
                Tutorials.Complete(Tutorials.Consume);
            }
            var device = GameplayInput.Default.LastDevice;
            if (device != _device)
            {
                _device = device;
                _dirty = true;
            }
            if (_dirty)
                Render();
        }

        public void Refresh() => _dirty = true;

        private string Pick()
        {
            if (Sticky(Tutorials.Grab) || Want(Tutorials.Grab, FeedableNear()))
                return Tutorials.Grab;
            if (Sticky(Tutorials.Dash) || Want(Tutorials.Dash, TelegraphNear()))
                return Tutorials.Dash;
            if (Want(Tutorials.Consume, _consumePending))
                return Tutorials.Consume;
            if (Want(Tutorials.Weapon2, _weapon2Pending && _weapons != null && _weapons.Weapon(1) != null))
                return Tutorials.Weapon2;
            if (Want(Tutorials.Ability, _ability != null && _ability.Equipped != null && _charge != null && _charge.Current >= _ability.Equipped.Cost))
                return Tutorials.Ability;
            if (Want(Tutorials.Move, true))
                return Tutorials.Move;
            if (Want(Tutorials.Nest, Interactions.Current != null && Interactions.Current.PromptKey == "prompt.nest.grow"))
                return Tutorials.Nest;
            if (Want(Tutorials.Attack, Tutorials.IsDone(Tutorials.Move)))
                return Tutorials.Attack;
            return null;
        }

        private bool Sticky(string id) => _active == id && !Tutorials.IsDone(id) && Time.time - _activeSince < ContextHold;

        private static bool Want(string id, bool condition) => condition && !Tutorials.IsDone(id);

        private bool FeedableNear()
        {
            var manager = EnemyManager.Current;
            if (manager == null || Tutorials.IsDone(Tutorials.Grab))
                return false;
            var origin = _creature.transform.position;
            var enemies = manager.Enemies;
            for (var i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy != null && enemy.IsFeedable && (enemy.transform.position - origin).sqrMagnitude <= GrabTriggerRange * GrabTriggerRange)
                    return true;
            }
            return false;
        }

        private bool TelegraphNear()
        {
            var manager = EnemyManager.Current;
            if (manager == null || Tutorials.IsDone(Tutorials.Dash))
                return false;
            var origin = _creature.transform.position;
            var enemies = manager.Enemies;
            for (var i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy != null && enemy.State == EnemyState.Attacking && (enemy.transform.position - origin).sqrMagnitude <= DashTriggerRange * DashTriggerRange)
                    return true;
            }
            return false;
        }

        private void Render()
        {
            _dirty = false;
            if (_active == null)
            {
                _box.RemoveFromClassList(ShownClass);
                return;
            }
            var format = LocalizationSettings.StringDatabase.GetLocalizedString(Table, "tutorial." + _active);
            var actionName = _active switch
            {
                Tutorials.Move => "Move",
                Tutorials.Attack => "Weapon1",
                Tutorials.Dash => "Dash",
                Tutorials.Grab or Tutorials.Nest => "Grab",
                Tutorials.Weapon2 => "Weapon2",
                Tutorials.Ability => "Ability",
                _ => null,
            };
            var icon = actionName != null ? Icon(actionName) : null;
            CurrentIcon = icon;
            _icon.style.display = icon != null ? DisplayStyle.Flex : DisplayStyle.None;
            _icon.style.backgroundImage = icon;
            _label.text = actionName == null ? format : string.Format(format, icon != null ? "" : Glyph(actionName)).Trim();
            _box.AddToClassList(ShownClass);
        }

        public Texture2D CurrentIcon { get; private set; }

        public Texture2D Icon(string actionName)
        {
            if (_glyphs == null || InputSystem.actions == null)
                return null;
            var action = InputSystem.actions.FindAction(GameplayInput.MapName + "/" + actionName);
            if (action == null)
                return null;
            var gamepad = GameplayInput.Default.LastDevice == InputDeviceKind.Gamepad;
            var group = gamepad ? "Gamepad" : "Keyboard&Mouse";
            var pad = Gamepad.current;
            var family = !gamepad ? GlyphFamily.Keyboard : pad != null ? GlyphRules.FamilyFor(pad.layout, pad.description.product) : GlyphFamily.Xbox;
            foreach (var binding in action.bindings)
            {
                if (binding.isComposite || binding.isPartOfComposite || binding.groups == null || !binding.groups.Contains(group))
                    continue;
                var texture = _glyphs.Find(GlyphRules.IconName(family, binding.effectivePath));
                if (texture != null)
                    return texture;
            }
            return null;
        }

        public static string Glyph(string actionName)
        {
            var action = InputSystem.actions != null ? InputSystem.actions.FindAction(GameplayInput.MapName + "/" + actionName) : null;
            if (action == null)
                return "[?]";
            if (GameplayInput.Default.LastDevice == InputDeviceKind.Gamepad)
            {
                var pad = Gamepad.current;
                if (pad != null && actionName != "Move")
                    foreach (var control in action.controls)
                        if (control.device == pad)
                            return "[" + control.displayName + "]";
                return "[" + First(action.GetBindingDisplayString(InputBinding.MaskByGroup("Gamepad"))) + "]";
            }
            return "[" + First(action.GetBindingDisplayString(InputBinding.MaskByGroup("Keyboard&Mouse"))) + "]";
        }

        private static string First(string display)
        {
            var split = display.IndexOf(" | ", System.StringComparison.Ordinal);
            return split > 0 ? display.Substring(0, split) : display;
        }

        private void Bind()
        {
            var spawned = CreatureSpawner.Spawned;
            if (spawned == _creature)
                return;
            Unhook();
            _creature = spawned;
            if (_creature == null)
                return;
            _motor = _creature.GetComponent<CreatureMotor>();
            _weapons = _creature.GetComponent<WeaponController>();
            _ability = _creature.GetComponent<AbilityController>();
            _charge = _creature.GetComponent<CreatureCharge>();
            if (_weapons != null)
                _weapons.AttackStarted += OnAttack;
            if (_motor != null)
                _motor.Dashed += OnDashed;
            if (_ability != null)
                _ability.Activated += OnAbility;
        }

        private void Unhook()
        {
            if (_weapons != null)
                _weapons.AttackStarted -= OnAttack;
            if (_motor != null)
                _motor.Dashed -= OnDashed;
            if (_ability != null)
                _ability.Activated -= OnAbility;
            _creature = null;
            _weapons = null;
            _motor = null;
            _ability = null;
            _charge = null;
        }

        private void OnAttack(WeaponController weapons)
        {
            if (Tutorials.IsDone(Tutorials.Move))
                Tutorials.Complete(Tutorials.Attack);
            if (weapons.ActiveSlot == 1 && _weapon2Pending)
            {
                _weapon2Pending = false;
                Tutorials.Complete(Tutorials.Weapon2);
            }
        }

        private void OnDashed()
        {
            if (_active == Tutorials.Dash)
                Tutorials.Complete(Tutorials.Dash);
        }

        private void OnAbility(AbilityController ability) => Tutorials.Complete(Tutorials.Ability);

        private void OnConsumeStarted(Consumable consumable)
        {
            if (_creature == null)
                return;
            Tutorials.Complete(Tutorials.Grab);
            _consumePending = !Tutorials.IsDone(Tutorials.Consume);
        }

        private void OnWeaponFound(string id) => _weapon2Pending = !Tutorials.IsDone(Tutorials.Weapon2);

        private void OnNestGrown(string id) => Tutorials.Complete(Tutorials.Nest);

        private void OnSettingsApplied(SettingsData data) => _dirty = true;

        private void OnCompleted(string id) => _dirty = true;
    }
}
