using AES.Tools.VContainer.Bootstrap.Framework;
using ParkingCat.Game.Booster;
using ParkingCat.UseCase.Booster;
using UnityEngine;
using VContainer;


namespace ParkingCat.Feature
{
    [CreateAssetMenu(menuName="Bootstrap/Features/Game Booster", fileName="GameBoosterFeature")]
    public sealed class GameBoosterFeature : AppFeatureSO
    {
        [SerializeField] BoosterSettings boosterSettings;

        public override void Install(IContainerBuilder builder, in FeatureContext ctx)
        {
            builder.RegisterInstance(boosterSettings).AsSelf();

            builder.Register<AddBoosterUseCase>(Lifetime.Scoped);
            builder.Register<ConsumeBoosterUseCase>(Lifetime.Scoped);
            builder.Register<GetBoosterCountUseCase>(Lifetime.Scoped);

            builder.Register<BoosterViewModel>(Lifetime.Scoped);
        }
        
        

    }
}