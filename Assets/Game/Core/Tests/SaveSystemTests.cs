using System;
using System.Collections.Generic;
using NUnit.Framework;
using TentacleVania.Core.Saves;
using UnityEngine;

namespace TentacleVania.Core.Tests
{
    public sealed class SaveSystemTests
    {
        private sealed class MemoryStorage : ISaveStorage
        {
            public readonly Dictionary<int, string> Files = new();
            public string Location => "memory";
            public bool Exists(int slot) => Files.ContainsKey(slot);
            public bool TryRead(int slot, out string json) => Files.TryGetValue(slot, out json);
            public void Write(int slot, string json) => Files[slot] = json;
            public void Delete(int slot) => Files.Remove(slot);
        }

        private sealed class FakeParticipant : ISaveParticipant
        {
            public string Weapon = "LashWhips";
            public string Restored;
            public void Capture(SaveData data) => data.Build.Weapon1 = Weapon;
            public void Restore(SaveData data) => Restored = data.Build.Weapon1;
        }

        private ISaveStorage _previous;
        private MemoryStorage _storage;

        [SetUp]
        public void SetUp()
        {
            _previous = SaveSystem.Storage;
            _storage = new MemoryStorage();
            SaveSystem.Storage = _storage;
        }

        [TearDown]
        public void TearDown() => SaveSystem.Storage = _previous;

        private static SaveData Sample()
        {
            var data = new SaveData { PlaytimeSeconds = 123.5 };
            data.Progress.Sector = "SpecimenHolding";
            data.Progress.LastNest = "SH_EntryNest";
            data.Build.Level = 4;
            data.Build.Brutality = 2;
            data.Build.Cunning = 1;
            data.Build.Weapon1 = "LashWhips";
            data.Build.Weapon2 = "CrusherMass";
            data.Build.Ability = "BioShield";
            data.Build.Mutations.Add("Gluttony");
            data.World.Set(WorldFlag.GrownNest, "SH_EntryNest");
            data.World.Set(WorldFlag.DestroyedRelay, "SH_Relay1");
            data.World.Set(WorldFlag.OpenedShortcut, "SH_T1");
            data.World.Set(WorldFlag.DefeatedBoss, "Warden");
            data.World.Set(WorldFlag.CollectedPickup, "SH_Log3");
            data.World.Set(WorldFlag.ExploredRoom, "SH_A1");
            data.Biomass.Carried = 40;
            data.Biomass.Dropped = true;
            data.Biomass.DroppedAmount = 120;
            data.Biomass.DroppedScene = "SH_A2";
            data.Biomass.DroppedPosition = new Vector3(1.5f, 0f, -3.25f);
            return data;
        }

        [Test]
        public void RoundTripKeepsEveryState()
        {
            var loaded = SaveSerializer.FromJson(SaveSerializer.ToJson(Sample()));
            var expected = Sample();
            Assert.That(loaded.PlaytimeSeconds, Is.EqualTo(expected.PlaytimeSeconds));
            Assert.That(JsonUtility.ToJson(loaded.Progress), Is.EqualTo(JsonUtility.ToJson(expected.Progress)));
            Assert.That(JsonUtility.ToJson(loaded.Build), Is.EqualTo(JsonUtility.ToJson(expected.Build)));
            Assert.That(JsonUtility.ToJson(loaded.World), Is.EqualTo(JsonUtility.ToJson(expected.World)));
            Assert.That(JsonUtility.ToJson(loaded.Biomass), Is.EqualTo(JsonUtility.ToJson(expected.Biomass)));
            Assert.That(loaded.Version, Is.EqualTo(SaveSerializer.CurrentVersion));
        }

        [Test]
        public void OlderSavesRunTheirMigrations()
        {
            SaveSerializer.RegisterMigration(SaveSerializer.CurrentVersion, data => data.Build.Level += 10);
            try
            {
                var json = SaveSerializer.ToJson(Sample());
                var migrated = SaveSerializer.FromJson(json, SaveSerializer.CurrentVersion + 1);
                Assert.That(migrated.Build.Level, Is.EqualTo(14));
                Assert.That(migrated.Version, Is.EqualTo(SaveSerializer.CurrentVersion + 1));
            }
            finally
            {
                SaveSerializer.UnregisterMigration(SaveSerializer.CurrentVersion);
            }
        }

        [Test]
        public void NewerSavesAreRejected()
        {
            var json = SaveSerializer.ToJson(Sample()).Replace($"\"Version\": {SaveSerializer.CurrentVersion}", $"\"Version\": {SaveSerializer.CurrentVersion + 5}");
            Assert.Throws<FormatException>(() => SaveSerializer.FromJson(json));
        }

