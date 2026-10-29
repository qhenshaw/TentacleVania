#!/usr/bin/env bash
# Zips a Windows player from Builds/<label>/ into Builds/<ProjectName>-<version>.zip and uploads it to the developer's
# Google Drive folder BUILD_UPLOAD_FOLDER (Tools/project.sh, under My Drive). The version comes from the version.txt
# that GameVersionBuild writes next to the exe ("v0.<commit count> (<hash>)"); the zip is named
# <ProjectName>-v0.<count>.zip, with -release added for a non-development build. Leaves out the *DoNotShip / *DontShip
# folders and the smoke-test log and screenshot. The local zip is deleted after a successful upload and kept otherwise.
#
# Upload routes, first available wins:
#   1. Google Drive for Desktop: copies into "<drive letter>:/My Drive/<BUILD_UPLOAD_FOLDER>/" and lets it sync.
#      Set BUILD_DRIVE_DIR to use another destination folder.
#   2. rclone: `rclone copyto` to <remote>:<BUILD_UPLOAD_FOLDER>/ (remote name from BUILD_RCLONE_REMOTE,
#      default gdrive; set it up once with `rclone config`).
#
# Usage: Tools/UnityCheck/unity-build-upload.sh <label> [--release] [--zip-only]
# Exits 0 when uploaded (or zipped with --zip-only), 1 when zipping or the upload failed, 2 when no upload route is set up.

set -u
cd "$(dirname "$0")/../.."
source Tools/project.sh

label=""
suffix=""
zip_only=0
for arg in "$@"; do
    case "$arg" in
        --release) suffix="-release" ;;
        --zip-only) zip_only=1 ;;
        *) label="$arg" ;;
    esac
done
[[ -z "$label" ]] && { echo "Usage: unity-build-upload.sh <label> [--release] [--zip-only]"; exit 1; }
dir="Builds/$label"
if [[ ! -f "$dir/$PROJECT_NAME.exe" ]]; then
    echo "No build at $dir/$PROJECT_NAME.exe."
    exit 1
fi

version=$(sed -n 's/^\(v[0-9][0-9.]*\).*/\1/p' "$dir/version.txt" 2>/dev/null | head -1)
[[ -z "$version" ]] && version="v0.$(git rev-list --count HEAD)"
name="$PROJECT_NAME-$version$suffix.zip"
zip="Builds/$name"

python -I - "$dir" "$zip" "$PROJECT_NAME" <<'EOF' || { echo "Zipping $dir failed."; exit 1; }
import os, sys, zipfile
src, dst, top = sys.argv[1], sys.argv[2], sys.argv[3]
skip_files = {"smoke.log", "smoke.png"}
with zipfile.ZipFile(dst + ".part", "w", zipfile.ZIP_DEFLATED, compresslevel=6) as z:
    for root, dirs, files in os.walk(src):
        dirs[:] = [d for d in dirs if "DoNotShip" not in d and "DontShip" not in d]
        for f in files:
            if root == src and f in skip_files:
                continue
            path = os.path.join(root, f)
            z.write(path, os.path.join(top, os.path.relpath(path, src)))
os.replace(dst + ".part", dst)
EOF
echo "Zipped $dir into $zip ($(du -h "$zip" | cut -f1))."
(( zip_only )) && exit 0

drive_dir="${BUILD_DRIVE_DIR:-}"
if [[ -z "$drive_dir" ]]; then
    for letter in G H I J K L M N O P Q R S T U V W X Y Z; do
        if [[ -d "/${letter,,}/My Drive" ]]; then
            drive_dir="/${letter,,}/My Drive/$BUILD_UPLOAD_FOLDER"
            break
        fi
    done
fi
if [[ -n "$drive_dir" ]]; then
    mkdir -p "$drive_dir" && cp "$zip" "$drive_dir/$name" || { echo "Copying to $drive_dir failed."; exit 1; }
    rm -f "$zip"
    echo "Copied $name to $drive_dir; Google Drive for Desktop uploads it in the background."
    exit 0
fi

remote="${BUILD_RCLONE_REMOTE:-gdrive}"
if command -v rclone >/dev/null 2>&1 && rclone listremotes 2>/dev/null | grep -qx "$remote:"; then
    rclone copyto "$zip" "$remote:$BUILD_UPLOAD_FOLDER/$name" --stats-one-line --stats 30s || { echo "rclone upload failed."; exit 1; }
    rm -f "$zip"
    echo "Uploaded $name to $remote:$BUILD_UPLOAD_FOLDER/."
    exit 0
fi

echo "No Google Drive upload route: sign in to Google Drive for Desktop (it mounts G:/My Drive), or install rclone and"
echo "run 'rclone config' to add a Google Drive remote named $remote. The zip is at $zip."
exit 2
