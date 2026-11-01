using System.Collections.Generic;
using NUnit.Framework;
using TentacleVania.Abilities.Feats;
using TentacleVania.Core.Progression;

namespace TentacleVania.Abilities.Tests
{
    public class FeatTests
    {
        [Test]
        public void WindowDropsOldEntries()
        {
            var counter = new FeatCounter(3, 10f);
            counter.Record(0f);
            counter.Record(5f);
            Assert.AreEqual(2, counter.Record(12f));
            Assert.IsFalse(counter.Done);
            Assert.AreEqual(3, counter.Record(13f));
            Assert.IsTrue(counter.Done);
        }

        [Test]
        public void CumulativeCounterNeverForgets()
        {
            var counter = new FeatCounter(2, 0f);
            counter.Record(0f);
            Assert.AreEqual(2, counter.Record(1000f));
        }

        [Test]
        public void ProgressEntriesRoundTrip()
        {
            var entries = new List<string>();
            FeatProgress.Store(entries, "AcidBloodFeat", 3);
            FeatProgress.Store(entries, "AcidBlood", 1);
            FeatProgress.Store(entries, "AcidBloodFeat", 4);
            Assert.AreEqual(4, FeatProgress.Parse(entries, "AcidBloodFeat"));
            Assert.AreEqual(1, FeatProgress.Parse(entries, "AcidBlood"));
            Assert.AreEqual(0, FeatProgress.Parse(entries, "Missing"));
        }
    }
}
