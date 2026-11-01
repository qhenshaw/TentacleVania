using TentacleVania.Combat;
using TentacleVania.Core.Characters;
using UnityEngine;

namespace TentacleVania.Abilities
{
    public sealed class AbilityContext
    {
        public AbilityContext(Transform owner, Health health, CharacterMaterials materials, System.Action refreshStatus)
        {
            Owner = owner;
            Health = health;
            Materials = materials;
            RefreshStatus = refreshStatus;
        }

        public Transform Owner { get; }
        public Health Health { get; }
        public CharacterMaterials Materials { get; }
        public System.Action RefreshStatus { get; }
    }

    public abstract class AbilityRuntime
    {
        public abstract bool IsActive { get; }
        public abstract void Activate();
        public abstract void Tick(float dt);
        public abstract void Cancel();
        public virtual void Dispose() { }
    }

    public abstract class AbilityDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Ability";
        [SerializeField, Min(0f)] private float _cost = 50f;
        [Tooltip("Biomass to reach tier 2, 3... at a nest (Balance Sheet: 800, 2,500). The tier count is this length + 1.")]
        [SerializeField] private int[] _tierCosts = System.Array.Empty<int>();

        public string DisplayName => Core.Text.LocalizedNames.Get(NameKey, _displayName);
        public string NameKey => _nameKey ??= "ability." + name + ".name";

        [System.NonSerialized] private string _nameKey;
        public float Cost => _cost;
        public int MaxTier => 1 + _tierCosts.Length;

        public int TierCost(int currentTier) => currentTier >= 1 && currentTier <= _tierCosts.Length ? _tierCosts[currentTier - 1] : 0;
        public virtual bool SlotAbility => true;

        public abstract AbilityRuntime CreateRuntime(AbilityContext context);
    }
}
