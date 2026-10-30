using NUnit.Framework;
using TentacleVania.Weapons.Combo;

namespace TentacleVania.Weapons.Tests
{
    public class ComboTests
    {
        private const float Reset = 0.6f;

        private static ComboCounter Counter() => new(3, Reset);

        [Test]
        public void StepsAdvanceAndWrapAfterTheFinisher()
        {
            var combo = Counter();
            var t = 0f;
            for (var expected = 0; expected < 4; expected++)
            {
                Assert.AreEqual(expected % 3, combo.Advance(t));
                t += 0.3f;
                combo.AttackEnded(t);
            }
        }

        [Test]
        public void CrossoverSharesOneCounter()
        {
            var combo = Counter();
            Assert.AreEqual(0, combo.Advance(0f));
            combo.AttackEnded(0.3f);
            Assert.AreEqual(1, combo.Advance(0.4f));
            combo.AttackEnded(0.7f);
            Assert.IsTrue(combo.IsFinisher(combo.Advance(0.8f)), "W1, W1, W2 ends on W2's finisher");
        }

        [Test]
        public void PauseLongerThanTheResetDelayRestartsAtStepOne()
        {
            var combo = Counter();
            combo.Advance(0f);
            combo.AttackEnded(0.3f);
            Assert.AreEqual(0, combo.Advance(0.3f + Reset + 0.01f));
        }

        [Test]
        public void HeavyResetsTheCombo()
        {
            var combo = Counter();
            combo.Advance(0f);
            combo.AttackEnded(0.3f);
            combo.Reset();
            Assert.AreEqual(0, combo.Advance(0.35f));
        }

        [Test]
        public void TapUnderTheThresholdIsALightAttack()
        {
            var button = new ChargeButton(0.35f, 1.5f);
            button.Update(true, true, 0f);
            Assert.AreEqual(ChargeEvent.None, button.Update(false, true, 0.1f));
            Assert.AreEqual(ChargeEvent.Tap, button.Update(false, false, 0.2f));
        }

        [Test]
        public void HoldPastTheThresholdFiresTheHeavyOnRelease()
        {
            var button = new ChargeButton(0.35f, 1.5f);
            button.Update(true, true, 0f);
            Assert.AreEqual(ChargeEvent.None, button.Update(false, true, 0.4f));
            Assert.IsTrue(button.IsCharging(0.4f));
            Assert.AreEqual(ChargeEvent.Heavy, button.Update(false, false, 0.6f));
        }

        [Test]
        public void HoldingPastTheMaximumFiresTheHeavyAutomatically()
        {
            var button = new ChargeButton(0.35f, 1.5f);
            button.Update(true, true, 0f);
            Assert.AreEqual(ChargeEvent.Heavy, button.Update(false, true, 1.5f));
            Assert.IsFalse(button.IsDown);
        }

        [Test]
        public void SoftLockPrefersCloseTargetsNearTheFacing()
        {
            Assert.Less(SoftLock.Score(3f, 0f), SoftLock.Score(3f, 40f));
            Assert.Less(SoftLock.Score(2f, 30f), SoftLock.Score(5f, 0f));
            Assert.IsTrue(SoftLock.InCone(UnityEngine.Vector3.forward, new UnityEngine.Vector3(0.6f, 0f, 1f), 90f));
            Assert.IsFalse(SoftLock.InCone(UnityEngine.Vector3.forward, new UnityEngine.Vector3(1f, 0f, 0.2f), 90f));
        }
    }
}
