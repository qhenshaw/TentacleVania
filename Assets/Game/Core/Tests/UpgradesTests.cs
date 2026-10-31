using NUnit.Framework;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Saves;

namespace TentacleVania.Core.Tests
{
    public sealed class UpgradesTests
    {
        [SetUp]
        public void SetUp() => SaveSystem.NewGame(0);

        [Test]
        public void AbilityTierUpgradeSpendsBiomassAndCountsTowardSize()
        {
            Biomass.Add(1000);
            Assert.That(Upgrades.AbilityTier("BioShield"), Is.EqualTo(1));
            Assert.That(Upgrades.TryUpgradeAbility("BioShield", 800, 2), Is.True);
            Assert.That(Upgrades.AbilityTier("BioShield"), Is.EqualTo(2));
            Assert.That(Biomass.Carried, Is.EqualTo(200));
            Assert.That(SaveSystem.Data.Build.BiomassSpent, Is.EqualTo(800));
            Assert.That(Upgrades.TryUpgradeAbility("BioShield", 0, 2), Is.False);
        }

        [Test]
        public void AbilityUpgradeFailsWithoutBiomass()
        {
            Biomass.Add(100);
            Assert.That(Upgrades.TryUpgradeAbility("BioShield", 800, 2), Is.False);
            Assert.That(Upgrades.AbilityTier("BioShield"), Is.EqualTo(1));
            Assert.That(Biomass.Carried, Is.EqualTo(100));
        }

        [Test]
        public void ModifierUnlockNeedsBiomassAndMutagen()
        {
            Biomass.Add(1500);
            Assert.That(Upgrades.TryUnlockModifier("Maelstrom", 1000, 1), Is.False);
            Upgrades.AddMutagen(1);
            Assert.That(Upgrades.TryUnlockModifier("Maelstrom", 1000, 1), Is.True);
            Assert.That(Upgrades.Mutagen, Is.EqualTo(0));
            Assert.That(Biomass.Carried, Is.EqualTo(500));
            Assert.That(Upgrades.TryUnlockModifier("Maelstrom", 0, 0), Is.False);
        }

        [Test]
        public void OneModifierEquippedPerWeapon()
        {
            Upgrades.GrantModifier("Maelstrom");
            Upgrades.GrantModifier("ChargedCyclone");
            Assert.That(Upgrades.EquipModifier("LashWhips", "Shatter"), Is.False);
            Assert.That(Upgrades.EquipModifier("LashWhips", "Maelstrom"), Is.True);
            Assert.That(Upgrades.EquipModifier("LashWhips", "ChargedCyclone"), Is.True);
            Assert.That(Upgrades.EquippedModifier("LashWhips"), Is.EqualTo("ChargedCyclone"));
            Assert.That(Upgrades.EquippedModifier("Lash"), Is.EqualTo(""));
            Assert.That(SaveSystem.Data.Build.EquippedModifiers.Count, Is.EqualTo(1));
            Assert.That(Upgrades.EquipModifier("LashWhips", null), Is.True);
            Assert.That(Upgrades.EquippedModifier("LashWhips"), Is.EqualTo(""));
        }
    }
}
