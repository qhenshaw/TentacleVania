using FMODUnity;
using UnityEngine;

namespace TentacleVania.Audio
{
    [CreateAssetMenu(menuName = "TentacleVania/Audio/Music Settings")]
    public sealed class MusicSettings : ScriptableObject
    {
        public const string ResourcePath = "MusicSettings";

        [SerializeField] private EventReference _arena;
        [Tooltip("Used by bosses whose BossDefinition has no music of its own.")]
        [SerializeField] private EventReference _bossTheme;
        [SerializeField] private EventReference _roomClearSting;
        [SerializeField] private EventReference _nestRest;
        [Tooltip("Plays while the main menu is the active scene.")]
        [SerializeField] private EventReference _menu;
        [SerializeField, Range(0f, 1f)] private float _menuIntensity = 0.5f;
        [SerializeField] private string _intensityParameter = "Intensity";
        [SerializeField, Min(0.05f)] private float _fadeIn = 1.5f;
        [SerializeField, Min(0.05f)] private float _fadeOut = 4f;
        [SerializeField, Min(0.1f)] private float _intensitySpeed = 0.35f;
        [Tooltip("The nest ambience stops once the creature walks this far from where it rested.")]
        [SerializeField, Min(0.5f)] private float _restLeaveDistance = 3f;

        public EventReference Arena => _arena;
        public EventReference BossTheme => _bossTheme;
        public EventReference RoomClearSting => _roomClearSting;
        public EventReference NestRest => _nestRest;
        public EventReference Menu => _menu;
        public float MenuIntensity => _menuIntensity;
        public string IntensityParameter => _intensityParameter;
        public float FadeIn => _fadeIn;
        public float FadeOut => _fadeOut;
        public float IntensitySpeed => _intensitySpeed;
        public float RestLeaveDistance => _restLeaveDistance;
    }
}
