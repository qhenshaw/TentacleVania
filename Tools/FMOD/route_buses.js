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
    var events = project.model.Event.findInstances();
    var routed = { Music: 0, SFX: 0 };
    for (var i = 0; i < events.length; i++) {
        var event = events[i];
        var target = event.getPath().indexOf("event:/Music/") == 0 ? music : sfx;
        if (event.mixerInput.output !== target)
            event.mixerInput.output = target;
        routed[target.name]++;
    }
    if (!Params.dryRun)
        project.save();
    result = "ok " + routed.Music + " events on bus:/Music, " + routed.SFX + " on bus:/SFX" + (Params.dryRun ? " (dry run, not saved)" : "");
} catch (e) {
    result = "error " + e;
}
console.log("FMOD_ROUTE: " + result);
