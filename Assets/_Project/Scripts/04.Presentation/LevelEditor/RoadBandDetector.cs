using System;
using System.Collections.Generic;
using UnityEngine;

namespace ParkingCat.LevelEditor.Util
{
    public static class RoadBandDetector
    {
        public struct BandInfo
        {
            public Vector2Int StartCell;
            public Dir4 Dir; // Right/Left or Up/Down
            public int Length; // run
            public int Thickness; // th
            public bool IsHorizontal; // true=H band, false=V band
        }

        public static bool TryGetBand(int[] road01, int w, int h, Vector2Int clicked, int endGapCells, out BandInfo band)
        {
            band = default;
            if (road01 == null || road01.Length != w * h) return false;

            bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < w && y < h;

            bool IsRoad(int x, int y)
            {
                if (!InBounds(x, y)) return false;
                return road01[y * w + x] == 1;
            }

            if (!IsRoad(clicked.x, clicked.y)) return false;

            int RunLen(int x, int y, int dx, int dy)
            {
                int len = 0;
                while (IsRoad(x + dx * (len + 1), y + dy * (len + 1))) len++;
                return len;
            }

            // 분해 점수(각 방향 런 길이까지 필요)
            void ScoreParts(int x, int y, out int lh, out int rh, out int dv, out int uv)
            {
                lh = RunLen(x, y, -1, 0);
                rh = RunLen(x, y, 1, 0);
                dv = RunLen(x, y, 0, -1);
                uv = RunLen(x, y, 0, 1);
            }

            // 점수
            void Score(int x, int y, out int sh, out int sv)
            {
                ScoreParts(x, y, out int lh, out int rh, out int dv, out int uv);
                sh = lh + rh;
                sv = dv + uv;
            }

            // 교차/코너 후보(ambiguous) 셀을 barrier로 쓸거임
            // 3차선 중앙이 thickness 때문에 (sh>1 && sv>1)로 교차로 오인되던 케이스를 줄이기 위해,
            // "진짜 교차로" 형태만 ambiguous 처리하도록 완화
            bool IsAmbiguous(int x, int y)
            {
                if (!IsRoad(x, y)) return true;

                ScoreParts(x, y, out int lh, out int rh, out int dv, out int uv);
                int sh = lh + rh;
                int sv = dv + uv;

                if (sh <= 0 && sv <= 0) return true;

                // 동점은 여전히 애매 셀로 취급 (코너/교차 후보)
                if (sh == sv) return true;

                // "진짜 교차로"만 ambiguous로 본다:
                // 양 축 모두 양방향으로 뻗고(좌/우 + 상/하),
                // 각 축에서 최소 한쪽이 2칸 이상 길게 뻗는 경우만 교차로로 판단
                bool hasLR = lh > 0 && rh > 0;
                bool hasDU = dv > 0 && uv > 0;
                bool looksLikeJunction = hasLR && hasDU
                                         && Mathf.Max(lh, rh) >= 2
                                         && Mathf.Max(dv, uv) >= 2;

                return looksLikeJunction;
            }

            // 클릭 셀 우세 방향 결정(동점이면 주변 투표로 결정)
            Score(clicked.x, clicked.y, out int csh, out int csv);
            if (csh <= 0 && csv <= 0) return false;

            bool wantH;

            if (csh == csv)
            {
                // 주변 셀들이 "수평 우세/수직 우세"인지 투표로 결정
                int hVote = 0, vVote = 0;

                void Vote(int x, int y)
                {
                    if (!IsRoad(x, y)) return;
                    Score(x, y, out int sh, out int sv);
                    if (sh > sv) hVote++;
                    else if (sv > sh) vVote++;
                }

                Vote(clicked.x - 1, clicked.y);
                Vote(clicked.x + 1, clicked.y);
                Vote(clicked.x, clicked.y - 1);
                Vote(clicked.x, clicked.y + 1);

                if (hVote == vVote)
                {
                    // 그래도 동점이면 가로 우선(원하면 false로 바꿔 세로 우선 가능)
                    wantH = true;
                }
                else
                {
                    wantH = hVote > vVote;
                }
            }
            else
            {
                wantH = csh > csv;
            }

            // ---- H/V 성향 맵 (중요: 엄격 비교로 되돌림) ----
            var isH = new bool[h, w];
            var isV = new bool[h, w];

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (!IsRoad(x, y)) continue;

                    Score(x, y, out int sh, out int sv);

                    if (sh > 0 && sh > sv) isH[y, x] = true;
                    if (sv > 0 && sv > sh) isV[y, x] = true;
                }

            // 클릭 셀이 코너/교차여서 맵에 안 들어가도 “시작은 허용”
            if (wantH) isH[clicked.y, clicked.x] = true;
            else isV[clicked.y, clicked.x] = true;

