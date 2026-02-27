using AES.Tools;
using AES.Tools.StateMachine.Core;


namespace ParkingCat.Game.Lagacy
{
    public static class InGameStateGraph
    {
        public static void Build(
            StateMachine sm,
            IState idle,
            IState anim,
            IState win,
            ObservableProperty<bool> isAnimating,
            ObservableProperty<bool> isWin)
        {
            sm.SetState(idle);

            var g = sm.BuildGraph();

            g.From(idle)
                .To(anim)
                .When(isAnimating.IsTrue())
                .Named("Idle->Anim")
                .Priority(10)
                .Add();
            g.From(anim)
                .To(idle)
                .When(isAnimating.IsFalse())
                .Named("Anim->Idle")
                .Priority(10)
                .Add();

            // Any -> Win (최우선)
            g.FromAny()
                .To(win)
                .When(isWin.IsTrue())
                .Named("Any->Win")
                .Priority(100)
                .Add();
        }
    }
}