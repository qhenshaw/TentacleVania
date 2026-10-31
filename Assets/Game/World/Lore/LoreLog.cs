using FMODUnity;
using UnityEngine;

namespace TentacleVania.World.Lore
{
    public enum LoreFormat
    {
        Text,
        Audio,
    }

    [CreateAssetMenu(menuName = "TentacleVania/World/Lore Log")]
    public sealed class LoreLog : ScriptableObject
    {
        public const string Table = "Dialogue";

        [SerializeField] private LoreFormat _format;
        [Tooltip("Sector id (SH, BL...).")]
        [SerializeField] private string _sector = "SH";
        [Tooltip("Audio logs: the recording; the body text is its subtitle transcript.")]
        [SerializeField] private EventReference _audio;

        public string Id => name;
        public LoreFormat Format => _format;
        public string Sector => _sector;
        public EventReference Audio => _audio;
        public string TitleKey => "lore." + name + ".title";
        public string FromKey => "lore." + name + ".from";
        public string BodyKey => "lore." + name + ".body";
    }
}
