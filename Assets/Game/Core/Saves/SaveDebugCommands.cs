using TentacleVania.Core.Debugging;
using UnityEngine;

namespace TentacleVania.Core.Saves
{
    public static class SaveDebugCommands
    {
        private static readonly string[] SlotNames = { "Slot 1", "Slot 2", "Slot 3", "Slot 4", "Slot 5" };
        private static int _slot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            DebugMenu.Choice(DebugMenu.Game, "Save slot", SlotNames, i => _slot = i);
            DebugMenu.Value(DebugMenu.Game, "Active save", () => SaveSystem.Active
                ? $"slot {SaveSystem.Slot + 1}, {SaveSystem.Data.PlaytimeSeconds / 60.0:F1} min, saved {SaveSystem.Data.SavedAtUtc}"
                : "none");
            DebugMenu.Toggle(DebugMenu.Game, "Autosave", () => Autosave.Enabled, v => Autosave.Enabled = v);
            DebugMenu.Value(DebugMenu.Game, "Autosaves", () => Autosave.Count == 0 ? "none" : $"{Autosave.Count}, last: {Autosave.LastReason}");
            DebugMenu.Button(DebugMenu.Game, "Save", () =>
            {
                if (!SaveSystem.Active || SaveSystem.Slot != _slot)
                    SaveSystem.NewGame(_slot);
                SaveSystem.Save();
            });
            DebugMenu.Button(DebugMenu.Game, "Load", () =>
            {
                if (!SaveSystem.Load(_slot))
                    Debug.LogWarning($"Save slot {_slot + 1} is empty");
            });
            DebugMenu.Button(DebugMenu.Game, "Wipe slot", () => SaveSystem.Delete(_slot));
            DebugMenu.Button(DebugMenu.Game, "Open save folder", () => Application.OpenURL("file:///" + SaveSystem.Storage.Location.Replace('\\', '/')));
        }
    }
}
