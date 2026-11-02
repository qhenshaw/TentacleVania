UnityEditor.SessionState.SetString("bossTour", "");
TentacleVania.Creature.CreatureVitals.GodMode = true;
var next = 0f;
var cleared = new System.Collections.Generic.HashSet<string>();
UnityEditor.EditorApplication.CallbackFunction tick = null;
tick = () =>
{
    if (!UnityEngine.Application.isPlaying) { UnityEditor.EditorApplication.update -= tick; return; }
    var d = TentacleVania.World.Run.RunDirector.Active;
    if (d == null || d.Loading || UnityEngine.Time.time < next) return;
    if (!TentacleVania.Core.Runs.RunSession.Active)
    {
        if (!cleared.Contains("END"))
        {
            cleared.Add("END");
            var last = TentacleVania.Core.Runs.RunSession.Last;
            UnityEditor.SessionState.SetString("bossTour", UnityEditor.SessionState.GetString("bossTour", "") + " | ENDED won=" + (last != null && last.Won) + " summary=" + (TentacleVania.UI.Run.RunSummaryScreen.Instance != null));
        }
        return;
    }
    next = UnityEngine.Time.time + 3f;
    var role = d.Current.Role;
    if (role == TentacleVania.Core.Runs.RoomRole.Boss || role == TentacleVania.Core.Runs.RoomRole.Finale)
    {
        var key = TentacleVania.Core.Runs.RunSession.Floor + d.CurrentScene;
        if (cleared.Contains(key)) return;
        cleared.Add(key);
        var c = TentacleVania.Combat.CombatTargets.Creature;
        var bosses = 0;
        foreach (var e in UnityEngine.Object.FindObjectsByType<TentacleVania.Bosses.BossEnemy>()) if (e.Health.IsAlive) bosses++;
        var arena = TentacleVania.World.Arenas.ArenaEncounter.Active;
        var onMesh = UnityEngine.AI.NavMesh.SamplePosition(c.transform.position, out var hit, 0.6f, UnityEngine.AI.NavMesh.AllAreas);
        UnityEditor.SessionState.SetString("bossTour", UnityEditor.SessionState.GetString("bossTour", "") + " | F" + TentacleVania.Core.Runs.RunSession.Floor + " " + d.CurrentScene + " " + role + " pos " + c.transform.position.ToString("0") + " navmesh " + onMesh + " bosses " + bosses + " arena " + (arena != null ? arena.State.ToString() : "none") + " stage " + TentacleVania.Core.Runs.RunSession.SizeStage);
        d.Skip();
        next = UnityEngine.Time.time + 1f;
    }
    else
    {
        d.JumpToBoss();
    }
};
UnityEditor.EditorApplication.update += tick;
return "tour on";
