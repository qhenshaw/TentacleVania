using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.Bosses.Seeper
{
    public sealed class SeeperVent : MonoBehaviour
    {
        private static readonly List<SeeperVent> Vents = new();

        public static IReadOnlyList<SeeperVent> All => Vents;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Vents.Clear();

        private void OnEnable() => Vents.Add(this);

        private void OnDisable() => Vents.Remove(this);
    }
}
