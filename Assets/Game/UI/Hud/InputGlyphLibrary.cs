using System.Collections.Generic;
using UnityEngine;

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
    }
}
