namespace TentacleVania.Core.Saves
{
    public interface ISaveParticipant
    {
        void Capture(SaveData data);
        void Restore(SaveData data);
    }
}
