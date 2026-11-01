using System;
using System.Collections.Generic;
using FMODUnity;
using TentacleVania.Abilities;
using TentacleVania.Enemies;
using UnityEngine;

namespace TentacleVania.Bosses
{
    [Serializable]
    public sealed class BossPhase
    {
        [Tooltip("The phase starts once health falls to this fraction (the first phase uses 1).")]
        [Range(0f, 1f)] public float StartsBelow = 1f;
        [Tooltip("Bit per attack index in the enemy definition; 0 allows every attack.")]
        public int AttackMask;
        [Min(0f)] public float MoveSpeedScale = 1f;
        [Tooltip("Adds summoned for sustain (consumable enemies).")]
        public Enemy AddPrefab;
        [Min(0)] public int MaxAdds = 2;
        [Min(0.1f)] public float AddInterval = 8f;
        [Tooltip("Adds summoned as the phase starts (after the intro for the first phase), within MaxAdds.")]
        [Min(0)] public int AddsOnStart;
        public EventReference Sting;
    }

    [CreateAssetMenu(menuName = "TentacleVania/Bosses/Boss Definition")]
    public sealed class BossDefinition : ScriptableObject
    {
        [Tooltip("Save id for the one-time kill.")]
        [SerializeField] private string _id;
        [Tooltip("UI string table key for the boss bar name.")]
        [SerializeField] private string _nameKey = "boss.test";
        [SerializeField] private List<BossPhase> _phases = new() { new() };
        [SerializeField] private AbilityDefinition _rewardAbility;
        [Tooltip("Optional second ability granted with the first (The Seeper: Vent Squeeze + Acid Resistance).")]
        [SerializeField] private AbilityDefinition _secondReward;
        [Tooltip("Optional pickup spawned where the boss dies (give the prefab's pickup a fixed save id).")]
        [SerializeField] private GameObject _dropPrefab;
        [Tooltip("Boss music; empty uses the shared boss theme (Audio/Resources/MusicSettings).")]
        [SerializeField] private EventReference _music;

        [Header("Beats (the intro lasts the enemy definition's spawn delay)")]
        [SerializeField] private EventReference _introSound;
        [SerializeField] private EventReference _deathSound;
        [SerializeField] private EventReference _absorbSound;
        [SerializeField, Min(0f)] private float _deathPause = 0.6f;
        [SerializeField, Min(0.1f)] private float _absorbDuration = 1.4f;
        [SerializeField, Min(0f)] private float _addSpawnDistance = 4f;

        private float[] _thresholds;

        public string Id => string.IsNullOrEmpty(_id) ? name : _id;
        public string NameKey => _nameKey;
        public IReadOnlyList<BossPhase> Phases => _phases;
        public AbilityDefinition RewardAbility => _rewardAbility;
        public AbilityDefinition SecondReward => _secondReward;
        public GameObject DropPrefab => _dropPrefab;
        public EventReference Music => _music;
        public EventReference IntroSound => _introSound;
        public EventReference DeathSound => _deathSound;
        public EventReference AbsorbSound => _absorbSound;
        public float DeathPause => _deathPause;
        public float AbsorbDuration => _absorbDuration;
        public float AddSpawnDistance => _addSpawnDistance;

        public IReadOnlyList<float> Thresholds
        {
            get
            {
                if (_thresholds == null || _thresholds.Length != _phases.Count)
                    _thresholds = new float[_phases.Count];
                for (var i = 0; i < _phases.Count; i++)
                    _thresholds[i] = _phases[i].StartsBelow;
                return _thresholds;
            }
        }

        private void OnValidate() => _thresholds = null;
    }
}
