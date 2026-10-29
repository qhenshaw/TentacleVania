#!/usr/bin/env bash
# Creates an agent worktree for parallel work: a sibling folder ../<ProjectName>-<name> checked out (detached) at the
# latest origin/main, sharing this repository and its LFS objects, then opens it in its own Unity Editor. The first
# open imports the project from scratch (its Library is separate), which takes a while. Start a Claude Code session in
# that folder afterwards; the Tools scripts there drive that folder's editor.
#
# Usage: Tools/worktree-add.sh <name>   (e.g. agent2)
# Remove one with: git worktree remove ../<ProjectName>-<name> (close its editor first).
# Exits 0 when the worktree exists and its editor is starting, 1 on failure.

set -eu
cd "$(dirname "$0")/.."
source Tools/project.sh
name="${1:?Usage: Tools/worktree-add.sh <name>}"
path="$(cd .. && pwd)/$PROJECT_NAME-$name"

if [[ ! -d "$path" ]]; then
    git fetch -q origin
    git worktree add -q --detach "$path" origin/main
    git -C "$path" update-index --skip-worktree .vscode/settings.json
    echo "Created worktree $path at $(git -C "$path" rev-parse --short HEAD)."
else
    echo "Worktree $path already exists."
fi

unity projects open "$(cygpath -w "$path")" --non-interactive >/dev/null
echo "Opening $path in Unity (first import takes several minutes)."
