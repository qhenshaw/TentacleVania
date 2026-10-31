using TentacleVania.World.Map;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace TentacleVania.UI.Map
{
    [AddComponentMenu("")]
    public sealed class TravelTransition : MonoBehaviour
    {
        private const string PanelPath = "TransitionPanelSettings";
        private const float PulseRate = 2.4f;

        private static TravelTransition _instance;

        private VisualElement _overlay;
        private Label _label;

        public static float ShownOpacity => _instance != null && _instance._overlay != null ? _instance._overlay.resolvedStyle.opacity : 0f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Hook()
        {
            if (!Application.isPlaying)
                return;
            FastTravel.PhaseChanged -= OnPhaseChanged;
            FastTravel.PhaseChanged += OnPhaseChanged;
        }

        private static void OnPhaseChanged(TravelPhase phase)
        {
            if (phase != TravelPhase.Covering || _instance != null)
                return;
            var panel = Resources.Load<PanelSettings>(PanelPath);
            if (panel == null)
                return;
            var host = new GameObject("TravelTransition");
            DontDestroyOnLoad(host);
            var document = host.AddComponent<UIDocument>();
            document.panelSettings = panel;
            _instance = host.AddComponent<TravelTransition>();
        }

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.StretchToParentSize();
            root.pickingMode = PickingMode.Ignore;
            _overlay = new VisualElement { pickingMode = PickingMode.Ignore };
            _overlay.style.position = Position.Absolute;
            _overlay.style.left = _overlay.style.top = _overlay.style.right = _overlay.style.bottom = 0f;
            _overlay.style.alignItems = Align.Center;
            _overlay.style.justifyContent = Justify.Center;
            _label = new Label(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "nest.travel.transit"));
            _label.style.fontSize = 30f;
            _label.style.letterSpacing = 8f;
            _label.style.color = new Color(0.95f, 0.5f, 0.8f);
            _overlay.Add(_label);
            root.Add(_overlay);
        }

        private void Update()
        {
            if (_overlay == null)
                return;
            var cover = FastTravel.Cover;
            var pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * PulseRate * Mathf.PI * 2f);
            _overlay.style.opacity = cover;
            _overlay.style.display = cover > 0.001f ? DisplayStyle.Flex : DisplayStyle.None;
            _overlay.style.backgroundColor = Color.Lerp(new Color(0.07f, 0.01f, 0.05f), new Color(0.22f, 0.03f, 0.15f), pulse);
            _label.style.opacity = 0.6f + 0.4f * pulse;
        }
    }
}
