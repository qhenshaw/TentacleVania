using UnityEngine;
using UnityEngine.InputSystem;

namespace TentacleVania.Core.Input
{
    public enum InputDeviceKind
    {
        Gamepad,
        KeyboardMouse,
    }

    public readonly struct ButtonInput
    {
        private readonly InputAction _action;

        public ButtonInput(InputAction action) => _action = action;

        public bool Pressed => _action != null && _action.WasPressedThisFrame();
        public bool Held => _action != null && _action.IsPressed();
        public bool Released => _action != null && _action.WasReleasedThisFrame();
    }

    public sealed class GameplayInput
    {
        public const string MapName = "Gameplay";

        public static readonly string[] ActionNames = { "Move", "AimStick", "AimPointer", "Weapon1", "Weapon2", "Dash", "Grab", "Ability", "Traverse", "Pause", "Map", "MapOverlay" };

        private static GameplayInput _default;

        private readonly InputActionAsset _asset;
        private readonly InputActionMap _map;
        private readonly InputAction _move;
        private readonly InputAction _aimStick;
        private readonly InputAction _aimPointer;

        public GameplayInput(InputActionAsset asset)
        {
            _asset = asset;
            _map = asset.FindActionMap(MapName, true);
            _move = _map.FindAction("Move", true);
            _aimStick = _map.FindAction("AimStick", true);
            _aimPointer = _map.FindAction("AimPointer", true);
            Weapon1 = new(_map.FindAction("Weapon1", true));
            Weapon2 = new(_map.FindAction("Weapon2", true));
            Dash = new(_map.FindAction("Dash", true));
            Grab = new(_map.FindAction("Grab", true));
            Ability = new(_map.FindAction("Ability", true));
            var traverse = _map.FindAction("Traverse");
            Traverse = traverse != null ? new(traverse) : default;
            var pause = _map.FindAction("Pause");
            Pause = pause != null ? new(pause) : default;
            var map = _map.FindAction("Map");
            Map = map != null ? new(map) : default;
            var overlay = _map.FindAction("MapOverlay");
            MapOverlay = overlay != null ? new(overlay) : default;
            _map.actionTriggered += OnActionTriggered;
            _map.Enable();
        }

        public static GameplayInput Default
        {
            get
            {
                var actions = InputSystem.actions;
                if (_default == null || _default._asset != actions)
                {
                    _default?.Release();
                    _default = new(actions);
                }
                return _default;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetDefault()
        {
            _default?.Release();
            _default = null;
        }

        private void Release()
        {
            _map.actionTriggered -= OnActionTriggered;
            if (_asset != null)
                _map.Disable();
        }

        public InputDeviceKind LastDevice { get; private set; } = InputDeviceKind.KeyboardMouse;

        public Vector2 Move => Vector2.ClampMagnitude(_move.ReadValue<Vector2>(), 1f);
        public Vector2 AimStick => _aimStick.ReadValue<Vector2>();
        public Vector2 AimPointer => _aimPointer.ReadValue<Vector2>();
        public ButtonInput Weapon1 { get; }
        public ButtonInput Weapon2 { get; }
        public ButtonInput Dash { get; }
        public ButtonInput Grab { get; }
        public ButtonInput Ability { get; }
        public ButtonInput Traverse { get; }
        public ButtonInput Pause { get; }
        public ButtonInput Map { get; }
        public ButtonInput MapOverlay { get; }

        public bool Enabled
        {
            get => _map.enabled;
            set
            {
                if (value) _map.Enable();
                else _map.Disable();
            }
        }

        private void OnActionTriggered(InputAction.CallbackContext context)
        {
            var device = context.control?.device;
            if (device is Gamepad) LastDevice = InputDeviceKind.Gamepad;
            else if (device is Keyboard or Mouse) LastDevice = InputDeviceKind.KeyboardMouse;
        }
    }
}
