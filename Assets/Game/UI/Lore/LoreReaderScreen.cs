using TentacleVania.Core.Progression;
using TentacleVania.Creature;
using TentacleVania.Creature.Movement;
using TentacleVania.World.Lore;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace TentacleVania.UI.Lore
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class LoreReaderScreen : MonoBehaviour
    {
        private const string Table = "UI";
        private const string OpenClass = "reader--open";

        private VisualElement _reader;
        private Label _header;
        private Label _title;
        private Label _from;
        private Label _body;
        private ScrollView _scroll;
        private Button _close;
        private CreatureMotor _lockedMotor;
        private Focusable _returnFocus;

        public static LoreReaderScreen Instance { get; private set; }

        public bool IsOpen { get; private set; }
        public string CurrentId { get; private set; }

        private void OnEnable()
        {
            Instance = this;
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.StretchToParentSize();
            root.pickingMode = PickingMode.Ignore;
            _reader = root.Q("reader");
            _header = root.Q<Label>("header");
            _title = root.Q<Label>("title");
            _from = root.Q<Label>("from");
            _body = root.Q<Label>("body");
            _scroll = root.Q<ScrollView>("scroll");
            _close = root.Q<Button>("close");
            _close.text = T(Table, "lore.close");
            _close.clicked += Close;
            _reader.RegisterCallback<NavigationCancelEvent>(_ => Close());
            LoreLogs.ReadRequested += Open;
        }

        private void OnDisable()
        {
            LoreLogs.ReadRequested -= Open;
            if (IsOpen)
                Close();
            if (Instance == this)
                Instance = null;
        }

        public void Open(string id)
        {
            var library = LoreLibrary.Instance;
            var log = library != null ? library.Find(id) : null;
            if (log == null)
                return;
            if (!IsOpen)
                _returnFocus = _reader.focusController?.focusedElement as Focusable;
            IsOpen = true;
            CurrentId = id;
            _header.text = T(Table, log.Format == LoreFormat.Audio ? "lore.header.audio" : "lore.header.text");
            _title.text = T(LoreLog.Table, log.TitleKey);
            var from = T(LoreLog.Table, log.FromKey);
            _from.text = from;
            _from.style.display = string.IsNullOrEmpty(from) ? DisplayStyle.None : DisplayStyle.Flex;
            _body.text = T(LoreLog.Table, log.BodyKey);
            _scroll.scrollOffset = Vector2.zero;
            _reader.AddToClassList(OpenClass);
            var spawned = CreatureSpawner.Spawned;
            if (_lockedMotor == null && spawned != null && spawned.TryGetComponent(out CreatureMotor motor))
            {
                _lockedMotor = motor;
                motor.LockControl();
            }
            LoreAudio.Play(log);
            _close.schedule.Execute(() => _close.Focus());
        }

        public void Close()
        {
            if (!IsOpen)
                return;
            IsOpen = false;
            CurrentId = null;
            _reader.RemoveFromClassList(OpenClass);
            LoreAudio.Stop();
            if (_lockedMotor != null)
                _lockedMotor.UnlockControl();
            _lockedMotor = null;
            if (_returnFocus != null)
                _returnFocus.Focus();
            _returnFocus = null;
        }

        private static string T(string table, string key)
        {
            var text = LocalizationSettings.StringDatabase.GetLocalizedString(table, key);
            return text != null && text.StartsWith("No translation found", System.StringComparison.Ordinal) ? "" : text;
        }
    }
}
