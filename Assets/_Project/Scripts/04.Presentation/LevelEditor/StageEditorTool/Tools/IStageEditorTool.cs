using ParkingCat.LevelEditor.StageEditorTool.Context;


namespace ParkingCat.LevelEditor.StageEditorTool.Tools
{
    public interface IStageEditorTool
    {
        /// <summary>Called every Update while this tool is active.</summary>
        void Tick(StageEditorToolContext ctx);

        /// <summary>Called when tool becomes active.</summary>
        void OnEnter(StageEditorToolContext ctx);

        /// <summary>Called when tool becomes inactive.</summary>
        void OnExit(StageEditorToolContext ctx);
    }
}