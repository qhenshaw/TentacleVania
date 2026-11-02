UnityEditor.SessionState.SetString("runLog", "");
var last = 0f;
var lastScene = "";
UnityEditor.EditorApplication.CallbackFunction tick = null;
tick = () =>
{
    if (!UnityEngine.Application.isPlaying) { UnityEditor.EditorApplication.update -= tick; return; }
    var d = TentacleVania.World.Run.RunDirector.Active;
    if (d == null) return;
    if (d.CurrentScene != lastScene && !d.Loading)
    {
        lastScene = d.CurrentScene;
        UnityEditor.SessionState.SetString("runLog", UnityEditor.SessionState.GetString("runLog", "") + d.CurrentScene + "/" + d.Current.Role + "/" + d.Current.Reward + " ");
    }
    if (UnityEngine.Time.time - last < UnityEditor.SessionState.GetFloat("runSkip", 0.8f)) return;
    last = UnityEngine.Time.time;
    if (TentacleVania.Core.Runs.RunSession.Active) d.Skip();
};
UnityEditor.EditorApplication.update += tick;
return "autopilot on";
