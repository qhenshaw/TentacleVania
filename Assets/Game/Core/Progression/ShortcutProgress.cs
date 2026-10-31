using System;
using TentacleVania.Core.Saves;
using UnityEngine;

namespace TentacleVania.Core.Progression
{
    public static class ShortcutProgress
    {
        public static event Action<string> Opened;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Opened = null;

        public static bool IsOpen(string id) => SaveSystem.Data.World.Has(WorldFlag.OpenedShortcut, id);

        public static bool Open(string id)
        {
            if (!SaveSystem.Data.World.Set(WorldFlag.OpenedShortcut, id))
                return false;
            Opened?.Invoke(id);
            Autosave.Request(AutosaveReason.ShortcutOpened);
            return true;
        }

        public static void CloseAll() => SaveSystem.Data.World.OpenedShortcuts.Clear();

        public static bool IsFarSide(Vector3 shortcut, Vector3 farDirection, Vector3 point)
        {
            var offset = point - shortcut;
            return offset.x * farDirection.x + offset.z * farDirection.z > 0f;
        }
    }
}
