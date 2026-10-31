using TentacleVania.Core.Debugging;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Saves;
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
            DebugMenu.Value(Tab, "Modifiers", () => $"{ModifierName(0)} / {ModifierName(1)}");
            DebugMenu.Toggle(Tab, "Weapon VFX", () => WeaponVfx.Enabled, v => WeaponVfx.Enabled = v);
            DebugMenu.Value(Tab, "Weapon VFX played", () =>
            {
                var creature = Combat.CombatTargets.Creature;
                var vfx = creature != null ? creature.GetComponent<WeaponVfx>() : null;
                return vfx != null ? vfx.Played.ToString() : "-";
            });
            DebugMenu.Value(Tab, "Mutagen", () => Upgrades.Mutagen.ToString());
            DebugMenu.Button(Tab, "+1 Mutagen", () => Upgrades.AddMutagen(1));
            DebugMenu.Button(Tab, "Give Crusher Mass", () => Collectibles.Grant(PickupKind.Weapon, "CrusherMass", 1));
            DebugMenu.Button(Tab, "Cycle weapon 1 modifier", () => CycleModifier(0));
            DebugMenu.Button(Tab, "Cycle weapon 2 modifier", () => CycleModifier(1));
            DebugMenu.Toggle(Tab, "Weapon tip meshes", () => WeaponTentacleView.ShowTipMeshes, v => WeaponTentacleView.ShowTipMeshes = v);
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

        private static string ModifierName(int slot)
        {
            var weapons = Controller;
            var modifier = weapons != null ? weapons.Modifier(slot) : null;
            return modifier != null ? modifier.name : "none";
        }

        private static void CycleModifier(int slot)
        {
            var weapons = Controller;
            var weapon = weapons != null ? weapons.Weapon(slot) : null;
            if (weapon == null || weapon.Modifiers.Length == 0)
                return;
            var weaponId = ContentCatalog.IdOf(weapon);
            var current = Upgrades.EquippedModifier(weaponId);
            var index = -1;
            for (var i = 0; i < weapon.Modifiers.Length; i++)
                if (weapon.Modifiers[i] != null && weapon.Modifiers[i].name == current)
                    index = i;
            if (index + 1 >= weapon.Modifiers.Length)
            {
                Upgrades.EquipModifier(weaponId, null);
                return;
            }
            var next = weapon.Modifiers[index + 1].name;
            Upgrades.GrantModifier(next);
            Upgrades.EquipModifier(weaponId, next);
        }

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
