# Extracts selected assets from a .unitypackage without importing the whole pack: copies each matching asset with its
# .meta (so GUIDs and import settings match the original) into a project folder, keeping the path below a prefix.
# Use it for purchased packs too large to import whole; Unity imports the copied files on the next refresh.
#
# Usage: python -I Tools/unitypackage-extract.py <package> --strip <source prefix> --dest <Assets/...> <regex>...
#        python -I Tools/unitypackage-extract.py <package> --list [regex]     (prints matching paths and sizes)
# Every regex is matched against the asset path inside the package (e.g. Assets/SomePack/Models/Prop_01.fbx).
# Folders are created as needed; their .meta files come from the package when it has them.
# --raw <dir> writes the matching assets without .meta into <dir> instead (for source textures to resize).

import argparse
import os
import re
import sys
import tarfile


def read_index(package):
    paths = {}
    sizes = {}
    with tarfile.open(package, "r:gz") as archive:
        for member in archive:
            guid, _, name = member.name.partition("/")
            if name == "pathname":
                paths[guid] = archive.extractfile(member).read().decode("utf-8").splitlines()[0].strip()
            elif name == "asset":
                sizes[guid] = member.size
    return paths, sizes


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("package")
    parser.add_argument("patterns", nargs="*")
    parser.add_argument("--list", action="store_true")
    parser.add_argument("--strip", default="")
    parser.add_argument("--dest", default="")
    parser.add_argument("--raw", default="")
    args = parser.parse_args()

    paths, sizes = read_index(args.package)
    patterns = [re.compile(p) for p in args.patterns]
    wanted = {g: p for g, p in paths.items() if not patterns or any(r.search(p) for r in patterns)}

    if args.list:
        for guid, path in sorted(wanted.items(), key=lambda item: item[1]):
            print(f"{path}\t{sizes.get(guid, 0)}")
        return 0

    if not args.raw and (not args.dest or not args.strip):
        print("--dest and --strip are required unless --list or --raw", file=sys.stderr)
        return 2

    folders = {p: g for g, p in paths.items() if g not in sizes}
    for path in list(wanted.values()):
        parent = os.path.dirname(path)
        while parent.startswith(args.strip) and len(parent) > len(args.strip.rstrip("/")):
            if parent in folders:
                wanted[folders[parent]] = parent
            parent = os.path.dirname(parent)

    def target(path):
        if args.raw:
            return os.path.join(args.raw, os.path.basename(path))
        return os.path.join(args.dest, path[len(args.strip):].lstrip("/"))

    written = 0
    with tarfile.open(args.package, "r:gz") as archive:
        for member in archive:
            guid, _, name = member.name.partition("/")
            if guid not in wanted or name not in ("asset", "asset.meta"):
                continue
            if args.raw and name == "asset.meta":
                continue
            path = target(wanted[guid]) + (".meta" if name == "asset.meta" else "")
            os.makedirs(os.path.dirname(path), exist_ok=True)
            with open(path, "wb") as out:
                out.write(archive.extractfile(member).read())
            written += 1
    for guid, path in wanted.items():
        if guid not in sizes and not args.raw:
            os.makedirs(target(path), exist_ok=True)
    print(f"wrote {written} files for {len(wanted)} assets")
    return 0


if __name__ == "__main__":
    sys.exit(main())
