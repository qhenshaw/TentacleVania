using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TentacleVania.Core.Platform
{
    public sealed class LocalAchievementBackend : IAchievementBackend
    {
        [Serializable]
        private sealed class Store
        {
            public List<string> Unlocked = new();
        }

        private readonly string _path;
        private Store _store;

        public LocalAchievementBackend(string path) => _path = path;

        private Store Data
        {
            get
            {
                if (_store != null)
                    return _store;
                _store = new Store();
                try
                {
                    if (File.Exists(_path))
                        JsonUtility.FromJsonOverwrite(File.ReadAllText(_path), _store);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"Achievements file unreadable, starting fresh: {exception.Message}");
                }
                return _store;
            }
        }

        public bool IsUnlocked(string id) => Data.Unlocked.Contains(id);

        public void Unlock(string id)
        {
            if (string.IsNullOrEmpty(id) || Data.Unlocked.Contains(id))
                return;
            Data.Unlocked.Add(id);
            Save();
            Debug.Log($"Achievement unlocked (local): {id}");
        }

        public void ResetAll()
        {
            Data.Unlocked.Clear();
            Save();
        }

        private void Save()
        {
            try
            {
                var folder = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(folder))
                    Directory.CreateDirectory(folder);
                File.WriteAllText(_path, JsonUtility.ToJson(_store, true));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Achievements file not saved: {exception.Message}");
            }
        }
    }
}
