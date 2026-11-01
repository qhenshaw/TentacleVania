namespace TentacleVania.Core.Platform
{
    public interface IAchievementBackend
    {
        bool IsUnlocked(string id);
        void Unlock(string id);
        void ResetAll();
    }
}
