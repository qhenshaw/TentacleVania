using System;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Saves;
using UnityEngine;

namespace TentacleVania.Core.Runs
{
    public static class MotherCall
    {
        public const int MaxRank = 20;
        public const int RankBaseCost = 25;
        public const int RankStepCost = 15;

        public static event Action Changed;

        public static MetaState Meta => SaveSystem.Data.Meta;

        public static string UnlockId(UnlockKind kind, string item) => kind switch
        {
            UnlockKind.Weapon => "weapon:" + item,
            UnlockKind.Ability => "ability:" + item,
            UnlockKind.StartingNotch => "notch:" + item,
            UnlockKind.HoarderTier => "hoarder:" + item,
            _ => "defiance:" + item,
        };

        public static int RankCost(int rank) => RankBaseCost + RankStepCost * Math.Max(0, rank);

        public static int Rank(MetaState meta, StatKind kind) => kind switch
        {
            StatKind.Brutality => meta.Brutality,
            StatKind.Ferocity => meta.Ferocity,
            StatKind.Tenacity => meta.Tenacity,
            _ => meta.Cunning,
        };

        public static bool TryBuyRank(MetaState meta, StatKind kind)
        {
            var rank = Rank(meta, kind);
            var cost = RankCost(rank);
            if (rank >= MaxRank || meta.Essence < cost)
                return false;
            meta.Essence -= cost;
            switch (kind)
            {
                case StatKind.Brutality:
                    meta.Brutality++;
                    break;
                case StatKind.Ferocity:
                    meta.Ferocity++;
                    break;
                case StatKind.Tenacity:
                    meta.Tenacity++;
                    break;
                default:
                    meta.Cunning++;
                    break;
            }
            return true;
        }

        public static bool IsUnlocked(MetaState meta, MotherCallStock.Entry entry) => entry == null || entry.Cost <= 0 || meta.Unlocks.Contains(entry.Id);

        public static bool IsUnlocked(MetaState meta, MotherCallStock stock, UnlockKind kind, string item) =>
            stock == null || IsUnlocked(meta, stock.Find(kind, item));

        public static bool TryUnlock(MetaState meta, MotherCallStock.Entry entry)
        {
            if (entry == null || IsUnlocked(meta, entry) || meta.Essence < entry.Cost)
                return false;
            meta.Essence -= entry.Cost;
            meta.Unlocks.Add(entry.Id);
            return true;
        }

        public static int Count(MetaState meta, UnlockKind kind)
        {
            var prefix = UnlockId(kind, "");
            var count = 0;
            foreach (var id in meta.Unlocks)
                if (id.StartsWith(prefix, StringComparison.Ordinal))
                    count++;
            return count;
        }

        public static bool BuyRank(StatKind kind)
        {
            if (!TryBuyRank(Meta, kind))
                return false;
            Bought();
            Stats.NotifyChanged();
            return true;
        }

        public static bool Unlock(MotherCallStock.Entry entry)
        {
            if (!TryUnlock(Meta, entry))
                return false;
            Bought();
            return true;
        }

        private static void Bought()
        {
            Changed?.Invoke();
            Autosave.Request(AutosaveReason.Purchase);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Changed = null;
    }
}
