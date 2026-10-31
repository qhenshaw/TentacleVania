# Adds or updates English entries in a Unity Localization string table through the open editor.
# Reads a UTF-8 file of "key<TAB>value" lines (blank lines and lines starting with # are skipped), writes a C# eval to
# Temp/loc-add.cs and runs it with Tools/UnityCheck/unity-long-eval.sh, which saves the table and its shared data.
#
# Usage: python -I Tools/loc-add.py <table> <entries.tsv>      (table: UI, Dialogue, ...)
# Prints "added N, updated M" from the editor. Existing keys get the new value.
import os
import subprocess
import sys


def main():
    if len(sys.argv) != 3:
        print(__doc__ or "Usage: python -I Tools/loc-add.py <table> <entries.tsv>")
        return 2
    table, source = sys.argv[1], sys.argv[2]
    entries = []
    with open(source, encoding="utf-8") as handle:
        for line in handle:
            line = line.rstrip("\r\n")
            if not line.strip() or line.startswith("#"):
                continue
            key, _, value = line.partition("\t")
            entries.append((key.strip(), value))
    if not entries:
        print("No entries")
        return 1

    def literal(text):
        return '@"' + text.replace('"', '""') + '"'

    pairs = ", ".join(f"{literal(k)}, {literal(v)}" for k, v in entries)
    code = f"""var collection = UnityEditor.Localization.LocalizationEditorSettings.GetStringTableCollection({literal(table)});
if (collection == null) return "no table {table}";
var locale = UnityEngine.Localization.Settings.LocalizationSettings.ProjectLocale ?? UnityEditor.Localization.LocalizationEditorSettings.GetLocales()[0];
var stringTable = (UnityEngine.Localization.Tables.StringTable)collection.GetTable(locale.Identifier);
var pairs = new string[] {{ {pairs} }};
var added = 0; var updated = 0;
for (var i = 0; i < pairs.Length; i += 2)
{{
    var entry = stringTable.GetEntry(pairs[i]);
    if (entry == null) {{ stringTable.AddEntry(pairs[i], pairs[i + 1]); added++; }}
    else {{ entry.Value = pairs[i + 1]; updated++; }}
}}
UnityEditor.EditorUtility.SetDirty(stringTable);
UnityEditor.EditorUtility.SetDirty(stringTable.SharedData);
UnityEditor.AssetDatabase.SaveAssets();
return "added " + added + ", updated " + updated;
"""
    root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    path = os.path.join(root, "Temp", "loc-add.cs")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as handle:
        handle.write(code)
    return subprocess.call(["bash", "Tools/UnityCheck/unity-long-eval.sh", "Temp/loc-add.cs", "120"], cwd=root)


if __name__ == "__main__":
    sys.exit(main())
