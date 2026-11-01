using System;
using TentacleVania.Core.Progression;
using UnityEngine;

namespace TentacleVania.World.Hoarder
{
    [Serializable]
    public sealed class StockEntry
    {
        [Tooltip("What is sold: Mutation, Weapon or Ability.")]
        public PickupKind Kind = PickupKind.Mutation;
        [Tooltip("Item id (asset name) sold.")]
        public string Mutation;
        [Tooltip("Floor unlock: sold only by Hoarders on this floor (0 = L0) or deeper.")]
        [Range(0, 3)] public int MinFloor;
        [Tooltip("Trinket id that unlocks it; empty = floor unlock, always in stock.")]
        public string Trinket;
        [Tooltip("Biomass; 0 = Balance Sheet price by notches (300 / 700 / 1,500); weapons and abilities need a price.")]
        [Min(0)] public int Price;
    }

    [CreateAssetMenu(menuName = "TentacleVania/World/Hoarder Stock")]
    public sealed class HoarderStock : ScriptableObject
    {
        [SerializeField] private StockEntry[] _entries = Array.Empty<StockEntry>();

        public StockEntry[] Entries => _entries;
    }
}