        [Test]
        public void WorldFlagsAreSetOnce()
        {
            var world = new WorldState();
            Assert.That(world.Set(WorldFlag.ClearedArena, "A1"), Is.True);
            Assert.That(world.Set(WorldFlag.ClearedArena, "A1"), Is.False);
            Assert.That(world.Has(WorldFlag.ClearedArena, "A1"), Is.True);
            Assert.That(world.Has(WorldFlag.GrownNest, "A1"), Is.False);
            Assert.That(world.Clear(WorldFlag.ClearedArena, "A1"), Is.True);
            Assert.That(world.Has(WorldFlag.ClearedArena, "A1"), Is.False);
            Assert.That(world.Set(WorldFlag.ClearedArena, ""), Is.False);
        }

        [Test]
        public void AsyncSavesLandAfterFlushInOrder()
        {
            SaveSystem.NewGame(3);
            SaveSystem.Data.Build.Level = 7;
            SaveSystem.SaveAsync();
            SaveSystem.Data.Build.Level = 9;
            SaveSystem.SaveAsync();
            SaveSystem.Flush();
            Assert.That(_storage.TryRead(3, out var json), Is.True);
            Assert.That(SaveSerializer.FromJson(json).Build.Level, Is.EqualTo(9));
        }

        [Test]
        public void SaveAndLoadGoThroughParticipants()
        {
            var participant = new FakeParticipant();
            SaveSystem.Register(participant);
            try
            {
                SaveSystem.NewGame(2);
                SaveSystem.Save();
                Assert.That(_storage.Exists(2), Is.True);
                participant.Weapon = "Changed";
                Assert.That(SaveSystem.Load(2), Is.True);
                Assert.That(participant.Restored, Is.EqualTo("LashWhips"));
                Assert.That(SaveSystem.Summary(2).Exists, Is.True);
                SaveSystem.Delete(2);
                Assert.That(SaveSystem.Summary(2).Exists, Is.False);
                Assert.That(SaveSystem.Load(2), Is.False);
            }
            finally
            {
                SaveSystem.Unregister(participant);
            }
        }
        [Test]
        public void VersionOneSavesMigrateToTheCurrentFormat()
        {
            var json = SaveSerializer.ToJson(new SaveData()).Replace($"\"Version\": {SaveSerializer.CurrentVersion}", "\"Version\": 1");
            var loaded = SaveSerializer.FromJson(json);
            Assert.That(loaded.Version, Is.EqualTo(SaveSerializer.CurrentVersion));
            Assert.That(loaded.Progress.ArenaInProgress, Is.EqualTo(""));
        }

        [Test]
        public void QuittingMidArenaCountsAsADeathOnLoad()
        {
            var data = new SaveData();
            data.Biomass.Carried = 300;
            data.Progress.ArenaInProgress = "SH/SH_A1";
            data.Progress.ArenaScene = "SH_A1";
            data.Progress.ArenaPosition = new UnityEngine.Vector3(4f, 0f, 9f);
            Assert.That(SaveSystem.ApplyMidArenaQuit(data), Is.True);
            Assert.That(data.Biomass.Carried, Is.EqualTo(0));
            Assert.That(data.Biomass.Dropped, Is.True);
            Assert.That(data.Biomass.DroppedAmount, Is.EqualTo(300));
            Assert.That(data.Biomass.DroppedScene, Is.EqualTo("SH_A1"));
            Assert.That(data.Progress.ArenaInProgress, Is.EqualTo(""));
            Assert.That(SaveSystem.ApplyMidArenaQuit(data), Is.False);
        }

        [Test]
        public void AFullWorldSaveStaysSmallAndRoundTrips()
        {
            var data = new SaveData();
            string[] sectors = { "SH", "AD", "QU", "RD", "BL", "CS", "BA", "AR", "CC", "DC", "BS", "MV" };
            foreach (WorldFlag flag in System.Enum.GetValues(typeof(WorldFlag)))
                foreach (var sector in sectors)
                    for (var i = 0; i < 30; i++)
                        data.World.Set(flag, $"{sector}/{sector}_Room{i:00}/SomeObjectName{i}");
            var json = SaveSerializer.ToJson(data);
            Assert.That(json.Length, Is.LessThan(SaveBudgetBytes), $"{json.Length} characters");
            var loaded = SaveSerializer.FromJson(json);
            foreach (WorldFlag flag in System.Enum.GetValues(typeof(WorldFlag)))
                Assert.That(loaded.World.List(flag).Count, Is.EqualTo(data.World.List(flag).Count), flag.ToString());
        }

        private const int SaveBudgetBytes = 1024 * 1024;
    }
}
