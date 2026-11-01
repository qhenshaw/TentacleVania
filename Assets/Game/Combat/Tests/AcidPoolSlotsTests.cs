using NUnit.Framework;
using TentacleVania.Combat.Damage;
using TentacleVania.Combat.Status;
using UnityEngine;

namespace TentacleVania.Combat.Tests
{
    public sealed class AcidPoolSlotsTests
    {
        [Test]
        public void PoolTicksOnLandingThenEveryIntervalUntilItExpires()
        {
            var slots = new AcidPoolSlots(4);
            var index = slots.Spawn(Vector3.zero, 1.5f, 4f, 6f, Team.Creature, 10f);
            var ticks = 0;
            for (var t = 10f; t < 15f; t += 0.05f)
            {
                if (slots.TickDue(index, t, 0.5f))
                    ticks++;
                slots.Expire(index, t);
            }
            Assert.That(ticks, Is.EqualTo(8));
            Assert.That(slots.ActiveCount, Is.EqualTo(0));
        }

        [Test]
        public void FullPoolReplacesTheOneEndingSoonest()
        {
            var slots = new AcidPoolSlots(2);
            var first = slots.Spawn(Vector3.zero, 1f, 4f, 1f, Team.Creature, 0f);
            var second = slots.Spawn(Vector3.one, 1f, 4f, 1f, Team.Creature, 1f);
            Assert.That(second, Is.Not.EqualTo(first));
            Assert.That(slots.Spawn(Vector3.up, 1f, 4f, 1f, Team.Site, 2f), Is.EqualTo(first));
            Assert.That(slots[first].Team, Is.EqualTo(Team.Site));
            Assert.That(slots.ActiveCount, Is.EqualTo(2));
        }

        [Test]
        public void CorrodeRaisesHealthAndArmorDamage()
        {
            var corrode = new ScaleModifier { HealthScale = 1.2f, ArmorScale = 1.5f };
            var info = DamageInfo.Create(10f, Team.Creature);
            var context = new DamageContext(info, Team.Site);
            corrode.Modify(ref context);
            Assert.That(context.HealthScale, Is.EqualTo(1.2f).Within(1e-4f));
            Assert.That(context.ArmorScale, Is.EqualTo(1.5f).Within(1e-4f));
        }
    }
}
