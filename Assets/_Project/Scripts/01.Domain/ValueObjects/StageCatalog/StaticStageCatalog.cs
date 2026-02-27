namespace ParkingCat.ValueObjects.StageCatalog
{
    public interface IStageCatalog
    {
        int MinLevel { get; }
        int MaxLevel { get; }

        bool IsValid(int level);
    }

    public sealed class StaticStageCatalog : IStageCatalog
    {
        public int MinLevel => 1;
        public int MaxLevel => 200; 

        public bool IsValid(int level) => level >= MinLevel && level <= MaxLevel;
    }
}