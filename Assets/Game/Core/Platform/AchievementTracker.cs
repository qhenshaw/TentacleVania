using TentacleVania.Core.Saves;
using UnityEngine;

namespace TentacleVania.Core.Platform
{
    public sealed class AchievementTracker : MonoBehaviour
    {
        private const float Interval = 2f;

        private AchievementList _list;
        private float _next;

        public static AchievementTracker Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (!Application.isPlaying || Instance != null)
                return;
            var go = new GameObject(nameof(AchievementTracker));
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<AchievementTracker>();
        }

        private void Awake() => _list = Resources.Load<AchievementList>(AchievementList.ResourcePath);

        private void Update()
        {
            var now = Time.unscaledTime;
            if (now < _next || SaveSystem.Data == null)
                return;
            _next = now + Interval;
            Achievements.Check(_list, SaveSystem.Data, SaveSystem.LivePlaytime);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
