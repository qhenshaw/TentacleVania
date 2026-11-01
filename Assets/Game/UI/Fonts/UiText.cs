using System.Collections.Generic;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace TentacleVania.UI.Fonts
{
    public static class UiText
    {
        public const string DefaultTable = "UI";

        private static readonly Dictionary<TextElement, string> Bound = new();
        private static readonly List<TextElement> Scratch = new();
        private static bool _hooked;

        public static void Bind(TextElement element, string key)
        {
            if (element == null)
                return;
            Hook();
            if (!Bound.ContainsKey(element))
                element.RegisterCallback<DetachFromPanelEvent>(OnDetached);
            Bound[element] = key;
            element.text = Get(key);
        }

        public static string Get(string key) => LocalizationSettings.StringDatabase.GetLocalizedString(DefaultTable, key);

        private static void OnDetached(DetachFromPanelEvent evt)
        {
            if (evt.target is TextElement element)
            {
                Bound.Remove(element);
                element.UnregisterCallback<DetachFromPanelEvent>(OnDetached);
            }
        }

        private static void Hook()
        {
            if (_hooked)
                return;
            _hooked = true;
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }

        private static void OnLocaleChanged(Locale locale)
        {
            Scratch.Clear();
            Scratch.AddRange(Bound.Keys);
            foreach (var element in Scratch)
                element.text = Get(Bound[element]);
        }
    }
}
