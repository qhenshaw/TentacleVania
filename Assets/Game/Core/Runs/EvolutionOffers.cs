using System;
using System.Collections.Generic;

namespace TentacleVania.Core.Runs
{
    public readonly struct EvolutionOffer
    {
        public EvolutionOffer(int slot, int option)
        {
            Slot = slot;
            Option = option;
        }

        public int Slot { get; }
        public int Option { get; }
    }

    public static class EvolutionOffers
    {
        public static int OpenSlots(IReadOnlyList<int> optionsPerSlot)
        {
            var open = 0;
            if (optionsPerSlot != null)
                foreach (var options in optionsPerSlot)
                    if (options > 0)
                        open++;
            return open;
        }

        public static List<EvolutionOffer> Pick(Random rng, IReadOnlyList<int> optionsPerSlot, List<EvolutionOffer> into = null)
        {
            into ??= new List<EvolutionOffer>(4);
            into.Clear();
            var open = OpenSlots(optionsPerSlot);
            if (open == 0)
                return into;
            if (open == 1 || rng.Next(2) == 0)
            {
                var chosen = rng.Next(open);
                for (var slot = 0; slot < optionsPerSlot.Count; slot++)
                {
                    if (optionsPerSlot[slot] <= 0)
                        continue;
                    if (chosen-- == 0)
                    {
                        for (var option = 0; option < optionsPerSlot[slot]; option++)
                            into.Add(new EvolutionOffer(slot, option));
                        break;
                    }
                }
                return into;
            }
            for (var slot = 0; slot < optionsPerSlot.Count; slot++)
                if (optionsPerSlot[slot] > 0)
                    into.Add(new EvolutionOffer(slot, rng.Next(optionsPerSlot[slot])));
            return into;
        }
    }
}
