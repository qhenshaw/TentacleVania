using NUnit.Framework;
using TentacleVania.Combat.Damage;
using TentacleVania.Combat.Status;

namespace TentacleVania.Combat.Tests
{
    public sealed class MinionSlotsTests
    {
        [Test]
        public void MinionsBiteOnTheirIntervalAndExpire()
        {
            var slots = new MinionSlots(4);
            var index = slots.Spawn(Team.Creature, 0f, 6f);
            Assert.That(slots.BiteDue(index, 0f, 0.6f), Is.True);
            Assert.That(slots.BiteDue(index, 0.3f, 0.6f), Is.False);
            Assert.That(slots.BiteDue(index, 0.6f, 0.6f), Is.True);
            Assert.That(slots.Expire(index, 5.9f), Is.False);
            Assert.That(slots.Expire(index, 6f), Is.True);
            Assert.That(slots.BiteDue(index, 7f, 0.6f), Is.False);
            Assert.That(slots.ActiveCount, Is.EqualTo(0));
        }

        [Test]
        public void FullSwarmReplacesTheOldest()
        {
            var slots = new MinionSlots(2);
            var a = slots.Spawn(Team.Creature, 0f, 6f);
            var b = slots.Spawn(Team.Creature, 1f, 6f);
            Assert.That(slots.Spawn(Team.Creature, 2f, 6f), Is.EqualTo(a));
            Assert.That(slots.Spawn(Team.Creature, 3f, 6f), Is.EqualTo(b));
            Assert.That(slots.ActiveCount, Is.EqualTo(2));
        }
    }
}
