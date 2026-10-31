using UnityEngine;

namespace TentacleVania.World.Pickups
{
    [CreateAssetMenu(menuName = "TentacleVania/World/Debug Pickup Set")]
    public sealed class PickupDebugSet : ScriptableObject
    {
        [SerializeField] private GameObject[] _pickups = System.Array.Empty<GameObject>();
        [SerializeField] private GameObject _loreTerminal;
        [SerializeField] private GameObject _hoarder;

        public GameObject[] Pickups => _pickups;
        public GameObject LoreTerminal => _loreTerminal;
        public GameObject Hoarder => _hoarder;
    }
}
