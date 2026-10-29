#!/usr/bin/env bash
# Finds staged files at or above a size limit that Git LFS doesn't track, so they don't get pushed as regular Git
# objects (GitHub warns above 50 MB and rejects files above 100 MB). With --fix it adds a .gitattributes rule for
# each one and restages it through LFS: an extension rule (*.ext) for binary types, or an exact-path rule for Unity
# YAML files and files without an extension, since those extensions also cover small text assets.
#
# Usage: Tools/lfs-check.sh [--fix] [limit-MB]   (default 10)
# Exits 0 when nothing needs LFS (or --fix handled it), 1 when files need LFS and --fix wasn't given.

set -u
cd "$(dirname "$0")/.."

limitMB=10 fix=false
for arg in "$@"; do
    case "$arg" in
        --fix) fix=true ;;
        [0-9]*) limitMB="$arg" ;;
        *) echo "Unknown option: $arg"; exit 2 ;;
    esac
done
limit=$(( limitMB * 1024 * 1024 ))

attr() { git check-attr "$1" -- "$2" | sed 's/.*: //'; }

found=false changed=false
while IFS= read -r -d '' file; do
    [[ -f "$file" ]] || continue
    size=$(stat -c %s "$file")
    (( size < limit )) && continue
    [[ "$(attr filter "$file")" == lfs ]] && continue
    found=true

    name=$(basename "$file")
    if [[ "$name" != *.* || "$(attr merge "$file")" == unityyamlmerge ]]; then
        pattern="/${file// /[[:space:]]}"
    else
        pattern="*.${name##*.}"
    fi
    echo "$file ($(( size / 1024 / 1024 )) MB) is not in LFS -> $pattern"

    if $fix; then
        rule="$pattern filter=lfs diff=lfs merge=lfs -text"
        grep -qxF "$rule" .gitattributes || { echo "$rule" >>.gitattributes; changed=true; }
        git rm --cached -q -- "$file"
        git add -- "$file"
    fi
done < <(git diff --cached --name-only -z --diff-filter=AM)

if $changed; then
    git add .gitattributes
    git diff --cached --name-only -z --diff-filter=AM | xargs -0 git add --renormalize --
fi

if ! $found; then
    echo "No staged files of ${limitMB} MB or more outside LFS."
elif $fix; then
    echo "Added the rules above to .gitattributes and restaged the files through LFS."
else
    echo "Run again with --fix to move them into LFS."
    exit 1
fi
exit 0
