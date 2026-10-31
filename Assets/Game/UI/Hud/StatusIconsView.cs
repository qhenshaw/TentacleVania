using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Combat.Status;
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
        private Health _creature;
        private StatusEffects _status;
        private bool _slowShown;
        private bool _rootShown;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _slow = root.Q<VisualElement>("status-slow");
            _root = root.Q<VisualElement>("status-root");
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
            }
            Set(_slow, ref _slowShown, _status != null && _status.IsActive(StatusKind.Slow));
            Set(_root, ref _rootShown, _status != null && _status.IsActive(StatusKind.Root));
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
