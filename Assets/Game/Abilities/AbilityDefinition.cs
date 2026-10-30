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

        public string DisplayName => _displayName;
        public float Cost => _cost;

        public abstract AbilityRuntime CreateRuntime(AbilityContext context);
    }
}
