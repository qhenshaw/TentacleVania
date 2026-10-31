var result;
try {
    var project = studio.project;
    var master = project.workspace.mixer.masterBus;
    function bus(name) {
        var found = project.lookup("bus:/" + name);
        if (found)
            return found;
        var group = project.create("MixerGroup");
        group.name = name;
        group.output = master;
        return group;
    }
    var music = bus("Music");
    var sfx = bus("SFX");
    var vo = bus("VO");
    var events = project.model.Event.findInstances();
    var routed = { Music: 0, SFX: 0, VO: 0 };
    for (var i = 0; i < events.length; i++) {
        var event = events[i];
        var path = event.getPath();
        var target = path.indexOf("event:/Music/") == 0 ? music : path.indexOf("event:/VO/") == 0 ? vo : sfx;
        if (event.mixerInput.output !== target)
            event.mixerInput.output = target;
        routed[target.name]++;
    }
    if (!Params.dryRun)
        project.save();
    result = "ok " + routed.Music + " events on bus:/Music, " + routed.SFX + " on bus:/SFX, " + routed.VO + " on bus:/VO" + (Params.dryRun ? " (dry run, not saved)" : "");
} catch (e) {
    result = "error " + e;
}
console.log("FMOD_ROUTE: " + result);
