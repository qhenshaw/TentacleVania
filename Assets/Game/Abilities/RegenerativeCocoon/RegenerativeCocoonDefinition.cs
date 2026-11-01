using FMODUnity;
using UnityEngine;

namespace TentacleVania.Abilities.RegenerativeCocoon
{
    [CreateAssetMenu(menuName = "TentacleVania/Abilities/Regenerative Cocoon")]
    public sealed class RegenerativeCocoonDefinition : AbilityDefinition
    {
        [Tooltip("Max health healed per second while cocooned, per tier (index 0 = tier 1).")]
        [SerializeField] private float[] _healPerSecond = { 0.12f, 0.18f, 0.18f };
        [Tooltip("Longest the cocoon lasts; it also ends at full health.")]
        [SerializeField, Min(0.5f)] private float _maxDuration = 4f;
        [Tooltip("Tier from which the cocoon absorbs one hit without breaking (Hardened cocoon).")]
        [SerializeField, Min(1)] private int _hardenedTier = 3;
        [SerializeField] private Transform _shell;
        [SerializeField] private EventReference _activateSound;
        [SerializeField] private EventReference _breakSound;

        public float MaxDuration => _maxDuration;
        public int HardenedTier => _hardenedTier;
        public Transform Shell => _shell;
        public EventReference ActivateSound => _activateSound;
        public EventReference BreakSound => _breakSound;

        public float HealPerSecondAt(int tier) =>
            _healPerSecond == null || _healPerSecond.Length == 0 ? 0f : _healPerSecond[Mathf.Clamp(tier - 1, 0, _healPerSecond.Length - 1)];

        public override AbilityRuntime CreateRuntime(AbilityContext context) => new RegenerativeCocoonRuntime(this, context);
    }
}
