using System;
using ParkingCat.Config;
using ParkingCat.Entities;
using ParkingCat.ValueObjects;
using UnityEngine;


namespace ParkingCat.Factories
{
    public static class StageFactory
    {
        public static StageRun CreateFromConfig(StageConfig cfg)
        {
            var stage = new Entities.Stage(
                stageId: cfg.StageId,
                variant: cfg.Variant,
                width: cfg.Width,
                height: cfg.Height,
                road01: cfg.Road01,
                difficulty: cfg.Difficulty
            );

            // ===== Cars (기존) =====
            if (cfg.Cars != null)
            {
                for (int i = 0; i < cfg.Cars.Length; i++)
                {
                    var c = cfg.Cars[i];

                    var car = new Car();
                    car.Id = c.CarId;
                    car.Pos = CalcOccupiedCenterWorld(c.HeadCell.x, c.HeadCell.y, c.Length, c.Dir);
                    car.HeadingRad = c.Dir.ToRad();
                    car.TurnCursor = 0;
                    car.TurnProgram = new ValueObjects.TurnProgram
                    {
                        Actions = c.TurnProgram ?? Array.Empty<TurnAction>(),
                        Loop = false
                    };
                    car.Type = c.Type;
                    car.RemainingEscapeMoves = c.EscapeMoveLimit;
                    
                    if (car.Type == CarType.CashTruck)
                    {
                        car.CashGold = Mathf.Max(0, c.InitialCashGold);
                        car.CashDecayElapsed = 0f;
                    }
                    
                    stage.AddCar(car);
                }
            }

           
            if (cfg.TrafficLights != null)
            {
                for (int i = 0; i < cfg.TrafficLights.Length; i++)
                {
                    var t = cfg.TrafficLights[i];
                    
                    stage.AddTrafficLight(new StageTrafficLight(
                        id: t.Id,
                        worldPos: t.WorldPos,
                        lineDir: t.LineDir,
                        phaseOffsetSeconds: t.PhaseOffsetSeconds
                    ));
                }
            }
            
            if (cfg.OneWayLanes != null)
            {
                for (int i = 0; i < cfg.OneWayLanes.Length; i++)
                    stage.AddOneWayLane(cfg.OneWayLanes[i]);
            }

            var run = new StageRun(stage, cfg.MoveLimit);
            
            run.InitTrafficLightsFromStage();
            
  

            return run;
        }


        static Vector2 CalcOccupiedCenterWorld(int headX, int headY, int length, Dir4 dir)
        {
            if (length <= 0) length = 1;

            // grid 상에서 점유 셀들의 평균(중앙)
            float sumX = 0f, sumY = 0f;

            for (int i = 0; i < length; i++)
            {
                int x = headX;
                int y = headY;
                
                switch (dir)
                {
                    case Dir4.Right: x = headX - i; y = headY;     break;
                    case Dir4.Left:  x = headX + i; y = headY;     break;
                    case Dir4.Up:    x = headX;     y = headY - i; break;
                    case Dir4.Down:  x = headX;     y = headY + i; break;
                    default:         x = headX;     y = headY;     break;
                }

                sumX += x;
                sumY += y;
            }

            float cx = sumX / length;
            float cy = sumY / length;

            // cell center 규칙 적용: (x+0.5, y+0.5)
            return new Vector2(cx + 0.5f, cy + 0.5f);
        }
    }
}
