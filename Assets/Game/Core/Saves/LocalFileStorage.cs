using System.IO;
using System.Text;

namespace TentacleVania.Core.Saves
{
    public sealed class LocalFileStorage : ISaveStorage
    {
        private readonly string _folder;

        public LocalFileStorage(string folder) => _folder = folder;

        public string Location => _folder;

        public bool Exists(int slot) => File.Exists(PathFor(slot));

        public bool TryRead(int slot, out string json)
        {
            var path = PathFor(slot);
            if (!File.Exists(path))
            {
                json = null;
                return false;
            }
            json = File.ReadAllText(path, Encoding.UTF8);
            return true;
        }

        public void Write(int slot, string json)
        {
            Directory.CreateDirectory(_folder);
            var path = PathFor(slot);
            var temp = path + ".tmp";
            File.WriteAllText(temp, json, Encoding.UTF8);
            if (File.Exists(path))
                File.Replace(temp, path, path + ".bak");
            else
                File.Move(temp, path);
        }

        public void Delete(int slot)
        {
            var path = PathFor(slot);
            if (File.Exists(path))
                File.Delete(path);
            if (File.Exists(path + ".bak"))
                File.Delete(path + ".bak");
        }

        private string PathFor(int slot) => Path.Combine(_folder, $"slot{slot + 1}.json");
    }
}
