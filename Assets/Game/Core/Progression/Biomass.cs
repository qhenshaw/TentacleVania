using System;
using TentacleVania.Core.Saves;
using UnityEngine;

namespace TentacleVania.Core.Progression
{
    public static class Biomass
    {
        public static event Action<int> Changed;
        public static event Action<int> Dropped;
        public static event Action<int> Reclaimed;

        public static int Carried => State.Carried;
        public static bool HasDrop => State.Dropped;
        public static int DropAmount => State.DroppedAmount;
        public static string DropScene => State.DroppedScene;
        public static Vector3 DropPosition => State.DroppedPosition;

        private static BiomassState State => SaveSystem.Data.Biomass;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Changed = null;
            Dropped = null;
            Reclaimed = null;
        }

        public static void Add(int amount)
        {
            if (BiomassRules.Add(State, amount) > 0)
                Changed?.Invoke(State.Carried);
        }

        public static void Reset()
        {
            State.Carried = 0;
            State.Dropped = false;
            State.DroppedAmount = 0;
            Changed?.Invoke(0);
        }

        public static bool Spend(int cost)
        {
            if (!BiomassRules.Spend(State, cost))
                return false;
            Changed?.Invoke(State.Carried);
            return true;
        }

        public static void Die(string scene, Vector3 position)
        {
            BiomassRules.Die(State, GameSettings.KeepBiomassOnDeath, scene, position);
            SaveSystem.Data.Progress.ArenaInProgress = "";
            Changed?.Invoke(State.Carried);
            if (State.Dropped)
                Dropped?.Invoke(State.DroppedAmount);
        }

        public static int Reclaim()
        {
            var amount = BiomassRules.Reclaim(State);
            if (amount <= 0)
                return 0;
            Changed?.Invoke(State.Carried);
            Reclaimed?.Invoke(amount);
            return amount;
        }
    }
}
