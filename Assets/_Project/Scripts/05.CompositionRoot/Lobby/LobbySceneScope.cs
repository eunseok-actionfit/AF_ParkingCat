using AES.Tools;
using AES.Tools.UI.Popups;
using AES.Tools.VContainer;
using JSAM;
using ParkingCat.Lobby;
using ParkingCat.Lobby.Popups;
using ParkingCat.Lobby.Popups.AdsRemove;
using ParkingCat.Lobby.Popups.HeartRefill;
using ParkingCat.Lobby.Popups.Settings;
using ParkingCat.Lobby.Shop;
using ParkingCat.Repository;
using ParkingCat.Stage.UseCases;
using ParkingCat.Stages;
using ParkingCat.UseCase.Home;
using ParkingCat.ValueObjects.StageCatalog;
using UnityEngine;
using VContainer;
using VContainer.Unity;


namespace ParkingCat
{
    public sealed class LobbySceneScope : LifetimeScope
    {
        [Header("Scene refs")]
        [SerializeField]
        MonoContext lobbySceneContext; // External
        
        [SerializeField] ShopThemeTable shopThemeTable;
        [SerializeField] ShopIconTable shopIconTable;
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(lobbySceneContext);
            
            builder.RegisterInstance(shopThemeTable);
            builder.RegisterInstance(shopIconTable);
            
            // Stage Catalog
            builder.Register<IStageCatalog, StaticStageCatalog>(Lifetime.Scoped);

            
            // ---------- ViewModels ----------
            builder.Register<ShopViewModel>(Lifetime.Scoped);
            builder.Register<LobbySceneViewModel>(Lifetime.Scoped);
            
            builder.Register<SettingsPopupVM>(Lifetime.Transient);
            builder.Register<HeartRefillPopupVM>(Lifetime.Transient);
            builder.Register<AdsRemovePopupVM>(Lifetime.Transient);
            
            // Func<T> 명시 등록
            builder.RegisterFactory<SettingsPopupVM>(
                resolver => () => resolver.Resolve<SettingsPopupVM>(),
                Lifetime.Scoped);

            builder.RegisterFactory<HeartRefillPopupVM>(
                resolver => () => resolver.Resolve<HeartRefillPopupVM>(),
                Lifetime.Scoped);

            builder.RegisterFactory<AdsRemovePopupVM>(
                resolver => () => resolver.Resolve<AdsRemovePopupVM>(),
                Lifetime.Scoped);
            
            builder.RegisterComponentInHierarchy<PopupHost>();
            builder.Register<PopupService>(Lifetime.Scoped).As<IPopupService>();
            
            builder.Register<LobbyPopupFlow>(Lifetime.Scoped).As<ILobbyPopupFlow>();
            builder.Register<JsonResourcesStageConfigRepository>(Lifetime.Singleton)
                .As<IStageConfigRepository>();
            // ---------- UseCases ----------
            // Home 쪽 기존
            builder.Register<GetHomeDefaultStageUseCase>(Lifetime.Scoped);
            builder.Register<CanStartStageUseCase>(Lifetime.Scoped);
            builder.Register<AddLifeFromRewardUseCase>(Lifetime.Scoped);
            builder.Register<RefillLivesWithCoinUseCase>(Lifetime.Scoped);
            builder.Register<GetStageMetaUseCase>(Lifetime.Scoped);
            
         
            builder.RegisterBuildCallback(container =>
            {
                var vm   = container.Resolve<LobbySceneViewModel>();
                
                if (lobbySceneContext != null) lobbySceneContext.SetViewModel(vm);
                
                ADS.HideBanner();

                AudioManager.StopAllMusic();
                AudioManager.PlayMusic(NewAudioLibraryMusic.Lobby, true);
            });
        }
    }
}