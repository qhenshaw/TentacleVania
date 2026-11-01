using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.Abilities.Feats
{
    [CreateAssetMenu(menuName = "TentacleVania/Abilities/Feat Catalog")]
    public sealed class FeatCatalog : ScriptableObject
    {
        public const string ResourcePath = "FeatCatalog";

        [SerializeField] private List<FeatDefinition> _feats = new();

        public IReadOnlyList<FeatDefinition> Feats => _feats;
    }
}
