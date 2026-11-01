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

        public static bool InStock(string mutationId, string requiredTrinket) => InStock(PickupKind.Mutation, mutationId, requiredTrinket);

        public static bool InStock(PickupKind kind, string id, string requiredTrinket) =>
            !Owns(kind, id) && (string.IsNullOrEmpty(requiredTrinket) || Collectibles.Trinkets.Contains(requiredTrinket));

        public static bool Owns(PickupKind kind, string id) => kind switch
        {
            PickupKind.Weapon => Collectibles.OwnedWeapons.Contains(id),
            PickupKind.Ability => BossProgress.IsAbilityUnlocked(id),
            _ => MutationLoadout.Owns(id),
        };

        public static bool TryBuyMutation(string mutationId, string requiredTrinket, int price) => TryBuy(PickupKind.Mutation, mutationId, requiredTrinket, price);

        public static bool TryBuy(PickupKind kind, string id, string requiredTrinket, int price)
        {
            if (string.IsNullOrEmpty(id) || !InStock(kind, id, requiredTrinket) || !Biomass.Spend(price))
                return false;
            Saves.SaveSystem.Data.Build.BiomassSpent += price;
            if (kind == PickupKind.Mutation)
                MutationLoadout.Grant(id);
            else
                Collectibles.Grant(kind, id, 1);
            Purchased?.Invoke(id);
            Saves.Autosave.Request(Saves.AutosaveReason.Purchase);
            return true;
        }
    }
}
