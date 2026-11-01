using System;
using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.Core.Platform
{
    public enum AchievementCondition
    {
        BossDefeated,
        GameCompleted,
        CompletedWithin,
        SizeStage,
        MutationsOwned,
        BonusNotches,
        Trinkets,
        LoreLogs,
        WeaponsOwned,
        BiomassSpent,
        MutationsAll,
    }

    [CreateAssetMenu(menuName = "TentacleVania/Platform/Achievement List")]
    public sealed class AchievementList : ScriptableObject
    {
        public const string ResourcePath = "AchievementList";

        [Serializable]
        public sealed class Entry
        {
            [Tooltip("Platform id (also the UI keys achievement.<Id>.name / .desc).")]
            public string Id;
            public AchievementCondition Condition;
            [Tooltip("Boss id for BossDefeated; comma-separated mutation ids for MutationsAll.")]
            public string Key;
            [Tooltip("Count, size stage (0-3), or seconds for CompletedWithin.")]
            public float Value;
            [Tooltip("Hidden until unlocked on the platform.")]
            public bool Hidden;
        }

        [SerializeField] private List<Entry> _entries = new();

        public IReadOnlyList<Entry> Entries => _entries;
    }
}
