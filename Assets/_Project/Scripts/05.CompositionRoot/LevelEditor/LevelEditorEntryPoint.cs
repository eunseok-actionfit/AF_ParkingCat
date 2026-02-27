using AES.Tools;
using VContainer.Unity;


namespace ParkingCat.LevelEditor
{
    public sealed class LevelEditorEntryPoint : IStartable
    {
        readonly LevelEditorViewModel _vm;
        readonly MonoContext _context;

        public LevelEditorEntryPoint(LevelEditorViewModel vm, MonoContext context)
        {
            _vm = vm;
            _context = context;
        }

        public void Start()
        {
            _context.SetViewModel(_vm);
            //_vm.ChangeState(GameStatus.Editing);
        }

    }
}