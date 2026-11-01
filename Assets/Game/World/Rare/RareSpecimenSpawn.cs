using TentacleVania.Core.Progression;
using TentacleVania.Enemies.Types;
using UnityEngine;
using UnityEngine.AI;

namespace TentacleVania.World.Rare
{
    public sealed class RareSpecimenSpawn : MonoBehaviour
    {
        [SerializeField] private RareSpecimen _prefab;
        [SerializeField] private string _saveId;
        [SerializeField] private PickupKind _kind = PickupKind.Mutation;
        [SerializeField] private string _itemId;

        private RareSpecimen _spawned;

        public RareSpecimen Spawned => _spawned;
        public string SaveId => _saveId;

        private void Start()
        {
            if (_prefab == null || string.IsNullOrEmpty(_saveId) || Collectibles.IsCollected(_saveId))
                return;
            var position = transform.position;
            if (NavMesh.SamplePosition(position, out var hit, 2f, NavMesh.AllAreas))
                position = hit.position;
            _spawned = Instantiate(_prefab, position, transform.rotation);
            _spawned.Configure(_saveId, _kind, _itemId);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(_spawned.gameObject, gameObject.scene);
        }

        private void OnDestroy()
        {
            if (_spawned != null)
                Destroy(_spawned.gameObject);
        }
    }
}
