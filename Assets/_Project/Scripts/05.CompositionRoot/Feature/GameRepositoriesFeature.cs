using AES.Tools.VContainer.Bootstrap.Framework;
using ParkingCat.Repository;
using ParkingCat.Repository.Booster;
using ParkingCat.Repository.PlayerEconomy;
using ParkingCat.Repository.PlayerProgress;
using ParkingCat.Repository.PlayerState;
using VContainer;


namespace ParkingCat.Feature
{
    public sealed class GameRepositoriesFeature : AppFeatureSO
    {
        public override void Install(IContainerBuilder builder, in FeatureContext ctx)
        {
            builder.Register<IPlayerProgressRepository, PlayerProgressRepository>(Lifetime.Singleton);
            builder.Register<IPlayerEconomyRepository, PlayerEconomyRepository>(Lifetime.Singleton);
            builder.Register<IPlayerStateRepository, PlayerStateRepository>(Lifetime.Singleton);

            builder.Register<IBoosterInventoryRepository, BoosterInventoryRepository>(Lifetime.Singleton);

        }
    }
}