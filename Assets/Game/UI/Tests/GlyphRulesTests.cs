using NUnit.Framework;
using TentacleVania.UI.Hud;

namespace TentacleVania.UI.Tests
{
    public sealed class GlyphRulesTests
    {
        [Test]
        public void GamepadButtonsMapPerFamily()
        {
            Assert.That(GlyphRules.IconName(GlyphFamily.Xbox, "<Gamepad>/buttonWest"), Is.EqualTo("xbox_button_x"));
            Assert.That(GlyphRules.IconName(GlyphFamily.PlayStation, "<Gamepad>/buttonWest"), Is.EqualTo("playstation_button_square"));
            Assert.That(GlyphRules.IconName(GlyphFamily.SteamDeck, "<Gamepad>/rightTrigger"), Is.EqualTo("steamdeck_button_r2"));
            Assert.That(GlyphRules.IconName(GlyphFamily.Xbox, "<Gamepad>/leftStick"), Is.EqualTo("xbox_stick_l"));
        }

        [Test]
        public void KeyboardAndMouseMapByControl()
        {
            Assert.That(GlyphRules.IconName(GlyphFamily.Keyboard, "<Keyboard>/e"), Is.EqualTo("keyboard_e"));
            Assert.That(GlyphRules.IconName(GlyphFamily.Keyboard, "<Keyboard>/leftShift"), Is.EqualTo("keyboard_shift"));
            Assert.That(GlyphRules.IconName(GlyphFamily.Keyboard, "<Mouse>/leftButton"), Is.EqualTo("mouse_left"));
            Assert.That(GlyphRules.IconName(GlyphFamily.Keyboard, "<Mouse>/delta"), Is.Null);
        }

        [Test]
        public void FamilyFromDeviceNames()
        {
            Assert.That(GlyphRules.FamilyFor("DualSenseGamepadHID", "DualSense Wireless Controller"), Is.EqualTo(GlyphFamily.PlayStation));
            Assert.That(GlyphRules.FamilyFor("XInputControllerWindows", "Steam Deck Controller"), Is.EqualTo(GlyphFamily.SteamDeck));
            Assert.That(GlyphRules.FamilyFor("XInputControllerWindows", "Xbox Controller"), Is.EqualTo(GlyphFamily.Xbox));
            Assert.That(GlyphRules.FamilyFor("Gamepad", null), Is.EqualTo(GlyphFamily.Xbox));
        }
    }
}
