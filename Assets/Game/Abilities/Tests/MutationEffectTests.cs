using NUnit.Framework;
using TentacleVania.Abilities.Grapple;
using TentacleVania.Abilities.Mutations;
using TentacleVania.Creature;
using TentacleVania.Creature.Movement;
using UnityEngine;

namespace TentacleVania.Abilities.Tests
{
    public sealed class MutationEffectTests
    {
        private GameObject _creature;
        private MutationHost _host;

        [SetUp]
        public void SetUp()
        {
            _creature = new GameObject("MutationTestCreature");
            _creature.AddComponent<CharacterController>();
            var motor = _creature.AddComponent<CreatureMotor>();
            var consume = _creature.AddComponent<CreatureConsume>();
            var grapple = _creature.AddComponent<TentacleGrapple>();
            _host = new(motor, consume, grapple, _creature.AddComponent<TentacleVania.Combat.Health>());
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_creature);

        private static T Make<T>() where T : MutationDefinition => ScriptableObject.CreateInstance<T>();

        [Test]
        public void WideDodgeLengthensDashIFrames()
        {
            var effect = Make<WideDodgeMutation>().CreateEffect();
            effect.Enable(_host);
            Assert.That(_host.Motor.MinDashInvulnerability, Is.EqualTo(0.3f).Within(1e-4f));
            effect.Disable(_host);
            Assert.That(_host.Motor.MinDashInvulnerability, Is.EqualTo(0f));
        }

        [Test]
        public void RapidDigestionHalvesTheHealDuration()
        {
            var effect = Make<RapidDigestionMutation>().CreateEffect();
            effect.Enable(_host);
            Assert.That(_host.Consume.HealDurationScale, Is.EqualTo(0.5f).Within(1e-4f));
            effect.Disable(_host);
            Assert.That(_host.Consume.HealDurationScale, Is.EqualTo(1f));
        }

        [Test]
        public void LungingMawTurnsOnGrabOnArrival()
        {
            var effect = Make<LungingMawMutation>().CreateEffect();
            effect.Enable(_host);
            Assert.That(_host.Grapple.GrabOnArrival, Is.True);
            effect.Disable(_host);
            Assert.That(_host.Grapple.GrabOnArrival, Is.False);
        }

        [Test]
        public void GluttonyBoostExpiresAfterItsWindow()
        {
            var effect = Make<GluttonyMutation>().CreateEffect();
            effect.Enable(_host);
            _host.Consume.NextConsumeSpeed = 1.3f;
            _host.Consume.NextHealScale = 1.5f;
            effect.Disable(_host);
            Assert.That(_host.Consume.NextConsumeSpeed, Is.EqualTo(1f));
            Assert.That(_host.Consume.NextHealScale, Is.EqualTo(1f));
        }

