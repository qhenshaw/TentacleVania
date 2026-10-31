using TentacleVania.Core.Characters;
using TentacleVania.Creature.Rig;
using UnityEngine;

namespace TentacleVania.Weapons.Data
{
    [CreateAssetMenu(menuName = "TentacleVania/Weapons/Weapon Tentacle Settings")]
    public sealed class WeaponTentacleSettings : ScriptableObject
    {
        [SerializeField, Range(Chains.MinJoints, Chains.MaxJoints)] private int _joints = Chains.DefaultJoints;
        [SerializeField, Min(0.1f)] private float _length = 2.4f;
        [SerializeField, Min(0f)] private float _archHeight = 0.25f;
        [SerializeField] private Vector3 _rootOffset = new(0.18f, 0.5f, 0.15f);
        [SerializeField, Min(0.001f)] private float _rootWidth = 0.14f;
        [SerializeField, Min(0.001f)] private float _tipWidth = 0.03f;
        [Tooltip("Seconds of the wind-up the tentacle takes to grow out of the body.")]
        [SerializeField, Min(0.01f)] private float _emergeTime = 0.08f;
        [Tooltip("Seconds it takes to pull back into the body after the move ends.")]
        [SerializeField, Min(0.01f)] private float _retractTime = 0.12f;
        [SerializeField] private SpringChainSettings _spring = new()
        {
            PoseStiffness = 30f,
            TipLooseness = 0.6f,
            Damping = 0.15f,
            Gravity = 2f,
            Iterations = 3,
        };

        public int Joints => _joints;
        public float Length => _length;
        public float ArchHeight => _archHeight;
        public Vector3 RootOffset => _rootOffset;
        public float RootWidth => _rootWidth;
        public float TipWidth => _tipWidth;
        public float EmergeTime => _emergeTime;
        public float RetractTime => _retractTime;
        public SpringChainSettings Spring => _spring;
    }
}
