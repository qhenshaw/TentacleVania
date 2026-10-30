using TentacleVania.Core;
using UnityEngine;

namespace TentacleVania.Creature
{
    public sealed class CreatureSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject _prefab;

        public static GameObject Spawned { get; private set; }

        private void Start()
        {
            Spawned = Instantiate(_prefab, transform.position, transform.rotation);
            CameraFocus.Target = Spawned.transform;
        }

        private void OnDestroy()
        {
            if (Spawned != null)
                Destroy(Spawned);
            Spawned = null;
        }
    }
}
