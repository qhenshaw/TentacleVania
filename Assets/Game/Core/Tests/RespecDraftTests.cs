using NUnit.Framework;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Saves;

namespace TentacleVania.Core.Tests
{
    public sealed class RespecDraftTests
    {
        [Test]
        public void DraftStartsAtOneAndAssignsTheTotal()
        {
            SaveSystem.NewGame(0);
            Stats.Set(StatKind.Brutality, 4);
            var draft = new RespecDraft();
            draft.Begin(Stats.TotalLevel);
            Assert.That(draft.Get(StatKind.Brutality), Is.EqualTo(1));
            Assert.That(draft.Unassigned, Is.EqualTo(3));
            Assert.That(draft.Apply(), Is.False);
            Assert.That(draft.Remove(StatKind.Ferocity), Is.False);
            Assert.That(draft.Add(StatKind.Tenacity) && draft.Add(StatKind.Tenacity) && draft.Add(StatKind.Cunning), Is.True);
            Assert.That(draft.Add(StatKind.Cunning), Is.False);
            Assert.That(draft.IsComplete, Is.True);
            Assert.That(draft.Apply(), Is.True);
            Assert.That(Stats.Brutality, Is.EqualTo(1));
            Assert.That(Stats.Tenacity, Is.EqualTo(3));
            Assert.That(Stats.Cunning, Is.EqualTo(2));
            Assert.That(Stats.TotalLevel, Is.EqualTo(7));
        }
    }
}
