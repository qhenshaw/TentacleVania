using NUnit.Framework;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Runs;
using TentacleVania.Core.Saves;
using UnityEngine;

namespace TentacleVania.Core.Tests
{
    public sealed class MotherCallTests
    {
        [Test]
        public void RanksCostMoreEachTimeAndSpendEssence()
        {
            var meta = new MetaState { Essence = MotherCall.RankCost(0) + MotherCall.RankCost(1) };
            Assert.That(MotherCall.TryBuyRank(meta, StatKind.Tenacity), Is.True);
            Assert.That(MotherCall.TryBuyRank(meta, StatKind.Tenacity), Is.True);
            Assert.That(meta.Tenacity, Is.EqualTo(2));
            Assert.That(meta.Essence, Is.Zero);
            Assert.That(MotherCall.TryBuyRank(meta, StatKind.Tenacity), Is.False);
            Assert.That(MotherCall.RankCost(2), Is.GreaterThan(MotherCall.RankCost(1)));
        }

        [Test]
        public void RanksStopAtTheCap()
        {
            var meta = new MetaState { Essence = 1000000, Brutality = MotherCall.MaxRank };
            Assert.That(MotherCall.TryBuyRank(meta, StatKind.Brutality), Is.False);
            Assert.That(meta.Brutality, Is.EqualTo(MotherCall.MaxRank));
        }

        [Test]
        public void UnlocksCostEssenceOnceAndFreeEntriesAreOwned()
        {
            var meta = new MetaState { Essence = 100 };
            var free = new MotherCallStock.Entry { Kind = UnlockKind.Weapon, Item = "LashWhips", Cost = 0 };
            var paid = new MotherCallStock.Entry { Kind = UnlockKind.Weapon, Item = "RailSpine", Cost = 80 };
            Assert.That(MotherCall.IsUnlocked(meta, free), Is.True);
            Assert.That(MotherCall.IsUnlocked(meta, paid), Is.False);
            Assert.That(MotherCall.TryUnlock(meta, paid), Is.True);
            Assert.That(MotherCall.IsUnlocked(meta, paid), Is.True);
            Assert.That(meta.Essence, Is.EqualTo(20));
            Assert.That(MotherCall.TryUnlock(meta, paid), Is.False);
        }

        [Test]
        public void CountsUnlocksByKind()
        {
            var meta = new MetaState();
            meta.Unlocks.Add(MotherCall.UnlockId(UnlockKind.StartingNotch, "1"));
            meta.Unlocks.Add(MotherCall.UnlockId(UnlockKind.StartingNotch, "2"));
            meta.Unlocks.Add(MotherCall.UnlockId(UnlockKind.Weapon, "RailSpine"));
            Assert.That(MotherCall.Count(meta, UnlockKind.StartingNotch), Is.EqualTo(2));
            Assert.That(MotherCall.Count(meta, UnlockKind.DeathDefiance), Is.Zero);
        }

        [Test]
        public void UnlistedItemsAreAvailable()
        {
            var stock = ScriptableObject.CreateInstance<MotherCallStock>();
            Assert.That(MotherCall.IsUnlocked(new MetaState(), stock, UnlockKind.Weapon, "Anything"), Is.True);
            Object.DestroyImmediate(stock);
        }
    }
}
