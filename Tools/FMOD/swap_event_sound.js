var result;
try {
    var project = studio.project;
    var event = project.lookup(Params.eventPath);
    if (!event)
        throw "no event " + Params.eventPath;
    var track = Params.layer ? event.addGroupTrack("Layer " + (event.groupTracks.length + 1)) : event.groupTracks[Params.track - 1];
    if (!track)
        throw "event has no group track " + Params.track;

    var assets = [];
    var longest = 0;
    for (var i = 0; i < Params.audioFiles.length; i++) {
        var asset = project.importAudioFile(Params.audioFiles[i]);
        if (!asset)
            throw "could not import " + Params.audioFiles[i];
        assets.push(asset);
        if (asset.length > longest)
            longest = asset.length;
    }

    var old = Params.layer ? [] : track.modules.slice();
    for (var j = 0; j < old.length; j++)
        project.deleteObject(old[j]);

    var sound;
    if (assets.length == 1) {
        sound = track.addSound(event.timeline, "SingleSound", 0, longest);
        sound.audioFile = assets[0];
    } else {
        sound = track.addSound(event.timeline, "MultiSound", 0, longest);
        for (var k = 0; k < assets.length; k++) {
            var entry = project.create("SingleSound");
            entry.audioFile = assets[k];
            sound.relationships.sounds.add(entry);
        }
    }
    if (Params.volume !== null)
        sound.volume = Params.volume;

    var markerTracks = Params.layer ? [] : event.markerTracks || [];
    for (var t = 0; t < markerTracks.length; t++) {
        var markers = markerTracks[t].markers || [];
        for (var m = 0; m < markers.length; m++)
            if (markers[m].entity == "LoopRegion")
                markers[m].length = longest;
    }

    if (!Params.dryRun)
        project.save();
    result = "ok " + event.getPath() + " " + sound.entity + " x" + assets.length + " " + longest.toFixed(2) + " s" + (Params.dryRun ? " (dry run, not saved)" : "");
} catch (e) {
    result = "error " + e;
}
console.log("FMOD_SWAP: " + result);
