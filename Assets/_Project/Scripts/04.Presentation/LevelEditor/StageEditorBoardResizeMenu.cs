using AES.Tools;
using UnityEngine;


namespace ParkingCat.LevelEditor
{
    /// <summary>
    /// 보드 사이즈 변경을 휠에서 분리하고, UI 버튼(메뉴)로 호출하기 위한 컴포넌트.
    /// UGUI Button의 OnClick에 Expand*/Shrink* 메서드를 연결해서 사용.
    /// </summary>
    public sealed class StageEditorBoardResizeMenu : ContextListenerBase
    {
      
        [Tooltip("New cells fill: 0=wall, 1=road")]
        [SerializeField]
        int fill01 = 0;

        [Tooltip("버튼 1회 클릭당 확장/축소할 칸 수")]
        [SerializeField]
        int step = 1;

        LevelEditorViewModel _editor;

        protected override void OnContextAvailable(IBindingContext ctx)
        {
            _editor = ctx.GetValue() as LevelEditorViewModel;
        }

        protected override void OnContextUnavailable()
        {
            _editor = null;
        }

        bool CanEdit() => _editor?.Current.Value != null && _editor.Mode.Value == EditorStageMode.Editing;

        void Apply(int delta, BoardResizeDir dir)
        {
            if (!CanEdit()) return;

            _editor.PushUndo();
            _editor.Current.Value.ResizeWithDirection(delta, dir, fill01);
            _editor.RefreshPreviewCommand.Execute();
        }

        // --- Expand ---
        public void ExpandUp() => Apply(+Mathf.Max(1, step), BoardResizeDir.Up);
        public void ExpandDown() => Apply(+Mathf.Max(1, step), BoardResizeDir.Down);
        public void ExpandLeft() => Apply(+Mathf.Max(1, step), BoardResizeDir.Left);
        public void ExpandRight() => Apply(+Mathf.Max(1, step), BoardResizeDir.Right);

        // --- Shrink ---
        public void ShrinkUp() => Apply(-Mathf.Max(1, step), BoardResizeDir.Up);
        public void ShrinkDown() => Apply(-Mathf.Max(1, step), BoardResizeDir.Down);
        public void ShrinkLeft() => Apply(-Mathf.Max(1, step), BoardResizeDir.Left);
        public void ShrinkRight() => Apply(-Mathf.Max(1, step), BoardResizeDir.Right);
    }
}
