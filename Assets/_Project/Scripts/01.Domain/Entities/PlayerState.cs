using ParkingCat.Entities.Economy;


namespace ParkingCat.Entities
{
    public class PlayerState
    {
        public PlayerProgress Progress { get; }
        public PlayerEconomy Economy { get; }
        
        public PlayerState(PlayerProgress progress, PlayerEconomy economy)
        {
            Progress = progress;
            Economy = economy;
        }
    }
}


