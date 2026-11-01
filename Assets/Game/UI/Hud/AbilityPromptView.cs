using TentacleVania.Abilities;
using TentacleVania.Combat;
using TentacleVania.Core.Input;
using TentacleVania.Enemies;
using TentacleVania.World.Arenas;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TentacleVania.UI.Hud
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class AbilityPromptView : MonoBehaviour
    {
        [SerializeField] private InputGlyphLibrary _glyphs;
        [Tooltip("An enemy this close counts as being in combat even outside an arena fight.")]
        [SerializeField, Min(0f)] private float _combatRange = 12f;
        [Tooltip("World offset from the creature's position to the prompt.")]
        [SerializeField] private Vector3 _offset = new(0.9f, 1.6f, 0f);
        [SerializeField, Min(8f)] private float _size = 34f;
        [SerializeField, Min(0f)] private float _pulseSpeed = 4f;

        private VisualElement _prompt;
        private Health _creature;
        private AbilityController _ability;
        private InputDeviceKind _iconDevice = (InputDeviceKind)(-1);
        private Gamepad _iconPad;
        private bool _shown;

        public bool Shown => _shown;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _prompt = new VisualElement { name = "ability-prompt", pickingMode = PickingMode.Ignore };
            _prompt.style.position = Position.Absolute;
            _prompt.style.width = _size;
            _prompt.style.height = _size;
            _prompt.style.marginLeft = -_size * 0.5f;
            _prompt.style.marginTop = -_size * 0.5f;
            _prompt.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            _prompt.style.display = DisplayStyle.None;
            root.Add(_prompt);
            _shown = false;
        }

        private void OnDisable() => _prompt?.RemoveFromHierarchy();

        private void LateUpdate()
        {
            var creature = CombatTargets.Creature;
            if (creature != _creature)
            {
                _creature = creature;
                _ability = creature != null ? creature.GetComponent<AbilityController>() : null;
            }
            var camera = UnityEngine.Camera.main;
            var show = _ability != null && _ability.IsReady && camera != null && CombatHud.Visible && InCombat(creature.transform.position);
            if (show)
            {
                RefreshIcon();
                show = _prompt.style.backgroundImage.value.texture != null;
            }
            if (show != _shown)
            {
                _shown = show;
                _prompt.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (!show)
                return;
            var world = creature.transform.position + camera.transform.right * _offset.x + Vector3.up * _offset.y;
            var position = RuntimePanelUtils.CameraTransformWorldToPanel(_prompt.panel, world, camera);
            _prompt.style.left = position.x;
            _prompt.style.top = position.y;
            _prompt.style.opacity = 0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * _pulseSpeed);
        }

        private bool InCombat(Vector3 position)
        {
            var arena = ArenaEncounter.Active;
            if (arena != null && arena.State is ArenaState.Spawning or ArenaState.Fighting or ArenaState.BetweenWaves)
                return true;
            var manager = EnemyManager.Current;
            if (manager == null)
                return false;
            var range = _combatRange * _combatRange;
            var enemies = manager.Enemies;
            for (var i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy.State is EnemyState.Dead or EnemyState.Consumed || !enemy.Health.IsAlive)
                    continue;
                if ((enemy.transform.position - position).sqrMagnitude <= range)
                    return true;
            }
            return false;
        }

        private void RefreshIcon()
        {
            var device = GameplayInput.Default.LastDevice;
            var pad = Gamepad.current;
            if (device == _iconDevice && pad == _iconPad)
                return;
            _iconDevice = device;
            _iconPad = pad;
            _prompt.style.backgroundImage = _glyphs != null ? _glyphs.ForAction("Ability") : null;
        }
    }
}
