using System;
using System.Collections.Generic;
using TentacleVania.Core.Saves;
using UnityEngine;

namespace TentacleVania.Core.Progression
{
    public static class Upgrades
    {
        public const int BaseTier = 1;

        public static event Action Changed;

        private static BuildState Build => SaveSystem.Data.Build;

        public static int Mutagen => Build.Mutagen;
        public static List<string> OwnedModifiers => Build.OwnedModifiers;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Changed = null;

        public static void AddMutagen(int amount)
        {
            if (amount <= 0)
                return;
            Build.Mutagen += amount;
            Changed?.Invoke();
        }

        public static int AbilityTier(string abilityId)
        {
            var value = Find(Build.AbilityTiers, abilityId);
            return value != null && int.TryParse(value, out var tier) ? Mathf.Max(BaseTier, tier) : BaseTier;
        }

        public static bool TryUpgradeAbility(string abilityId, int cost, int maxTier)
        {
            var tier = AbilityTier(abilityId);
            if (string.IsNullOrEmpty(abilityId) || tier >= maxTier || !Biomass.Spend(cost))
                return false;
            Build.BiomassSpent += cost;
            SetAbilityTier(abilityId, tier + 1);
            Autosave.Request(AutosaveReason.Purchase);
            return true;
        }

        public static void SetAbilityTier(string abilityId, int tier)
        {
            if (string.IsNullOrEmpty(abilityId))
                return;
            Set(Build.AbilityTiers, abilityId, Mathf.Max(BaseTier, tier).ToString());
            Changed?.Invoke();
        }

        public static bool OwnsModifier(string modifierId) => !string.IsNullOrEmpty(modifierId) && OwnedModifiers.Contains(modifierId);

        public static bool CanUnlock(string modifierId, int biomassCost, int mutagenCost) =>
            !string.IsNullOrEmpty(modifierId) && !OwnsModifier(modifierId) && Biomass.Carried >= biomassCost && Mutagen >= mutagenCost;

        public static bool TryUnlockModifier(string modifierId, int biomassCost, int mutagenCost)
        {
            if (!CanUnlock(modifierId, biomassCost, mutagenCost) || !Biomass.Spend(biomassCost))
                return false;
            Build.BiomassSpent += biomassCost;
            Build.Mutagen -= mutagenCost;
            OwnedModifiers.Add(modifierId);
            Changed?.Invoke();
            Autosave.Request(AutosaveReason.Purchase);
            return true;
        }

        public static bool GrantModifier(string modifierId)
        {
            if (string.IsNullOrEmpty(modifierId) || OwnsModifier(modifierId))
                return false;
            OwnedModifiers.Add(modifierId);
            Changed?.Invoke();
            return true;
        }

        public static string EquippedModifier(string weaponId) => Find(Build.EquippedModifiers, weaponId) ?? "";

        public static bool EquipModifier(string weaponId, string modifierId)
        {
            if (string.IsNullOrEmpty(weaponId))
                return false;
            if (string.IsNullOrEmpty(modifierId))
            {
                if (!Remove(Build.EquippedModifiers, weaponId))
                    return false;
            }
            else
            {
                if (!OwnsModifier(modifierId))
                    return false;
                Set(Build.EquippedModifiers, weaponId, modifierId);
            }
            Changed?.Invoke();
            return true;
        }

        public static void Clear()
        {
            Build.Mutagen = 0;
            Build.AbilityTiers.Clear();
            OwnedModifiers.Clear();
            Build.EquippedModifiers.Clear();
            Changed?.Invoke();
        }

        private static string Find(List<string> pairs, string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;
            foreach (var pair in pairs)
                if (pair.Length > key.Length && pair[key.Length] == '=' && pair.StartsWith(key, StringComparison.Ordinal))
                    return pair.Substring(key.Length + 1);
            return null;
        }

        private static void Set(List<string> pairs, string key, string value)
        {
            Remove(pairs, key);
            pairs.Add(key + "=" + value);
        }

        private static bool Remove(List<string> pairs, string key)
        {
            for (var i = 0; i < pairs.Count; i++)
            {
                var pair = pairs[i];
                if (pair.Length > key.Length && pair[key.Length] == '=' && pair.StartsWith(key, StringComparison.Ordinal))
                {
                    pairs.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
    }
}
