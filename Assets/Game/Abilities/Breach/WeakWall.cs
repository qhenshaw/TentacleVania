using System.Collections.Generic;
using FMODUnity;
using TentacleVania.Core.Progression;
using UnityEngine;
using UnityEngine.VFX;

namespace TentacleVania.Abilities.Breach
{
    public sealed class WeakWall : MonoBehaviour
    {
        private static readonly List<WeakWall> Registry = new();

        [Tooltip("Save id; unique per wall. Defaults to the scene and object name.")]
        [SerializeField] private string _id;
        [SerializeField] private GameObject _intact;
        [SerializeField] private GameObject _broken;
        [SerializeField] private VisualEffect _debris;
        [SerializeField] private EventReference _breakSound;
        [Tooltip("Half the wall's width along its local X, for the reach check.")]
        [SerializeField, Min(0.1f)] private float _halfWidth = 1.5f;

        public static IReadOnlyList<WeakWall> All => Registry;
        public string Id => string.IsNullOrEmpty(_id) ? $"{gameObject.scene.name}/{name}" : _id;
        public bool IsBroken { get; private set; }
        public float HalfWidth => _halfWidth;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Registry.Clear();

        private void OnEnable()
        {
            Registry.Add(this);
            SetBroken(BreachedWalls.IsBreached(Id));
        }

        private void OnDisable() => Registry.Remove(this);

        public Vector3 ClosestPoint(Vector3 point)
        {
            var local = transform.InverseTransformPoint(point);
            local.x = Mathf.Clamp(local.x, -_halfWidth, _halfWidth);
            local.y = 0f;
            local.z = 0f;
            return transform.TransformPoint(local);
        }

        public void Smash()
        {
            if (IsBroken)
                return;
            SetBroken(true);
            BreachedWalls.Breach(Id);
            if (_debris != null)
            {
                _debris.Reinit();
                _debris.Play();
            }
            if (!_breakSound.IsNull)
                RuntimeManager.PlayOneShot(_breakSound, transform.position);
        }

        public void Restore() => SetBroken(false);

        private void SetBroken(bool broken)
        {
            IsBroken = broken;
            if (_intact != null)
                _intact.SetActive(!broken);
            if (_broken != null)
                _broken.SetActive(broken);
        }
    }
}
