using TentacleVania.Core.Debugging;
using TentacleVania.Creature;
using UnityEngine;

namespace TentacleVania.Weapons
{
    public static class WeaponDebugCommands
    {
        public const string Tab = "Weapons";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            DebugMenu.Value(Tab, "Weapon 1 (X / LMB)", () => WeaponName(0));
            DebugMenu.Value(Tab, "Weapon 2 (Y / RMB)", () => WeaponName(1));
            DebugMenu.Value(Tab, "Next combo step", () =>
            {
                var weapons = Controller;
                return weapons != null ? (weapons.NextComboStep + 1).ToString() : "-";
            });
            DebugMenu.Value(Tab, "Current attack", () =>
            {
                var weapons = Controller;
                if (weapons == null || !weapons.IsAttacking)
                    return "-";
                return $"{weapons.ActiveMove.Name} ({weapons.ActiveKind}, {weapons.Phase})";
            });
        }

        private static WeaponController Controller =>
            CreatureSpawner.Spawned != null ? CreatureSpawner.Spawned.GetComponent<WeaponController>() : null;

        private static string WeaponName(int slot)
        {
            var weapons = Controller;
            if (weapons == null)
                return "-";
            var weapon = weapons.Weapon(slot);
            return weapon != null ? weapon.DisplayName : "empty";
        }
    }
}
