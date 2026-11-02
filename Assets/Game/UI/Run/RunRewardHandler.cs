using System.Collections.Generic;
using TentacleVania.Abilities.Mutations;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Runs;
using TentacleVania.Core.Saves;
using TentacleVania.Creature;
using TentacleVania.Weapons;
using UnityEngine;

namespace TentacleVania.UI.Run
{
    public static class RunRewardHandler
    {
        private static readonly List<string> Candidates = new(64);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Hook()
        {
            RunSession.Started += OnStarted;
            RunSession.RewardGranted += OnReward;
        }

        private static void OnStarted()
        {
            if (!Application.isPlaying)
                return;
            MutationLoadout.Clear();
            Upgrades.ClearEquippedModifiers();
        }

        private static void OnReward(RunReward reward)
        {
            if (!Application.isPlaying || CreatureSpawner.Spawned == null)
                return;
            switch (reward)
            {
                case RunReward.Mutation:
                case RunReward.Challenge:
                    GrantMutation();
                    break;
                case RunReward.Evolution:
                    GrantEvolution();
                    break;
            }
        }

        private static void GrantMutation()
        {
            var controller = CreatureSpawner.Spawned.GetComponent<MutationController>();
            if (controller == null || controller.Catalog == null)
                return;
            Candidates.Clear();
            var used = controller.UsedNotches();
            foreach (var entry in controller.Catalog.Entries)
                if (entry is MutationDefinition definition && !MutationLoadout.Equipped.Contains(entry.name) && MutationLoadout.Fits(used, definition.Notches, MutationLoadout.NotchPool))
                    Candidates.Add(entry.name);
            if (Candidates.Count == 0)
                return;
            var id = Candidates[Random.Range(0, Candidates.Count)];
            Collectibles.Grant(PickupKind.Mutation, id, 1);
            controller.Equip(id);
        }

        private static void GrantEvolution()
        {
            var weapons = CreatureSpawner.Spawned.GetComponent<WeaponController>();
            if (weapons == null)
                return;
            var open = 0;
            var pick = -1;
            for (var slot = 0; slot < WeaponController.Slots; slot++)
            {
                var weapon = weapons.Weapon(slot);
                if (weapon == null || weapon.Modifiers.Length == 0 || !string.IsNullOrEmpty(Upgrades.EquippedModifier(ContentCatalog.IdOf(weapon))))
                    continue;
                open++;
                if (pick < 0 || Random.value < 0.5f)
                    pick = slot;
            }
            if (pick >= 0)
            {
                var weapon = weapons.Weapon(pick);
                var modifier = weapon.Modifiers[Random.Range(0, weapon.Modifiers.Length)];
                Upgrades.GrantModifier(modifier.name);
                Upgrades.EquipModifier(ContentCatalog.IdOf(weapon), modifier.name);
                UnityEngine.Debug.Log($"Run evolution: {weapon.name} gains {modifier.name}");
                open--;
            }
            RunSession.EvolutionsOpen = Mathf.Max(0, open);
        }
    }
}
