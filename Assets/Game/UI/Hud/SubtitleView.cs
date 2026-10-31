using TentacleVania.Audio;
using TentacleVania.Core;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace TentacleVania.UI.Hud
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class SubtitleView : MonoBehaviour
    {
        private const string ShownClass = "hud__subtitle--shown";
        private const string RadioClass = "hud__subtitle--radio";
        private const string CraneClass = "hud__subtitle--crane";

        private VisualElement _box;
        private Label _speaker;
        private Label _text;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _box = root.Q("subtitle");
            _speaker = root.Q<Label>("subtitle-speaker");
            _text = root.Q<Label>("subtitle-text");
            PaDirector.LineStarted += OnLineStarted;
            PaDirector.LineEnded += OnLineEnded;
            OnLineEnded();
        }

        private void OnDisable()
        {
            PaDirector.LineStarted -= OnLineStarted;
            PaDirector.LineEnded -= OnLineEnded;
        }

        private void OnLineStarted(string text, PaChannel channel)
        {
            if (_box == null || !GameSettings.Subtitles)
                return;
            _speaker.text = LocalizationSettings.StringDatabase.GetLocalizedString(PaScript.Table, channel switch
            {
                PaChannel.PA => "speaker.pa",
                PaChannel.Radio => "speaker.radio",
                _ => "speaker.crane",
            });
            _text.text = text;
            _box.EnableInClassList(RadioClass, channel == PaChannel.Radio);
            _box.EnableInClassList(CraneClass, channel == PaChannel.Crane);
            _box.AddToClassList(ShownClass);
        }

        private void OnLineEnded()
        {
            if (_box != null)
                _box.RemoveFromClassList(ShownClass);
        }
    }
}
