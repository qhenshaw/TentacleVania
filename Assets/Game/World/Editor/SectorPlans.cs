using System;
using System.Collections.Generic;
using System.Linq;

namespace TentacleVania.World.Editor
{
    public static class SectorPlans
    {
        private static readonly Func<SectorPlan>[] Factories =
        {
            SpecimenHoldingPlan.Create,
            BioLabsPlan.Create,
        };

        public static IEnumerable<SectorPlan> All => Factories.Select(create => create());

        public static SectorPlan For(string sector) => All.FirstOrDefault(plan => plan.Sector == sector);
    }
}
