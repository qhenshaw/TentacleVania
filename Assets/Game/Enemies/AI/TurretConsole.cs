using System.Collections.Generic;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using UnityEngine;

namespace TentacleVania.Enemies.AI
{
    public sealed class TurretConsole : MonoBehaviour
    {
        private static readonly List<TurretConsole> Consoles = new();

        [SerializeField] private Health _health;
        [SerializeField, Min(1f)] private float _maxHealth = 60f;
        [SerializeField] private Renderer _screen;
        [SerializeField] private Material _deadMaterial;

        private bool _overloaded;

        public static IReadOnlyList<TurretConsole> All => Consoles;
        public bool Online => !_overloaded && _health != null && _health.IsAlive;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Consoles.Clear();

        public static bool Powers(Vector3 turret, float range)
        {
            var range2 = range * range;
            var linked = false;
            for (var i = 0; i < Consoles.Count; i++)
            {
                var offset = Consoles[i].transform.position - turret;
                offset.y = 0f;
                if (offset.sqrMagnitude > range2)
                    continue;
                if (Consoles[i].Online)
                    return true;
                linked = true;
            }
            return !linked;
        }

        public void Overload()
        {
            _overloaded = true;
            Shutdown();
        }

        private void Awake()
        {
            if (_health != null)
                _health.Configure(Team.Site, _maxHealth);
        }

        private void OnEnable()
        {
            Consoles.Add(this);
            if (_health != null)
                _health.Died += OnDied;
        }

        private void OnDisable()
        {
            Consoles.Remove(this);
            if (_health != null)
                _health.Died -= OnDied;
        }

        private void OnDied(Health health) => Shutdown();

        private void Shutdown()
        {
            if (_screen != null && _deadMaterial != null)
                _screen.sharedMaterial = _deadMaterial;
        }
    }
}
