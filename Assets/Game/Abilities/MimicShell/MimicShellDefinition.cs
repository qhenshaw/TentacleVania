using FMODUnity;
using UnityEngine;

namespace TentacleVania.Abilities.MimicShell
{
    [CreateAssetMenu(menuName = "TentacleVania/Abilities/Mimic Shell")]
    public sealed class MimicShellDefinition : AbilityDefinition
    {
        [Tooltip("Disguise seconds per tier (index 0 = tier 1); tier 3 (Infiltrate) lasts until the creature attacks, capped here.")]
        [SerializeField] private float[] _duration = { 6f, 9f, 30f };
        [Tooltip("Damage multiplier of the ambush (the first hit out of the disguise), per tier.")]
        [SerializeField] private float[] _ambush = { 2f, 2.5f, 2.5f };
        [SerializeField, Min(1f)] private float _ambushStagger = 2f;
        [SerializeField, ColorUsage(false, true)] private Color _disguiseGlow = new(0.55f, 0.6f, 0.5f, 1f);
        [SerializeField, Range(0f, 1f)] private float _disguiseAmount = 0.6f;
        [SerializeField] private EventReference _activateSound;
        [SerializeField] private EventReference _ambushSound;

        public float AmbushStagger => _ambushStagger;
        public Color DisguiseGlow => _disguiseGlow;
        public float DisguiseAmount => _disguiseAmount;
        public EventReference ActivateSound => _activateSound;
        public EventReference AmbushSound => _ambushSound;

        public float DurationAt(int tier) => At(_duration, tier);
        public float AmbushAt(int tier) => At(_ambush, tier);

        private static float At(float[] values, int tier) =>
            values == null || values.Length == 0 ? 0f : values[Mathf.Clamp(tier - 1, 0, values.Length - 1)];

        public override AbilityRuntime CreateRuntime(AbilityContext context) => new MimicShellRuntime(this, context);
    }
}
