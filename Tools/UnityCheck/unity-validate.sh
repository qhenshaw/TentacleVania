#!/usr/bin/env bash
# Validates game content in the open editor through Assets/Editor/ContentValidator.cs (also on the menu
# TentacleVania > Validate Content): room scenes have NavMesh assets and Addressables entries, shipped sector
# registries are in MapAtlas, arena waves have prefabs, NameKey/TextKey strings exist in a string table, FMOD
# EventReferences resolve, prefabs have no missing scripts, runtime code avoids ParticleSystem, AudioSource,
# MaterialPropertyBlock and Shader.Find, and every MonoBehaviour/ScriptableObject has its own file. task-finish.sh
# runs it. Warnings (an enemy missing from the debug spawn list) print but don't fail.
#
# Usage: Tools/UnityCheck/unity-validate.sh [timeout-seconds]   (default 300)
# Exits 0 when there are no errors, 1 when there are, 2 when Unity can't be reached or the run failed.

set -u
source "$(dirname "$0")/common.sh"
timeout="${1:-300}"

wait_idle 180 || exit 2
mkdir -p Temp
echo 'return ContentValidator.Run();' > Temp/unity-validate.cs
out=$(bash Tools/UnityCheck/unity-long-eval.sh Temp/unity-validate.cs "$timeout" 2>&1)
code=$?
echo "$out"
(( code != 0 )) && exit 2
grep -q '^Content validation:' <<<"$out" || exit 2
grep -q '^ERROR ' <<<"$out" && exit 1
exit 0
