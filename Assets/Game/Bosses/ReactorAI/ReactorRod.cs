using System;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using UnityEngine;

namespace TentacleVania.Bosses.ReactorAI
{
    public sealed class ReactorRod : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [SerializeField] private Collider _collider;
        [SerializeField, Min(1f)] private float _maxHealth = 150f;
        [SerializeField] private Transform _rod;

        public event Action<ReactorRod> Pulled;

        public bool Inserted { get; private set; } = true;

        private void Awake()
        {
            if (_health != null)
                _health.Configure(Team.Site, _maxHealth);
        }

        private void OnEnable()
        {
            if (_health != null)
                _health.Died += OnDied;
        }

        private void OnDisable()
        {
            if (_health != null)
                _health.Died -= OnDied;
        }

        private void OnDied(Health health)
        {
            Inserted = false;
            Show(false);
            Pulled?.Invoke(this);
        }

        public void Restore()
        {
            Inserted = true;
            if (_health != null)
                _health.Configure(Team.Site, _maxHealth);
            Show(true);
        }

        private void Show(bool inserted)
        {
            if (_rod != null)
                _rod.gameObject.SetActive(inserted);
            if (_collider != null)
                _collider.enabled = inserted;
        }
    }
}
