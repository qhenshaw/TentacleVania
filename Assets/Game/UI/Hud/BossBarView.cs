using TentacleVania.Bosses;
using TentacleVania.Enemies;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UIElements;

namespace TentacleVania.UI.Hud
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class BossBarView : MonoBehaviour
    {
        private const string Table = "UI";
        private const string RewardKey = "boss.reward";
        private const string HiddenClass = "hud__boss--hidden";
        private const string RewardShownClass = "hud__reward--shown";
        private const string StunnedClass = "hud__boss--stunned";

        [SerializeField, Min(0f)] private float _drainDelay = 0.5f;
        [SerializeField, Min(0f)] private float _drainSpeed = 0.4f;
        [SerializeField, Min(0.1f)] private float _rewardSeconds = 3.5f;

        private readonly LocalizedString _rewardFormat = new(Table, RewardKey);
        private LocalizedString _name;
        private VisualElement _bar;
        private Label _nameLabel;
        private VisualElement _trail;
        private VisualElement _fill;
        private VisualElement _ticks;
        private VisualElement _staggerFill;
        private Label _reward;
        private BarTrail _health;
        private BossEnemy _shown;
        private string _rewardText = "{0}";
        private float _rewardUntil;
        private bool _rewardVisible;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _bar = root.Q("boss");
            _nameLabel = root.Q<Label>("boss-name");
            _trail = root.Q("boss-trail");
            _fill = root.Q("boss-fill");
            _ticks = root.Q("boss-ticks");
            _staggerFill = root.Q("boss-stagger-fill");
            _reward = root.Q<Label>("reward");
            _health = new(_drainDelay, _drainSpeed, 1f);
            _shown = null;
            _rewardVisible = false;
            _bar?.AddToClassList(HiddenClass);
            _rewardFormat.StringChanged += OnRewardFormat;
            BossEnemy.Absorbed += OnAbsorbed;
        }

        private void OnDisable()
        {
            _rewardFormat.StringChanged -= OnRewardFormat;
            BossEnemy.Absorbed -= OnAbsorbed;
            SetName(null);
        }

        private void LateUpdate()
        {
            if (_bar == null)
                return;
            var boss = BossEnemy.Active;
            var alive = boss != null && boss.Health.IsAlive;
            if (boss != _shown)
                Show(boss);
            if (_shown != null)
            {
                var dt = Time.unscaledDeltaTime;
                _health.Update(alive ? _shown.Health.Normalized : 0f, dt);
                SetWidth(_trail, _health.Trail);
                SetWidth(_fill, _health.Shown);
                SetWidth(_staggerFill, alive ? _shown.Stagger.Normalized : 0f);
                _bar.EnableInClassList(StunnedClass, alive && _shown.IsStaggerBroken);
                _bar.EnableInClassList(HiddenClass, !alive && _health.Trail <= 0.001f);
            }
            var showReward = Time.unscaledTime < _rewardUntil;
            if (showReward != _rewardVisible && _reward != null)
            {
                _rewardVisible = showReward;
                _reward.EnableInClassList(RewardShownClass, showReward);
            }
        }

        private void Show(BossEnemy boss)
        {
            _shown = boss;
            _ticks.Clear();
            if (boss == null)
            {
                _bar.AddToClassList(HiddenClass);
                SetName(null);
                return;
            }
            _health.Snap(boss.Health.Normalized);
            var thresholds = boss.Boss.Thresholds;
            for (var i = 1; i < thresholds.Count; i++)
            {
                var tick = new VisualElement { pickingMode = PickingMode.Ignore };
                tick.AddToClassList("hud__boss-tick");
                tick.style.left = Length.Percent(thresholds[i] * 100f);
                _ticks.Add(tick);
            }
            SetName(boss.Boss.NameKey);
            _bar.RemoveFromClassList(HiddenClass);
        }

        private void SetName(string key)
        {
            if (_name != null)
                _name.StringChanged -= OnName;
            _name = null;
            if (string.IsNullOrEmpty(key))
                return;
            _name = new LocalizedString(Table, key);
            _name.StringChanged += OnName;
        }

        private void OnName(string value) => _nameLabel.text = value;

        private void OnRewardFormat(string value) => _rewardText = value;

        private void OnAbsorbed(BossEnemy boss)
        {
            var ability = boss.Boss.RewardAbility;
            if (ability == null || _reward == null)
                return;
            _reward.text = string.Format(_rewardText, ability.DisplayName);
            _rewardUntil = Time.unscaledTime + _rewardSeconds;
        }

        private static void SetWidth(VisualElement element, float normalized) =>
            element.style.width = Length.Percent(Mathf.Clamp01(normalized) * 100f);
    }
}
