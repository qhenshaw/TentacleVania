using TentacleVania.Core;
using TentacleVania.Core.Progression;
using UnityEngine;

namespace TentacleVania.Creature
{
    public sealed class CreatureSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject _prefab;

        public static GameObject Spawned { get; private set; }

        private void Start()
        {
            var position = transform.position;
            var rotation = transform.rotation;
            if (Nests.TryGetRespawn(gameObject.scene.name, out var nestPosition, out var yaw))
            {
                position = nestPosition;
                rotation = Quaternion.Euler(0f, yaw, 0f);
            }
            Spawned = Instantiate(_prefab, position, rotation);
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
