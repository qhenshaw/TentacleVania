using UnityEngine;

namespace TentacleVania.Camera
{
    public sealed class CameraBounds : MonoBehaviour
    {
        [SerializeField] private Vector2 _size = new(20f, 20f);

        public static CameraBounds Active { get; private set; }

        public Vector2 Size => _size;
        public float FloorHeight => transform.position.y;
        public Vector2 Min => new Vector2(transform.position.x, transform.position.z) - _size * 0.5f;
        public Vector2 Max => new Vector2(transform.position.x, transform.position.z) + _size * 0.5f;

        private void OnEnable() => Active = this;

        private void OnDisable()
        {
            if (Active == this)
                Active = null;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, new Vector3(_size.x, 0f, _size.y));
        }
    }
}
