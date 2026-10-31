using System.Collections.Generic;
using TentacleVania.Core.Saves;
using TentacleVania.Core.Scenes;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UIElements;

namespace TentacleVania.UI.Menu
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class MainMenuScreen : MonoBehaviour
    {
        private const string Table = "UI";

        [SerializeField] private string _gameplayScene = SceneFlow.FirstGameplay;

        private readonly List<(LocalizedString text, LocalizedString.ChangeHandler handler)> _bindings = new();
        private Button _play;

        public static MainMenuScreen Instance { get; private set; }

        private void OnEnable()
        {
            Instance = this;
            var root = GetComponent<UIDocument>().rootVisualElement;
            Bind(root.Q<Label>("title"), "menu.title");
            _play = root.Q<Button>("play");
            Bind(_play, "menu.play");
            _play.clicked += Play;
            var settings = root.Q<Button>("settings");
            Bind(settings, "menu.settings");
            settings.clicked += OpenSettings;
            var quit = root.Q<Button>("quit");
            Bind(quit, "menu.quit");
            quit.clicked += Quit;
            _play.schedule.Execute(() => _play.Focus());
        }

        private void OnDisable()
        {
            foreach (var (text, handler) in _bindings)
                text.StringChanged -= handler;
            _bindings.Clear();
            if (Instance == this)
                Instance = null;
        }

        public void Play()
        {
            _play.SetEnabled(false);
            const int slot = 0;
            if (SaveSystem.Exists(slot) && SaveSystem.Load(slot))
            {
                SceneFlow.Load(SaveSystem.ContinueScene(_gameplayScene));
                return;
            }
            SaveSystem.NewGame(slot);
            SceneFlow.Load(_gameplayScene);
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void OpenSettings()
        {
            var screen = Settings.SettingsScreen.Instance;
            if (screen != null)
                screen.Open(() => _play.Focus());
        }

        private void Bind(TextElement element, string key)
        {
            var text = new LocalizedString(Table, key);
            LocalizedString.ChangeHandler handler = value => element.text = value;
            text.StringChanged += handler;
            _bindings.Add((text, handler));
        }
    }
}
