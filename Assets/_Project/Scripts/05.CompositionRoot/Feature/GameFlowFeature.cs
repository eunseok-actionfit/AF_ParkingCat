using AES.Tools.VContainer.Bootstrap.Framework;
using VContainer;


namespace ParkingCat.Feature
{
    public class GameFlowFeature : AppFeatureSO
    {
        public override void Install(IContainerBuilder builder, in FeatureContext ctx)
        {
            builder.Register<SceneFlow.SceneFlow>(Lifetime.Scoped);
        }
    }
}