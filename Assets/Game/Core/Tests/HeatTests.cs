using NUnit.Framework;
using TentacleVania.Core.Runs;
using TentacleVania.Core.Saves;

namespace TentacleVania.Core.Tests
{
    public sealed class HeatTests
    {
        [Test]
        public void HeatUnlocksAfterAWonRun()
        {
            var meta = new MetaState();
            Assert.That(Heat.Unlocked(meta), Is.False);
            meta.Runs.Add(new RunRecord { Won = false });
            Assert.That(Heat.Unlocked(meta), Is.False);
            meta.Runs.Add(new RunRecord { Won = true });
            Assert.That(Heat.Unlocked(meta), Is.True);
        }

        [Test]
        public void RanksClampAndAddUp()
        {
            var meta = new MetaState();
            Heat.SetRank(meta, HeatKind.Hardened, 9);
            Heat.SetRank(meta, HeatKind.Swarm, 1);
            Heat.SetRank(meta, HeatKind.Starved, -2);
            Assert.That(Heat.Rank(meta, HeatKind.Hardened), Is.EqualTo(Heat.MaxRank(HeatKind.Hardened)));
            Assert.That(Heat.Rank(meta, HeatKind.Starved), Is.Zero);
            Assert.That(Heat.Total(meta), Is.EqualTo(Heat.MaxRank(HeatKind.Hardened) + 1));
        }

        [Test]
        public void ModifiersScaleWithRanks()
        {
            var meta = new MetaState();
            Assert.That(Heat.Scale(meta, HeatKind.Hardened), Is.EqualTo(1f));
            Assert.That(Heat.EssenceScale(meta), Is.EqualTo(1f));
            Heat.SetRank(meta, HeatKind.Hardened, 2);
            Heat.SetRank(meta, HeatKind.Starved, 2);
            Heat.SetRank(meta, HeatKind.Swarm, 2);
            Assert.That(Heat.Scale(meta, HeatKind.Hardened), Is.EqualTo(1f + 2 * Heat.Step(HeatKind.Hardened)).Within(1e-5f));
            Assert.That(Heat.HealScale(meta), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(Heat.ExtraEnemies(meta), Is.EqualTo(2));
            Assert.That(Heat.EssenceScale(meta), Is.EqualTo(1f + 6 * Heat.EssencePerHeat).Within(1e-5f));
        }
    }
}
