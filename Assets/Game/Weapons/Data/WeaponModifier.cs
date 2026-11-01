using UnityEngine;

namespace TentacleVania.Weapons.Data
{
    public enum ModifierKind
    {
        Maelstrom,
        ChargedCyclone,
        Shatter,
        BoneBreaker,
        Bloodletter,
        FrenzyEdge,
        ChainHook,
        FeedingLine,
        MeltingArmor,
        AcidBath,
        OverloadChain,
        ConductiveField,
        ScorchedEarth,
        Cauterize,
    }

    [CreateAssetMenu(menuName = "TentacleVania/Weapons/Weapon Modifier")]
    public sealed class WeaponModifier : ScriptableObject
    {
        [Tooltip("UI table key prefix: <key>.name and <key>.effect.")]
        [SerializeField] private string _textKey = "modifier.maelstrom";
        [SerializeField] private ModifierKind _kind;

        [Header("Unlock cost (Balance Sheet: 1,000 biomass + 1 Mutagen)")]
        [SerializeField, Min(0)] private int _biomassCost = 1000;
        [SerializeField, Min(0)] private int _mutagenCost = 1;

        [Header("Effect (proposed values)")]
        [Tooltip("Maelstrom: finisher duration multiplier. Charged Cyclone: extra charge per finisher hit. Shatter: damage taken multiplier. Bone Breaker: grab threshold (fraction of max HP).")]
        [SerializeField, Min(0f)] private float _value = 1.5f;
        [Tooltip("Shatter and Bone Breaker: seconds the effect lasts.")]
        [SerializeField, Min(0f)] private float _duration = 6f;
        [Tooltip("Maelstrom: finisher hits knock enemies farther than this (m) inward instead of outward.")]
        [SerializeField, Min(0f)] private float _minDistance = 1.5f;

        public string TextKey => _textKey;
        public ModifierKind Kind => _kind;
        public int BiomassCost => _biomassCost;
        public int MutagenCost => _mutagenCost;
        public float Value => _value;
        public float Duration => _duration;
        public float MinDistance => _minDistance;
    }
}
