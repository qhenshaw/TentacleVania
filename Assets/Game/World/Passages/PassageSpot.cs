using FMODUnity;
using TentacleVania.Core.Interaction;
using TentacleVania.World.Map;
using UnityEngine;
using UnityEngine.VFX;

namespace TentacleVania.World.Passages
{
    public sealed class PassageSpot : MonoBehaviour, ITraversal
    {
        private static readonly System.Collections.Generic.List<Vector3> Route = new(16);

        [Tooltip("The traversal ability that uses this spot (Burrow, VentSqueeze).")]
        [SerializeField] private string _abilityId = "Burrow";
        [Tooltip("The spot on the other side; the creature comes out there, facing along its forward.")]
        [SerializeField] private PassageSpot _exit;
        [SerializeField, Min(0.1f)] private float _radius = 1.4f;
        [SerializeField] private VisualEffect _dirt;
        [SerializeField] private EventReference _digSound;

        public string AbilityId => _abilityId;
        public Vector3 Position => transform.position;
        public float Radius => _radius;
        public bool CanTraverse => isActiveAndEnabled && _exit != null && FastTravel.Phase == TravelPhase.Idle;
        public PassageSpot Exit => _exit;

        private void OnEnable() => Traversals.Register(this);

        private void OnDisable() => Traversals.Unregister(this);

        public void Traverse()
        {
            if (!CanTraverse)
                return;
            var exit = _exit.transform;
            var forward = exit.forward;
            var yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            Route.Clear();
            var streamer = Streaming.RoomStreamer.Active;
            if (streamer != null)
                TunnelRoute.Build(streamer.Registry.Rooms, transform.position, exit.position, Route);
            if (Route.Count == 0)
                Route.Add(transform.position);
            if (!FastTravel.Tunnel(Route, exit.position, yaw))
                return;
            Play();
            _exit.Play();
            if (!_digSound.IsNull)
                RuntimeManager.PlayOneShot(_digSound, transform.position);
        }

        private void Play()
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
