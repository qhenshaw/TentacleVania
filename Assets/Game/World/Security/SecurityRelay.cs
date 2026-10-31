using System.Collections.Generic;
using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Core.Progression;
using UnityEngine;
using UnityEngine.VFX;

namespace TentacleVania.World.Security
{
    [RequireComponent(typeof(Health))]
    public sealed class SecurityRelay : MonoBehaviour
    {
        private static readonly List<SecurityRelay> Live = new();

        [Tooltip("Security system this relay belongs to; its door uses the same id.")]
        [SerializeField] private string _system = "SH";
        [Tooltip("Relay id within the system; when empty, the object name is used.")]
        [SerializeField] private string _id;
        [SerializeField, Min(1f)] private float _maxHealth = 60f;
        [SerializeField] private GameObject _intact;
        [SerializeField] private GameObject _broken;
        [SerializeField] private Collider _hitCollider;
        [SerializeField] private VisualEffect _burst;
        [SerializeField] private EventReference _destroySound;

        private Health _health;

        public static IReadOnlyList<SecurityRelay> All => Live;

        public string System => _system;
        public string Id => string.IsNullOrEmpty(_id) ? name : _id;
        public bool IsDestroyed { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Live.Clear();

        private void Awake()
        {
            _health = GetComponent<Health>();
            _health.Configure(Team.Site, _maxHealth);
            _health.Bleeds = false;
        }

        private void OnEnable()
        {
            Live.Add(this);
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            Live.Remove(this);
            _health.Died -= OnDied;
        }

        private void Start()
        {
            if (SecurityRelays.IsDestroyed(_system, Id))
                Break(false);
            else
                Show(false);
        }

        public void DestroyRelay()
        {
            if (!IsDestroyed)
                _health.Kill();
        }

        public void Restore()
        {
            _health.ResetToFull();
            Show(false);
        }

        private void OnDied(Health health)
        {
            SecurityRelays.Destroy(_system, Id);
            Break(true);
        }

        private void Break(bool effects)
        {
            Show(true);
            if (!effects)
                return;
            if (_burst != null)
            {
                _burst.Reinit();
                _burst.Play();
            }
            if (!_destroySound.IsNull)
                RuntimeManager.PlayOneShot(_destroySound, transform.position);
        }

        private void Show(bool broken)
        {
            IsDestroyed = broken;
            if (_intact != null)
                _intact.SetActive(!broken);
            if (_broken != null)
                _broken.SetActive(broken);
            if (_hitCollider != null)
                _hitCollider.enabled = !broken;
        }
    }
}
