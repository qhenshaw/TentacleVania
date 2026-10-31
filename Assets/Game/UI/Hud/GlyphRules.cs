namespace TentacleVania.UI.Hud
{
    public enum GlyphFamily
    {
        Xbox,
        PlayStation,
        SteamDeck,
        Keyboard,
    }

    public static class GlyphRules
    {
        public static GlyphFamily FamilyFor(string layout, string product)
        {
            var name = (layout ?? "") + " " + (product ?? "");
            if (name.Contains("Steam Deck") || name.Contains("SteamDeck"))
                return GlyphFamily.SteamDeck;
            if (name.Contains("DualShock") || name.Contains("DualSense") || name.Contains("PlayStation") || name.Contains("Wireless Controller"))
                return GlyphFamily.PlayStation;
            return GlyphFamily.Xbox;
        }

        public static string IconName(GlyphFamily family, string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            var slash = path.LastIndexOf('/');
            var control = slash >= 0 ? path.Substring(slash + 1) : path;
            if (path.StartsWith("<Keyboard>"))
                return KeyboardKey(control);
            if (path.StartsWith("<Mouse>"))
                return control switch
                {
                    "leftButton" => "mouse_left",
                    "rightButton" => "mouse_right",
                    "middleButton" => "mouse_scroll",
                    _ => null,
                };
            return family switch
            {
                GlyphFamily.PlayStation => PlayStation(control),
                GlyphFamily.SteamDeck => SteamDeck(control),
                _ => Xbox(control),
            };
        }

        private static string KeyboardKey(string control) => control switch
        {
            "leftShift" or "rightShift" => "keyboard_shift",
            "leftCtrl" or "rightCtrl" => "keyboard_ctrl",
            "leftAlt" or "rightAlt" => "keyboard_alt",
            "space" => "keyboard_space",
            "escape" => "keyboard_escape",
            "enter" => "keyboard_enter",
            "tab" => "keyboard_tab",
            "upArrow" => "keyboard_arrow_up",
            "downArrow" => "keyboard_arrow_down",
            "leftArrow" => "keyboard_arrow_left",
            "rightArrow" => "keyboard_arrow_right",
            _ => "keyboard_" + control.ToLowerInvariant(),
        };

        private static string Xbox(string control) => control switch
        {
            "buttonSouth" => "xbox_button_a",
            "buttonEast" => "xbox_button_b",
            "buttonWest" => "xbox_button_x",
            "buttonNorth" => "xbox_button_y",
            "leftShoulder" => "xbox_lb",
            "rightShoulder" => "xbox_rb",
            "leftTrigger" => "xbox_lt",
            "rightTrigger" => "xbox_rt",
            "start" => "xbox_button_menu",
            "select" => "xbox_button_view",
            "leftStick" => "xbox_stick_l",
            "rightStick" => "xbox_stick_r",
            "leftStickPress" => "xbox_ls",
            "rightStickPress" => "xbox_rs",
            "up" => "xbox_dpad_up",
            "down" => "xbox_dpad_down",
            "left" => "xbox_dpad_left",
            "right" => "xbox_dpad_right",
            _ => null,
        };

        private static string PlayStation(string control) => control switch
        {
            "buttonSouth" => "playstation_button_cross",
            "buttonEast" => "playstation_button_circle",
            "buttonWest" => "playstation_button_square",
            "buttonNorth" => "playstation_button_triangle",
            "leftShoulder" => "playstation_trigger_l1",
            "rightShoulder" => "playstation_trigger_r1",
            "leftTrigger" => "playstation_trigger_l2",
            "rightTrigger" => "playstation_trigger_r2",
            "start" => "playstation5_button_options",
            "select" => "playstation5_button_create",
            "leftStick" => "playstation_stick_l",
            "rightStick" => "playstation_stick_r",
            "leftStickPress" => "playstation_button_l3",
            "rightStickPress" => "playstation_button_r3",
            "up" => "playstation_dpad_up",
            "down" => "playstation_dpad_down",
            "left" => "playstation_dpad_left",
            "right" => "playstation_dpad_right",
            _ => null,
        };

        private static string SteamDeck(string control) => control switch
        {
            "buttonSouth" => "steamdeck_button_a",
            "buttonEast" => "steamdeck_button_b",
            "buttonWest" => "steamdeck_button_x",
            "buttonNorth" => "steamdeck_button_y",
            "leftShoulder" => "steamdeck_button_l1",
            "rightShoulder" => "steamdeck_button_r1",
            "leftTrigger" => "steamdeck_button_l2",
            "rightTrigger" => "steamdeck_button_r2",
            "start" => "steamdeck_button_options",
            "select" => "steamdeck_button_view",
            "leftStick" => "steamdeck_stick_l",
            "rightStick" => "steamdeck_stick_r",
            "leftStickPress" => "steamdeck_stick_l_press",
            "rightStickPress" => "steamdeck_stick_r_press",
            "up" => "steamdeck_dpad_up",
            "down" => "steamdeck_dpad_down",
            "left" => "steamdeck_dpad_left",
            "right" => "steamdeck_dpad_right",
            _ => null,
        };
    }
}
