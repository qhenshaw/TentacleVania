using TentacleVania.Combat.Damage;
using UnityEngine;

namespace TentacleVania.Abilities.Feats
{
    public enum FeatCondition
    {
        Kill,
        Consume,
        ConsumeUnderPressure,
        StaggerBreak,
        AbilityMultiHit,
    }

    [CreateAssetMenu(menuName = "TentacleVania/Abilities/Feat")]
    public sealed class FeatDefinition : ScriptableObject
    {
        [Tooltip("Mutation id (ContentCatalog asset name) granted when the feat completes.")]
        [SerializeField] private string _mutationId;
        [Tooltip("UI table key of the feat's name for progress toasts.")]
        [SerializeField] private string _titleKey;
        [SerializeField] private FeatCondition _condition;
        [SerializeField, Min(1)] private int _count = 5;
        [Tooltip("Seconds the count must happen within; 0 counts across the whole run and saves the progress.")]
        [SerializeField, Min(0f)] private float _window;
        [Tooltip("The target must have this status (Kill, Consume).")]
        [SerializeField] private StatusKind _targetStatus;
        [Tooltip("Above 0, the creature must be below this health fraction.")]
        [SerializeField, Range(0f, 1f)] private float _creatureBelow;
        [SerializeField] private bool _machinesOnly;
        [Tooltip("The hit must come from an ability (HitFlags.Ability).")]
        [SerializeField] private bool _abilityOnly;
        [SerializeField] private bool _electricOnly;
        [Tooltip("Distinct enemies needed (ConsumeUnderPressure: attackers during the consume; AbilityMultiHit: targets of one ability use).")]
        [SerializeField, Min(1)] private int _minTargets = 3;
        [Tooltip("A kill that doesn't match resets the progress (an 'only X' challenge).")]
        [SerializeField] private bool _streak;

        public string Id => name;
        public string MutationId => _mutationId;
        public string TitleKey => _titleKey;
        public FeatCondition Condition => _condition;
        public int Count => _count;
        public float Window => _window;
        public bool Cumulative => _window <= 0f;
        public StatusKind TargetStatus => _targetStatus;
        public float CreatureBelow => _creatureBelow;
        public bool MachinesOnly => _machinesOnly;
        public bool AbilityOnly => _abilityOnly;
        public bool ElectricOnly => _electricOnly;
        public int MinTargets => _minTargets;
        public bool Streak => _streak;

        public bool MatchesHit(in DamageInfo info, bool targetIsMachine, bool targetHasStatus, float creatureFraction)
        {
            if (_abilityOnly && (info.Flags & HitFlags.Ability) == 0)
                return false;
            if (_electricOnly && info.Type != DamageType.Electric)
                return false;
            if (_machinesOnly && !targetIsMachine)
                return false;
            if (_targetStatus != StatusKind.None && !targetHasStatus)
                return false;
            return _creatureBelow <= 0f || creatureFraction < _creatureBelow;
        }
    }
}
