using UnityEngine;

namespace TentacleVania.Bosses.Warden
{
    public sealed class WardenHall : MonoBehaviour
    {
        [Tooltip("Hall floor size: x along the long walls (beams sit at x offsets), y across them (beam length).")]
        [SerializeField] private Vector2 _size = new(24f, 18f);

        public Vector2 Size => _size;

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.1f, 0.5f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(_size.x, 0.1f, _size.y));
        }
    }
}
