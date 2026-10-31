using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.Core.Interaction
{
    public static class Interactions
    {
        private static readonly List<IInteractable> Registered = new(16);

        private static IHoldInteractable _holding;
        private static float _holdStarted;

        public static IInteractable Current { get; private set; }
        public static float HoldProgress { get; private set; }
        public static bool IsHolding => _holding != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Registered.Clear();
            Current = null;
            _holding = null;
            HoldProgress = 0f;
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

        public static void Clear()
        {
            Current = null;
            CancelHold();
        }

        public static bool Press(float now)
        {
            if (Current == null || !Current.CanInteract)
                return false;
            if (Current is IHoldInteractable holdable && holdable.HoldDuration > 0f)
            {
                _holding = holdable;
                _holdStarted = now;
                HoldProgress = 0f;
                return true;
            }
            Current.Interact();
            return true;
        }

        public static void TickHold(bool held, float now)
        {
            if (_holding == null)
                return;
            if (Current != _holding || !_holding.CanInteract)
            {
                CancelHold();
                return;
            }
            var target = _holding;
            HoldProgress = HoldRules.Progress(now - _holdStarted, target.HoldDuration);
            switch (HoldRules.Resolve(held, now - _holdStarted, target.HoldDuration))
            {
                case HoldResult.Tap:
                    CancelHold();
                    target.Interact();
                    break;
                case HoldResult.Hold:
                    CancelHold();
                    target.InteractHold();
                    break;
            }
        }

        public static void CancelHold()
        {
            _holding = null;
            HoldProgress = 0f;
        }

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
