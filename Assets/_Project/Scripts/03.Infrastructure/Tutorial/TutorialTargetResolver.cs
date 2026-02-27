using ParkingCat.Game.Car;
using System.Linq;
using ParkingCat.Game;


static class TutorialTargetResolver
{
    public static int ResolveCarId(GameViewModel vm, TutorialSequenceSO.TutorialTargetSelector sel)
    {
        switch (sel.mode)
        {
            case TutorialSequenceSO.TutorialTargetSelector.Mode.ByExplicitCarId:
                return sel.explicitCarId;

            case TutorialSequenceSO.TutorialTargetSelector.Mode.ByCarsListIndex:
                if (sel.carsListIndex < 0 || sel.carsListIndex >= vm.Cars.Count) return -1;
                return vm.Cars[sel.carsListIndex].CarId;

            case TutorialSequenceSO.TutorialTargetSelector.Mode.ByCarTypeNth:
            {
                int found = -1;
                int count = 0;
                for (int i = 0; i < vm.Cars.Count; i++)
                {
                    var c = vm.Cars[i];
                    if (c.Type.Value != sel.carType) continue;
                    if (count == sel.nth) { found = c.CarId; break; }
                    count++;
                }
                return found;
            }

            default:
                return -1;
        }
    }
}