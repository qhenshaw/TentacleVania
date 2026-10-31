using TentacleVania.Combat.Damage;
using UnityEngine;

namespace TentacleVania.Weapons.Data
{
    [CreateAssetMenu(menuName = "TentacleVania/Weapons/Weapon Definition")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        public const int ComboSteps = 3;

        [SerializeField] private string _displayName = "Weapon";

        [Header("Base values (Balance Sheet, tier 1)")]
        [SerializeField, Min(0f)] private float _damage = 12f;
        [SerializeField, Min(0f)] private float _stagger = 12f;
        [SerializeField, Min(0f)] private float _armorDamageScale = 1f;
        [SerializeField] private DamageType _damageType = DamageType.Physical;

        [Header("Moves")]
        [SerializeField] private WeaponMove[] _steps = new WeaponMove[ComboSteps];
        [SerializeField] private WeaponMove _heavy = new();
        [SerializeField] private WeaponMove _dashAttack = new();

        [Header("Modifiers (unlocked with biomass + Mutagen, one equipped)")]
        [SerializeField] private WeaponModifier[] _modifiers = System.Array.Empty<WeaponModifier>();

        [Header("Tip mesh")]
        [Tooltip("Prefab drawn at the weapon tentacle's tip while attacking, modelled along +Y from its base.")]
        [SerializeField] private GameObject _tipMesh;
        [SerializeField, Min(0.01f)] private float _tipScale = 1f;
        [Tooltip("Metres the mesh's base sinks into the tentacle tip, so no gap shows.")]
        [SerializeField, Min(0f)] private float _tipOverlap = 0.1f;

        public string DisplayName => _displayName;
        public float Damage => _damage;
        public float Stagger => _stagger;
        public float ArmorDamageScale => _armorDamageScale;
        public DamageType DamageType => _damageType;
        public WeaponMove Heavy => _heavy;
        public WeaponMove DashAttack => _dashAttack;
        public WeaponModifier[] Modifiers => _modifiers;
        public GameObject TipMesh => _tipMesh;
        public float TipScale => _tipScale;
        public float TipOverlap => _tipOverlap;

        public WeaponMove Step(int index) => _steps[Mathf.Clamp(index, 0, _steps.Length - 1)];
    }
}
