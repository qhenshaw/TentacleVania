using NUnit.Framework;
using TentacleVania.Core.Runs;
using TentacleVania.Core.Saves;

namespace TentacleVania.Core.Tests
{
    public sealed class RunSaveTests
    {
        [Test]
        public void AlphaSavesMigrateToAnEmptyMetaAndNoRun()
        {
            var old = new SaveData();
            old.Run.Active = true;
            old.Meta.Essence = 99;
            var json = SaveSerializer.ToJson(old).Replace($"\"Version\": {SaveSerializer.CurrentVersion}", "\"Version\": 2");
            var loaded = SaveSerializer.FromJson(json);
            Assert.That(loaded.Run.Active, Is.False);
            Assert.That(loaded.Meta, Is.Not.Null);
            Assert.That(loaded.Meta.Runs, Is.Empty);
        }

        [Test]
        public void MetaAndRunSurviveARoundTrip()
        {
            var data = new SaveData();
            data.Meta.Essence = 240;
            data.Meta.Unlocks.Add("weapon.BoneScythes");
            data.Meta.Runs.Add(new RunRecord { Seed = 7, Floor = 2, Essence = 120, Won = false });
            data.Run.Active = true;
            data.Run.Seed = 1234;
            data.Run.Floor = 1;
            data.Run.Step = 5;
            data.Run.Cleared = true;
            data.Run.UsedRooms.Add("1:3");
            var loaded = SaveSerializer.FromJson(SaveSerializer.ToJson(data));
            Assert.That(loaded.Meta.Essence, Is.EqualTo(240));
            Assert.That(loaded.Meta.Unlocks, Is.EquivalentTo(new[] { "weapon.BoneScythes" }));
            Assert.That(loaded.Meta.Runs[0].Floor, Is.EqualTo(2));
            Assert.That(loaded.Run.Active, Is.True);
            Assert.That(loaded.Run.Step, Is.EqualTo(5));
            Assert.That(loaded.Run.Cleared, Is.True);
            Assert.That(loaded.Run.UsedRooms, Is.EquivalentTo(new[] { "1:3" }));
        }

        [Test]
        public void SessionRestoresAndCapturesRunState()
        {
            var state = new RunState { Seed = 42, Floor = 2, Step = 7, Essence = 90, SizeStage = 9, StepsSinceMutation = 2, EvolutionsOpen = 1 };
            RunSession.Restore(state);
            var captured = new RunState();
            RunSession.Capture(captured);
            Assert.That(captured.Seed, Is.EqualTo(42));
            Assert.That(captured.Floor, Is.EqualTo(2));
            Assert.That(captured.Step, Is.EqualTo(7));
            Assert.That(captured.Essence, Is.EqualTo(90));
            Assert.That(captured.SizeStage, Is.EqualTo(RunSession.MaxSizeStage));
            Assert.That(captured.EvolutionsOpen, Is.EqualTo(1));
            RunSession.Restore(new RunState());
        }
    }
}
