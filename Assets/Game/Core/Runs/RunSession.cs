using System;
using System.Globalization;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Saves;
using UnityEngine;

namespace TentacleVania.Core.Runs
{
    public static class RunSession
    {
        public const int MaxSizeStage = 3;

        public static bool Active { get; private set; }
        public static int Seed { get; private set; }
        public static int Floor { get; set; }
        public static int Step { get; set; }
        public static int Essence { get; private set; }
        public static int StepsSinceMutation { get; set; }
        public static int StepsSinceEvolution { get; set; }
        public static int EvolutionsOpen { get; set; } = 2;
        public static int SizeStage { get; private set; }
        public static int BiomassReward { get; set; } = 150;
        public static int EssenceReward { get; set; } = 20;
        public static int FloorEssence { get; set; } = 50;
        public static string RespawnScene { get; set; }
        public static bool DefianceUsed { get; set; }
        public static int Rooms { get; set; }
        public static int Rerolls { get; set; }
        public static float Seconds { get; set; }
        public static RunRecord Last { get; private set; }
        public static bool DefianceReady => Active && !DefianceUsed && MotherCall.Count(SaveSystem.Data.Meta, UnlockKind.DeathDefiance) > 0;

        public static event Action<RunReward> RewardGranted;
        public static event Action Started;
        public static event Action Resumed;
        public static event Action<int> FloorCleared;
        public static event Action<bool> Ended;
        public static event Action DefianceTriggered;

        public static RewardContext Context => new() { Floor = Floor, Step = Step, EvolutionsOpen = EvolutionsOpen, StepsSinceMutation = StepsSinceMutation, StepsSinceEvolution = StepsSinceEvolution };

        public static void Begin(int seed)
        {
            Active = true;
            Seed = seed;
            Floor = 0;
            Step = 0;
            Essence = 0;
            StepsSinceMutation = 0;
            StepsSinceEvolution = 0;
            EvolutionsOpen = 2;
            SizeStage = 0;
            DefianceUsed = false;
            Rooms = 0;
            Seconds = 0f;
            Rerolls = MotherCall.Count(SaveSystem.Data.Meta, UnlockKind.RerollToken);
            SaveSystem.Data.Meta.RunsStarted++;
            MutationLoadout.ResetBonusNotches();
            for (var i = MotherCall.Count(SaveSystem.Data.Meta, UnlockKind.StartingNotch); i > 0; i--)
                MutationLoadout.AddBonusNotch();
            Biomass.Reset();
            Started?.Invoke();
            Stats.NotifyChanged();
        }

        public static void Resume(RunState state)
        {
            Restore(state);
            Active = true;
            Resumed?.Invoke();
            Stats.NotifyChanged();
        }

        public static void Capture(RunState into)
        {
            into.Active = Active;
            into.Seed = Seed;
            into.Floor = Floor;
            into.Step = Step;
            into.Essence = Essence;
            into.SizeStage = SizeStage;
            into.StepsSinceMutation = StepsSinceMutation;
            into.StepsSinceEvolution = StepsSinceEvolution;
            into.EvolutionsOpen = EvolutionsOpen;
            into.DefianceUsed = DefianceUsed;
            into.Rooms = Rooms;
            into.Rerolls = Rerolls;
            into.Seconds = Seconds;
        }

        public static void Restore(RunState from)
        {
            Seed = from.Seed;
            Floor = from.Floor;
            Step = from.Step;
            Essence = from.Essence;
            SizeStage = Mathf.Clamp(from.SizeStage, 0, MaxSizeStage);
            StepsSinceMutation = from.StepsSinceMutation;
            StepsSinceEvolution = from.StepsSinceEvolution;
            EvolutionsOpen = from.EvolutionsOpen;
            DefianceUsed = from.DefianceUsed;
            Rooms = from.Rooms;
            Rerolls = from.Rerolls;
            Seconds = from.Seconds;
        }

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
            StepsSinceEvolution = reward == RunReward.Evolution ? 0 : StepsSinceEvolution + 1;
            RewardGranted?.Invoke(reward);
        }

        public static void AddEssence(int amount)
        {
            amount = Mathf.Max(0, amount);
            Essence += amount;
            SaveSystem.Data.Meta.Essence += amount;
        }

        public static void ClearFloor(int floor)
        {
            SizeStage = Math.Min(MaxSizeStage, SizeStage + 1);
            AddEssence(FloorEssence);
            MutationLoadout.AddBonusNotch();
            Stats.NotifyChanged();
            FloorCleared?.Invoke(floor);
        }

        public static RunRecord Record(bool won) => new()
        {
            Seed = Seed,
            Floor = Floor,
            Step = Step,
            Essence = Essence,
            Rooms = Rooms,
            Seconds = Seconds,
            Won = won,
            EndedAtUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
        };

        public static void End(bool won)
        {
            if (!Active)
                return;
            Last = Record(won);
            SaveSystem.Data.Meta.Runs.Add(Last);
            SaveSystem.Data.Run = new();
            Active = false;
            Ended?.Invoke(won);
            Autosave.Request(AutosaveReason.RunEnded);
        }

        public static void Suspend() => Active = false;

        public static void Defied() => DefianceTriggered?.Invoke();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Active = false;
            EvolutionsOpen = 2;
            SizeStage = 0;
            RespawnScene = null;
            RewardGranted = null;
            Started = null;
            Resumed = null;
            FloorCleared = null;
            Ended = null;
            DefianceTriggered = null;
        }
    }
}
