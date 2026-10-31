using TentacleVania.Core.Saves;
using UnityEngine;

namespace TentacleVania.Core.Progression
{
    public static class BiomassRules
    {
        public static int Add(BiomassState state, int amount)
        {
            if (amount <= 0)
                return 0;
            state.Carried += amount;
            return amount;
        }

        public static int Die(BiomassState state, bool keepOnDeath, string scene, Vector3 position)
        {
            if (keepOnDeath)
                return 0;
            var lost = state.Dropped ? state.DroppedAmount : 0;
            state.Dropped = state.Carried > 0;
            state.DroppedAmount = state.Carried;
            state.DroppedScene = state.Dropped ? scene : "";
            state.DroppedPosition = state.Dropped ? position : Vector3.zero;
            state.Carried = 0;
            return lost;
        }

        public static int Reclaim(BiomassState state)
        {
            if (!state.Dropped)
                return 0;
            var amount = state.DroppedAmount;
            state.Carried += amount;
            state.Dropped = false;
            state.DroppedAmount = 0;
            state.DroppedScene = "";
            state.DroppedPosition = Vector3.zero;
            return amount;
        }

        public static bool Spend(BiomassState state, int cost)
        {
            if (cost < 0 || state.Carried < cost)
                return false;
            state.Carried -= cost;
            return true;
        }
    }
}
