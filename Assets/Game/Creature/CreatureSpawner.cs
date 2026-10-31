using TentacleVania.Core;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Scenes;
using UnityEngine;

namespace TentacleVania.Creature
{
    public sealed class CreatureSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject _prefab;

        public static GameObject Spawned { get; private set; }

        private bool _done;

        private void Start() => TrySpawn();

        private void Update()
        {
            if (!_done)
                TrySpawn();
        }

        private void TrySpawn()
        {
            if (!LoadGate.IsOpen)
                return;
            _done = true;
            enabled = false;
            var position = transform.position;
            var rotation = transform.rotation;
            if (SpawnOverride.TryGet(gameObject.scene.name, out var overridePosition, out var overrideYaw))
            {
                position = overridePosition;
                rotation = Quaternion.Euler(0f, overrideYaw, 0f);
                SpawnOverride.Clear();
            }
            else if (Nests.TryGetRespawn(gameObject.scene.name, out var nestPosition, out var yaw))
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
