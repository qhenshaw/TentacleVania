using NUnit.Framework;
using TentacleVania.Combat.Damage;
using TentacleVania.Enemies.Data;
using TentacleVania.Enemies.Traps;
using UnityEngine;
using UnityEditor;

namespace TentacleVania.Enemies.Tests
{
    public sealed class EnemyDataTests
    {
        private static EnemyDefinition Load(string name) =>
            AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"Assets/Game/Enemies/Data/Enemies/{name}.asset");

        [Test]
        public void SurgeonMatchesTheBalanceSheet()
        {
            var surgeon = Load("Surgeon");
            Assert.That(surgeon, Is.Not.Null);
            Assert.That(surgeon.MaxHealth, Is.EqualTo(70f));
            Assert.That(surgeon.MaxStagger, Is.EqualTo(40f));
            Assert.That(surgeon.ConsumeHeal, Is.EqualTo(0.1f).Within(1e-4f));
            Assert.That(surgeon.Biomass, Is.EqualTo(5));
            Assert.That(surgeon.Attacks.Count, Is.GreaterThan(0));
            foreach (var attack in surgeon.Attacks)
            {
                Assert.That(attack.Kind, Is.EqualTo(AttackKind.Melee));
                Assert.That(attack.Telegraph, Is.EqualTo(Telegraph.Dodgeable));
                Assert.That(attack.Damage, Is.EqualTo(10f));
                Assert.That(attack.WindUpSound.IsNull, Is.False);
            }
        }

        [Test]
        public void HazmatThrowsARootTrap()
        {
            var hazmat = Load("Hazmat");
            Assert.That(hazmat.MaxHealth, Is.EqualTo(110f));
            Assert.That(hazmat.MaxStagger, Is.EqualTo(70f));
            var found = false;
            foreach (var attack in hazmat.Attacks)
            {
                if (attack.Kind != AttackKind.Trap)
                    continue;
                found = true;
                Assert.That(attack.TrapPrefab, Is.Not.Null);
                Assert.That(attack.Status, Is.EqualTo(StatusKind.Root));
                Assert.That(attack.StatusDuration, Is.EqualTo(1.5f));
                Assert.That(attack.Telegraph, Is.EqualTo(Telegraph.Dodgeable));
            }
            Assert.That(found, Is.True);
        }

        [TestCase(1.2f, 0f, true)]
        [TestCase(1.2f, 1.19f, true)]
        [TestCase(1.2f, 1.2f, true)]
        [TestCase(1.2f, 1.21f, false)]
        public void TrapCatchesExactlyInsideItsRadius(float radius, float distance, bool caught)
        {
            var center = new Vector3(3f, 0f, -2f);
            var position = center + new Vector3(distance * 0.6f, 1.5f, distance * 0.8f);
            Assert.That(ContainmentTrap.Contains(center, radius, position), Is.EqualTo(caught));
        }

        [Test]
        public void TurretIsAStationaryWallMachine()
        {
            var turret = Load("Turret");
            Assert.That(turret.MaxHealth, Is.EqualTo(120f));
            Assert.That(turret.MaxStagger, Is.EqualTo(80f));
            Assert.That(turret.MoveSpeed, Is.EqualTo(0f));
            Assert.That(turret.KnockbackScale, Is.EqualTo(0f));
            Assert.That(turret.SpawnOnWall, Is.True);
            Assert.That(turret.ConsumeHeal, Is.EqualTo(0.08f).Within(1e-4f));
            Assert.That(turret.Biomass, Is.EqualTo(15));
            Assert.That(turret.Attacks.Count, Is.EqualTo(1));
            Assert.That(turret.Attacks[0].Kind, Is.EqualTo(AttackKind.Projectile));
            Assert.That(turret.Attacks[0].Damage, Is.EqualTo(10f));
            Assert.That(turret.Attacks[0].Telegraph, Is.EqualTo(Telegraph.Dodgeable));
            Assert.That(turret.Machine, Is.True);
        }

        [Test]
        public void MedDroneIsAHealingMachineWithoutAttacks()
        {
            var drone = Load("MedDrone");
            Assert.That(drone.MaxHealth, Is.EqualTo(60f));
            Assert.That(drone.MaxStagger, Is.EqualTo(30f));
            Assert.That(drone.ConsumeHeal, Is.EqualTo(0.08f).Within(1e-4f));
            Assert.That(drone.Biomass, Is.EqualTo(15));
            Assert.That(drone.Machine, Is.True);
            Assert.That(drone.HealPerSecond, Is.EqualTo(8f));
            Assert.That(drone.HealRange, Is.EqualTo(6f));
            Assert.That(drone.Attacks.Count, Is.EqualTo(0));
            Assert.That(drone.SpawnOnWall, Is.False);
        }

        [Test]
        public void HumansAreHealable()
        {
            foreach (var name in new[] { "Scientist", "SecurityGuard", "Handler", "Surgeon", "LabSecurity", "Hazmat" })
                Assert.That(Load(name).Machine, Is.False, name);
        }
    }
}
