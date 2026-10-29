#!/usr/bin/env bash
# Finishes a task the way CLAUDE.md's Git workflow requires: checks compilation and shaders, runs the edit-mode tests,
# stages everything, moves large files into LFS, commits, and on a feature branch merges into main with --no-ff,
# deletes the branch, and pushes main. On main it commits and pushes. Stops at the first failing step. It pulls main
# before merging, since other agents push to it; in an agent worktree it merges onto origin/main detached and pushes
# HEAD:main, retrying when another agent pushed first. A merge conflict aborts and leaves you on the branch.
#
# Paths listed in <git common dir>/info/keep-local (one per line; local tweaks such as a renderer setting) are
# never staged, so they stay uncommitted in the working tree.
#
# Usage: Tools/task-finish.sh "<Imperative subject (TV-NN)>" ["optional body"]
# Exits 0 when pushed, 1 when a check failed (nothing committed), 2 when Unity can't be reached.

set -u
cd "$(dirname "$0")/.."
subject="${1:?Usage: Tools/task-finish.sh \"<Imperative subject (TV-NN)>\" [\"body\"]}"
body="${2:-}"
trailer="Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"

bash Tools/UnityCheck/unity-check.sh || exit $?
bash Tools/UnityCheck/unity-test.sh || exit $?

git add -A 2>&1 | grep -v "LF will be replaced" || true
keep="$(git rev-parse --git-common-dir)/info/keep-local"
if [[ -f "$keep" ]]; then
    while IFS= read -r path; do
        [[ -n "$path" && "$path" != \#* ]] && git reset -q -- "$path" 2>/dev/null
    done < <(tr -d '\r' < "$keep")
fi
bash Tools/lfs-check.sh --fix || exit 1
if git diff --cached --quiet; then
    echo "Nothing to commit."
else
    message="$subject"
    [[ -n "$body" ]] && message+=$'\n\n'"$body"
    message+=$'\n\n'"$trailer"
    git commit -q -F - <<<"$message" || exit 1
    echo "Committed $(git rev-parse --short HEAD): $subject"
fi

branch=$(git branch --show-current)
has_origin() { git remote get-url origin >/dev/null 2>&1; }
push_main() {
    has_origin || { echo "No origin remote; main is at $(git rev-parse --short HEAD), not pushed."; return; }
    git push -q 2>&1 | grep -v "^Uploading LFS\|^$" || true
    echo "Pushed main at $(git rev-parse --short HEAD)."
}
[[ "$branch" == main ]] && { push_main; exit 0; }

task=$(grep -o 'TV-[0-9]*' <<<"$subject" | head -1)
message="Merge $branch${task:+ ($task)}"$'\n\n'"$trailer"

merge_conflict() {
    git merge --abort 2>/dev/null
    git switch -q "$branch"
    echo "Merging $branch into the latest main conflicts. Merge origin/main into $branch, resolve, check and rerun."
    exit 1
}

if [[ "$(git rev-parse --git-dir)" != "$(git rev-parse --git-common-dir)" ]]; then
    for attempt in 1 2 3; do
        git fetch -q origin || exit 1
        git switch -q --detach origin/main || exit 1
        git merge -q --no-ff "$branch" -m "$message" || merge_conflict
        if git push -q origin HEAD:main 2>/dev/null; then
            git branch -D "$branch" >/dev/null
            echo "Merged $branch into main."
            echo "Pushed main at $(git rev-parse --short HEAD) (this worktree stays detached on it)."
            exit 0
        fi
        git switch -q "$branch"
    done
    echo "Pushing to main was rejected three times (other agents pushing); rerun."
    exit 1
fi

git checkout -q main || exit 1
if has_origin; then git pull -q --ff-only || exit 1; fi
git merge -q --no-ff "$branch" -m "$message" || merge_conflict
git branch -d "$branch" >/dev/null || exit 1
echo "Merged $branch into main."
push_main
