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

    [CreateAssetMenu(menuName = "TentacleVania/World/Room Registry")]
    public sealed class RoomRegistry : ScriptableObject
    {
        [Tooltip("Rooms whose bounds come this close to the creature load.")]
        [SerializeField, Min(0f)] private float _loadRadius = 18f;
        [Tooltip("Loaded rooms unload once they are farther than this (keeps rooms at the edge from reloading).")]
        [SerializeField, Min(0f)] private float _unloadRadius = 28f;
        [SerializeField] private List<RoomEntry> _rooms = new();

        public float LoadRadius => _loadRadius;
        public float UnloadRadius => Mathf.Max(_unloadRadius, _loadRadius);
        public List<RoomEntry> Rooms => _rooms;

        public int IndexOf(string scene)
        {
            for (var i = 0; i < _rooms.Count; i++)
                if (_rooms[i].Scene == scene)
                    return i;
            return -1;
        }
    }
}
