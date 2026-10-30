using TentacleVania.Combat.Damage;
using UnityEngine;

namespace TentacleVania.Abilities.BioShield
{
    public sealed class ShieldAbsorb : IDamageModifier
    {
        public const int ModifierOrder = 900;

        public int Order => ModifierOrder;
        public float Capacity { get; private set; }
        public float Remaining { get; private set; }
        public float AbsorbedThisFrame { get; private set; }
        public bool IsDepleted => Remaining <= 0f;

        public void Fill(float capacity)
        {
            Capacity = Mathf.Max(0f, capacity);
            Remaining = Capacity;
            AbsorbedThisFrame = 0f;
        }

        public void ClearFrame() => AbsorbedThisFrame = 0f;

        public float Absorb(float incoming)
        {
            if (incoming <= 0f || Remaining <= 0f)
                return incoming;
            var absorbed = Mathf.Min(Remaining, incoming);
            Remaining -= absorbed;
            AbsorbedThisFrame += absorbed;
            return incoming - absorbed;
        }

        public void Modify(ref DamageContext context)
        {
            var incoming = context.Info.Amount * context.HealthScale;
            if (incoming <= 0f)
                return;
            var passed = Absorb(incoming);
            context.HealthScale *= passed / incoming;
        }
    }
}
