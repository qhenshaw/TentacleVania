using TentacleVania.Core;
using TentacleVania.Core.Saves;
using UnityEngine;
using UnityEngine.UIElements;

namespace TentacleVania.UI.Hud
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class SpeedrunTimerView : MonoBehaviour
    {
        private Label _label;
        private long _shown = -1;

        public string Text => _label != null ? _label.text : "";

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _label = new Label { name = "speedrun-timer", pickingMode = PickingMode.Ignore };
            _label.AddToClassList("hud__speedrun");
            _label.style.position = Position.Absolute;
            _label.style.right = 24;
            _label.style.top = 16;
            _label.style.fontSize = 22;
            _label.style.color = new Color(0.85f, 0.9f, 0.8f, 0.9f);
            _label.style.unityFontStyleAndWeight = FontStyle.Bold;
            root.Add(_label);
            _shown = -1;
        }

        private void OnDisable()
        {
            if (_label != null)
                _label.RemoveFromHierarchy();
            _label = null;
        }

        private void Update()
        {
            if (_label == null)
                return;
            var visible = GameSettings.SpeedrunTimer && SaveSystem.Data != null;
            _label.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible)
                return;
            var seconds = (long)SaveSystem.LivePlaytime;
            if (seconds == _shown)
                return;
            _shown = seconds;
            _label.text = Format(seconds);
        }

        public static string Format(long seconds) => $"{seconds / 3600}:{seconds / 60 % 60:00}:{seconds % 60:00}";
    }
}
