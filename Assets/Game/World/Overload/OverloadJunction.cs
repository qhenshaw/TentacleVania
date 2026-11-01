using FMODUnity;
using TentacleVania.Core.Interaction;
using TentacleVania.Enemies;
using UnityEngine;
using UnityEngine.VFX;

namespace TentacleVania.World.Overload
{
    public sealed class OverloadJunction : MonoBehaviour, ITraversal
    {
        [SerializeField, Min(0.1f)] private float _radius = 1.6f;
        [Tooltip("Machines (EnemyDefinition.Machine) within this distance of the junction are stunned.")]
        [SerializeField, Min(0f)] private float _shutdownRadius = 12f;
        [SerializeField, Min(0f)] private float _shutdownTime = 6f;
        [SerializeField, Min(0f)] private float _cooldown = 10f;
        [SerializeField] private Renderer _lamp;
        [SerializeField] private Material _readyLight;
        [SerializeField] private Material _spentLight;
        [SerializeField] private VisualEffect _surge;
        [SerializeField] private EventReference _surgeSound;

        private float _readyAt;
        private bool _spentShown;

        public static int Shutdowns { get; private set; }
        public string AbilityId => OverloadDoor.Ability;
        public Vector3 Position => transform.position;
        public float Radius => _radius;
        public bool CanTraverse => isActiveAndEnabled && Time.time >= _readyAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Shutdowns = 0;

        private void OnEnable()
        {
            Traversals.Register(this);
            _readyAt = 0f;
            SetLamp(false);
        }

        private void OnDisable() => Traversals.Unregister(this);

        private void Update()
        {
            if (_spentShown && Time.time >= _readyAt)
                SetLamp(false);
        }

        public void Traverse()
        {
            if (!CanTraverse)
                return;
            _readyAt = Time.time + _cooldown;
            SetLamp(true);
            if (_surge != null)
            {
                _surge.Reinit();
                _surge.Play();
            }
            if (!_surgeSound.IsNull)
                RuntimeManager.PlayOneShot(_surgeSound, transform.position);
            var manager = EnemyManager.Current;
            if (manager == null)
                return;
            var radiusSq = _shutdownRadius * _shutdownRadius;
            var enemies = manager.Enemies;
            for (var i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || enemy.Definition == null || !enemy.Definition.Machine || !enemy.Health.IsAlive)
                    continue;
                var offset = enemy.transform.position - transform.position;
                offset.y = 0f;
                if (offset.sqrMagnitude > radiusSq)
                    continue;
                enemy.Stun(_shutdownTime);
                Shutdowns++;
            }
        }

        private void SetLamp(bool spent)
        {
            _spentShown = spent;
            var material = spent ? _spentLight : _readyLight;
            if (_lamp != null && material != null)
                _lamp.sharedMaterial = material;
        }
    }
}
