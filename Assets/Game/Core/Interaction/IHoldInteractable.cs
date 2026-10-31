namespace TentacleVania.Core.Interaction
{
    public interface IHoldInteractable : IInteractable
    {
        float HoldDuration { get; }
        void InteractHold();
    }
}
