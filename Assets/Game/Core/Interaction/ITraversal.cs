using UnityEngine;

namespace TentacleVania.Core.Interaction
{
    public interface ITraversal
    {
        string AbilityId { get; }
        Vector3 Position { get; }
        float Radius { get; }
        bool CanTraverse { get; }
        void Traverse();
    }
}
