using UnityEngine;

namespace TentacleVania.Core.Interaction
{
    public interface IInteractable
    {
        Vector3 Position { get; }
        float Radius { get; }
        string PromptKey { get; }
        bool CanInteract { get; }
        void Interact();
    }
}
