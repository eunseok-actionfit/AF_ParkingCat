using AES.Tools;
using UnityEngine;
using VContainer;
using VContainer.Unity;


namespace ParkingCat.LevelEditor
{
    public sealed class LevelEditorScope : LifetimeScope
    {
        [SerializeField] MonoContext context;
        protected override void Configure(IContainerBuilder builder)
        {
            // 레벨 에디터 VM
            builder.Register<LevelEditorViewModel>(Lifetime.Scoped);
            builder.RegisterComponent(context);
            
            builder.RegisterEntryPoint<LevelEditorEntryPoint>(Lifetime.Scoped);
        }
    }
}