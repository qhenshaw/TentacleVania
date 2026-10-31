using System;
using System.Collections.Generic;

namespace TentacleVania.Core.Saves
{
    public static class SaveSlots
    {
        public static SaveSummary[] All()
        {
            var summaries = new SaveSummary[SaveSystem.SlotCount];
            for (var i = 0; i < summaries.Length; i++)
                summaries[i] = SaveSystem.Summary(i);
            return summaries;
        }

        public static int MostRecent(IReadOnlyList<SaveSummary> summaries)
        {
            var best = -1;
            for (var i = 0; i < summaries.Count; i++)
            {
                var summary = summaries[i];
                if (summary == null || !summary.Exists)
                    continue;
                if (best < 0 || string.CompareOrdinal(summary.SavedAtUtc, summaries[best].SavedAtUtc) > 0)
                    best = i;
            }
            return best < 0 ? -1 : summaries[best].Slot;
        }

        public static int FirstEmpty(IReadOnlyList<SaveSummary> summaries)
        {
            for (var i = 0; i < summaries.Count; i++)
                if (summaries[i] != null && !summaries[i].Exists)
                    return summaries[i].Slot;
            return -1;
        }

        public static string Playtime(double seconds)
        {
            var time = TimeSpan.FromSeconds(Math.Max(0d, seconds));
            var hours = (int)time.TotalHours;
            return hours > 0 ? $"{hours}:{time.Minutes:00}:{time.Seconds:00}" : $"{time.Minutes}:{time.Seconds:00}";
        }
    }
}
