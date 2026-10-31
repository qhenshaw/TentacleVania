using UnityEngine;

namespace TentacleVania.Audio
{
    public static class PaRules
    {
        public static string SaveKey(string sequence, bool perSector, string sector) =>
            perSector && !string.IsNullOrEmpty(sector) ? sequence + "@" + sector : sequence;

        public static float LineSeconds(int characters, float voiceSeconds, float secondsPerCharacter, float minimum) =>
            Mathf.Max(minimum, voiceSeconds, characters * secondsPerCharacter);
    }
}
