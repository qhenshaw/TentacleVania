using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TentacleVania.Core.Saves
{
    public static class SaveSystem
    {
        public const int SlotCount = 5;

        private static readonly List<ISaveParticipant> Participants = new(16);
        private static ISaveStorage _storage;
        private static double _playtimeAnchor;

        public static event Action<SaveData> Saved;
        public static event Action<SaveData> Loaded;

        public static int Slot { get; private set; }
        public static SaveData Data { get; private set; } = new();
        public static bool Active { get; private set; }

        public static ISaveStorage Storage
        {
            get => _storage ??= new LocalFileStorage(Path.Combine(Application.persistentDataPath, "Saves"));
            set => _storage = value;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Participants.Clear();
            Data = new();
            Slot = 0;
            Active = false;
            Saved = null;
            Loaded = null;
        }

        public static void Register(ISaveParticipant participant)
        {
            if (participant == null || Participants.Contains(participant))
                return;
            Participants.Add(participant);
            if (Active)
                participant.Restore(Data);
        }

        public static void Unregister(ISaveParticipant participant) => Participants.Remove(participant);

        public static void NewGame(int slot)
        {
            Slot = ClampSlot(slot);
            Data = new();
            Active = true;
            _playtimeAnchor = Time.realtimeSinceStartupAsDouble;
        }

        public static void Save()
        {
            if (!Active)
                NewGame(Slot);
            foreach (var participant in Participants)
                participant.Capture(Data);
            var now = Time.realtimeSinceStartupAsDouble;
            Data.PlaytimeSeconds += now - _playtimeAnchor;
            _playtimeAnchor = now;
            Data.SavedAtUtc = DateTime.UtcNow.ToString("o");
            var scene = SceneManager.GetActiveScene().name;
            if (!string.IsNullOrEmpty(scene))
                Data.Progress.Scene = scene;
            Storage.Write(Slot, SaveSerializer.ToJson(Data));
            Saved?.Invoke(Data);
        }

        public static bool Load(int slot)
        {
            slot = ClampSlot(slot);
            if (!Storage.TryRead(slot, out var json))
                return false;
            Data = SaveSerializer.FromJson(json);
            Slot = slot;
            Active = true;
            _playtimeAnchor = Time.realtimeSinceStartupAsDouble;
            foreach (var participant in Participants)
                participant.Restore(Data);
            Loaded?.Invoke(Data);
            return true;
        }

        public static void Delete(int slot)
        {
            slot = ClampSlot(slot);
            Storage.Delete(slot);
            if (slot == Slot)
                Active = false;
        }

        public static SaveSummary Summary(int slot)
        {
            slot = ClampSlot(slot);
            var summary = new SaveSummary { Slot = slot };
            if (!Storage.TryRead(slot, out var json))
                return summary;
            try
            {
                var data = SaveSerializer.FromJson(json);
                summary.Exists = true;
                summary.Sector = data.Progress.Sector;
                summary.Level = data.Build.Level;
                summary.PlaytimeSeconds = data.PlaytimeSeconds;
                summary.SavedAtUtc = data.SavedAtUtc;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Save slot {slot + 1} could not be read: {exception.Message}");
            }
            return summary;
        }

        private static int ClampSlot(int slot) => Mathf.Clamp(slot, 0, SlotCount - 1);
    }
}
