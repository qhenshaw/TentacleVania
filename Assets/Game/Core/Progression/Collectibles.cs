using System;
using System.Collections.Generic;
using TentacleVania.Core.Saves;
using UnityEngine;

namespace TentacleVania.Core.Progression
{
    public enum PickupKind
    {
        Biomass,
        Mutagen,
        Trinket,
        Weapon,
        Mutation,
        Notch,
    }

    public static class Collectibles
    {
        public delegate void FoundHandler(PickupKind kind, string itemId, int amount);

        public static event FoundHandler Found;
        public static event Action<string> WeaponFound;

        public static List<string> OwnedWeapons => SaveSystem.Data.Build.OwnedWeapons;
        public static List<string> Trinkets => SaveSystem.Data.Build.Trinkets;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Found = null;
            WeaponFound = null;
        }

        public static bool IsCollected(string pickupId) => SaveSystem.Data.World.Has(WorldFlag.CollectedPickup, pickupId);

        public static bool Collect(string pickupId, PickupKind kind, string itemId, int amount)
        {
            if (!SaveSystem.Data.World.Set(WorldFlag.CollectedPickup, pickupId))
                return false;
            Grant(kind, itemId, amount);
            Autosave.Request(AutosaveReason.Pickup);
            return true;
        }

        public static void Grant(PickupKind kind, string itemId, int amount)
        {
            switch (kind)
            {
                case PickupKind.Biomass:
                    Biomass.Add(amount);
                    break;
                case PickupKind.Mutagen:
                    Upgrades.AddMutagen(amount);
                    break;
                case PickupKind.Trinket:
                    AddUnique(Trinkets, itemId);
                    break;
                case PickupKind.Weapon:
                    AddUnique(OwnedWeapons, itemId);
                    WeaponFound?.Invoke(itemId);
                    break;
                case PickupKind.Mutation:
                    MutationLoadout.Grant(itemId);
                    break;
                case PickupKind.Notch:
                    MutationLoadout.AddBonusNotch();
                    break;
            }
            Found?.Invoke(kind, itemId, amount);
        }

        private static void AddUnique(List<string> list, string id)
        {
            if (!string.IsNullOrEmpty(id) && !list.Contains(id))
                list.Add(id);
        }
    }
}
