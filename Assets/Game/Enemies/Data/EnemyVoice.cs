using FMODUnity;
using UnityEngine;

namespace TentacleVania.Enemies.Data
{
    [CreateAssetMenu(menuName = "TentacleVania/Enemies/Enemy Voice")]
    public sealed class EnemyVoice : ScriptableObject
    {
        [Tooltip("When the enemy first notices the creature (leaves Spawning).")]
        [SerializeField] private EventReference _alert;
        [SerializeField] private EventReference _pain;
        [SerializeField] private EventReference _death;
        [Tooltip("Grabbed by the maw (humans scream; machines alarm).")]
        [SerializeField] private EventReference _grabbed;
        [Tooltip("Seconds between pain sounds from one enemy.")]
        [SerializeField, Min(0f)] private float _painCooldown = 1.2f;

        public EventReference Alert => _alert;
        public EventReference Pain => _pain;
        public EventReference Death => _death;
        public EventReference Grabbed => _grabbed;
        public float PainCooldown => _painCooldown;
    }
}
