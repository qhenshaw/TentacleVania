using System;
using UnityEngine;

namespace TentacleVania.World.Hoarder
{
    [Serializable]
    public sealed class StockEntry
    {
        [Tooltip("Mutation id (asset name) sold.")]
        public string Mutation;
        [Tooltip("Trinket id that unlocks it; empty = floor unlock, always in stock.")]
        public string Trinket;
        [Tooltip("Biomass; 0 = Balance Sheet price by notches (300 / 700 / 1,500).")]
        [Min(0)] public int Price;
    }

    [CreateAssetMenu(menuName = "TentacleVania/World/Hoarder Stock")]
    public sealed class HoarderStock : ScriptableObject
    {
        [SerializeField] private StockEntry[] _entries = Array.Empty<StockEntry>();

        public StockEntry[] Entries => _entries;
    }
}
