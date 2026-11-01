using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TentacleVania.Core.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TentacleVania.Core.Saves
{
    public static class SaveSystem
    {
        public const int SlotCount = 5;

        private const string BootstrapScene = SceneFlow.Bootstrap;

        private static readonly List<ISaveParticipant> Participants = new(16);
        private static readonly object WriteLock = new();
        private static Task _pendingWrite = Task.CompletedTask;
        private static ISaveStorage _storage;
        private static double _playtimeAnchor;

        public static event Action<SaveData> Saved;
        public static event Action<SaveData> Loaded;

        public static int Slot { get; private set; }
        public static SaveData Data { get; private set; } = new();
        public static bool Active { get; private set; }
        public static double LivePlaytime => Data.PlaytimeSeconds + (Time.realtimeSinceStartupAsDouble - _playtimeAnchor);

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
            var json = Capture();
            Flush();
            Storage.Write(Slot, json);
            Saved?.Invoke(Data);
        }

        public static void SaveAsync()
        {
            var json = Capture();
            var slot = Slot;
            var storage = Storage;
            lock (WriteLock)
                _pendingWrite = _pendingWrite.ContinueWith(_ => Write(storage, slot, json), TaskScheduler.Default);
            Saved?.Invoke(Data);
        }

        public static void Flush()
        {
            Task pending;
            lock (WriteLock)
                pending = _pendingWrite;
            pending.Wait();
        }

        private static string Capture()
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
            if (!string.IsNullOrEmpty(scene) && scene != BootstrapScene && scene != SceneFlow.MainMenu)
                Data.Progress.Scene = scene;
            return SaveSerializer.ToJson(Data);
        }

        private static void Write(ISaveStorage storage, int slot, string json)
        {
            try
            {
                storage.Write(slot, json);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        public static bool Load(int slot)
        {
            slot = ClampSlot(slot);
            Flush();
            if (!Storage.TryRead(slot, out var json))
                return false;
            Data = SaveSerializer.FromJson(json);
            ApplyMidArenaQuit(Data);
            Slot = slot;
            Active = true;
            _playtimeAnchor = Time.realtimeSinceStartupAsDouble;
            foreach (var participant in Participants)
                participant.Restore(Data);
            Loaded?.Invoke(Data);
            return true;
        }

        public static bool ApplyMidArenaQuit(SaveData data)
        {
            var progress = data.Progress;
            if (string.IsNullOrEmpty(progress.ArenaInProgress))
                return false;
            Progression.BiomassRules.Die(data.Biomass, GameSettings.KeepBiomassOnDeath, progress.ArenaScene, progress.ArenaPosition);
            progress.ArenaInProgress = "";
            return true;
        }

        public static void Delete(int slot)
        {
            slot = ClampSlot(slot);
            Flush();
            Storage.Delete(slot);
            if (slot == Slot)
                Active = false;
        }

        public static bool Exists(int slot)
        {
            Flush();
            return Storage.Exists(ClampSlot(slot));
        }

        public static string ContinueScene(string fallback)
        {
            var progress = Data.Progress;
            if (!string.IsNullOrEmpty(progress.LastNestScene))
                return progress.LastNestScene;
            return string.IsNullOrEmpty(progress.Scene) ? fallback : progress.Scene;
        }

        public static SaveSummary Summary(int slot)
        {
            slot = ClampSlot(slot);
            Flush();
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
