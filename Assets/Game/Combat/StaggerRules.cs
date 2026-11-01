using UnityEngine;

namespace TentacleVania.Combat
{
    public static class StaggerRules
    {
        public static float ExtraStun { get; set; }
        public static float WindowDamageScale { get; set; } = 1f;
        public static float CorrodedScale { get; set; } = 1f;
        public static float MachineElectricScale { get; set; } = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ExtraStun = 0f;
            WindowDamageScale = 1f;
            CorrodedScale = 1f;
            MachineElectricScale = 1f;
        }
    }
}
