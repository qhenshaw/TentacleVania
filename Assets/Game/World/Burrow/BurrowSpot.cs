using FMODUnity;
using TentacleVania.Core.Interaction;
using TentacleVania.World.Map;
using UnityEngine;
using UnityEngine.VFX;

namespace TentacleVania.World.Burrow
{
    public sealed class BurrowSpot : MonoBehaviour, ITraversal
    {
        public const string Ability = "Burrow";

        private static readonly Vector3[] Path = new Vector3[1];

        [Tooltip("The soft spot on the other side of the barrier; the creature comes up there, facing along its forward.")]
        [SerializeField] private BurrowSpot _exit;
        [SerializeField, Min(0.1f)] private float _radius = 1.4f;
        [SerializeField] private VisualEffect _dirt;
        [SerializeField] private EventReference _digSound;

        public string AbilityId => Ability;
        public Vector3 Position => transform.position;
        public float Radius => _radius;
        public bool CanTraverse => isActiveAndEnabled && _exit != null && FastTravel.Phase == TravelPhase.Idle;
        public BurrowSpot Exit => _exit;

        private void OnEnable() => Traversals.Register(this);

        private void OnDisable() => Traversals.Unregister(this);

        public void Traverse()
        {
            if (!CanTraverse)
                return;
            Path[0] = transform.position;
            var forward = _exit.transform.forward;
            var yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            if (!FastTravel.Tunnel(Path, _exit.transform.position, yaw))
                return;
            PlayDirt();
            _exit.PlayDirt();
            if (!_digSound.IsNull)
                RuntimeManager.PlayOneShot(_digSound, transform.position);
        }

        private void PlayDirt()
        {
            if (_dirt == null)
                return;
            _dirt.Reinit();
            _dirt.Play();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.6f, 0.4f, 0.2f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, _radius);
            if (_exit != null)
                Gizmos.DrawLine(transform.position, _exit.transform.position);
        }
    }
}
