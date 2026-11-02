var seed = UnityEditor.SessionState.GetInt("runSeed", 1);
UnityEditor.SessionState.SetString("sweepLog", "");
TentacleVania.Creature.CreatureVitals.GodMode = true;
var issues = new System.Text.StringBuilder();
var errors = 0;
var rooms = 0;
var lastRoom = "";
var next = 0f;
UnityEngine.Application.LogCallback onLog = (message, stack, type) =>
{
    if (type == UnityEngine.LogType.Error || type == UnityEngine.LogType.Exception || type == UnityEngine.LogType.Assert)
    {
        errors++;
        if (errors <= 3)
            issues.Append(" | " + type + ": " + message.Split('\n')[0]);
    }
};
UnityEngine.Application.logMessageReceived += onLog;
TentacleVania.World.Run.RunDirector.Active.Restart(seed);
UnityEditor.EditorApplication.CallbackFunction tick = null;
tick = () =>
{
    if (!UnityEngine.Application.isPlaying)
    {
        UnityEngine.Application.logMessageReceived -= onLog;
        UnityEditor.EditorApplication.update -= tick;
        return;
    }
    UnityEngine.Time.timeScale = 3f;
    var d = TentacleVania.World.Run.RunDirector.Active;
    if (d == null)
        return;
    if (!TentacleVania.Core.Runs.RunSession.Active)
    {
        var last = TentacleVania.Core.Runs.RunSession.Last;
        if (last != null && last.Seed == seed)
        {
            UnityEditor.SessionState.SetString("sweepLog", "done seed " + seed + " won=" + last.Won + " floor " + (last.Floor + 1) + " rooms " + rooms + " errors " + errors + issues);
            UnityEngine.Application.logMessageReceived -= onLog;
            UnityEditor.EditorApplication.update -= tick;
        }
        return;
    }
    if (d.Loading)
        return;
    var key = TentacleVania.Core.Runs.RunSession.Floor + "/" + TentacleVania.Core.Runs.RunSession.Step + "/" + d.CurrentScene;
    if (key != lastRoom)
    {
        lastRoom = key;
        rooms++;
        var c = TentacleVania.Combat.CombatTargets.Creature;
        if (c == null || !UnityEngine.AI.NavMesh.SamplePosition(c.transform.position, out var hit, 1f, UnityEngine.AI.NavMesh.AllAreas))
            issues.Append(" | F" + TentacleVania.Core.Runs.RunSession.Floor + " " + d.CurrentScene + ": creature off the NavMesh at " + (c != null ? c.transform.position.ToString("0") : "none"));
        next = UnityEngine.Time.realtimeSinceStartup + 0.4f;
    }
    if (UnityEngine.Time.realtimeSinceStartup < next)
        return;
    next = UnityEngine.Time.realtimeSinceStartup + 0.25f;
    var mutation = TentacleVania.UI.Run.MutationPickScreen.Instance;
    if (mutation != null && mutation.IsOpen)
    {
        if (mutation.Swapping)
        {
            mutation.Back();
            mutation.Back();
        }
        else
        {
            mutation.Choose(0);
        }
        return;
    }
    var evolution = TentacleVania.UI.Run.EvolutionPickScreen.Instance;
    if (evolution != null && evolution.IsOpen)
    {
        evolution.Choose(0);
        return;
    }
    d.Skip();
};
UnityEditor.EditorApplication.update += tick;
return "sweeping seed " + seed;
