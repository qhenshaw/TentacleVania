using NUnit.Framework;
using TentacleVania.Enemies.Data;
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
            Assert.That(surgeon.MaxHealth, Is.EqualTo(35f));
            Assert.That(surgeon.MaxStagger, Is.EqualTo(20f));
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
    }
}
