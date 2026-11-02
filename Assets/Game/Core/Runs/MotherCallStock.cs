using System;
using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.Core.Runs
{
    public enum UnlockKind
    {
        Weapon,
        Ability,
        StartingNotch,
        DeathDefiance,
        HoarderTier,
        RerollToken,
    }

    [CreateAssetMenu(menuName = "TentacleVania/Runs/Mother's Call Stock")]
    public sealed class MotherCallStock : ScriptableObject
    {
        public const string ResourcePath = "MotherCallStock";

        [Serializable]
        public sealed class Entry
        {
            public UnlockKind Kind;
            [Tooltip("Content catalog id for weapons and abilities; any unique id for the rest.")]
            public string Item;
            [Tooltip("Essence; 0 = owned from the start.")]
            [Min(0)] public int Cost;

            public string Id => MotherCall.UnlockId(Kind, Item);
        }

        private static MotherCallStock _loaded;

        [SerializeField] private List<Entry> _entries = new();

        public static MotherCallStock Loaded => _loaded != null ? _loaded : _loaded = Resources.Load<MotherCallStock>(ResourcePath);

        public IReadOnlyList<Entry> Entries => _entries;

        public Entry Find(UnlockKind kind, string item)
        {
            foreach (var entry in _entries)
                if (entry.Kind == kind && entry.Item == item)
                    return entry;
            return null;
        }
    }
}
