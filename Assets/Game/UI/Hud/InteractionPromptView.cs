using System.Collections.Generic;
using TentacleVania.Core.Interaction;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UIElements;

namespace TentacleVania.UI.Hud
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class InteractionPromptView : MonoBehaviour
    {
        private const string Table = "UI";
        private const string HiddenClass = "hud__prompt--hidden";

        private readonly Dictionary<string, Binding> _bindings = new(4);

        private Label _label;
        private string _shownKey;

        private sealed class Binding
        {
            public LocalizedString Text;
            public LocalizedString.ChangeHandler Handler;
            public string Value = "";
        }

        private void OnEnable()
        {
            _label = GetComponent<UIDocument>().rootVisualElement.Q<Label>("prompt");
            _shownKey = null;
            if (_label != null)
                _label.AddToClassList(HiddenClass);
        }

        private void OnDisable()
        {
            foreach (var binding in _bindings.Values)
                binding.Text.StringChanged -= binding.Handler;
            _bindings.Clear();
        }

        private void LateUpdate()
        {
            if (_label == null)
                return;
            var current = Interactions.Current;
            var key = current != null ? current.PromptKey : null;
            if (key == _shownKey)
            {
                if (key != null && _bindings.TryGetValue(key, out var live) && _label.text != live.Value)
                    _label.text = live.Value;
                return;
            }
            _shownKey = key;
            if (string.IsNullOrEmpty(key))
            {
                _label.AddToClassList(HiddenClass);
                return;
            }
            _label.text = BindingFor(key).Value;
            _label.RemoveFromClassList(HiddenClass);
        }

        private Binding BindingFor(string key)
        {
            if (_bindings.TryGetValue(key, out var binding))
                return binding;
            binding = new Binding { Text = new LocalizedString(Table, key) };
            binding.Handler = value => binding.Value = value;
            binding.Text.StringChanged += binding.Handler;
            _bindings.Add(key, binding);
            return binding;
        }
    }
}
