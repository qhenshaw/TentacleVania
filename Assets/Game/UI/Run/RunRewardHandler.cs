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
        private static readonly List<int> Options = new(2);
        private static readonly List<EvolutionOffer> Evolutions = new(4);
        private static int _draws;

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
            _draws = 0;
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
                    GrantMutation();
                    break;
                case RunReward.Challenge:
                    if (Rng().Next(2) != 0 || !GrantEvolution())
                        GrantMutation();
                    break;
                case RunReward.Evolution:
                    if (!GrantEvolution())
                        GrantMutation();
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
            var rng = Rng();
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

        private static bool GrantEvolution()
        {
            var weapons = CreatureSpawner.Spawned.GetComponent<WeaponController>();
            if (weapons == null)
                return false;
            OpenOptions(weapons);
            EvolutionOffers.Pick(Rng(), Options, Evolutions);
            if (Evolutions.Count == 0)
                return false;
            var screen = EvolutionPickScreen.Instance;
            if (screen != null)
                screen.Open(weapons, Evolutions);
            else
                ApplyEvolution(weapons, Evolutions[0]);
            return true;
        }

        public static void ApplyEvolution(WeaponController weapons, EvolutionOffer offer)
        {
            var weapon = weapons.Weapon(offer.Slot);
            if (weapon == null || offer.Option < 0 || offer.Option >= weapon.Modifiers.Length)
                return;
            var modifier = weapon.Modifiers[offer.Option];
            Upgrades.GrantModifier(modifier.name);
            Upgrades.EquipModifier(ContentCatalog.IdOf(weapon), modifier.name);
            OpenOptions(weapons);
            RunSession.EvolutionsOpen = EvolutionOffers.OpenSlots(Options);
        }

        private static void OpenOptions(WeaponController weapons)
        {
            Options.Clear();
            for (var slot = 0; slot < WeaponController.Slots; slot++)
            {
                var weapon = weapons.Weapon(slot);
                var open = weapon != null && string.IsNullOrEmpty(Upgrades.EquippedModifier(ContentCatalog.IdOf(weapon)));
                Options.Add(open ? weapon.Modifiers.Length : 0);
            }
        }

        private static System.Random Rng() => new(unchecked(RunSession.Seed * 31 + RunSession.Floor * 1009 + RunSession.Step * 7 + _draws++));
    }
}
