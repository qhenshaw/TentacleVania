using System;
using UnityEngine;

namespace TentacleVania.Core.Progression
{
    public static class Shop
    {
        private static readonly int[] MutationPriceByNotches = { 300, 700, 1500 };

        public static event Action<string> Purchased;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Purchased = null;

        public static int MutationPrice(int notches) => MutationPriceByNotches[Mathf.Clamp(notches, 1, MutationPriceByNotches.Length) - 1];

        public static bool InStock(string mutationId, string requiredTrinket) =>
            !MutationLoadout.Owns(mutationId) && (string.IsNullOrEmpty(requiredTrinket) || Collectibles.Trinkets.Contains(requiredTrinket));

        public static bool TryBuyMutation(string mutationId, string requiredTrinket, int price)
        {
            if (string.IsNullOrEmpty(mutationId) || !InStock(mutationId, requiredTrinket) || !Biomass.Spend(price))
                return false;
            Saves.SaveSystem.Data.Build.BiomassSpent += price;
            MutationLoadout.Grant(mutationId);
            Purchased?.Invoke(mutationId);
            Saves.Autosave.Request(Saves.AutosaveReason.Purchase);
            return true;
        }
    }
}
