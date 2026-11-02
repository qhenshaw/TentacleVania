using System;
using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.Core.Progression
{
    [CreateAssetMenu(menuName = "TentacleVania/Progression/Difficulty Curve")]
    public sealed class DifficultyCurve : ScriptableObject
    {
        public const string ResourcePath = "DifficultyCurve";

        [Serializable]
        public sealed class SectorScale
        {
            [Tooltip("Sector id (SH, AD, ...).")]
            public string Sector;
            [Tooltip("Enemy max health and stagger (bosses keep their own numbers).")]
            [Min(0.1f)] public float Health = 1f;
            [Tooltip("Enemy outgoing damage, bosses included.")]
            [Min(0.1f)] public float Damage = 1f;
            [Tooltip("Enemy biomass (bosses use their own definition's).")]
            [Min(0f)] public float Biomass = 1f;
            [Tooltip("Biomass per cache in this sector.")]
            [Min(0)] public int Cache = 250;
            [Tooltip("Planned biomass income for the sector (Tools/economy-model.py --solve); not read by the game.")]
            [Min(0)] public int Target;
        }

        private static readonly SectorScale Neutral = new();
        private static DifficultyCurve _loaded;

        [SerializeField] private List<SectorScale> _sectors = new();
        [Header("Run room depth (placeholder)")]
        [Tooltip("Extra enemy health and stagger per room deeper into a run floor.")]
        [SerializeField, Min(0f)] private float _healthPerRoom = 0.04f;
        [Tooltip("Extra enemy outgoing damage per room deeper into a run floor.")]
        [SerializeField, Min(0f)] private float _damagePerRoom = 0.02f;

        public IReadOnlyList<SectorScale> Sectors => _sectors;

        public static string CurrentSector { get; set; }
        public static int RoomDepth { get; set; }

        public static SectorScale Current => For(CurrentSector);
        public static float DepthHealth => RoomScale(RoomDepth, Loaded() != null ? _loaded._healthPerRoom : 0f);
        public static float DepthDamage => RoomScale(RoomDepth, Loaded() != null ? _loaded._damagePerRoom : 0f);

        public static float RoomScale(int depth, float perRoom) => 1f + Mathf.Max(0, depth) * perRoom;

        public static SectorScale For(string sector) => Loaded() != null ? _loaded.Find(sector) : Neutral;

        private static DifficultyCurve Loaded()
        {
            if (_loaded == null)
                _loaded = Resources.Load<DifficultyCurve>(ResourcePath);
            return _loaded;
        }

        public SectorScale Find(string sector)
        {
            if (!string.IsNullOrEmpty(sector))
                foreach (var scale in _sectors)
                    if (scale.Sector == sector)
                        return scale;
            return Neutral;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            CurrentSector = null;
            RoomDepth = 0;
        }
    }
}
