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

    var asset = project.importAudioFile(Params.audioFile);
    var event = project.create("Event");
    event.name = name;
    event.folder = folder;
    var track = event.addGroupTrack("Audio 1");
    var sound = track.addSound(event.timeline, "SingleSound", 0, asset.length);
    sound.audioFile = asset;
    if (Params.loop) {
        var region = project.create("LoopRegion");
        region.position = 0;
        region.length = asset.length;
        region.timeline = event.timeline;
        region.markerTrack = event.markerTracks[0];
    }
    event.relationships.banks.add(bank);
    var sfxBus = project.lookup("bus:/SFX");
    if (sfxBus)
        event.mixerInput.output = sfxBus;

    project.save();
    result = "ok " + event.getPath();
} catch (e) {
    result = "error " + e;
}
console.log("FMOD_PLACEHOLDER: " + result);
