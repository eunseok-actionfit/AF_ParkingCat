// Application/Stage/Mappers/StageRunMapper.cs
using ParkingCat.Entities;
using ParkingCat.Stage.DTOs;
using ParkingCat.ValueObjects;


namespace ParkingCat.Stage.Mapping
{
    public static class StageRunMapper
    {
        public static StageRunDto ToDto(StageRun run)
        {
            var s = run.Stage;

            var dto = new StageRunDto
            {
                StageId = s.StageId,
                Variant = s.Variant,
                Width = s.Width,
                Height = s.Height,
                Grid01 = Road01ToGrid(s.Road01, s.Width, s.Height),
                RemainingMoves = run.RemainingMoves,
                Difficulty = s.Difficulty,
                Status = run.Status,

                PendingCoin = run.PendingCoin,
                EscapedCount = run.EscapedCount,
                

                Cars = new StageRunDto.CarDto[s.Cars.Count],
            };


            for (int i = 0; i < s.Cars.Count; i++)
            {
                var c = s.Cars[i];
                dto.Cars[i] = new StageRunDto.CarDto
                {
                    Id = c.Id,
                    Pos = c.Pos,
                    HeadingRad = c.HeadingRad,
                    TurnCursor = c.TurnCursor,
                    RuntimeState = c.RuntimeState,
                    
                    TurnActions = c.TurnProgram?.Actions ?? System.Array.Empty<TurnAction>(),
                    TurnLoop = c.TurnProgram?.Loop ?? false,
                    
                    Type = c.Type,
                    RemainingEscapeMoves = c.RemainingEscapeMoves,
                    
                    CashGold = c.CashGold,
                    CashCountdown01 = TickCashTruckUseCase.GetProgress01(c),
                };
            }
            
            dto.TrafficLights = new StageRunDto.TrafficLightDto[run.TrafficLights.Count];
            for (int i = 0; i < run.TrafficLights.Count; i++)
            {
                var t = run.TrafficLights[i];
                dto.TrafficLights[i] = new StageRunDto.TrafficLightDto
                {
                    Id = t.Id,
                    WorldPos = t.WorldPos,
                    LineDir = t.LineDir,
                    PhaseOffsetSeconds = t.PhaseOffsetSeconds, 
                    State = t.State,
                    RemainSeconds = t.RemainSeconds,
                };
            }
            
            // --- OneWayLanes ---
            if (s.OneWayLanes != null)
            {
                dto.OneWayLanes = new StageRunDto.OneWayLaneDto[s.OneWayLanes.Count];

                for (int i = 0; i < s.OneWayLanes.Count; i++)
                {
                    var ow = s.OneWayLanes[i];
                    dto.OneWayLanes[i] = new StageRunDto.OneWayLaneDto
                    {
                        StartCell = ow.StartCell,
                        Dir = ow.Dir,
                        Length = ow.Length,
                        Thickness = ow.Thickness,
                        LaneIndex = ow.LaneIndex,
                        AllowedDir = ow.AllowedDir,
                    };
                }
            }
            else
            {
                dto.OneWayLanes = System.Array.Empty<StageRunDto.OneWayLaneDto>();
            }

            return dto;
        }

        static int[,] Road01ToGrid(int[] road01, int w, int h)
        {
            var g = new int[h, w];
            if (road01 == null) return g;

            // row-major: index = y*w + x  (StageConfig/Stage 주석과 동일) 
            int n = road01.Length;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    g[y, x] = (idx >= 0 && idx < n) ? road01[idx] : 0;
                }
            return g;
        }
    }
}