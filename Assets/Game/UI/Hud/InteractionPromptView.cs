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

        private const float RingSize = 22f;

        private HoldRing _holdRing;
        private bool _ringShown;
        private string _measuredText;
        private float _shownProgress = -1f;

        private void OnEnable()
        {
            _label = GetComponent<UIDocument>().rootVisualElement.Q<Label>("prompt");
            _shownKey = null;
            if (_label != null)
            {
                _label.AddToClassList(HiddenClass);
                _holdRing = new HoldRing(RingSize) { name = "prompt-hold" };
                _holdRing.style.position = Position.Absolute;
                _holdRing.style.left = Length.Percent(50f);
                _holdRing.style.top = Length.Percent(50f);
                _holdRing.style.marginTop = -RingSize * 0.5f;
                _holdRing.style.display = DisplayStyle.None;
                _label.Add(_holdRing);
            }
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
            var progress = Interactions.HoldProgress;
            if (!Mathf.Approximately(progress, _shownProgress))
            {
                _shownProgress = progress;
                _holdRing.Progress = progress;
            }
            var menu = Nest.NestMenuScreen.Instance;
            var current = menu != null && menu.IsOpen ? null : Interactions.Current;
            var key = current != null ? current.PromptKey : null;
            var hold = current is IHoldInteractable;
            if (hold != _ringShown)
            {
                _ringShown = hold;
                _holdRing.style.display = hold ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (_ringShown && _label.text != _measuredText)
            {
                _measuredText = _label.text;
                var width = _label.MeasureTextSize(_measuredText, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined).x;
                _holdRing.style.marginLeft = width * 0.5f + 12f;
            }
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
