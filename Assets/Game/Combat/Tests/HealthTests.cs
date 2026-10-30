using NUnit.Framework;
using TentacleVania.Combat.Damage;
using UnityEngine;

namespace TentacleVania.Combat.Tests
{
    public class HealthTests
    {
        private GameObject _go;
        private Health _health;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("HealthTest");
            _health = _go.AddComponent<Health>();
            _health.Configure(Team.Site, 40f);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_go);

        [Test]
        public void DiedFiresOnceOnTheKillingBlow()
        {
            var deaths = 0;
            _health.Died += _ => deaths++;
            _health.TakeDamage(DamageInfo.Create(30f, Team.Creature));
            _health.TakeDamage(DamageInfo.Create(30f, Team.Creature));
            _health.TakeDamage(DamageInfo.Create(30f, Team.Creature));
            Assert.AreEqual(1, deaths);
            Assert.IsFalse(_health.IsAlive);
        }

        [Test]
        public void HealIsClampedToMax()
        {
            _health.TakeDamage(DamageInfo.Create(10f, Team.Creature));
            Assert.AreEqual(10f, _health.Heal(25f), 1e-4f);
            Assert.AreEqual(40f, _health.Current, 1e-4f);
        }

        [Test]
        public void DeadTargetsCannotBeHealed()
        {
            _health.Kill();
            Assert.AreEqual(0f, _health.Heal(10f));
        }

        [Test]
        public void AttackerOutgoingModifiersApply()
        {
            var attackerGo = new GameObject("Attacker");
            try
            {
                var attacker = attackerGo.AddComponent<Health>();
                attacker.Configure(Team.Creature, 100f);
                attacker.Outgoing.Add(new ScaleModifier { HealthScale = 2f });
                var info = DamageInfo.Create(5f, Team.Creature);
                info.Attacker = attacker;
                Assert.AreEqual(10f, _health.TakeDamage(info).HealthDamage, 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(attackerGo);
            }
        }

        [Test]
        public void GlobalHitEventSeesAppliedHitsOnly()
        {
            var hits = 0;
            void OnHit(Health target, in DamageInfo info, in DamageResult result) => hits++;
            CombatEvents.HitLanded += OnHit;
            try
            {
                _health.TakeDamage(DamageInfo.Create(5f, Team.Creature));
                _health.TakeDamage(DamageInfo.Create(5f, Team.Site));
                Assert.AreEqual(1, hits);
            }
            finally
            {
                CombatEvents.HitLanded -= OnHit;
            }
        }
    }
}
