using ParkingCat.LevelEditor.StageEditorTool.Context;
using ParkingCat.LevelEditor.Util;
using UnityEngine;

namespace ParkingCat.LevelEditor.StageEditorTool.Tools
{
    public sealed class StageEditorTrafficLightTool : IStageEditorTool
    {
        public void OnEnter(StageEditorToolContext ctx) { }
        public void OnExit(StageEditorToolContext ctx) { }

        public void Tick(StageEditorToolContext ctx)
        {
            var cfg = ctx.Cfg;
            if (cfg == null) return;

            // 좌클릭: 선택 or 배치
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                if (!ctx.TryGetMouseCell(out var cell)) return;

                int hitIdx = FindTrafficLightIndexNearCell(cfg, cell);

                if (hitIdx >= 0)
                {
                    cfg.SelectedTrafficLightIndex.Value = hitIdx;
                }
                else
                {
                    ctx.PushUndo?.Invoke();

                    // 새로 만들기
                    var vm = new TrafficLightConfigViewModel();
                    vm.Id.Value = NextTrafficLightId(cfg);

                    // 월드 그리드 규칙: 셀 중심 (x+0.5, y+0.5)
                    vm.WorldPos.Value = new Vector2(cell.x + 0.5f, cell.y + 0.5f);

                    vm.LineDir.Value = Dir4.Right;
                    vm.LineLength.Value = 1;
                    vm.PhaseOffsetSeconds.Value = 0f;

                    cfg.TrafficLights.Add(vm);
                    cfg.SelectedTrafficLightIndex.Value = cfg.TrafficLights.Count - 1;

                    ctx.RefreshPreview?.Invoke();
                }
            }

            // 우클릭: 삭제
            if (UnityEngine.Input.GetMouseButtonDown(1))
            {
                if (!ctx.TryGetMouseCell(out var cell)) return;

                int hitIdx = FindTrafficLightIndexNearCell(cfg, cell);
                if (hitIdx >= 0)
                {
                    cfg.SelectedTrafficLightIndex.Value = hitIdx;
                    ctx.PushUndo?.Invoke();
                    cfg.TrafficLights.RemoveAt(hitIdx);
                    cfg.SelectedTrafficLightIndex.Value = -1;

                    ctx.RefreshPreview?.Invoke();
                }
            }

            // 선택된 항목 조작
            int sel = cfg.SelectedTrafficLightIndex.Value;
            if (sel < 0 || sel >= cfg.TrafficLights.Count) return;

            var tl = cfg.TrafficLights[sel];

            // R: 방향 회전
            if (UnityEngine.Input.GetKeyDown(ctx.CarRotateKey))
            {
                ctx.PushUndo?.Invoke();
                tl.LineDir.Value = Dir4Util.NextCW(tl.LineDir.Value);
                ctx.RefreshPreview?.Invoke();
            }
        }

        // “셀 근처”에 있는 신호등 찾기: WorldPos를 cell로 환산해서 비교
        static int FindTrafficLightIndexNearCell(LevelConfigViewModel cfg, Vector2Int cell)
        {
            int best = -1;
            float bestSqr = float.MaxValue;

            for (int i = 0; i < cfg.TrafficLights.Count; i++)
            {
                var p = cfg.TrafficLights[i].WorldPos.Value;

                // WorldPos가 셀 중심(x+0.5,y+0.5)이므로, 해당 셀과 비교
                var c = new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));
                var d = c - cell;
                float sqr = d.sqrMagnitude;

                if (sqr < bestSqr && sqr <= 0) // 동일 셀만 히트
                {
                    bestSqr = sqr;
                    best = i;
                }
            }

            return best;
        }

        static int NextTrafficLightId(LevelConfigViewModel cfg)
        {
            int max = 0;
            for (int i = 0; i < cfg.TrafficLights.Count; i++)
                max = Mathf.Max(max, cfg.TrafficLights[i].Id.Value);
            return max + 1;
        }
    }
}
