using TentacleVania.Combat;
using TentacleVania.Core.Debugging;
using TentacleVania.Core.Progression;
using UnityEngine;

namespace TentacleVania.World.Transit
{
    public static class TransitDebugCommands
    {
        private const string Tab = WorldDebugCommands.Tab;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            DebugMenu.Value(Tab, "Nearest elevator", () =>
            {
                var elevator = Nearest();
                return elevator == null ? "none" : $"to {elevator.DestinationScene}, {(elevator.Powered ? "powered" : "no power")}";
            });
            DebugMenu.Button(Tab, "Power elevators (defeat Warden)", () => BossProgress.Defeat("Warden", ""));
            DebugMenu.Button(Tab, "Ride nearest elevator", () =>
            {
                var elevator = Nearest();
                if (elevator != null)
                    elevator.Ride();
            });
        }

        private static SectorElevator Nearest()
        {
            var creature = CombatTargets.Creature;
            if (creature == null)
                return null;
            SectorElevator best = null;
            var bestDistance = float.MaxValue;
            foreach (var elevator in Object.FindObjectsByType<SectorElevator>())
            {
                var distance = (elevator.transform.position - creature.transform.position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = elevator;
                }
            }
            return best;
        }
    }
}
