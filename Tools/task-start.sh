#!/usr/bin/env bash
# Starts work on a task: requires a clean working tree, updates main from GitHub, and creates and switches to a new
# feature branch from it. Small fixes that stay on main don't need this. In an agent worktree (see Tools/worktree-add.sh)
# main is checked out elsewhere, so the branch starts from origin/main instead.
#
# Usage: Tools/task-start.sh <feature/short-name> [--carry]
# --carry stashes uncommitted work (untracked files included), branches from the updated main and restores it there,
# for when edits started before the branch did.
# Paths listed in <git common dir>/info/keep-local don't count as uncommitted changes (see task-finish.sh).
# Exits 0 on the new branch, 1 when the tree is dirty (without --carry) or the branch already exists.

set -eu
cd "$(dirname "$0")/.."
branch="${1:?Usage: Tools/task-start.sh <feature/short-name> [--carry]}"
carry="${2:-}"
stashed=0

keep="$(git rev-parse --git-common-dir)/info/keep-local"
dirty="$(git status --porcelain)"
if [[ -f "$keep" && -n "$dirty" ]]; then
    dirty="$(grep -v -F -f <(tr -d '\r' < "$keep" | grep -v '^#' | grep -v '^$') <<<"$dirty" || true)"
fi
if [[ -n "$dirty" && "$carry" == --carry ]]; then
    git stash push -q -u -m "task-start carry"
    stashed=1
elif [[ -n "$dirty" ]]; then
    echo "The working tree has uncommitted changes; commit or stash them first."
    git status --short | head -20
    exit 1
fi
if git show-ref --verify --quiet "refs/heads/$branch"; then
    echo "Branch $branch already exists."
    if (( stashed )); then
        git stash pop -q
    fi
    exit 1
fi

if [[ "$(git rev-parse --git-dir)" != "$(git rev-parse --git-common-dir)" ]]; then
    git fetch -q origin
    git switch -q -c "$branch" --no-track origin/main
else
    git checkout -q main
    git remote get-url origin >/dev/null 2>&1 && git pull -q --ff-only
    git checkout -q -b "$branch"
fi
if (( stashed )); then
    git stash pop -q
fi
echo "On $branch (from main $(git rev-parse --short HEAD))."
