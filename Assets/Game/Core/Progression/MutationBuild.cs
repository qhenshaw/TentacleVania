using System;

namespace TentacleVania.Core.Progression
{
    [Flags]
    public enum MutationBuild
    {
        None = 0,
        Stagger = 1 << 0,
        Bleed = 1 << 1,
        Parasites = 1 << 2,
        Thorns = 1 << 3,
        Healing = 1 << 4,
        Acid = 1 << 5,
        LowHealth = 1 << 6,
        Spines = 1 << 7,
        Fire = 1 << 8,
        AbilitySpam = 1 << 9,
        Consuming = 1 << 10,
        BioElectric = 1 << 11,
    }
}
