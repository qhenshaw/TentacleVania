namespace TentacleVania.Core.Saves
{
    public interface ISaveStorage
    {
        string Location { get; }
        bool Exists(int slot);
        bool TryRead(int slot, out string json);
        void Write(int slot, string json);
        void Delete(int slot);
    }
}
