using UnityEngine;

namespace TentacleVania.Bosses.Automaton
{
    public sealed class AutomatonTheatre : MonoBehaviour
    {
        [Tooltip("Round operating theatre centred on the table: wall radius and table radius.")]
        [SerializeField, Min(1f)] private float _radius = 11f;
        [SerializeField, Min(0f)] private float _tableRadius = 2.5f;

        public float Radius => _radius;
        public float TableRadius => _tableRadius;

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, _radius);
            Gizmos.DrawWireSphere(transform.position, _tableRadius);
        }
    }
}
