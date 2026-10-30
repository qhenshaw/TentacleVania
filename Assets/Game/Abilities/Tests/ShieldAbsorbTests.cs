using NUnit.Framework;
using TentacleVania.Abilities.BioShield;
using TentacleVania.Combat.Damage;

namespace TentacleVania.Abilities.Tests
{
    public class ShieldAbsorbTests
    {
        [Test]
        public void AbsorbsUntilTheCapacityIsUsed()
        {
            var shield = new ShieldAbsorb();
            shield.Fill(40f);
            Assert.AreEqual(0f, shield.Absorb(25f));
            Assert.AreEqual(15f, shield.Remaining, 1e-4f);
            Assert.AreEqual(10f, shield.Absorb(25f), 1e-4f);
            Assert.IsTrue(shield.IsDepleted);
            Assert.AreEqual(8f, shield.Absorb(8f));
        }

        [Test]
        public void ScalesTheHitDownByWhatItAbsorbs()
        {
            var shield = new ShieldAbsorb();
            shield.Fill(10f);
            var context = new DamageContext(DamageInfo.Create(40f, Team.Site), Team.Creature);
            shield.Modify(ref context);
            Assert.AreEqual(0.75f, context.HealthScale, 1e-4f);
            Assert.AreEqual(10f, shield.AbsorbedThisFrame, 1e-4f);
        }

        [Test]
        public void RespectsEarlierScaling()
        {
            var shield = new ShieldAbsorb();
            shield.Fill(40f);
            var context = new DamageContext(DamageInfo.Create(20f, Team.Site), Team.Creature) { HealthScale = 0.5f };
            shield.Modify(ref context);
            Assert.AreEqual(0f, context.HealthScale, 1e-4f);
            Assert.AreEqual(30f, shield.Remaining, 1e-4f);
        }

        [Test]
        public void RefillingResetsTheCapacity()
        {
            var shield = new ShieldAbsorb();
            shield.Fill(40f);
            shield.Absorb(40f);
            shield.Fill(40f);
            Assert.AreEqual(40f, shield.Remaining);
            Assert.AreEqual(0f, shield.AbsorbedThisFrame);
        }
    }
}