        [Test]
        public void ShatterpointLengthensStunAndRaisesStunnedDamage()
        {
            var effect = Make<ShatterpointMutation>().CreateEffect();
            effect.Enable(_host);
            Assert.That(Combat.StaggerRules.ExtraStun, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(Combat.StaggerRules.WindowDamageScale, Is.EqualTo(1.3f).Within(1e-4f));
            effect.Disable(_host);
            Assert.That(Combat.StaggerRules.ExtraStun, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(Combat.StaggerRules.WindowDamageScale, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void MeltingPointSpeedsCorrodedStagger()
        {
            var effect = Make<MeltingPointMutation>().CreateEffect();
            effect.Enable(_host);
            Assert.That(Combat.StaggerRules.CorrodedScale, Is.EqualTo(1.5f).Within(1e-4f));
            effect.Disable(_host);
            Assert.That(Combat.StaggerRules.CorrodedScale, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void CrushingCoilsAddsAnOutgoingModifier()
        {
            var before = _host.Health.Outgoing.Count;
            var effect = Make<CrushingCoilsMutation>().CreateEffect();
            effect.Enable(_host);
            Assert.That(_host.Health.Outgoing.Count, Is.EqualTo(before + 1));
            effect.Disable(_host);
            Assert.That(_host.Health.Outgoing.Count, Is.EqualTo(before));
        }

        [Test]
        public void ThornsDamageTheAttackerAndJaggedRecoilAddsStagger()
        {
            var attacker = new GameObject("ThornsAttacker").AddComponent<Combat.Health>();
            try
            {
                _host.Health.Configure(Combat.Damage.Team.Creature, 100f);
                attacker.Configure(Combat.Damage.Team.Site, 100f);
                MutationStrikes.Thorns(_host.Health, attacker, 6f);
                Assert.That(attacker.Current, Is.EqualTo(94f).Within(1e-3f));
                var effect = Make<JaggedRecoilMutation>().CreateEffect();
                effect.Enable(_host);
                Assert.That(MutationStrikes.ThornsStaggerScale, Is.EqualTo(1.5f).Within(1e-4f));
                effect.Disable(_host);
                Assert.That(MutationStrikes.ThornsStaggerScale, Is.EqualTo(0f));
            }
            finally
            {
                Object.DestroyImmediate(attacker.gameObject);
            }
        }

        [Test]
        public void LongReachAndRavenousMawScaleTheConsume()
        {
            var reach = Make<LongReachMutation>().CreateEffect();
            var maw = Make<RavenousMawMutation>().CreateEffect();
            reach.Enable(_host);
            maw.Enable(_host);
            Assert.That(_host.Consume.ReachScale, Is.EqualTo(1.5f).Within(1e-4f));
            Assert.That(_host.Consume.HealScale, Is.EqualTo(1.25f).Within(1e-4f));
            Assert.That(_host.Consume.HealScale * _host.Consume.LowHealthHealScale, Is.EqualTo(1.5f).Within(1e-4f));
            reach.Disable(_host);
            maw.Disable(_host);
            Assert.That(_host.Consume.ReachScale, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(_host.Consume.HealScale, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(_host.Consume.LowHealthHealScale, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void OpenWoundsRaisesTheBleedCapAndRate()
        {
            var effect = Make<OpenWoundsMutation>().CreateEffect();
            effect.Enable(_host);
            Assert.That(Combat.Status.StatusEffects.BleedExtraStacks, Is.EqualTo(2));
            Assert.That(Combat.Status.StatusEffects.BleedRateScale, Is.EqualTo(1.3f).Within(1e-4f));
            effect.Disable(_host);
            Assert.That(Combat.Status.StatusEffects.BleedExtraStacks, Is.EqualTo(0));
            Assert.That(Combat.Status.StatusEffects.BleedRateScale, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void GrabRulesRaiseTheThresholdForEliteAndLowHealth()
        {
            var prey = new GameObject("GrabPrey");
            try
            {
                var health = prey.AddComponent<Combat.Health>();
                health.Configure(Combat.Damage.Team.Site, 100f);
                var consumable = prey.AddComponent<Combat.Consumable>();
                Assert.That(consumable.EffectiveThreshold(0.25f), Is.EqualTo(0.25f).Within(1e-4f));
                var grasp = Make<HungryGraspMutation>();
                var effect = grasp.CreateEffect();
                effect.Enable(_host);
                consumable.Elite = true;
                Assert.That(consumable.EffectiveThreshold(0.25f), Is.EqualTo(grasp.ThresholdAt(Core.Progression.Stats.SizeStage)).Within(1e-4f));
                effect.Disable(_host);
                consumable.Elite = false;
                _host.Health.Configure(Combat.Damage.Team.Creature, 100f);
                _host.Health.SetFraction(0.2f);
                var hunger = Make<DesperateHungerMutation>().CreateEffect();
                hunger.Enable(_host);
                hunger.Tick(_host, 0f);
                Assert.That(consumable.EffectiveThreshold(0.25f), Is.EqualTo(0.35f).Within(1e-4f));
                hunger.Disable(_host);
                Assert.That(Combat.Consumable.ThresholdBonus, Is.EqualTo(0f).Within(1e-4f));
            }
            finally
            {
                Object.DestroyImmediate(prey);
            }
        }
    }
}
