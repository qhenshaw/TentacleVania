using System;
using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.Core.Saves
{
    public static class SaveSerializer
    {
        public const int CurrentVersion = 1;

        private static readonly Dictionary<int, Action<SaveData>> Migrations = new();

        [Serializable]
        private sealed class Header
        {
            public int Version;
        }

        public static void RegisterMigration(int fromVersion, Action<SaveData> migrate) => Migrations[fromVersion] = migrate;

        public static void UnregisterMigration(int fromVersion) => Migrations.Remove(fromVersion);

        public static string ToJson(SaveData data)
        {
            data.Version = CurrentVersion;
            return JsonUtility.ToJson(data, true);
        }

        public static SaveData FromJson(string json, int currentVersion = CurrentVersion)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new FormatException("Save file is empty");
            var header = JsonUtility.FromJson<Header>(json);
            if (header.Version > currentVersion)
                throw new FormatException($"Save version {header.Version} is newer than this build ({currentVersion})");
            var data = JsonUtility.FromJson<SaveData>(json);
            for (var version = header.Version; version < currentVersion; version++)
                if (Migrations.TryGetValue(version, out var migrate))
                    migrate(data);
            data.Version = currentVersion;
            return data;
        }
    }
}
