var target = UnityEditor.SessionState.GetString("gpuToggle", "TentacleVania.Core.GameSettings.TentacleMerge");
var split = target.LastIndexOf('.');
var type = System.AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(target.Substring(0, split))).First(t => t != null);
var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
var property = type.GetProperty(target.Substring(split + 1), flags);
var field = type.GetField(target.Substring(split + 1), flags);
System.Action<bool> set = v => { if (property != null) property.SetValue(null, v); else field.SetValue(null, v); };
var original = property != null ? (bool)property.GetValue(null) : (bool)field.GetValue(null);
var timings = new UnityEngine.FrameTiming[1];
var sums = new double[2];
var counts = new int[2];
var frames = 0;
UnityEditor.SessionState.SetString("gpuAb", "running");
UnityEditor.EditorApplication.CallbackFunction tick = null;
tick = () =>
{
    if (!UnityEditor.EditorApplication.isPlaying)
    {
        UnityEditor.EditorApplication.update -= tick;
        return;
    }
    frames++;
    var on = frames / 60 % 2 == 0;
    set(on);
    UnityEngine.FrameTimingManager.CaptureFrameTimings();
    if (frames % 60 > 10 && UnityEngine.FrameTimingManager.GetLatestTimings(1, timings) > 0 && timings[0].gpuFrameTime > 0)
    {
        sums[on ? 0 : 1] += timings[0].gpuFrameTime;
        counts[on ? 0 : 1]++;
    }
    if (frames < 600)
        return;
    UnityEditor.EditorApplication.update -= tick;
    set(original);
    var onMs = sums[0] / System.Math.Max(1, counts[0]);
    var offMs = sums[1] / System.Math.Max(1, counts[1]);
    UnityEditor.SessionState.SetString("gpuAb", target + " at " + UnityEngine.Screen.width + "x" + UnityEngine.Screen.height + ": on " + onMs.ToString("0.000") + " ms, off " + offMs.ToString("0.000") + " ms, cost " + (onMs - offMs).ToString("0.000") + " ms GPU (" + counts[0] + "/" + counts[1] + " frames)");
};
UnityEditor.EditorApplication.update += tick;
return "timing " + target;
