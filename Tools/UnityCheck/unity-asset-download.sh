#!/usr/bin/env bash
# Downloads purchased Asset Store packages (from the account signed in to the open Unity Editor) into the editor's
# Asset Store cache, the same as Package Manager > My Assets > Download, then waits and prints each package's path.
# It drives the editor's internal AssetStoreDownloadManager by reflection (the unity CLI has no download command).
# The product id is the number at the end of the package's Asset Store URL. Importing is a separate step:
# `unity assets import <path>` or an AssetDatabase.ImportPackage eval, after adding the pack to the Asset Ledger.
#
# Usage: Tools/UnityCheck/unity-asset-download.sh <productId> [productId...]
# Exits 0 when every package finished, 1 on a download error, 2 when the editor can't be reached or it timed out.

set -u
cd "$(dirname "$0")/../.."
source Tools/UnityCheck/common.sh

(( $# > 0 )) || { echo "Usage: unity-asset-download.sh <productId> [productId...]"; exit 2; }
ids=$(printf '%sL, ' "$@")
ids=${ids%, }
mkdir -p Temp/AssetDownload
run_mode() {
    sed "s/PRODUCT_IDS/$ids/; s/MODE/$1/" Tools/UnityCheck/evals/asset-download.cs > Temp/AssetDownload/eval.cs
    Tools/UnityCheck/unity-long-eval.sh Temp/AssetDownload/eval.cs 60
}

run_mode start >/dev/null || { echo "Could not reach the Unity Editor."; exit 2; }
start=$SECONDS
while (( SECONDS - start < 1800 )); do
    status=$(run_mode status) || exit 2
    if grep -q "Error" <<<"$status"; then
        echo "$status"
        exit 1
    fi
    if ! grep -q "Downloading\|Connecting\|Decrypting\|InProgress" <<<"$status"; then
        echo "$status"
        exit 0
    fi
    sleep 5
done
echo "Timed out; last status:"
echo "$status"
exit 2
