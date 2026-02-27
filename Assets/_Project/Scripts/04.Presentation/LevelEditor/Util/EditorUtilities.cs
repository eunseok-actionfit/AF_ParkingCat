using System;
using ParkingCat.Game.Car.Binding;
using ParkingCat.ValueObjects;
using UnityEngine;


namespace ParkingCat.LevelEditor.Util
{
    /// <summary>
    /// CarTurnMode <-> TurnAction[] 변환을 한 곳에서 관리.
    /// (도메인 TurnAction은 Straight/Left/Right만 존재)
    /// </summary>
    public static class CarTurnMapper
    {
        public static TurnAction[] ToActions(TurnShape shape)
        {
            return shape switch
            {
                TurnShape.Left => TurnPresets.Left,
                TurnShape.Right => TurnPresets.Right,
                TurnShape.UTurnLeft => TurnPresets.UTurnLeft,
                TurnShape.UTurnRight => TurnPresets.UTurnRight,
                TurnShape.LeftRight => TurnPresets.LeftRight,
                TurnShape.RightLeft => TurnPresets.RightLeft,
                _ => Array.Empty<TurnAction>()
            };
        }

        public static TurnShape FromActions(TurnAction[] program)
        {
            if (program == null || program.Length == 0)
                return TurnShape.Straight;

            // [Left]
            if (program.Length == 1 && program[0] == TurnAction.Left) return TurnShape.Left;
            // [Right]
            if (program.Length == 1 && program[0] == TurnAction.Right) return TurnShape.Right;
            // [Left, Left]
            if (program.Length == 2 && program[0] == TurnAction.Left && program[1] == TurnAction.Left)
                return TurnShape.UTurnLeft;
            // [Right, Right]
            if (program.Length == 2 && program[0] == TurnAction.Right && program[1] == TurnAction.Right)
                return TurnShape.UTurnRight;
            

            if (program.Length == 2 && program[0] == TurnAction.Left && program[1] == TurnAction.Right)
                return TurnShape.LeftRight;
            
            if (program.Length == 2 && program[0] == TurnAction.Right && program[1] == TurnAction.Left)
                return TurnShape.RightLeft;
            
            return TurnShape.Straight;
        }
    }

    /// <summary>
    /// Dir4 관련 공용 연산.
    /// </summary>
    public static class Dir4Util
    {
        public static Vector2Int ToDelta(Dir4 dir)
        {
            return dir switch
            {
                Dir4.Up => new Vector2Int(0, 1),
                Dir4.Down => new Vector2Int(0, -1),
                Dir4.Left => new Vector2Int(-1, 0),
                Dir4.Right => new Vector2Int(1, 0),
                _ => Vector2Int.right
            };
        }

        // Right -> Down -> Left -> Up -> Right  (y+가 Up인 그리드 기준)
        public static Dir4 NextCW(Dir4 dir)
        {
            return dir switch
            {
                Dir4.Right => Dir4.Down,
                Dir4.Down => Dir4.Left,
                Dir4.Left => Dir4.Up,
                Dir4.Up => Dir4.Right,
                _ => Dir4.Right
            };
        }

        public static Dir4 PrevCW(Dir4 dir)
        {
            return dir switch
            {
                Dir4.Right => Dir4.Up,
                Dir4.Up => Dir4.Left,
                Dir4.Left => Dir4.Down,
                Dir4.Down => Dir4.Right,
                _ => Dir4.Right
            };
        }
    }

    /// <summary>
    /// Car 배치/히트테스트/검증.
    /// </summary>
    public static class CarPlacement
    {
        public static bool ContainsCell(CarConfigViewModel car, Vector2Int cell)
        {
            var head = car.HeadCell.Value;
            int len = Mathf.Max(1, car.Length.Value);
            var d = Dir4Util.ToDelta(car.Dir.Value);

            for (int k = 0; k < len; k++)
            {
                int x = head.x - d.x * k;   
                int y = head.y - d.y * k; 
                if (cell.x == x && cell.y == y) return true;
            }

            return false;
        }


        public static bool CanPlace(LevelConfigViewModel cfg, CarConfigViewModel car, bool requireRoadUnderCar)
        {
            var head = car.HeadCell.Value;
            int len = Mathf.Max(1, car.Length.Value);
            var d = Dir4Util.ToDelta(car.Dir.Value);

            for (int k = 0; k < len; k++)
            {
                int x = head.x - d.x * k;   
                int y = head.y - d.y * k;  

                if (!cfg.InBounds(x, y)) return false;
                if (requireRoadUnderCar && cfg.GetCell(x, y) == 0) return false;
            }

            return true;
        }

        
        public static CarConfigViewModel GetSelectedCar(LevelConfigViewModel cfg)
        {
            int idx = cfg.SelectedCarIndex.Value;
            if (idx < 0 || idx >= cfg.Cars.Count) return null;
            return cfg.Cars[idx];
        }

        public static int FindCarIndexAtCell(LevelConfigViewModel cfg, Vector2Int cell)
        {
            for (int i = 0; i < cfg.Cars.Count; i++)
            {
                if (ContainsCell(cfg.Cars[i], cell))
                    return i;
            }
            return -1;
        }

       
        public static bool CanPlace(LevelConfigViewModel cfg, CarConfigViewModel car)
            => CanPlace(cfg, car, requireRoadUnderCar: true);

        public static TurnShape NextTurnMode(TurnShape m)
        {
            return m switch
            {
                TurnShape.Straight => TurnShape.Left,
                TurnShape.Left => TurnShape.Right,
                TurnShape.Right => TurnShape.UTurnLeft,
                TurnShape.UTurnLeft => TurnShape.UTurnRight,
                TurnShape.UTurnRight => TurnShape.Straight,
                _ => TurnShape.Straight
            };
        }

        public static TurnShape PrevTurnMode(TurnShape m)
        {
            return m switch
            {
                TurnShape.Left => TurnShape.Straight,
                TurnShape.Right => TurnShape.Left,
                TurnShape.UTurnLeft => TurnShape.Right,
                TurnShape.UTurnRight => TurnShape.UTurnLeft,
                TurnShape.Straight => TurnShape.UTurnRight,
                _ => TurnShape.Straight
            };
        }

        public static int NextCarId(LevelConfigViewModel cfg)
        {
            int max = 0;
            for (int i = 0; i < cfg.Cars.Count; i++)
                max = Math.Max(max, cfg.Cars[i].CarId.Value);
            return max + 1;
        }
    }
}
