using TentacleVania.Combat;
using UnityEngine;

namespace TentacleVania.Audio
{
    public sealed class PaZone : MonoBehaviour
    {
        [Tooltip("PA script sequence played once when the creature first enters the box (sector entries).")]
        [SerializeField] private string _sequence = "sh.entry";
        [SerializeField] private Vector3 _size = new(6f, 4f, 6f);
        [SerializeField, Min(0f)] private float _delay = 1f;

        private float _insideSince = -1f;
        private bool _done;

        private void Update()
        {
            if (_done || PaDirector.Instance == null)
                return;
            var creature = CombatTargets.Creature;
            if (creature == null || !Contains(creature.transform.position))
            {
                _insideSince = -1f;
                return;
            }
            if (_insideSince < 0f)
                _insideSince = Time.time;
            if (Time.time - _insideSince < _delay)
                return;
            _done = true;
            PaDirector.Instance.Play(_sequence);
        }

        private bool Contains(Vector3 point)
        {
            var local = transform.InverseTransformPoint(point);
            var half = _size * 0.5f;
            return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y - half.y) <= half.y && Mathf.Abs(local.z) <= half.z;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.6f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(new Vector3(0f, _size.y * 0.5f, 0f), _size);
        }
    }
}
