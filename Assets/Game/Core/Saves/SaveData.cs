using System;
using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.Core.Saves
{
    [Serializable]
    public sealed class SaveData
    {
        public int Version = SaveSerializer.CurrentVersion;
        public string SavedAtUtc = "";
        public double PlaytimeSeconds;
        public ProgressState Progress = new();
        public BuildState Build = new();
        public WorldState World = new();
        public BiomassState Biomass = new();
    }

    [Serializable]
    public sealed class ProgressState
    {
        public string Sector = "";
        public string Scene = "";
        public string LastNest = "";
        public string LastNestScene = "";
        public Vector3 RespawnPosition;
        public float RespawnYaw;
        public string ArenaInProgress = "";
        public string ArenaScene = "";
        public Vector3 ArenaPosition;
        public bool Completed;
    }

    [Serializable]
    public sealed class BuildState
    {
        public int Level = 1;
        public int Brutality = 1;
        public int Ferocity = 1;
        public int Tenacity = 1;
        public int Cunning = 1;
        public int BiomassSpent;
        public string Weapon1 = "";
        public string Weapon2 = "";
        public string Ability = "";
        public List<string> Abilities = new();
        public List<string> Mutations = new();
        public List<string> OwnedMutations = new();
        public int BonusNotches;
        public int Mutagen;
        public List<string> AbilityTiers = new();
        public List<string> OwnedModifiers = new();
        public List<string> EquippedModifiers = new();
        public List<string> OwnedWeapons = new();
        public List<string> Trinkets = new();
    }

    public enum WorldFlag
    {
        GrownNest,
        DestroyedRelay,
        OpenedShortcut,
        DefeatedBoss,
        CollectedPickup,
        ExploredRoom,
        ClearedArena,
        PlayedLine,
        CollectedLog,
        SeenTutorial,
        BreachedWall,
        OverloadedNode,
        DiedToBoss,
    }

    [Serializable]
    public sealed class WorldState
    {
        public List<string> GrownNests = new();
        public List<string> DestroyedRelays = new();
        public List<string> OpenedShortcuts = new();
        public List<string> DefeatedBosses = new();
        public List<string> CollectedPickups = new();
        public List<string> ExploredRooms = new();
        public List<string> ClearedArenas = new();
        public List<string> PlayedLines = new();
        public List<string> CollectedLogs = new();
        public List<string> SeenTutorials = new();
        public List<string> BreachedWalls = new();
        public List<string> OverloadedNodes = new();
        public List<string> BossDeaths = new();

        public List<string> List(WorldFlag flag) => flag switch
        {
            WorldFlag.GrownNest => GrownNests,
            WorldFlag.DestroyedRelay => DestroyedRelays,
            WorldFlag.OpenedShortcut => OpenedShortcuts,
            WorldFlag.DefeatedBoss => DefeatedBosses,
            WorldFlag.CollectedPickup => CollectedPickups,
            WorldFlag.ExploredRoom => ExploredRooms,
            WorldFlag.PlayedLine => PlayedLines,
            WorldFlag.CollectedLog => CollectedLogs,
            WorldFlag.SeenTutorial => SeenTutorials,
            WorldFlag.BreachedWall => BreachedWalls,
            WorldFlag.OverloadedNode => OverloadedNodes,
            WorldFlag.DiedToBoss => BossDeaths,
            _ => ClearedArenas,
        };

        public bool Has(WorldFlag flag, string id) => !string.IsNullOrEmpty(id) && List(flag).Contains(id);

        public bool Set(WorldFlag flag, string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;
            var list = List(flag);
            if (list.Contains(id))
                return false;
            list.Add(id);
            return true;
        }

        public bool Clear(WorldFlag flag, string id) => !string.IsNullOrEmpty(id) && List(flag).Remove(id);
    }

    [Serializable]
    public sealed class BiomassState
    {
        public int Carried;
        public bool Dropped;
        public int DroppedAmount;
        public string DroppedScene = "";
        public Vector3 DroppedPosition;
    }

    [Serializable]
    public sealed class SaveSummary
    {
        public int Slot;
        public bool Exists;
        public string Sector = "";
        public int Level;
        public double PlaytimeSeconds;
        public string SavedAtUtc = "";
    }
}
