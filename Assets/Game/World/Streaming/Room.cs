using UnityEngine;

namespace TentacleVania.World.Streaming
{
    public sealed class Room : MonoBehaviour
    {
        [Tooltip("The room's CameraBounds; enabled again when the creature enters, so it becomes the active bounds.")]
        [SerializeField] private Behaviour _cameraBounds;

        private void OnEnable()
        {
            RoomStreamer.CurrentRoomChanged += Apply;
            Apply(RoomStreamer.CurrentScene);
        }

        private void OnDisable() => RoomStreamer.CurrentRoomChanged -= Apply;

        private void Apply(string scene)
        {
            if (_cameraBounds == null || RoomStreamer.Active == null)
                return;
            _cameraBounds.enabled = false;
            if (scene == gameObject.scene.name)
                _cameraBounds.enabled = true;
        }
    }
}
