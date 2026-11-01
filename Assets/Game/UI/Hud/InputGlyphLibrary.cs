using System.Collections.Generic;
using TentacleVania.Core.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TentacleVania.UI.Hud
{
    [CreateAssetMenu(menuName = "TentacleVania/UI/Input Glyph Library")]
    public sealed class InputGlyphLibrary : ScriptableObject
    {
        [Tooltip("Every glyph texture (Kenney Input Prompts, filled), looked up by file name.")]
        [SerializeField] private Texture2D[] _icons = System.Array.Empty<Texture2D>();

        private Dictionary<string, Texture2D> _byName;

        public int Count => _icons.Length;

        public Texture2D Find(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            if (_byName == null)
            {
                _byName = new Dictionary<string, Texture2D>(_icons.Length);
                foreach (var icon in _icons)
                    if (icon != null)
                        _byName[icon.name] = icon;
            }
            return _byName.TryGetValue(name, out var texture) ? texture : null;
        }

        public Texture2D ForAction(string actionName)
        {
            if (InputSystem.actions == null)
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
                var texture = Find(GlyphRules.IconName(family, binding.effectivePath));
                if (texture != null)
                    return texture;
            }
            return null;
        }
    }
}
