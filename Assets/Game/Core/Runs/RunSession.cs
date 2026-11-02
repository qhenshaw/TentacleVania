using System;
using TentacleVania.Core.Progression;
using UnityEngine;

namespace TentacleVania.Core.Runs
{
    public static class RunSession
    {
        public static bool Active { get; private set; }
        public static int Seed { get; private set; }
        public static int Floor { get; set; }
        public static int Step { get; set; }
        public static int Essence { get; private set; }
        public static int StepsSinceMutation { get; set; }
        public static int EvolutionsOpen { get; set; } = 2;
        public static int BiomassReward { get; set; } = 150;
        public static int EssenceReward { get; set; } = 20;
        public static int FloorEssence { get; set; } = 50;
        public static int SizeStage { get; private set; }
        public const int MaxSizeStage = 3;

        public static event Action<RunReward> RewardGranted;
        public static event Action Started;
        public static event Action<int> FloorCleared;
        public static event Action Ended;

        public static void Begin(int seed)
        {
            Active = true;
            Seed = seed;
            Floor = 0;
            Step = 0;
            Essence = 0;
            StepsSinceMutation = 0;
            EvolutionsOpen = 2;
            SizeStage = 0;
            MutationLoadout.ResetBonusNotches();
            Started?.Invoke();
            Stats.NotifyChanged();
        }

        public static RewardContext Context => new() { Floor = Floor, Step = Step, EvolutionsOpen = EvolutionsOpen, StepsSinceMutation = StepsSinceMutation };

        public static void Grant(RunReward reward)
        {
            switch (reward)
            {
                case RunReward.None:
                case RunReward.Shop:
                    return;
                case RunReward.Biomass:
                    Biomass.Add(BiomassReward);
                    break;
                case RunReward.Essence:
                    AddEssence(EssenceReward);
                    break;
            }
            StepsSinceMutation = reward == RunReward.Mutation || reward == RunReward.Challenge ? 0 : StepsSinceMutation + 1;
            RewardGranted?.Invoke(reward);
        }

        public static void AddEssence(int amount) => Essence += Mathf.Max(0, amount);

        public static void ClearFloor(int floor)
        {
            SizeStage = Math.Min(MaxSizeStage, SizeStage + 1);
            AddEssence(FloorEssence);
            MutationLoadout.AddBonusNotch();
            Stats.NotifyChanged();
            FloorCleared?.Invoke(floor);
        }

        public static void End()
        {
            if (!Active)
                return;
            Active = false;
            Ended?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Active = false;
            EvolutionsOpen = 2;
            SizeStage = 0;
            RewardGranted = null;
            Started = null;
            FloorCleared = null;
            Ended = null;
        }
    }
}
