var totals = new System.Collections.Generic.Dictionary<string, long>();
var counts = new System.Collections.Generic.Dictionary<string, int>();
var last = UnityEditorInternal.ProfilerDriver.lastFrameIndex;
var first = System.Math.Max(UnityEditorInternal.ProfilerDriver.firstFrameIndex, last - 120);
var frames = 0;
for (var frame = first; frame <= last; frame++)
{
    using (var view = UnityEditorInternal.ProfilerDriver.GetRawFrameDataView(frame, 0))
    {
        if (view == null || !view.valid)
            continue;
        frames++;
        var allocId = view.GetMarkerId("GC.Alloc");
        var names = new System.Collections.Generic.List<string>();
        var remaining = new System.Collections.Generic.List<int>();
        for (var i = 0; i < view.sampleCount; i++)
        {
            while (remaining.Count > 0 && remaining[remaining.Count - 1] == 0)
            {
                remaining.RemoveAt(remaining.Count - 1);
                names.RemoveAt(names.Count - 1);
            }
            if (remaining.Count > 0)
                remaining[remaining.Count - 1]--;
            var marker = view.GetSampleMarkerId(i);
            if (marker == allocId)
            {
                var path = names.Count == 0 ? "?" : string.Join(" > ", names.GetRange(System.Math.Max(0, names.Count - 3), System.Math.Min(3, names.Count)));
                var bytes = view.GetSampleMetadataCount(i) > 0 ? view.GetSampleMetadataAsLong(i, 0) : 0;
                totals[path] = (totals.TryGetValue(path, out var t) ? t : 0) + bytes;
                counts[path] = (counts.TryGetValue(path, out var c) ? c : 0) + 1;
            }
            names.Add(view.GetSampleName(i));
            remaining.Add(view.GetSampleChildrenCount(i));
        }
    }
}
UnityEditorInternal.ProfilerDriver.enabled = false;
UnityEngine.Profiling.Profiler.enabled = false;
var list = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, long>>(totals);
list.Sort((a, b) => b.Value.CompareTo(a.Value));
var r = "frames=" + frames + "\n";
for (var i = 0; i < System.Math.Min(15, list.Count); i++)
    r += list[i].Value + " B x" + counts[list[i].Key] + "  " + list[i].Key + "\n";
return r;
