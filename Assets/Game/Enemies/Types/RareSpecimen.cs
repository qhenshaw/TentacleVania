using TentacleVania.Combat;
using TentacleVania.Core.Progression;
using UnityEngine;

namespace TentacleVania.Enemies.Types
{
    public sealed class RareSpecimen : MonoBehaviour
    {
        [Tooltip("Save id of this carrier; once consumed it never appears again.")]
        [SerializeField] private string _saveId;
        [SerializeField] private PickupKind _kind = PickupKind.Mutation;
        [Tooltip("Weapon or mutation id granted when it's consumed (design: rare specimens must be consumed).")]
        [SerializeField] private string _itemId;

        private Consumable _consumable;

        public string SaveId => _saveId;
        public string ItemId => _itemId;
        public PickupKind Kind => _kind;
        public bool Taken => !string.IsNullOrEmpty(_saveId) && Collectibles.IsCollected(_saveId);

        public void Configure(string saveId, PickupKind kind, string itemId)
        {
            _saveId = saveId;
            _kind = kind;
            _itemId = itemId;
        }

        private void Awake() => TryGetComponent(out _consumable);

        private void OnEnable() => CombatEvents.Consumed += OnConsumed;

        private void OnDisable() => CombatEvents.Consumed -= OnConsumed;

        private void OnConsumed(Consumable consumable)
        {
            if (consumable == null || consumable != _consumable || string.IsNullOrEmpty(_saveId))
                return;
            Collectibles.Collect(_saveId, _kind, _itemId, 1);
        }
    }
}
