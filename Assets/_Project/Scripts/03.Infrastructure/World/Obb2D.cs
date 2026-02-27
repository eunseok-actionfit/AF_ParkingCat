using UnityEngine;


namespace ParkingCat.World
{
    /// <summary>
    /// 2D 평면에서의 OBB(Oriented Bounding Box)를 표현한다.<br/>
    /// 차량 충돌 및 영역 겹침 판정에 사용된다.
    /// </summary>
    /// <remarks>
    /// `AxisX`, `AxisY`는 정규화된 축 벡터를 전제로 한다.<br/>
    /// `Half`는 (폭/2, 길이/2) 순서로 저장된다.
    /// </remarks>
    public readonly struct Obb2D
    {
        // 박스 중심 좌표
        public readonly Vector2 Center;

        // 로컬 X축 단위 벡터(우측)
        public readonly Vector2 AxisX;

        // 로컬 Y축 단위 벡터(전방)
        public readonly Vector2 AxisY;

        // 반치수(x=폭/2, y=길이/2)
        public readonly Vector2 Half;

        /// <summary>
        /// 중심, 헤딩, 치수로 OBB를 생성한다.
        /// </summary>
        /// <param name="center">중심 좌표</param>
        /// <param name="headingRad">전방 방향 각도(라디안)</param>
        /// <param name="length">길이</param>
        /// <param name="width">폭</param>
        public Obb2D(Vector2 center, float headingRad, float length, float width)
        {
            Center = center;

            float c = Mathf.Cos(headingRad);
            float s = Mathf.Sin(headingRad);

            AxisY = new Vector2(c, s);
            AxisX = new Vector2(s, -c);

            Half = new Vector2(width * 0.5f, length * 0.5f);
        }

        /// <summary>
        /// OBB의 네 꼭짓점을 시계 방향으로 채운다.<br/>
        /// 호출자는 길이 4 이상의 배열을 제공해야 한다.
        /// </summary>
        /// <param name="out4">꼭짓점을 받을 배열</param>
        public void GetCorners(Vector2[] out4)
        {
            Vector2 rx = AxisX * Half.x;
            Vector2 fy = AxisY * Half.y;

            out4[0] = Center + rx + fy;
            out4[1] = Center - rx + fy;
            out4[2] = Center - rx - fy;
            out4[3] = Center + rx - fy;
        }
    }

    /// <summary>
    /// 2D SAT(Separating Axis Theorem) 기반 겹침 판정을 제공한다.<br/>
    /// 두 OBB의 충돌 여부를 빠르게 판단한다.
    /// </summary>
    /// <remarks>
    /// 축은 두 OBB의 로컬 축 4개만 사용한다.<br/>
    /// 입력 OBB의 축이 정규화되어 있어야 한다.
    /// </remarks>
    public static class Sat2D
    {
        /// <summary>
        /// 두 OBB가 겹치는지 판정한다.
        /// </summary>
        /// <param name="a">첫 번째 OBB</param>
        /// <param name="b">두 번째 OBB</param>
        /// <returns>겹치면 <c>true</c>, 아니면 <c>false</c></returns>
        public static bool Overlap(in Obb2D a, in Obb2D b)
        {
            if (!OverlapOnAxis(in a, in b, a.AxisX)) return false;
            if (!OverlapOnAxis(in a, in b, a.AxisY)) return false;
            if (!OverlapOnAxis(in a, in b, b.AxisX)) return false;
            if (!OverlapOnAxis(in a, in b, b.AxisY)) return false;
            return true;
        }

        /// <summary>
        /// 지정 축에서 두 OBB의 투영 구간이 겹치는지 확인한다.
        /// </summary>
        /// <param name="a">첫 번째 OBB</param>
        /// <param name="b">두 번째 OBB</param>
        /// <param name="axis">검사 축 단위 벡터</param>
        /// <returns>겹치면 <c>true</c>, 아니면 <c>false</c></returns>
        static bool OverlapOnAxis(in Obb2D a, in Obb2D b, Vector2 axis)
        {
            Project(in a, axis, out float aMin, out float aMax);
            Project(in b, axis, out float bMin, out float bMax);
            return !(aMax < bMin || bMax < aMin);
        }

        /// <summary>
        /// OBB를 지정 축으로 투영한 구간을 계산한다.
        /// </summary>
        /// <param name="o">대상 OBB</param>
        /// <param name="axis">투영 축 단위 벡터</param>
        /// <param name="min">투영 최소값</param>
        /// <param name="max">투영 최대값</param>
        static void Project(in Obb2D o, Vector2 axis, out float min, out float max)
        {
            float centerProjection = Vector2.Dot(o.Center, axis);

            float radius =
                Mathf.Abs(Vector2.Dot(o.AxisX, axis)) * o.Half.x +
                Mathf.Abs(Vector2.Dot(o.AxisY, axis)) * o.Half.y;

            min = centerProjection - radius;
            max = centerProjection + radius;
        }
    }
}
