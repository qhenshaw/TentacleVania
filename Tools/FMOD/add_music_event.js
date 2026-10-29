var result;
try {
    var project = studio.project;
    if (project.lookup(Params.eventPath))
        throw "event already exists: " + Params.eventPath;
    var bank = project.lookup("bank:/" + Params.bank);
    if (!bank)
        throw "no bank named " + Params.bank;

    var parts = Params.eventPath.replace(/^event:\//, "").split("/");
    var name = parts.pop();
    var folder = project.workspace.masterEventFolder;
    var path = "event:";
    for (var i = 0; i < parts.length; i++) {
        path += "/" + parts[i];
        var next = project.lookup(path);
        if (!next) {
            next = project.create("EventFolder");
            next.name = parts[i];
            next.folder = folder;
        }
        folder = next;
    }

    var event = project.create("Event");
    event.name = name;
    event.folder = folder;

    var preset = project.lookup("parameter:/" + Params.parameter);
    var parameter = preset ? preset.parameter : project.workspace.addGameParameter({ name: Params.parameter, type: project.parameterType.User, min: 0, max: 1 });
    if (!parameter)
        throw "could not create parameter " + Params.parameter;
    event.addGameParameter(preset ? preset : parameter);

    var longest = 0;
    var assets = [];
    for (var a = 0; a < Params.audioFiles.length; a++) {
        var asset = project.importAudioFile(Params.audioFiles[a]);
        if (!asset)
            throw "could not import " + Params.audioFiles[a];
        assets.push(asset);
        if (asset.length > longest)
            longest = asset.length;
    }

    for (var s = 0; s < assets.length; s++) {
        var track = event.addGroupTrack(Params.trackNames[s]);
        var sound = track.addSound(event.timeline, "SingleSound", 0, longest);
        sound.audioFile = assets[s];
        var fade = Params.fades[s];
        if (fade) {
            var automator = track.mixerGroup.addAutomator("volume");
            var curve = automator.addAutomationCurve(parameter);
            curve.addAutomationPoint(0, -80);
            curve.addAutomationPoint(fade[0], -80);
            curve.addAutomationPoint(fade[1], 0);
            curve.addAutomationPoint(1, 0);
        }
    }

    var region = project.create("LoopRegion");
    region.position = 0;
    region.length = longest;
    region.timeline = event.timeline;
    region.markerTrack = event.markerTracks[0];

    event.relationships.banks.add(bank);
    var musicBus = project.lookup("bus:/Music");
    if (musicBus)
        event.mixerInput.output = musicBus;
    if (!Params.dryRun)
        project.save();
    result = "ok " + event.getPath() + " " + assets.length + " stems, " + longest.toFixed(2) + " s loop, parameter " + Params.parameter + (Params.dryRun ? " (dry run, not saved)" : "");
} catch (e) {
    result = "error " + e;
}
console.log("FMOD_MUSIC: " + result);
