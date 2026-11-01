#!/usr/bin/env bash
# Enters play mode from the open scene, lets it run in real time to a frame count (Player Settings' Run In Background
# and the editor's Interaction Mode set to No Throttling keep play mode running while the editor is unfocused; if frames
# stall anyway it steps a few and resumes, never leaving play paused), captures the Game view to a PNG outside Assets/, leaves play mode and prints the PNG path. Read the PNG
# to check what the game looks like at runtime; unity-capture.sh only sees the Scene view. Play runs at the editor's 60 FPS cap,
# so 60 frames is about a second, and --at files fire within a few frames of their target (polled every 0.3 s).
#
# Usage: Tools/UnityCheck/unity-play-capture.sh [--frames 150] [--w 960] [--h 540] [--setup file.cs] [--at <frame> file.cs]... [out.png]
#   --setup  a C# eval file (statements ending in `return <value>;`) run once at frame 20, e.g. to turn on auto-fire;
#            its result is printed
#   --at     runs another eval file once the given frame is reached (repeatable, in order), e.g. to pause, resume and
#            then read state
#   out.png defaults to Temp/Captures/play-<timestamp>.png at the project root
# Refreshes the asset database first, so edited USS, UXML and scripts are current.
# Exits 0 with the path, 2 when Unity can't be reached, play mode didn't start or the capture failed.

set -u
source "$(dirname "$0")/common.sh"

frames=150 width=960 height=540 out=""
steps=()
while (( $# )); do
    case "$1" in
        --frames) frames="$2"; shift ;;
        --w) width="$2"; shift ;;
        --h) height="$2"; shift ;;
        --setup) steps+=("20" "$2"); shift ;;
        --at) steps+=("$2" "$3"); shift 2 ;;
        -*) echo "Unknown option: $1"; exit 2 ;;
        *) out="$1" ;;
    esac
    shift
done
[[ -z "$out" ]] && out="Temp/Captures/play-$(date +%Y%m%d-%H%M%S).png"
mkdir -p Temp/Captures

if [[ "$(eval_result 'return UnityEditor.EditorApplication.isPlaying;' 2>/dev/null)" == true ]]; then
    echo "Unity is already in play mode; stop it first."
    exit 2
fi
eval_result 'UnityEditor.AssetDatabase.Refresh(); return 0;' >/dev/null 2>&1
wait_idle 180 || exit 2
echo "Playing $(eval_result 'return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;' 2>/dev/null)"

trap 'eval_result "UnityEditor.EditorApplication.isPaused = false; return 0;" >/dev/null 2>&1; "${Unity[@]}" editor.playmode stop >/dev/null 2>&1; wait_until 30 "!UnityEditor.EditorApplication.isPlaying" >/dev/null 2>&1; eval_result "PlayFromMainMenu.Bypass = false; return 0;" >/dev/null 2>&1; rm -rf Assets/Temp Assets/Temp.meta' EXIT
eval_result 'UnityEditor.EditorPrefs.SetInt("InteractionMode", 1); PlayFromMainMenu.Bypass = true; return 0;' >/dev/null 2>&1
"${Unity[@]}" editor.playmode start >/dev/null 2>&1

start=$SECONDS frame=-1 next=0 lastFrame=-1 stalledSince=$SECONDS
limit=$(( frames / 30 + 120 ))
(( limit < 300 )) && limit=300
while (( SECONDS - start < limit )); do
    if (( frame > lastFrame )); then lastFrame=$frame; stalledSince=$SECONDS; fi
    if (( frame >= 1 && SECONDS - stalledSince >= 3 )); then
        frame=$(eval_result 'if (!UnityEditor.EditorApplication.isPlaying) return -1; UnityEditor.EditorApplication.isPaused = true; for (int i = 0; i < 10; i++) UnityEditor.EditorApplication.Step(); UnityEditor.EditorApplication.isPaused = false; return UnityEngine.Time.frameCount;' 2>/dev/null)
    else
        frame=$(eval_result 'if (!UnityEditor.EditorApplication.isPlaying) return -1; return UnityEngine.Time.frameCount;' 2>/dev/null)
    fi
    while (( next < ${#steps[@]} )) && (( ${frame:--1} >= ${steps[next]} )); do
        file=${steps[next+1]}
        wrapped="Temp/Captures/at-$(basename "$file")"
        { echo 'try {'; cat "$file"; echo; echo '} catch (System.Exception e) { return "EXCEPTION " + e.GetType().Name + ": " + e.Message + " | " + e.StackTrace; }'; } > "$wrapped"
        result=$("${Unity[@]}" eval_file --file "$(cygpath -m "$wrapped")" 2>&1)
        value="$(json_string result <<<"$result")$(json_number result <<<"$result")"
        [[ -z "$value" ]] && value="ERROR $(json_string message <<<"$result")"
        echo "Frame $frame $(basename "$file"): $value"
        next=$((next + 2))
    done
    (( ${frame:--1} >= frames )) && break
    sleep 0.3
done
if (( ${frame:--1} < frames )); then
    echo "Play mode didn't reach frame $frames (last frame ${frame:--1})."
    exit 2
fi

"${Unity[@]}" capture game_view --width "$width" --height "$height" --save_path Temp/unity-play-capture.png >/dev/null 2>&1
if [[ ! -f Assets/Temp/unity-play-capture.png ]]; then
    echo "Game view capture failed."
    exit 2
fi
mkdir -p "$(dirname "$out")"
mv -f Assets/Temp/unity-play-capture.png "$out"
echo "$out"
