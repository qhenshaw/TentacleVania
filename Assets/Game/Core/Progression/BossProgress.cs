using System;
using System.Collections.Generic;
using TentacleVania.Core.Saves;
using UnityEngine;

namespace TentacleVania.Core.Progression
{
    public static class BossProgress
    {
        public static event Action<string> Defeated;
        public static event Action<string> AbilityUnlocked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Defeated = null;
            AbilityUnlocked = null;
        }

        public static bool IsDefeated(string id) => SaveSystem.Data.World.Has(WorldFlag.DefeatedBoss, id);

        public static bool IsAbilityUnlocked(string ability) => SaveSystem.Data.Build.Abilities.Contains(ability);

        public static void Defeat(string id, string ability)
        {
            SaveSystem.Data.World.Set(WorldFlag.DefeatedBoss, id);
            if (!string.IsNullOrEmpty(ability) && !IsAbilityUnlocked(ability))
            {
                SaveSystem.Data.Build.Abilities.Add(ability);
                AbilityUnlocked?.Invoke(ability);
            }
            Defeated?.Invoke(id);
            Autosave.Request(AutosaveReason.BossDefeated);
        }

        public static void Revive(string id) => SaveSystem.Data.World.Clear(WorldFlag.DefeatedBoss, id);

        public static bool HasDiedTo(string id) => SaveSystem.Data.World.Has(WorldFlag.DiedToBoss, id);

        public static void RecordDeath(string id)
        {
            if (SaveSystem.Data.World.Set(WorldFlag.DiedToBoss, id))
                Autosave.Request(AutosaveReason.Death);
        }

        public static int PhaseFor(float normalizedHealth, IReadOnlyList<float> startsBelow)
        {
            var phase = 0;
            for (var i = 1; i < startsBelow.Count; i++)
                if (normalizedHealth <= startsBelow[i])
                    phase = i;
            return phase;
        }
    }
}
