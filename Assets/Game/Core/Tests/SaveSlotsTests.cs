using NUnit.Framework;
using TentacleVania.Core.Saves;

namespace TentacleVania.Core.Tests
{
    public sealed class SaveSlotsTests
    {
        private static SaveSummary Slot(int slot, string savedAt) => new() { Slot = slot, Exists = savedAt != null, SavedAtUtc = savedAt ?? "" };

        [Test]
        public void MostRecentPicksTheLatestSavedSlot()
        {
            var slots = new[] { Slot(0, "2026-10-30T10:00:00Z"), Slot(1, null), Slot(2, "2026-10-31T09:00:00Z"), Slot(3, "2026-10-29T23:00:00Z") };
            Assert.That(SaveSlots.MostRecent(slots), Is.EqualTo(2));
        }

        [Test]
        public void MostRecentIsNoneWhenEverySlotIsEmpty()
        {
            Assert.That(SaveSlots.MostRecent(new[] { Slot(0, null), Slot(1, null) }), Is.EqualTo(-1));
        }

        [Test]
        public void FirstEmptySkipsUsedSlots()
        {
            Assert.That(SaveSlots.FirstEmpty(new[] { Slot(0, "a"), Slot(1, "b"), Slot(2, null) }), Is.EqualTo(2));
            Assert.That(SaveSlots.FirstEmpty(new[] { Slot(0, "a") }), Is.EqualTo(-1));
        }

        [Test]
        public void PlaytimeShowsHoursOnlyWhenNeeded()
        {
            Assert.That(SaveSlots.Playtime(65), Is.EqualTo("1:05"));
            Assert.That(SaveSlots.Playtime(3 * 3600 + 7 * 60 + 9), Is.EqualTo("3:07:09"));
            Assert.That(SaveSlots.Playtime(-4), Is.EqualTo("0:00"));
        }
    }
}
