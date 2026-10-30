using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.Combat
{
    public sealed class Hurtbox : MonoBehaviour
    {
        private static readonly Dictionary<Collider, Hurtbox> Registry = new();

        [SerializeField] private Collider _collider;
        [SerializeField] private Health _health;

        public Collider Collider => _collider;
        public Health Health => _health;

        public static bool TryGet(Collider collider, out Hurtbox hurtbox) => Registry.TryGetValue(collider, out hurtbox);

        private void OnEnable()
        {
            if (_collider != null)
                Registry[_collider] = this;
        }

        private void OnDisable()
        {
            if (_collider != null && Registry.TryGetValue(_collider, out var current) && current == this)
                Registry.Remove(_collider);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Registry.Clear();
    }
}
