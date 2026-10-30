using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TentacleVania.Core.Scenes
{
    public static class SceneFlow
    {
        public const string Bootstrap = "Bootstrap";
        public const string MainMenu = "MainMenu";
        public const string FirstGameplay = "CombatArena";

        private static int _pending;

        public static bool IsLoading => _pending > 0;
        public static bool HasBootstrap => SceneManager.GetSceneByName(Bootstrap).isLoaded;

        public static event Action<Scene> Loaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            _pending = 0;
            Loaded = null;
        }

        public static void Load(string sceneName)
        {
            if (IsLoading)
                return;
            if (!HasBootstrap)
            {
                SceneManager.LoadScene(sceneName);
                return;
            }
            for (var i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name == Bootstrap || !scene.isLoaded)
                    continue;
                Track(SceneManager.UnloadSceneAsync(scene));
            }
            var load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (load == null)
                return;
            _pending++;
            load.completed += _ =>
            {
                _pending--;
                var loaded = Newest(sceneName);
                SceneManager.SetActiveScene(loaded);
                Loaded?.Invoke(loaded);
            };
        }

        public static void Reload() => Load(SceneManager.GetActiveScene().name);

        private static Scene Newest(string sceneName)
        {
            for (var i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name == sceneName && scene.isLoaded)
                    return scene;
            }
            return default;
        }

        private static void Track(AsyncOperation operation)
        {
            if (operation == null)
                return;
            _pending++;
            operation.completed += _ => _pending--;
        }
    }
}
