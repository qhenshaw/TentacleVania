using System.Linq;
using NUnit.Framework;
using TentacleVania.Core.Input;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TentacleVania.Core.Tests
{
    public class GameplayInputTests
    {
        private const string AssetPath = "Assets/InputSystem_Actions.inputactions";
        private const string Gamepad = "Gamepad";
        private const string KeyboardMouse = "Keyboard&Mouse";

        private static InputActionAsset LoadAsset() => AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath);

        private static bool HasBinding(InputAction action, string group) =>
            action.bindings.Any(b => !b.isComposite && b.groups != null && b.groups.Split(';').Contains(group));

        [Test]
        public void ProjectWideActionsAreTheGameplayAsset() =>
            Assert.AreEqual(AssetPath, AssetDatabase.GetAssetPath(InputSystem.actions));

        [Test]
        public void GameplayMapHasEveryAction()
        {
            var map = LoadAsset().FindActionMap(GameplayInput.MapName);
            Assert.NotNull(map);
            foreach (var name in GameplayInput.ActionNames)
                Assert.NotNull(map.FindAction(name), name);
        }

        [TestCase("Move")]
        [TestCase("Weapon1")]
        [TestCase("Weapon2")]
        [TestCase("Dash")]
        [TestCase("Grab")]
        [TestCase("Ability")]
        public void ActionIsBoundForGamepadAndKeyboardMouse(string name)
        {
            var action = LoadAsset().FindActionMap(GameplayInput.MapName).FindAction(name);
            Assert.IsTrue(HasBinding(action, Gamepad), $"{name} has no gamepad binding");
            Assert.IsTrue(HasBinding(action, KeyboardMouse), $"{name} has no keyboard and mouse binding");
        }

        [Test]
        public void AimIsSplitByDevice()
        {
            var map = LoadAsset().FindActionMap(GameplayInput.MapName);
            Assert.IsTrue(HasBinding(map.FindAction("AimStick"), Gamepad));
            Assert.IsTrue(HasBinding(map.FindAction("AimPointer"), KeyboardMouse));
        }

        [Test]
        public void WrapperBindsToTheAsset()
        {
            var copy = Object.Instantiate(LoadAsset());
            try
            {
                var input = new GameplayInput(copy);
                Assert.IsTrue(input.Enabled);
                Assert.AreEqual(Vector2.zero, input.Move);
                input.Enabled = false;
                Assert.IsFalse(input.Enabled);
            }
            finally
            {
                Object.DestroyImmediate(copy);
            }
        }
    }
}
