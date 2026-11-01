using TentacleVania.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TentacleVania.UI.Hud
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class LetterboxView : MonoBehaviour
    {
        [Tooltip("Height of each bar as a percentage of the screen.")]
        [SerializeField, Range(0f, 25f)] private float _barPercent = 11f;
        [SerializeField, Min(0.01f)] private float _slideTime = 0.35f;

        private VisualElement _top;
        private VisualElement _bottom;
        private float _amount;
        private float _shown = -1f;

        public float Amount => _amount;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _top = Bar(true);
            _bottom = Bar(false);
            root.Insert(0, _bottom);
            root.Insert(0, _top);
            _amount = Cutscene.Active ? 1f : 0f;
            _shown = -1f;
            Apply();
        }

        private void OnDisable()
        {
            _top?.RemoveFromHierarchy();
            _bottom?.RemoveFromHierarchy();
        }

        private static VisualElement Bar(bool top)
        {
            var bar = new VisualElement { name = top ? "letterbox-top" : "letterbox-bottom", pickingMode = PickingMode.Ignore };
            bar.style.position = Position.Absolute;
            bar.style.left = 0f;
            bar.style.right = 0f;
            if (top)
                bar.style.top = 0f;
            else
                bar.style.bottom = 0f;
            bar.style.backgroundColor = Color.black;
            return bar;
        }

        private void LateUpdate()
        {
            _amount = Mathf.MoveTowards(_amount, Cutscene.Active ? 1f : 0f, Time.unscaledDeltaTime / _slideTime);
            Apply();
        }

        private void Apply()
        {
            if (Mathf.Approximately(_amount, _shown))
                return;
            _shown = _amount;
            var height = Length.Percent(_barPercent * Mathf.SmoothStep(0f, 1f, _amount));
            _top.style.height = height;
            _bottom.style.height = height;
            var display = _amount > 0f ? DisplayStyle.Flex : DisplayStyle.None;
            _top.style.display = display;
            _bottom.style.display = display;
        }
    }
}
