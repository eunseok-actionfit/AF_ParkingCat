using System;
using ParkingCat.ValueObjects;
using UnityEngine;

namespace ParkingCat
{
    /// <summary>
    /// 격자 기반 방향을 정의한다.<br/>
    /// 상하좌우 이동 및 회전에 사용된다.
    /// </summary>
    public enum Dir4
    {
        Up = 0,
        Right = 1,
        Down = 2,
        Left = 3
    }

    /// <summary>
    /// `Dir` 열거형에 대한 방향 계산 유틸리티를 제공한다.<br/>
    /// 격자 이동 및 회전 연산을 단순화한다.
    /// </summary>
    public static class DirUtil
    {
        /// <summary>
        /// 방향을 정수 격자 이동 벡터로 변환한다.
        /// </summary>
        /// <param name="dir">기준 방향</param>
        /// <returns>격자 이동 델타</returns>
        public static Vector2Int ToDelta(this Dir4 dir) => dir switch
        {
            Dir4.Up => Vector2Int.up,
            Dir4.Right => Vector2Int.right,
            Dir4.Down => Vector2Int.down,
            Dir4.Left => Vector2Int.left,
            _ => Vector2Int.zero,
        };
        
        public static float ToRad(this Dir4 dir) => dir switch
        {
            Dir4.Right => 0f,
            Dir4.Up => MathF.PI * 0.5f,
            Dir4.Left => MathF.PI,
            Dir4.Down => -MathF.PI * 0.5f,
            _ => 0f
        };

        /// <summary>
        /// 방향을 시계 방향으로 90도 회전한다.
        /// </summary>
        /// <param name="dir">기준 방향</param>
        /// <returns>회전된 방향</returns>
        public static Dir4 RotateCW(this Dir4 dir)
            => (Dir4)(((int)dir + 1) % 4);

        /// <summary>
        /// 방향을 반시계 방향으로 90도 회전한다.
        /// </summary>
        /// <param name="dir">기준 방향</param>
        /// <returns>회전된 방향</returns>
        public static Dir4 RotateCCW(this Dir4 dir)
            => (Dir4)(((int)dir + 3) % 4);

        /// <summary>
        /// 현재 방향의 반대 방향을 반환한다.
        /// </summary>
        /// <param name="dir">기준 방향</param>
        /// <returns>반대 방향</returns>
        public static Dir4 Opposite(this Dir4 dir)
            => (Dir4)(((int)dir + 2) % 4);
        
        // heading을 4방향으로 양자화(현재 프로젝트의 Dir4 정의에 맞춤)
        public static Dir4 DirFromHeading(float rad)
        {
            // 기준: Right=0, Up=+90, Left=180, Down=-90 형태로 매핑
            // rad를 -pi..pi 로 정규화 후, 사분면 기준으로 nearest 4-dir
            while (rad > Mathf.PI) rad -= 2f * Mathf.PI;
            while (rad < -Mathf.PI) rad += 2f * Mathf.PI;

            // 0:Right, 1:Up, 2:Left, 3:Down 로 근사
            float deg = rad * Mathf.Rad2Deg;
            // -180..180 → 0..360
            if (deg < 0) deg += 360f;

            // Right(0), Up(90), Left(180), Down(270)
            int q = Mathf.RoundToInt(deg / 90f) % 4;

            return q switch
            {
                0 => Dir4.Right,
                1 => Dir4.Up,
                2 => Dir4.Left,
                3 => Dir4.Down,
                _ => Dir4.Right
            };
        }
    }

    /// <summary>
    /// 회전 동작을 방향에 적용하는 유틸리티를 제공한다.<br/>
    /// `TurnAction`을 격자 방향 변화로 변환한다.
    /// </summary>
    public static class TurnUtil
    {
        /// <summary>
        /// 회전 동작을 현재 방향에 적용한다.
        /// </summary>
        /// <param name="currentDir">현재 방향</param>
        /// <param name="action">적용할 회전 동작</param>
        /// <returns>변경된 방향</returns>
        public static Dir4 Apply(Dir4 currentDir, TurnAction action) => action switch
        {
            TurnAction.Straight => currentDir,
            TurnAction.Left => currentDir.RotateCCW(),
            TurnAction.Right => currentDir.RotateCW(),
            _ => currentDir
        };
    }
}
