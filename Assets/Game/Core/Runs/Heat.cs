using System;
using System.Collections.Generic;
using TentacleVania.Core.Saves;

namespace TentacleVania.Core.Runs
{
    public enum HeatKind
    {
        Hardened,
        Vicious,
        Swarm,
        Starved,
        Gouged,
    }

    public static class Heat
    {
        public const float EssencePerHeat = 0.05f;

        private static readonly int[] MaxRanks = { 3, 3, 2, 2, 2 };
        private static readonly float[] PerRank = { 0.2f, 0.15f, 1f, 0.25f, 0.25f };

        public static int Kinds => MaxRanks.Length;

        public static event Action Changed;

        public static int MaxRank(HeatKind kind) => MaxRanks[(int)kind];

        public static float Step(HeatKind kind) => PerRank[(int)kind];

        public static bool Unlocked(MetaState meta)
        {
            foreach (var run in meta.Runs)
                if (run.Won)
                    return true;
            return false;
        }

        public static int Rank(MetaState meta, HeatKind kind)
        {
            var index = (int)kind;
            return index < meta.Heat.Count ? Math.Clamp(meta.Heat[index], 0, MaxRanks[index]) : 0;
        }

        public static void SetRank(MetaState meta, HeatKind kind, int rank)
        {
            while (meta.Heat.Count < MaxRanks.Length)
                meta.Heat.Add(0);
            meta.Heat[(int)kind] = Math.Clamp(rank, 0, MaxRank(kind));
            Changed?.Invoke();
        }

        public static int Total(MetaState meta)
        {
            var total = 0;
            for (var i = 0; i < MaxRanks.Length; i++)
                total += Rank(meta, (HeatKind)i);
            return total;
        }

        public static float Scale(MetaState meta, HeatKind kind) => 1f + Rank(meta, kind) * Step(kind);

        public static float HealScale(MetaState meta) => Math.Max(0f, 1f - Rank(meta, HeatKind.Starved) * Step(HeatKind.Starved));

        public static int ExtraEnemies(MetaState meta) => Rank(meta, HeatKind.Swarm);

        public static float EssenceScale(MetaState meta) => 1f + Total(meta) * EssencePerHeat;

        public static MetaState Live => RunSession.Active ? SaveSystem.Data.Meta : Empty;

        private static readonly MetaState Empty = new();

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Changed = null;
    }
}
