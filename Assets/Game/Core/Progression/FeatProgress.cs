using System.Collections.Generic;
using TentacleVania.Core.Saves;

namespace TentacleVania.Core.Progression
{
    public static class FeatProgress
    {
        public delegate void ProgressHandler(string titleKey, int count, int target);

        public static event ProgressHandler Progressed;

        private static List<string> Saved => SaveSystem.Data.Progress.Feats;

        public static int Count(string id) => Parse(Saved, id);

        public static int Parse(List<string> entries, string id)
        {
            foreach (var entry in entries)
                if (entry.Length > id.Length && entry[id.Length] == '=' && entry.StartsWith(id) && int.TryParse(entry.Substring(id.Length + 1), out var count))
                    return count;
            return 0;
        }

        public static void Store(List<string> entries, string id, int count)
        {
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.Length > id.Length && entry[id.Length] == '=' && entry.StartsWith(id))
                {
                    entries[i] = id + "=" + count;
                    return;
                }
            }
            entries.Add(id + "=" + count);
        }

        public static void SetCount(string id, int count) => Store(Saved, id, count);

        public static void Report(string titleKey, int count, int target) => Progressed?.Invoke(titleKey, count, target);

        public static void ResetAll() => Saved.Clear();
    }
}
