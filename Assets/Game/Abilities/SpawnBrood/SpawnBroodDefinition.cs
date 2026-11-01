using FMODUnity;
using UnityEngine;

namespace TentacleVania.Abilities.SpawnBrood
{
    [CreateAssetMenu(menuName = "TentacleVania/Abilities/Spawn Brood")]
    public sealed class SpawnBroodDefinition : AbilityDefinition
    {
        [Tooltip("Minions per use, per tier (index 0 = tier 1); they are the parasite swarm's pooled minions (Combat/Status/ParasiteSwarm).")]
        [SerializeField] private int[] _minions = { 3, 4, 6 };
        [SerializeField, Min(0f)] private float _activeTime = 0.4f;
        [SerializeField] private EventReference _activateSound;

        public int MinionsAt(int tier) => _minions == null || _minions.Length == 0 ? 1 : _minions[Mathf.Clamp(tier - 1, 0, _minions.Length - 1)];
        public float ActiveTime => _activeTime;
        public EventReference ActivateSound => _activateSound;

        public override AbilityRuntime CreateRuntime(AbilityContext context) => new SpawnBroodRuntime(this, context);
    }
}
