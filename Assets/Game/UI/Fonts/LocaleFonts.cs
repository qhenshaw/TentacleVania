using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace TentacleVania.UI.Fonts
{
    [AddComponentMenu("")]
    public sealed class LocaleFonts : MonoBehaviour
    {
        private const string SettingsPath = "Fonts/UITextSettings";
        private const string FontPathFormat = "Fonts/NotoSansCJK-{0}-SDF";

        private static LocaleFonts _instance;

        private PanelTextSettings _settings;

        public static string Region { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            Region = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (_instance != null || !Application.isPlaying)
                return;
            var host = new GameObject("LocaleFonts");
            DontDestroyOnLoad(host);
            _instance = host.AddComponent<LocaleFonts>();
        }

        private void Awake() => _settings = Resources.Load<PanelTextSettings>(SettingsPath);

        private void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += Apply;
            if (LocalizationSettings.SelectedLocale != null)
                Apply(LocalizationSettings.SelectedLocale);
        }

        private void OnDisable() => LocalizationSettings.SelectedLocaleChanged -= Apply;

        private void Apply(Locale locale)
        {
            if (_settings == null)
                return;
            var region = LocaleFontRules.RegionFor(locale != null ? locale.Identifier.Code : null);
            if (region == Region)
                return;
            Region = region;
            _settings.fallbackFontAssets.Clear();
            if (region == null)
                return;
            var font = Resources.Load<FontAsset>(string.Format(FontPathFormat, region));
            if (font != null)
                _settings.fallbackFontAssets.Add(font);
        }
    }
}
