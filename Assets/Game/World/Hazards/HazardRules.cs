using System.Collections.Generic;
using TentacleVania.Combat.Damage;
using TentacleVania.Core.Progression;
using UnityEngine;

namespace TentacleVania.World.Hazards
{
    public enum HazardKind
    {
        Electric,
        Fire,
        Acid,
        Radiation,
        Cryo,
    }

    public static class HazardRules
    {
        private static readonly HashSet<string> Forced = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Forced.Clear();

        public static string ResistanceFor(HazardKind kind) => kind switch
        {
            HazardKind.Electric => "ElectricResistance",
            HazardKind.Fire => "FireResistance",
            HazardKind.Acid => "AcidResistance",
            HazardKind.Radiation => "RadiationResistance",
            _ => null,
        };

        public static DamageType DamageTypeFor(HazardKind kind) => kind switch
        {
            HazardKind.Electric => DamageType.Electric,
            HazardKind.Fire => DamageType.Fire,
            HazardKind.Acid => DamageType.Acid,
            HazardKind.Radiation => DamageType.Radiation,
            _ => DamageType.Physical,
        };

        public static bool Damages(HazardKind kind) => kind != HazardKind.Cryo;

        public static bool Inside(Vector3 local, Vector3 size) =>
            Mathf.Abs(local.x) <= size.x * 0.5f && local.y >= -0.5f && local.y <= size.y && Mathf.Abs(local.z) <= size.z * 0.5f;

        public static bool IsResisted(HazardKind kind)
        {
            var resistance = ResistanceFor(kind);
            return resistance != null && (Forced.Contains(resistance) || BossProgress.IsAbilityUnlocked(resistance));
        }

        public static bool IsForced(string resistance) => Forced.Contains(resistance);

        public static void SetForced(string resistance, bool on)
        {
            if (on)
                Forced.Add(resistance);
            else
                Forced.Remove(resistance);
        }
    }
}
