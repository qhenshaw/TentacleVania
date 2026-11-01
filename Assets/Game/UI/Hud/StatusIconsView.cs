using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Combat.Status;
using TentacleVania.Creature;
using UnityEngine;
using UnityEngine.UIElements;

namespace TentacleVania.UI.Hud
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class StatusIconsView : MonoBehaviour
    {
        private const string ActiveClass = "hud__status-icon--active";

        private VisualElement _slow;
        private VisualElement _root;
        private VisualElement _stun;
        private CreatureVitals _vitals;
        private bool _stunShown;
        private Health _creature;
        private StatusEffects _status;
        private bool _slowShown;
        private bool _rootShown;
        private VisualElement _bleed;
        private VisualElement _burn;
        private VisualElement _corrode;
        private bool _corrodeShown;
        private VisualElement[] _bleedPips;
        private bool _bleedShown;
        private bool _burnShown;
        private int _bleedStacks = -1;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _slow = root.Q<VisualElement>("status-slow");
            _root = root.Q<VisualElement>("status-root");
            _stun = root.Q<VisualElement>("status-stun");
            _bleed = root.Q<VisualElement>("status-bleed");
            _burn = root.Q<VisualElement>("status-burn");
            _corrode = root.Q<VisualElement>("status-corrode");
            _corrodeShown = false;
            if (_bleed != null)
            {
                _bleed.Clear();
                _bleedPips = StatusPips.Build(_bleed);
            }
            _bleedShown = false;
            _burnShown = false;
            _bleedStacks = -1;
            _stunShown = false;
            _slowShown = false;
            _rootShown = false;
        }

        private void LateUpdate()
        {
            if (_slow == null || _root == null)
                return;
            var creature = CombatTargets.Creature;
            if (creature != _creature)
            {
                _creature = creature;
                _status = creature != null ? creature.GetComponent<StatusEffects>() : null;
                _vitals = creature != null ? creature.GetComponent<CreatureVitals>() : null;
            }
            Set(_slow, ref _slowShown, _status != null && _status.IsActive(StatusKind.Slow));
            Set(_root, ref _rootShown, _status != null && _status.IsActive(StatusKind.Root));
            if (_bleed != null)
            {
                Set(_bleed, ref _bleedShown, _status != null && _status.IsActive(StatusKind.Bleed));
                var stacks = _status != null ? _status.BleedStacks : 0;
                if (stacks != _bleedStacks)
                {
                    _bleedStacks = stacks;
                    StatusPips.Set(_bleedPips, stacks);
                }
            }
            if (_burn != null)
                Set(_burn, ref _burnShown, _status != null && _status.IsActive(StatusKind.Burn));
            if (_corrode != null)
                Set(_corrode, ref _corrodeShown, _status != null && _status.IsActive(StatusKind.Corrode));
            if (_stun != null)
                Set(_stun, ref _stunShown, _vitals != null && _vitals.IsStunned);
        }

        private static void Set(VisualElement icon, ref bool shown, bool active)
        {
            if (active == shown)
                return;
            shown = active;
            if (active)
                icon.AddToClassList(ActiveClass);
            else
                icon.RemoveFromClassList(ActiveClass);
        }
    }
}
