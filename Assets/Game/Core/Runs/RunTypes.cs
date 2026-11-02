namespace TentacleVania.Core.Runs
{
    public enum RoomRole
    {
        Entry,
        Combat,
        Elite,
        Shop,
        Boss,
    }

    public enum RunReward
    {
        None,
        Mutation,
        Evolution,
        Biomass,
        Essence,
        Shop,
        Challenge,
    }

    public readonly struct DoorOffer
    {
        public DoorOffer(RoomRole role, RunReward reward, int room)
        {
            Role = role;
            Reward = reward;
            Room = room;
        }

        public RoomRole Role { get; }
        public RunReward Reward { get; }
        public int Room { get; }
    }
}
