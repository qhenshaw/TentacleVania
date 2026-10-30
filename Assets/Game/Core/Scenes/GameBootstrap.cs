using UnityEngine;

namespace TentacleVania.Core.Scenes
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private string _firstScene = SceneFlow.MainMenu;

        private void Start() => SceneFlow.Load(_firstScene);
    }
}
