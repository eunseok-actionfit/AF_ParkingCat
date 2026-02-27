using AES.Tools.VContainer.Bootstrap;
using AES.Tools.VContainer.Bootstrap.Framework;
using Cysharp.Threading.Tasks;
using ParkingCat.Game.Args;
using ParkingCat.Game.Booster;
using ParkingCat.Repository;
using ParkingCat.UseCase.Booster;
using UnityEngine;
using VContainer;
using VContainer.Unity;


namespace ParkingCat.Feature
{
    
    [CreateAssetMenu(menuName="Bootstrap/Features/FirstSceneFeature", fileName="FirstSceneFeature")]
    public sealed class FirstSceneFeature : AppFeatureSO
    {
        [SerializeField] string lobbySceneKey = "Lobby";
        [SerializeField] string gameSceneKey = "InGame";
        
        public const int NoobMaxLevel = 20;

        public override async UniTask Initialize(LifetimeScope rootScope,  FeatureContext ctx) 
        {
            var progress = await rootScope.Container.Resolve<IPlayerProgressRepository>().LoadAsync();
            var settings = rootScope.Container.Resolve<BootstrapSettings>();

            int level = progress.GetDefaultStage(1, NoobMaxLevel + 1);
            bool isNoob = level <= NoobMaxLevel;
            if (isNoob)
            {
                rootScope.Container.Resolve<ISceneArgsCarrier>().Set(new StageEnterArgs() { StageNumber = level});
            }
            
            settings.SetFirstSceneKey(isNoob ? gameSceneKey : lobbySceneKey);
        }

    }
}