            // ---- FloodFill (ambiguous 셀로 확장 금지) ----
            bool Accept(int x, int y)
            {
                if (!InBounds(x, y)) return false;
                if (!IsRoad(x, y)) return false;

                // 클릭 셀만 ambiguous 예외 허용, 그 외 ambiguous는 barrier
                if (!(x == clicked.x && y == clicked.y) && IsAmbiguous(x, y))
                    return false;

                return wantH ? isH[y, x] : isV[y, x];
            }

            var visited = new bool[h, w];
            var q = new Queue<Vector2Int>();
            q.Enqueue(clicked);
            visited[clicked.y, clicked.x] = true;

            int minX = clicked.x, maxX = clicked.x;
            int minY = clicked.y, maxY = clicked.y;

            while (q.Count > 0)
            {
                var c = q.Dequeue();
                minX = Mathf.Min(minX, c.x);
                maxX = Mathf.Max(maxX, c.x);
                minY = Mathf.Min(minY, c.y);
                maxY = Mathf.Max(maxY, c.y);

                TryEnq(c.x + 1, c.y);
                TryEnq(c.x - 1, c.y);
                TryEnq(c.x, c.y + 1);
                TryEnq(c.x, c.y - 1);
            }

            void TryEnq(int x, int y)
            {
                if (!InBounds(x, y)) return;
                if (visited[y, x]) return;
                if (!Accept(x, y)) return;
                visited[y, x] = true;
                q.Enqueue(new Vector2Int(x, y));
            }

            // bbox -> band
            int rawLen, rawTh;

            if (wantH)
            {
                rawLen = maxX - minX + 1;
                rawTh = maxY - minY + 1;
            }
            else
            {
                rawLen = maxY - minY + 1;
                rawTh = maxX - minX + 1;
            }

            if (rawLen <= 0 || rawTh <= 0) return false;

            // ---- 기존 endGapCells trim 로직 유지 ----
            bool IsJunctionColumn_H(int x, int y0, int th) => IsRoad(x, y0 - 1) || IsRoad(x, y0 + th);
            bool IsJunctionRow_V(int x0, int y, int th) => IsRoad(x0 - 1, y) || IsRoad(x0 + th, y);

            bool NearGapH(int x, int y0, int th)
            {
                if (endGapCells <= 0) return false;

                for (int d = 1; d <= endGapCells; d++)
                    if (IsJunctionColumn_H(x - d, y0, th) || IsJunctionColumn_H(x + d, y0, th))
                        return true;

                return false;
            }

            bool NearGapV(int x0, int y, int th)
            {
                if (endGapCells <= 0) return false;

                for (int d = 1; d <= endGapCells; d++)
                    if (IsJunctionRow_V(x0, y - d, th) || IsJunctionRow_V(x0, y + d, th))
                        return true;

                return false;
            }

            if (wantH)
            {
                int x0 = minX;
                int y0 = minY;
                int th = rawTh;
                int run = rawLen;

                int trimL = 0;
                while (trimL < run && (IsJunctionColumn_H(x0 + trimL, y0, th) || NearGapH(x0 + trimL, y0, th)))
                    trimL++;

                int trimR = 0;
                while (trimR < run - trimL && (IsJunctionColumn_H(x0 + (run - 1 - trimR), y0, th) || NearGapH(x0 + (run - 1 - trimR), y0, th)))
                    trimR++;

                int finalRun = Math.Max(0, run - trimL - trimR);
                if (finalRun <= 0) return false;

                band = new BandInfo
                {
                    IsHorizontal = true,
                    StartCell = new Vector2Int(x0 + trimL, y0),
                    Dir = Dir4.Right,
                    Length = finalRun,
                    Thickness = th
                };

                return true;
            }
            else
            {
                int x0 = minX;
                int y0 = minY;
                int th = rawTh;
                int run = rawLen;

                int trimB = 0;
                while (trimB < run && (IsJunctionRow_V(x0, y0 + trimB, th) || NearGapV(x0, y0 + trimB, th)))
                    trimB++;

                int trimT = 0;
                while (trimT < run - trimB && (IsJunctionRow_V(x0, y0 + (run - 1 - trimT), th) || NearGapV(x0, y0 + (run - 1 - trimT), th)))
                    trimT++;

                int finalRun = Math.Max(0, run - trimB - trimT);
                if (finalRun <= 0) return false;

                band = new BandInfo
                {
                    IsHorizontal = false,
                    StartCell = new Vector2Int(x0, y0 + trimB),
                    Dir = Dir4.Up,
                    Length = finalRun,
                    Thickness = th
                };

                return true;
            }
        }

        public static string MakeBandKey(in BandInfo b)
            => $"{b.StartCell.x},{b.StartCell.y}|{(int)b.Dir}|{b.Length}|{b.Thickness}|{(b.IsHorizontal ? "H" : "V")}";
    }
}
