using System;
using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.World.Layout
{
    [CreateAssetMenu(menuName = "TentacleVania/World/Layout Dressing Set")]
    public sealed class DressingSet : ScriptableObject
    {
        [Serializable]
        public sealed class Prop
        {
            public GameObject Prefab;
            public DressingZone Zones = DressingZone.Wall;
            public PropCategory Category = PropCategory.Dynamic;
            [Tooltip("Footprint radius in metres, for spacing and keeping doorways clear.")]
            [Min(0.05f)] public float Radius = 0.5f;
            [Min(0f)] public float Weight = 1f;
            public Vector3 Offset;
            [Range(-180f, 180f)] public float Rotation;
            [Tooltip("Turn by a random yaw instead of facing away from the wall.")]
            public bool RandomYaw;
        }

        [Serializable]
        public sealed class ZoneRule
        {
            public DressingZone Zone = DressingZone.Wall;
            [Tooltip("Chance that each candidate spot in this zone gets a prop.")]
            [Range(0f, 1f)] public float Density = 0.3f;
            [Tooltip("Minimum distance in metres between props placed in this zone and any other prop.")]
            [Min(0f)] public float Spacing = 1f;
        }

        [SerializeField] private List<Prop> _props = new();
        [SerializeField] private List<ZoneRule> _zones = new()
        {
            new ZoneRule { Zone = DressingZone.Wall, Density = 0.35f, Spacing = 1.2f },
            new ZoneRule { Zone = DressingZone.Corner, Density = 0.6f, Spacing = 1f },
            new ZoneRule { Zone = DressingZone.Center, Density = 0.08f, Spacing = 3f },
            new ZoneRule { Zone = DressingZone.NearDoor, Density = 0.3f, Spacing = 1f },
        };
        [Tooltip("Most Cover props (blocking movement and projectiles) one room or hallway may get.")]
        [SerializeField, Min(0)] private int _maxCoverPerSpace = 2;
        [Tooltip("Cells inside an opening kept clear of props, measured from the opening into the space.")]
        [SerializeField, Min(0f)] private float _doorClearance = 1.5f;
        [Tooltip("Hallways only get Wall-zone props with a footprint radius up to this, placed against their walls.")]
        [SerializeField, Min(0f)] private float _hallwayMaxRadius = 0.35f;

        public IReadOnlyList<Prop> Props => _props;
        public IReadOnlyList<ZoneRule> Zones => _zones;
        public int MaxCoverPerSpace => _maxCoverPerSpace;
        public float DoorClearance => _doorClearance;
        public float HallwayMaxRadius => _hallwayMaxRadius;

        public ZoneRule RuleFor(DressingZone zone)
        {
            foreach (var rule in _zones)
                if (rule.Zone == zone)
                    return rule;
            return null;
        }
    }
}
