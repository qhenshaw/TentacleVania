# Exports every Unity Localization string table collection to CSV through the open editor (the Localization
# package's CSV exporter: key, id and one column per locale), one file per table, for translators.
# Writes a C# eval to Temp/loc-export.cs and runs it with Tools/UnityCheck/unity-long-eval.sh.
#
# Usage: python -I Tools/loc-export.py [output folder]      (default Builds/Localization; prints the files written)
import os
import subprocess
import sys

EVAL = r'''
var folder = System.IO.Path.GetFullPath(@"{folder}");
System.IO.Directory.CreateDirectory(folder);
var written = "";
foreach (var collection in UnityEditor.Localization.LocalizationEditorSettings.GetStringTableCollections())
{{
    var path = System.IO.Path.Combine(folder, collection.TableCollectionName + ".csv");
    using (var writer = new System.IO.StreamWriter(path, false, new System.Text.UTF8Encoding(false)))
        UnityEditor.Localization.Plugins.CSV.Csv.Export(writer, collection);
    written += path + "\n";
}}
return written;
'''


def main():
    folder = sys.argv[1] if len(sys.argv) > 1 else os.path.join("Builds", "Localization")
    root = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
    os.chdir(root)
    eval_path = os.path.join("Temp", "loc-export.cs")
    with open(eval_path, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(EVAL.format(folder=os.path.abspath(folder).replace("\\", "/")))
    return subprocess.call(["bash", "Tools/UnityCheck/unity-long-eval.sh", eval_path, "120"])


if __name__ == "__main__":
    sys.exit(main())
