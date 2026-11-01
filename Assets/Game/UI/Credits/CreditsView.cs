using TentacleVania.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace TentacleVania.UI.Credits
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class CreditsView : MonoBehaviour
    {
        private const string Table = "UI";

        [SerializeField, Min(1f)] private float _duration = 24f;
        [SerializeField, Min(0f)] private float _fadeTime = 1.5f;
        [Tooltip("Seconds before a button press can skip the credits.")]
        [SerializeField, Min(0f)] private float _skipAfter = 2f;

        private VisualElement _panel;
        private Label _text;
        private Label _hint;
        private float _started = -1f;

        public bool Showing => _started >= 0f;

        public static float Scroll(float elapsed, float duration, float viewHeight, float textHeight)
        {
            var t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
            return Mathf.Lerp(viewHeight, -textHeight, t);
        }

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _panel = new VisualElement { name = "credits", pickingMode = PickingMode.Ignore };
            _panel.style.position = Position.Absolute;
            _panel.style.left = 0f;
            _panel.style.right = 0f;
            _panel.style.top = 0f;
            _panel.style.bottom = 0f;
            _panel.style.backgroundColor = Color.black;
            _panel.style.overflow = Overflow.Hidden;
            _panel.style.display = DisplayStyle.None;
            _text = new Label { pickingMode = PickingMode.Ignore };
            _text.style.position = Position.Absolute;
            _text.style.left = 0f;
            _text.style.right = 0f;
            _text.style.unityTextAlign = TextAnchor.UpperCenter;
            _text.style.whiteSpace = WhiteSpace.Normal;
            _text.style.color = new Color(0.85f, 0.95f, 0.88f);
            _text.style.fontSize = 26f;
            _hint = new Label { pickingMode = PickingMode.Ignore };
            _hint.style.position = Position.Absolute;
            _hint.style.right = 40f;
            _hint.style.bottom = 30f;
            _hint.style.color = new Color(0.8f, 0.9f, 0.85f, 0.6f);
            _hint.style.fontSize = 16f;
            _panel.Add(_text);
            _panel.Add(_hint);
            root.Add(_panel);
            Finale.CreditsRequested += Show;
            if (Finale.RollingCredits)
                Show();
        }

        private void OnDisable()
        {
            Finale.CreditsRequested -= Show;
            _panel?.RemoveFromHierarchy();
            _started = -1f;
        }

        private void Show()
        {
            _started = Time.unscaledTime;
            _text.text = LocalizationSettings.StringDatabase.GetLocalizedString(Table, "credits.body");
            _hint.text = LocalizationSettings.StringDatabase.GetLocalizedString(Table, "credits.skip");
            _panel.style.display = DisplayStyle.Flex;
            _panel.style.opacity = 0f;
        }

        private void LateUpdate()
        {
            if (_started < 0f)
                return;
            var elapsed = Time.unscaledTime - _started;
            _panel.style.opacity = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, _fadeTime));
            var view = _panel.resolvedStyle.height;
            var text = _text.resolvedStyle.height;
            if (!float.IsNaN(view) && !float.IsNaN(text))
                _text.style.top = Scroll(elapsed, _duration, view, text);
            if (elapsed >= _duration || (elapsed >= _skipAfter && SkipPressed()))
                Finish();
        }

        private static bool SkipPressed()
        {
            var pad = Gamepad.current;
            if (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame))
                return true;
            var keyboard = Keyboard.current;
            return keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame);
        }

        private void Finish()
        {
            _started = -1f;
            _panel.style.display = DisplayStyle.None;
            Finale.FinishCredits();
        }
    }
}
