using TentacleVania.World.Opening;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace TentacleVania.UI.Hud
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class OpeningSkipView : MonoBehaviour
    {
        private const string Table = "UI";

        private VisualElement _box;
        private HoldRing _ring;
        private Label _label;
        private bool _shown;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _box = new VisualElement { name = "opening-skip", pickingMode = PickingMode.Ignore };
            _box.style.position = Position.Absolute;
            _box.style.right = 48f;
            _box.style.bottom = 40f;
            _box.style.flexDirection = FlexDirection.Row;
            _box.style.alignItems = Align.Center;
            _box.style.display = DisplayStyle.None;
            _label = new Label { pickingMode = PickingMode.Ignore };
            _label.style.color = new Color(0.8f, 0.95f, 0.85f, 0.85f);
            _label.style.fontSize = 18f;
            _label.style.marginRight = 10f;
            _ring = new HoldRing(26f);
            _box.Add(_label);
            _box.Add(_ring);
            root.Add(_box);
            _shown = false;
        }

        private void OnDisable() => _box?.RemoveFromHierarchy();

        private void LateUpdate()
        {
            var playing = OpeningSequence.Playing;
            if (playing != _shown)
            {
                _shown = playing;
                _box.style.display = playing ? DisplayStyle.Flex : DisplayStyle.None;
                if (playing)
                    _label.text = LocalizationSettings.StringDatabase.GetLocalizedString(Table, "opening.holdToSkip");
            }
            if (playing)
                _ring.Progress = OpeningSequence.SkipProgress;
        }
    }
}
