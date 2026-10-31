using TentacleVania.Core.Saves;
using UnityEngine;
using UnityEngine.UIElements;

namespace TentacleVania.UI.Hud
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class SaveIndicatorView : MonoBehaviour
    {
        private const string VisibleClass = "hud__saving--visible";

        [SerializeField, Min(0.1f)] private float _showSeconds = 1.2f;

        private VisualElement _icon;
        private bool _visible;

        private void OnEnable()
        {
            _icon = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("saving");
            _visible = false;
        }

        private void LateUpdate()
        {
            if (_icon == null)
                return;
            var show = Time.unscaledTime - Autosave.LastSaveTime < _showSeconds;
            if (show == _visible)
                return;
            _visible = show;
            if (show)
                _icon.AddToClassList(VisibleClass);
            else
                _icon.RemoveFromClassList(VisibleClass);
        }
    }
}
