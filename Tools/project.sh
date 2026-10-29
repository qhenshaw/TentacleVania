#!/usr/bin/env bash
# Project settings shared by the Tools scripts. Sourced by Tools/UnityCheck/common.sh and the scripts that don't use it;
# fill in the placeholders once when importing the tools into a project.

# Product name: the player exe (Builds/<label>/<name>.exe), build zip names, the Drive folder, agent worktree folders.
PROJECT_NAME="TentacleVania"

# Root C# namespace and assembly prefix (<RootNamespace>.<Area>); unity-test.sh runs only tests under it by default.
ROOT_NAMESPACE="TentacleVania"

# Scene the gameplay checks (GC profile, regression sweep) open, and the entry scene the full-flow check opens.
GAMEPLAY_TEST_SCENE="Assets/Game/Scenes/CombatArena.unity"
BOOTSTRAP_SCENE="Assets/Game/Scenes/Bootstrap.unity"

# Build upload destination, relative to "My Drive" (Google Drive for Desktop) or the rclone remote's root.
# BUILD_DRIVE_DIR (an absolute folder) and BUILD_RCLONE_REMOTE (default gdrive) override it from the environment.
BUILD_UPLOAD_FOLDER="Builds/$PROJECT_NAME"
