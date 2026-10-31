using System;
using System.Collections.Generic;
using TentacleVania.Core.Saves;
using UnityEngine;

namespace TentacleVania.Core.Progression
{
    public static class MutationLoadout
    {
        private static readonly int[] PoolByStage = { 3, 5, 7, 9 };

        public static event Action Changed;

        public static List<string> Owned => SaveSystem.Data.Build.OwnedMutations;
        public static List<string> Equipped => SaveSystem.Data.Build.Mutations;
        public static int NotchPool => PoolFor(Stats.SizeStage, SaveSystem.Data.Build.BonusNotches);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Changed = null;

        public static int PoolFor(int sizeStage, int bonusNotches) => PoolByStage[Mathf.Clamp(sizeStage, 0, PoolByStage.Length - 1)] + Mathf.Max(0, bonusNotches);

        public static bool Fits(int used, int cost, int pool) => cost > 0 && used + cost <= pool;

        public static bool Owns(string id) => !string.IsNullOrEmpty(id) && Owned.Contains(id);

        public static bool IsEquipped(string id) => !string.IsNullOrEmpty(id) && Equipped.Contains(id);

        public static bool Grant(string id)
        {
            if (string.IsNullOrEmpty(id) || Owns(id))
                return false;
            Owned.Add(id);
            Changed?.Invoke();
            return true;
        }

        public static bool Equip(string id, int cost, int used)
        {
            if (!Owns(id) || IsEquipped(id) || !Fits(used, cost, NotchPool))
                return false;
            Equipped.Add(id);
            Changed?.Invoke();
            return true;
        }

        public static bool Unequip(string id)
        {
            if (!Equipped.Remove(id))
                return false;
            Changed?.Invoke();
            return true;
        }

        public static void Clear()
        {
            Owned.Clear();
            Equipped.Clear();
            Changed?.Invoke();
        }
    }
}
