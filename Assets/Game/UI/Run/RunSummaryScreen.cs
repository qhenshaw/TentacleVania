using System.Collections;
using TentacleVania.Abilities.Mutations;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Runs;
using TentacleVania.Core.Saves;
using TentacleVania.Core.Scenes;
using TentacleVania.Creature;
using TentacleVania.UI.Fonts;
using TentacleVania.Weapons;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace TentacleVania.UI.Run
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class RunSummaryScreen : MonoBehaviour
    {
        private const string Table = "UI";
        private const string OpenClass = "pick--open";

        [SerializeField, Min(0f)] private float _deathDelay = 2f;
        [SerializeField, Min(0f)] private float _victoryDelay = 0.5f;

        private VisualElement _pick;
        private Label _title;
        private Label _hint;
        private VisualElement _rows;
        private Button _next;
        private string _build = "";
        private string _evolutions = "";

        public static RunSummaryScreen Instance { get; private set; }

        public bool IsOpen { get; private set; }

        private void OnEnable()
        {
            Instance = this;
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.StretchToParentSize();
            root.pickingMode = PickingMode.Ignore;
            _pick = root.Q("pick");
            _title = root.Q<Label>("title");
            _hint = root.Q<Label>("hint");
            _rows = root.Q("rows");
            _next = root.Q<Button>("next");
            var menu = root.Q<Button>("menu");
            UiText.Bind(_next, "run.summary.next");
            UiText.Bind(menu, "run.summary.menu");
            _next.clicked += NextCreature;
            menu.clicked += MainMenu;
            RunSession.Ended += OnEnded;
        }

        private void OnDisable()
        {
            RunSession.Ended -= OnEnded;
            if (Instance == this)
                Instance = null;
        }

        public void NextCreature()
        {
            if (!IsOpen)
                return;
            Close();
            SceneFlow.Load(SaveSystem.RunScene);
        }

        public void MainMenu()
        {
            if (!IsOpen)
                return;
            Close();
            SceneFlow.Load(SceneFlow.MainMenu);
        }

        private void OnEnded(bool won)
        {
            CaptureBuild();
            StartCoroutine(ShowAfter(won, won ? _victoryDelay : _deathDelay));
        }

        private IEnumerator ShowAfter(bool won, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            Show(won, RunSession.Last);
        }

        private void Show(bool won, RunRecord record)
        {
            IsOpen = true;
            _pick.AddToClassList(OpenClass);
            _title.text = Text(won ? "run.summary.won" : "run.summary.died");
            _hint.text = Text(won ? "run.summary.wonHint" : "run.summary.diedHint");
            _rows.Clear();
            if (record != null)
            {
                Line("run.summary.floor", (record.Floor + 1).ToString());
                Line("run.summary.rooms", record.Rooms.ToString());
                var seconds = Mathf.FloorToInt(record.Seconds);
                Line("run.summary.time", $"{seconds / 60}:{seconds % 60:00}");
                Line("run.summary.essence", string.Format(Text("run.summary.essenceValue"), record.Essence, SaveSystem.Data.Meta.Essence));
            }
            Line("run.summary.mutations", string.IsNullOrEmpty(_build) ? Text("run.pod.none") : _build);
            Line("run.summary.evolutions", string.IsNullOrEmpty(_evolutions) ? Text("run.pod.none") : _evolutions);
            _next.schedule.Execute(() => _next.Focus());
        }

        private void Close()
        {
            IsOpen = false;
            _pick.RemoveFromClassList(OpenClass);
        }

        private void CaptureBuild()
        {
            _build = "";
            _evolutions = "";
            var spawned = CreatureSpawner.Spawned;
            if (spawned == null)
                return;
            if (spawned.TryGetComponent(out MutationController mutations))
                foreach (var id in MutationLoadout.Equipped)
                {
                    var definition = mutations.Find(id);
                    if (definition != null)
                        _build += (_build.Length > 0 ? ", " : "") + Text(definition.TextKey + ".name");
                }
            if (spawned.TryGetComponent(out WeaponController weapons))
                for (var slot = 0; slot < WeaponController.Slots; slot++)
                {
                    var modifier = WeaponController.EquippedModifier(weapons.Weapon(slot));
                    if (modifier != null)
                        _evolutions += (_evolutions.Length > 0 ? ", " : "") + Text(modifier.TextKey + ".name");
                }
        }

        private void Line(string key, string value)
        {
            var line = new VisualElement();
            line.AddToClassList("pod__row");
            var label = new Label(Text(key));
            label.AddToClassList("pod__label");
            var text = new Label(value);
            text.AddToClassList("pod__value");
            text.style.whiteSpace = WhiteSpace.Normal;
            line.Add(label);
            line.Add(text);
            _rows.Add(line);
        }

        private static string Text(string key) => LocalizationSettings.StringDatabase.GetLocalizedString(Table, key);
    }
}
