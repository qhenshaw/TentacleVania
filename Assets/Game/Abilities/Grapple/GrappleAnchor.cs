using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.Abilities.Grapple
{
    public sealed class GrappleAnchor : MonoBehaviour
    {
        private static readonly List<GrappleAnchor> Live = new();

        [Tooltip("Where the creature lands; put it past the chasm edge.")]
        [SerializeField] private Transform _landing;

        public static IReadOnlyList<GrappleAnchor> All => Live;
        public Vector3 Point => transform.position;
        public Vector3 Landing => _landing != null ? _landing.position : transform.position;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Live.Clear();

        private void OnEnable() => Live.Add(this);

        private void OnDisable() => Live.Remove(this);
    }
}
