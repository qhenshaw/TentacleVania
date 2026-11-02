using TentacleVania.Core.Debugging;
using TentacleVania.Core.Runs;
using TentacleVania.Core.Scenes;
using UnityEngine;

namespace TentacleVania.World.Run
{
    public static class RunDebugCommands
    {
        private const string Tab = "Run";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            DebugMenu.Button(Tab, "Start run", () => SceneFlow.Load(RunKit.RootScene));
            DebugMenu.Button(Tab, "Skip room", () =>
            {
                if (RunDirector.Active != null)
                    RunDirector.Active.Skip();
            });
            DebugMenu.Button(Tab, "Restart run (new seed)", () =>
            {
                if (RunDirector.Active != null)
                    RunDirector.Active.Restart(System.Environment.TickCount);
            });
            DebugMenu.Button(Tab, "Jump to floor boss", () =>
            {
                if (RunDirector.Active != null)
                    RunDirector.Active.JumpToBoss();
            });
            DebugMenu.Button(Tab, "Add reroll token", () => RunSession.Rerolls++);
            DebugMenu.Button(Tab, "Reroll doors", () =>
            {
                if (RunDirector.Active != null)
                    RunDirector.Active.Reroll();
            });
            DebugMenu.Button(Tab, "Grant mutation reward", () => RunSession.Grant(RunReward.Mutation));
            DebugMenu.Button(Tab, "Grant evolution reward", () => RunSession.Grant(RunReward.Evolution));
            DebugMenu.Value(Tab, "Run", () => RunSession.Active ? $"seed {RunSession.Seed}, floor {RunSession.Floor + 1}, step {RunSession.Step}, essence {RunSession.Essence}" : "not in a run");
            DebugMenu.Value(Tab, "Room", () =>
            {
                var director = RunDirector.Active;
                if (director == null)
                    return "none";
                return $"{director.CurrentScene} ({director.Current.Role}, {director.Current.Reward}){(director.RoomCleared ? " cleared" : "")}{(director.Loading ? " loading" : "")}";
            });
            DebugMenu.Value(Tab, "Plan", () =>
            {
                var director = RunDirector.Active;
                return director == null || director.Plan == null ? "none" : string.Join(" ", director.Plan.Steps);
            });
        }
    }
}
