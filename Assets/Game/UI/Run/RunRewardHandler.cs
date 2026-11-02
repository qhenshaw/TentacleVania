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
        private static readonly List<MutationDefinition> Pool = new(64);
        private static readonly List<MutationBuild> PoolBuilds = new(64);
        private static readonly List<MutationBuild> PickedBuilds = new(16);
        private static readonly List<int> Picks = new(4);
        private static readonly List<MutationDefinition> Offered = new(4);

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
            Pool.Clear();
            PoolBuilds.Clear();
            foreach (var entry in controller.Catalog.Entries)
                if (entry is MutationDefinition definition && !MutationLoadout.Equipped.Contains(entry.name))
                {
                    Pool.Add(definition);
                    PoolBuilds.Add(definition.Builds);
                }
            if (Pool.Count == 0)
                return;
            PickedBuilds.Clear();
            foreach (var id in MutationLoadout.Equipped)
            {
                var equipped = controller.Find(id);
                if (equipped != null)
                    PickedBuilds.Add(equipped.Builds);
            }
            var rng = new System.Random(unchecked(RunSession.Seed * 31 + RunSession.Floor * 1009 + RunSession.Step));
            MutationOffers.Pick(rng, PoolBuilds, PickedBuilds, MutationOffers.Count, Picks);
            Offered.Clear();
            foreach (var index in Picks)
                Offered.Add(Pool[index]);
            var screen = MutationPickScreen.Instance;
            if (screen != null)
            {
                screen.Open(controller, Offered);
                return;
            }
            var used = controller.UsedNotches();
            foreach (var offer in Offered)
                if (MutationLoadout.Fits(used, offer.Notches, MutationLoadout.NotchPool))
                {
                    MutationLoadout.Grant(offer.name);
                    controller.Equip(offer.name);
                    return;
                }
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
