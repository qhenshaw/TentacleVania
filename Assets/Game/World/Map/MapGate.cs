using UnityEngine;

namespace TentacleVania.World.Map
{
    public sealed class MapGate : MonoBehaviour
    {
        [Tooltip("Optional id; the map shows a generic 'blocked' mark once the gate's room is explored.")]
        [SerializeField] private string _id;

        public string Id => string.IsNullOrEmpty(_id) ? name : _id;

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.7f);
            Gizmos.DrawWireCube(transform.position + Vector3.up, new Vector3(2f, 2f, 0.3f));
        }
    }
}
