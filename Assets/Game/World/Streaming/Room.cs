using TentacleVania.Core.Saves;
using UnityEngine;

namespace TentacleVania.World.Streaming
{
    public sealed class Room : MonoBehaviour
    {
        [Tooltip("The room's CameraBounds; enabled again when the creature enters, so it becomes the active bounds.")]
        [SerializeField] private Behaviour _cameraBounds;
        [Tooltip("Sector id (SH, BL...); saved as the current sector when the creature enters, for the save slot list.")]
        [SerializeField] private string _sector;

        private void OnEnable()
        {
            RoomStreamer.CurrentRoomChanged += Apply;
            Apply(RoomStreamer.CurrentScene);
        }

        private void OnDisable() => RoomStreamer.CurrentRoomChanged -= Apply;

        private void Apply(string scene)
        {
            var current = RoomStreamer.Active == null || scene == gameObject.scene.name;
            if (current && !string.IsNullOrEmpty(_sector))
                SaveSystem.Data.Progress.Sector = _sector;
            if (_cameraBounds == null || RoomStreamer.Active == null)
                return;
            _cameraBounds.enabled = current;
        }
    }
}
