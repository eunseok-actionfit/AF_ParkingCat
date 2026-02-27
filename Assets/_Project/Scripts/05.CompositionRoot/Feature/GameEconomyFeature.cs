#nullable enable

using System;
using System.Collections.Generic;
using AES.Tools;
using AES.Tools.VContainer;
using AES.Tools.VContainer.Bootstrap.Framework;
using Cysharp.Threading.Tasks;
using ParkingCat.Economy;
using ParkingCat.Entities.Economy;
using ParkingCat.Repository;
using ParkingCat.Service;
using ParkingCat.Time;
using ParkingCat.UseCase.Economy;
using ParkingCat.UseCase.Economy.Coin;
using ParkingCat.UseCase.Economy.Lives;
using UnityEngine;
using UnityEngine.Purchasing.Security;
using VContainer;
using VContainer.Unity;


namespace ParkingCat.Feature
{
    [CreateAssetMenu(menuName = "Bootstrap/Features/Game Economy", fileName = "GameEconomyFeature")]
    public sealed class GameEconomyFeature : AppFeatureSO, IProvideCapabilities
    {
        [SerializeField] int maxLives = 5;
        [SerializeField] float regenMinutes = 30f;

        public void Provide(IDictionary<Type, IFeatureCapability> caps)
        {
            caps[typeof(IIapRewardApplierCapability)] = new IapRewardApplierCapability();
        }

        public override void Install(IContainerBuilder builder, in FeatureContext ctx)
        {
            builder.Register<ITimeProvider, SystemTimeProvider>(Lifetime.Singleton);

            builder.RegisterInstance(new LivesConfig(
                maxLives: maxLives,
                regenInterval: TimeSpan.FromMinutes(regenMinutes),
                initialLives: maxLives));

            builder.Register<RefreshLivesUseCase>(Lifetime.Scoped);
            builder.Register<GetLivesStatusUseCase>(Lifetime.Scoped);
            builder.Register<ConsumeLifeForStageUseCase>(Lifetime.Scoped);

            builder.Register<GetCoinStatusUseCase>(Lifetime.Scoped);
            builder.Register<AddCoinUseCase>(Lifetime.Scoped);
            builder.Register<SpendCoinUseCase>(Lifetime.Scoped);

            builder.Register<LivesViewModel>(Lifetime.Scoped);
            builder.Register<CoinViewModel>(Lifetime.Scoped);

            builder.Register<AddUnlimitedLivesMinutesUseCase>(Lifetime.Scoped);
            builder.Register<RemoveAdsUseCase>(Lifetime.Scoped);
            builder.Register<GetAdsRemoveUseCase>(Lifetime.Scoped);

            builder.Register<GameIapRewardApplier>(Lifetime.Singleton)
                .As<IIapRewardApplier>();
        }

        public override async UniTask Initialize(LifetimeScope rootScope, FeatureContext ctx)
        {
            var repoService = rootScope.Container.Resolve<IPlayerEconomyRepository>();
            var repo = await repoService.LoadAsync();

            // if (repo.RemoveAds)
            // {
            //     ADS.SetRuntimeDisabled(true);
            // }

            if (ctx.Capabilities.TryGetCapability<IIapRewardApplierCapability>(out var cap))
            {
                cap.RewardApplier = rootScope.Container.Resolve<IIapRewardApplier>();
            }
            
        }
    }

}