using System.Linq;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static partial class TestGamepad
{
    private const string DeviceName = "ClaudeTestPad";

    [OnCodeLoaded]
    private static void OnCodeLoaded() => EditorApplication.playModeStateChanged += state =>
    {
        if (state is PlayModeStateChange.ExitingPlayMode or PlayModeStateChange.EnteredEditMode)
            Remove();
    };

    public static Gamepad Device =>
        InputSystem.devices.OfType<Gamepad>().FirstOrDefault(d => d.name.StartsWith(DeviceName)) ?? InputSystem.AddDevice<Gamepad>(DeviceName);

    public static void Set(Vector2 move, Vector2 aim = default, params GamepadButton[] held)
    {
        var state = new GamepadState { leftStick = move, rightStick = aim };
        foreach (var button in held)
            state = state.WithButton(button);
        InputSystem.QueueStateEvent(Device, state);
    }

    public static void Release() => Set(Vector2.zero);

    public static int Remove()
    {
        var removed = 0;
        foreach (var device in InputSystem.devices.Where(d => d.name.StartsWith(DeviceName)).ToArray())
        {
            InputSystem.RemoveDevice(device);
            removed++;
        }
        return removed;
    }
}
