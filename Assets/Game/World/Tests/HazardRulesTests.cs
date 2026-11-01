using NUnit.Framework;
using TentacleVania.Combat.Damage;
using TentacleVania.World.Hazards;
using UnityEngine;

namespace TentacleVania.World.Tests
{
    public class HazardRulesTests
    {
        [TestCase(HazardKind.Electric, "ElectricResistance", DamageType.Electric)]
        [TestCase(HazardKind.Fire, "FireResistance", DamageType.Fire)]
        [TestCase(HazardKind.Acid, "AcidResistance", DamageType.Acid)]
        [TestCase(HazardKind.Radiation, "RadiationResistance", DamageType.Radiation)]
        public void EachDamagingHazardHasItsResistanceAndType(HazardKind kind, string resistance, DamageType type)
        {
            Assert.AreEqual(resistance, HazardRules.ResistanceFor(kind));
            Assert.AreEqual(type, HazardRules.DamageTypeFor(kind));
            Assert.IsTrue(HazardRules.Damages(kind));
        }

        [Test]
        public void CryoSlowsWithoutDamageOrResistance()
        {
            Assert.IsFalse(HazardRules.Damages(HazardKind.Cryo));
            Assert.IsNull(HazardRules.ResistanceFor(HazardKind.Cryo));
        }

        [Test]
        public void InsideTestsTheBoxOverTheFloor()
        {
            var size = new Vector3(6f, 2f, 4f);
            Assert.IsTrue(HazardRules.Inside(new Vector3(2.9f, 0.1f, -1.9f), size));
            Assert.IsFalse(HazardRules.Inside(new Vector3(3.1f, 0.1f, 0f), size));
            Assert.IsFalse(HazardRules.Inside(new Vector3(0f, 2.5f, 0f), size));
        }
    }
}
