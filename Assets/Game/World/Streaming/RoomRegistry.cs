using System;
using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.World.Streaming
{
    [Serializable]
    public sealed class RoomEntry
    {
        [Tooltip("Room scene name, also its Addressables address.")]
        public string Scene;
        public Vector3 Center;
        public Vector3 Size;
        public List<string> Neighbours = new();
    }

    public enum MapMarkerKind
    {
        Nest,
        Relay,
        SecurityDoor,
        Gate,
        Hoarder,
    }

    [Serializable]
    public sealed class MapMarker
    {
        public MapMarkerKind Kind;
        [Tooltip("Save id: nest id, or <system>/<relay> for relays.")]
        public string Id;
        [Tooltip("Room scene the marker is in.")]
        public string Room;
        public Vector3 Position;
        public float Yaw;
    }

    [CreateAssetMenu(menuName = "TentacleVania/World/Room Registry")]
    public sealed class RoomRegistry : ScriptableObject
    {
        [Tooltip("Rooms whose bounds come this close to the creature load.")]
        [SerializeField, Min(0f)] private float _loadRadius = 18f;
        [Tooltip("Loaded rooms unload once they are farther than this (keeps rooms at the edge from reloading).")]
        [SerializeField, Min(0f)] private float _unloadRadius = 28f;
        [SerializeField] private List<RoomEntry> _rooms = new();

        [Header("Map")]
        [Tooltip("Sector id (SH, BL...): the Dialogue table's sector.<id> names it.")]
        [SerializeField] private string _sector = "SH";
        [Tooltip("Root scene of the sector (fast travel loads it).")]
        [SerializeField] private string _rootScene;
        [SerializeField] private Color _color = new(0.75f, 0.25f, 0.45f, 1f);
        [Tooltip("Baked by RoomRegistryTools.Update from the room scenes.")]
        [SerializeField] private List<MapMarker> _markers = new();

        public float LoadRadius => _loadRadius;
        public float UnloadRadius => Mathf.Max(_unloadRadius, _loadRadius);
        public List<RoomEntry> Rooms => _rooms;
        public string Sector => _sector;
        public string RootScene => _rootScene;
        public Color Color => _color;
        public List<MapMarker> Markers => _markers;

        public MapMarker FindMarker(MapMarkerKind kind, string id)
        {
            foreach (var marker in _markers)
                if (marker.Kind == kind && marker.Id == id)
                    return marker;
            return null;
        }

        public int IndexOf(string scene)
        {
            for (var i = 0; i < _rooms.Count; i++)
                if (_rooms[i].Scene == scene)
                    return i;
            return -1;
        }
    }
}
