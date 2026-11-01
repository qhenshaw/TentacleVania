using UnityEngine;

namespace TentacleVania.Enemies.Data
{
    [CreateAssetMenu(menuName = "TentacleVania/Enemies/Elite Settings")]
    public sealed class EliteSettings : ScriptableObject
    {
        public const string ResourcePath = "EliteSettings";

        [SerializeField, Min(1f)] private float _healthScale = 1.8f;
        [SerializeField, Min(0f)] private float _armorBonus = 40f;
        [SerializeField, Min(1f)] private float _staggerScale = 1.5f;
        [SerializeField, Min(1f)] private float _damageScale = 1.25f;
        [SerializeField, Min(1f)] private float _bodyScale = 1.15f;
        [Tooltip("Pickup an elite drops on death with DropChance (design: Mutagen, about 2 per sector with the secrets).")]
        [SerializeField] private GameObject _drop;
        [SerializeField, Range(0f, 1f)] private float _dropChance = 0.5f;

        private static EliteSettings _instance;

        public static EliteSettings Instance => _instance != null ? _instance : _instance = Resources.Load<EliteSettings>(ResourcePath);

        public float HealthScale => _healthScale;
        public float ArmorBonus => _armorBonus;
        public float StaggerScale => _staggerScale;
        public float DamageScale => _damageScale;
        public float BodyScale => _bodyScale;
        public GameObject Drop => _drop;
        public float DropChance => _dropChance;
    }
}
