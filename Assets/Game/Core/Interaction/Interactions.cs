using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.Core.Interaction
{
    public static class Interactions
    {
        private static readonly List<IInteractable> Registered = new(16);

        public static IInteractable Current { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Registered.Clear();
            Current = null;
        }

        public static void Register(IInteractable interactable)
        {
            if (interactable != null && !Registered.Contains(interactable))
                Registered.Add(interactable);
        }

        public static void Unregister(IInteractable interactable)
        {
            Registered.Remove(interactable);
            if (Current == interactable)
                Current = null;
        }

        public static IInteractable Refresh(Vector3 position)
        {
            Current = Nearest(position);
            return Current;
        }

        public static void Clear() => Current = null;

        public static bool TryInteract()
        {
            if (Current == null || !Current.CanInteract)
                return false;
            Current.Interact();
            return true;
        }

        public static IInteractable Nearest(Vector3 position)
        {
            IInteractable best = null;
            var bestDistance = float.MaxValue;
            foreach (var interactable in Registered)
            {
                if (!interactable.CanInteract)
                    continue;
                var offset = interactable.Position - position;
                offset.y = 0f;
                var distance = offset.sqrMagnitude;
                if (distance > interactable.Radius * interactable.Radius || distance >= bestDistance)
                    continue;
                best = interactable;
                bestDistance = distance;
            }
            return best;
        }
    }
}
