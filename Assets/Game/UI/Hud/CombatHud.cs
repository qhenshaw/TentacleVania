using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Combat.Status;
using TentacleVania.Creature;
using TentacleVania.Enemies;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

namespace TentacleVania.UI.Hud
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class CombatHud : MonoBehaviour
    {
        private const int EnemyBarCount = 32;
        private const int HealthTicks = 10;
        private const string ChargeFullClass = "hud-bar--charge-full";
        private const string FeedableClass = "enemy-bar--feedable";

        [SerializeField, Min(0f)] private float _drainDelay = 0.5f;
        [SerializeField, Min(0f)] private float _drainSpeed = 0.6f;
        [SerializeField, Min(0f)] private float _healFillSpeed = 0.35f;
        [SerializeField, Min(0f)] private float _pulseSpeed = 5f;
        [SerializeField, Min(0f)] private float _enemyBarShowTime = 4f;
        [SerializeField, Min(0.01f)] private float _enemyBarFadeTime = 0.5f;
        [SerializeField, Min(0f)] private float _enemyBarClearance = 0.5f;

        private readonly EnemyBar[] _enemyBars = new EnemyBar[EnemyBarCount];

        private VisualElement _root;
        private VisualElement _player;
        private VisualElement _healthTrail;
        private VisualElement _healthHeal;
        private VisualElement _healthFill;
        private VisualElement _chargeBar;
        private VisualElement _chargeFill;
        private VisualElement _chargeGlow;
        private BarTrail _health;
        private Health _creature;
        private CreatureCharge _charge;
        private int _frame;

        public static bool Visible { get; set; } = true;
        public static bool AlwaysShowEnemyBars { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            Visible = true;
            AlwaysShowEnemyBars = false;
        }

        private void OnEnable()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _root.pickingMode = PickingMode.Ignore;
            _player = _root.Q("player");
            _healthTrail = _root.Q("health-trail");
            _healthHeal = _root.Q("health-heal");
            _healthFill = _root.Q("health-fill");
            _chargeBar = _root.Q("charge");
            _chargeFill = _root.Q("charge-fill");
            _chargeGlow = _root.Q("charge-glow");
            _health = new(_drainDelay, _drainSpeed, _healFillSpeed);
            _creature = null;

            var ticks = _root.Q("health-ticks");
            for (var i = 1; i < HealthTicks; i++)
            {
                var tick = new VisualElement { pickingMode = PickingMode.Ignore };
                tick.AddToClassList("hud-bar__tick");
                tick.style.left = Length.Percent(100f * i / HealthTicks);
                ticks.Add(tick);
            }

            var layer = _root.Q("enemy-layer");
            for (var i = 0; i < _enemyBars.Length; i++)
            {
                _enemyBars[i] = new(new(_drainDelay, _drainSpeed, _healFillSpeed));
                layer.Add(_enemyBars[i].Root);
            }
        }

        private void OnDisable()
        {
            _root.Q("health-ticks").Clear();
            _root.Q("enemy-layer").Clear();
        }

        private void LateUpdate()
        {
            var dt = Time.deltaTime;
            _root.style.display = Visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!Visible)
                return;
            UpdatePlayer(dt);
            UpdateEnemies(Time.time, dt);
        }

        private void UpdatePlayer(float dt)
        {
            var creature = CombatTargets.Creature;
            if (creature != _creature)
            {
                _creature = creature;
                _charge = creature != null ? creature.GetComponent<CreatureCharge>() : null;
                if (creature != null)
                    _health.Snap(creature.Normalized);
            }
            _player.style.display = creature != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (creature == null)
                return;

            _health.Update(creature.Normalized, dt);
            SetWidth(_healthTrail, _health.Trail);
            SetWidth(_healthHeal, _health.Value);
            SetWidth(_healthFill, _health.Shown);

            var full = _charge != null && _charge.IsFull;
            SetWidth(_chargeFill, _charge != null ? _charge.Meter.Normalized : 0f);
            _chargeBar.EnableInClassList(ChargeFullClass, full);
            _chargeGlow.style.opacity = full ? Pulse() * 0.6f : 0f;
        }

        private void UpdateEnemies(float now, float dt)
        {
            _frame++;
            var manager = EnemyManager.Current;
            var camera = UnityEngine.Camera.main;
            if (manager != null && camera != null)
            {
                var enemies = manager.Enemies;
                for (var i = 0; i < enemies.Count; i++)
                {
                    var enemy = enemies[i];
                    if (enemy.State == EnemyState.Dead || enemy.State == EnemyState.Consumed || enemy.IsBoss)
                        continue;
                    var bar = Find(enemy) ?? Acquire(enemy);
                    if (bar == null)
                        continue;
                    bar.Seen = _frame;
                    UpdateEnemyBar(bar, camera, now, dt);
                }
            }
            foreach (var bar in _enemyBars)
            {
                if (bar.IsAssigned && bar.Seen != _frame)
                    bar.Release();
            }
        }

        private void UpdateEnemyBar(EnemyBar bar, UnityEngine.Camera camera, float now, float dt)
        {
            var enemy = bar.Enemy;
            var health = enemy.Health;
            var hp = health.Normalized;
            var stagger = enemy.Stagger.Normalized;
            var armor = health.MaxArmor > 0f ? health.Armor / health.MaxArmor : 0f;
            if (hp != bar.LastHealth || stagger != bar.LastStagger || armor != bar.LastArmor)
            {
                bar.LastHit = now;
                bar.LastHealth = hp;
                bar.LastStagger = stagger;
                bar.LastArmor = armor;
            }

            var feedable = enemy.IsFeedable;
            var since = now - bar.LastHit;
            var alpha = AlwaysShowEnemyBars || feedable ? 1f : 1f - Mathf.Clamp01((since - _enemyBarShowTime) / _enemyBarFadeTime);
            var world = enemy.transform.position + Vector3.up * bar.Height;
            var visible = alpha > 0f && camera.WorldToViewportPoint(world).z > 0f;
            bar.Root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            bar.Trail.Update(hp, dt);
            if (!visible)
                return;

            var position = RuntimePanelUtils.CameraTransformWorldToPanel(_root.panel, world, camera);
            bar.Root.style.left = position.x;
            bar.Root.style.top = position.y;
            bar.Root.style.opacity = alpha;
            SetWidth(bar.HealthTrail, bar.Trail.Trail);
            SetWidth(bar.HealthFill, bar.Trail.Shown);
            SetWidth(bar.Armor, armor);
            SetWidth(bar.StaggerFill, stagger);
            bar.Root.EnableInClassList(FeedableClass, feedable);
            bar.SetStatus(enemy.Status);
            bar.HealthFill.style.opacity = feedable ? 0.55f + 0.45f * Pulse() : 1f;
        }

        private EnemyBar Find(Enemy enemy)
        {
            foreach (var bar in _enemyBars)
            {
                if (bar.IsAssigned && bar.Enemy == enemy)
                    return bar;
            }
            return null;
        }

        private EnemyBar Acquire(Enemy enemy)
        {
            foreach (var bar in _enemyBars)
            {
                if (bar.IsAssigned)
                    continue;
                var height = enemy.TryGetComponent<NavMeshAgent>(out var agent) ? agent.height : 2f;
                bar.Assign(enemy, height + _enemyBarClearance);
                return bar;
            }
            return null;
        }

        private float Pulse() => 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * _pulseSpeed);

        private static void SetWidth(VisualElement element, float normalized) =>
            element.style.width = Length.Percent(Mathf.Clamp01(normalized) * 100f);

        private sealed class EnemyBar
        {
            public EnemyBar(BarTrail trail)
            {
                Trail = trail;
                Root = Element("enemy-bar");
                var healthBar = Element("enemy-bar__health");
                HealthTrail = Element("enemy-bar__trail");
                HealthFill = Element("enemy-bar__fill");
                Armor = Element("enemy-bar__armor");
                var staggerBar = Element("enemy-bar__stagger");
                StaggerFill = Element("enemy-bar__stagger-fill");
                healthBar.Add(HealthTrail);
                healthBar.Add(HealthFill);
                healthBar.Add(Armor);
                staggerBar.Add(StaggerFill);
                var statusRow = Element("enemy-bar__status");
                Bleed = Element("enemy-bar__bleed");
                Burn = Element("enemy-bar__burn");
                BleedPips = StatusPips.Build(Bleed);
                statusRow.Add(Bleed);
                statusRow.Add(Burn);
                Static = Element("enemy-bar__static");
                StaticFill = Element("enemy-bar__static-fill");
                Static.Add(StaticFill);
                statusRow.Add(Static);
                Root.Add(healthBar);
                Root.Add(staggerBar);
                Root.Add(statusRow);
                Root.style.display = DisplayStyle.None;
            }

            public VisualElement Root { get; }
            public VisualElement HealthTrail { get; }
            public VisualElement HealthFill { get; }
            public VisualElement Armor { get; }
            public VisualElement StaggerFill { get; }
            public VisualElement Bleed { get; }
            public VisualElement Burn { get; }
            public VisualElement[] BleedPips { get; }
            public int BleedStacks { get; private set; }
            public bool Burning { get; private set; }
            public VisualElement Static { get; }
            public VisualElement StaticFill { get; }
            public float StaticShown { get; private set; }
            public BarTrail Trail { get; }
            public Enemy Enemy { get; private set; }
            public bool IsAssigned { get; private set; }
            public float Height { get; private set; }
            public int Seen { get; set; }
            public float LastHit { get; set; }
            public float LastHealth { get; set; }
            public float LastStagger { get; set; }
            public float LastArmor { get; set; }

            public void Assign(Enemy enemy, float height)
            {
                Enemy = enemy;
                IsAssigned = true;
                Height = height;
                LastHit = float.NegativeInfinity;
                LastHealth = enemy.Health.Normalized;
                LastStagger = enemy.Stagger.Normalized;
                LastArmor = enemy.Health.MaxArmor > 0f ? enemy.Health.Armor / enemy.Health.MaxArmor : 0f;
                Trail.Snap(LastHealth);
                BleedStacks = -1;
                Burning = true;
                StaticShown = -1f;
                SetStatus(enemy.Status);
            }

            public void SetStatus(StatusEffects status)
            {
                var stacks = status != null ? status.BleedStacks : 0;
                var burning = status != null && status.IsActive(StatusKind.Burn);
                if (stacks != BleedStacks)
                {
                    BleedStacks = stacks;
                    Bleed.style.display = stacks > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                    StatusPips.Set(BleedPips, stacks);
                }
                if (burning != Burning)
                {
                    Burning = burning;
                    Burn.style.display = burning ? DisplayStyle.Flex : DisplayStyle.None;
                }
                var charge = status != null ? status.StaticNormalized : 0f;
                if (!Mathf.Approximately(charge, StaticShown))
                {
                    Static.style.display = charge > 0f ? DisplayStyle.Flex : DisplayStyle.None;
                    SetWidth(StaticFill, charge);
                    StaticShown = charge;
                }
            }

            public void Release()
            {
                Enemy = null;
                IsAssigned = false;
                Root.style.display = DisplayStyle.None;
            }

            private static VisualElement Element(string className)
            {
                var element = new VisualElement { pickingMode = PickingMode.Ignore };
                element.AddToClassList(className);
                return element;
            }
        }
    }
}
