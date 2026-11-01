# Scans runtime C# and UXML under Assets/Game for player-facing text that bypasses the string tables:
# string literals with letters assigned to .text / text=, passed to new Label(...) / Button text, or in UXML text="..."
# attributes. Skips Editor and Tests folders, *DebugCommands.cs, the Debug UI and the benchmark (developer-only text).
# Prints file:line: snippet for each hit and exits 1 when anything is found, 0 when clean.
#
# Usage: python -I Tools/loc-scan.py
import os
import re
import sys

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "Game"))
SKIP_DIRS = {"Editor", "Tests", "Debug", "Benchmark"}
CS_PATTERNS = [
    re.compile(r'\.text\s*=\s*\$?"([^"]*[A-Za-z][^"]*)"'),
    re.compile(r'\btext\s*=\s*\$?"([^"]*[A-Za-z][^"]*)"'),
    re.compile(r'new\s+Label\(\s*\$?"([^"]*[A-Za-z][^"]*)"'),
]
UXML_PATTERN = re.compile(r'\btext="([^"]*[A-Za-z][^"]*)"')


def skipped(path):
    parts = set(os.path.relpath(path, ROOT).split(os.sep))
    return bool(parts & SKIP_DIRS) or path.endswith("DebugCommands.cs")


def main():
    hits = []
    for folder, dirs, files in os.walk(ROOT):
        dirs[:] = [d for d in dirs if d not in SKIP_DIRS]
        for name in files:
            path = os.path.join(folder, name)
            if skipped(path) or not (name.endswith(".cs") or name.endswith(".uxml")):
                continue
            patterns = CS_PATTERNS if name.endswith(".cs") else [UXML_PATTERN]
            with open(path, encoding="utf-8", errors="replace") as handle:
                for number, line in enumerate(handle, 1):
                    for pattern in patterns:
                        match = pattern.search(line)
                        if match:
                            hits.append(f"{os.path.relpath(path, os.path.dirname(ROOT))}:{number}: {line.strip()}")
                            break
    for hit in hits:
        print(hit)
    print(f"{len(hits)} hard-coded string(s)" if hits else "clean")
    return 1 if hits else 0


if __name__ == "__main__":
    sys.exit(main())
