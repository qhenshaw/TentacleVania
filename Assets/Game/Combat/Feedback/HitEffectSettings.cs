using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.VFX;

namespace TentacleVania.Combat.Feedback
{
    [CreateAssetMenu(menuName = "TentacleVania/Combat/Hit Effect Settings")]
    public sealed class HitEffectSettings : ScriptableObject
    {
        [Serializable]
        public sealed class Side
        {
            public VisualEffect Impact;
            public VisualEffect BloodSpray;
            public DecalProjector BloodDecal;
            [Min(1)] public int PoolSize = 8;
            [Tooltip("Decal width in metres at intensity 0 and 1.")]
            public Vector2 DecalSize = new(0.8f, 1.6f);
        }

        [SerializeField] private Side _creature = new();
        [SerializeField] private Side _enemy = new();

        [Header("Rules")]
        [Tooltip("A hit on the creature leaves a decal when it takes at least this fraction of its max health.")]
        [SerializeField, Range(0f, 1f)] private float _creatureBigHit = 0.1f;
        [SerializeField, Range(0f, 1f)] private float _lightIntensity = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _heavyIntensity = 1f;
        [SerializeField, Range(0f, 1f)] private float _killIntensity = 1f;

        [Header("Decals")]
        [SerializeField, Min(1)] private int _decalCap = 128;
        [Tooltip("Metres a decal is pushed along the hit direction from below the hit point.")]
        [SerializeField, Min(0f)] private float _decalOffset = 0.4f;
        [Tooltip("Seconds a recycled decal takes to fade out before it moves.")]
        [SerializeField, Min(0f)] private float _recycleFade = 0.15f;
        [SerializeField] private LayerMask _groundLayers = 1;

        public Side Creature => _creature;
        public Side Enemy => _enemy;
        public float CreatureBigHit => _creatureBigHit;
        public float LightIntensity => _lightIntensity;
        public float HeavyIntensity => _heavyIntensity;
        public float KillIntensity => _killIntensity;
        public int DecalCap => _decalCap;
        public float DecalOffset => _decalOffset;
        public float RecycleFade => _recycleFade;
        public LayerMask GroundLayers => _groundLayers;
    }
}
