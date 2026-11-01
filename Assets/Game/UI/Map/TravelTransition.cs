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
        private Texture2D _vignette;
        private TravelKind _kind = (TravelKind)(-1);

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
            _label = new Label();
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
            if (FastTravel.Kind != _kind)
            {
                _kind = FastTravel.Kind;
                _label.text = LocalizationSettings.StringDatabase.GetLocalizedString("UI", _kind == TravelKind.Elevator ? "transit.elevator" : _kind == TravelKind.Freight ? "transit.freight" : _kind == TravelKind.Corridor ? "transit.corridor" : _kind == TravelKind.Crawl ? "transit.crawl" : "nest.travel.transit");
            }
            var cover = FastTravel.Cover;
            var pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * PulseRate * Mathf.PI * 2f);
            _overlay.style.opacity = cover;
            _overlay.style.display = cover > 0.001f ? DisplayStyle.Flex : DisplayStyle.None;
            if (FastTravel.Kind == TravelKind.Tunnel)
            {
                if (_vignette == null)
                    _vignette = Vignette();
                _overlay.style.backgroundImage = _vignette;
                _overlay.style.backgroundColor = Color.clear;
                _label.style.display = DisplayStyle.None;
                return;
            }
            _overlay.style.backgroundImage = StyleKeyword.None;
            _label.style.display = FastTravel.Kind == TravelKind.Recover ? DisplayStyle.None : DisplayStyle.Flex;
            _overlay.style.backgroundColor = Color.Lerp(new Color(0.07f, 0.01f, 0.05f), new Color(0.22f, 0.03f, 0.15f), pulse);
            _label.style.opacity = 0.6f + 0.4f * pulse;
        }

        private static Texture2D Vignette()
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = (x + 0.5f) / size * 2f - 1f;
                    var dy = (y + 0.5f) / size * 2f - 1f;
                    var alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 1.05f, Mathf.Sqrt(dx * dx + dy * dy)));
                    pixels[y * size + x] = new Color32(4, 0, 6, (byte)(Mathf.Clamp01(0.35f + alpha * 0.65f) * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }
    }
}
