using System;
using UnityEngine;


namespace ParkingCat.LevelEditor.Util
{
    /// <summary>
    /// Helpers for operating on row-major int[] grid (y*w + x), values are 0/1.
    /// Centralizes resize/copy/paste/fill logic used by editor tools.
    /// </summary>
    public static class EditorGridOps
    {
        public static bool InBounds(int x, int y, int w, int h)
            => x >= 0 && y >= 0 && x < w && y < h;

        public static int Get(int[] arr, int w, int h, int x, int y, int fallback = 0)
        {
            if (arr == null) return fallback;
            if (!InBounds(x, y, w, h)) return fallback;
            int idx = y * w + x;
            return (uint)idx < (uint)arr.Length ? arr[idx] : fallback;
        }

        public static void Set(int[] arr, int w, int h, int x, int y, int v01)
        {
            if (arr == null) return;
            if (!InBounds(x, y, w, h)) return;
            int idx = y * w + x;

            if ((uint)idx < (uint)arr.Length)
                arr[idx] = v01 != 0 ? 1 : 0;
        }

        public static int[] ResizePreserve(int[] oldArr, int oldW, int oldH, int newW, int newH, int fill01 = 0)
        {
            oldW = Math.Max(1, oldW);
            oldH = Math.Max(1, oldH);

            newW = Math.Max(1, newW);
            newH = Math.Max(1, newH);

            oldArr ??= Array.Empty<int>();
            var next = new int[newW * newH];

            if (fill01 != 0)
                for (int i = 0; i < next.Length; i++)
                    next[i] = 1;

            int copyW = Math.Min(oldW, newW);
            int copyH = Math.Min(oldH, newH);

            for (int y = 0; y < copyH; y++)
            {
                for (int x = 0; x < copyW; x++)
                {
                    int oldIdx = y * oldW + x;
                    int newIdx = y * newW + x;

                    if ((uint)oldIdx < (uint)oldArr.Length)
                        next[newIdx] = oldArr[oldIdx];
                }
            }

            return next;
        }

        public static int[] CopyRect(int[] arr, int gridW, int gridH, RectInt r)
        {
            int w = Math.Max(1, r.width);
            int h = Math.Max(1, r.height);

            var buf = new int[w * h];
            arr ??= Array.Empty<int>();

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int sx = r.x + x;
                    int sy = r.y + y;
                    buf[y * w + x] = Get(arr, gridW, gridH, sx, sy, 0);
                }

            return buf;
        }

        public static void FillRectDirect(int[] arr, int gridW, int gridH, RectInt r, int v01)
        {
            if (arr == null) return;

            int x0 = r.x;
            int y0 = r.y;
            int x1 = r.x + r.width;
            int y1 = r.y + r.height;

            for (int y = y0; y < y1; y++)
            {
                if ((uint)y >= (uint)gridH) continue;

                for (int x = x0; x < x1; x++)
                {
                    if ((uint)x >= (uint)gridW) continue;
                    arr[y * gridW + x] = v01 != 0 ? 1 : 0;
                }
            }
        }

        public static void PasteRectDirect(int[] arr, int gridW, int gridH, RectInt r, int bufW, int bufH, int[] buf, bool overwriteZero)
        {
            if (arr == null || buf == null) return;

            for (int y = 0; y < bufH; y++)
            {
                int ty = r.y + y;
                if ((uint)ty >= (uint)gridH) continue;

                for (int x = 0; x < bufW; x++)
                {
                    int v = buf[y * bufW + x];
                    if (!overwriteZero && v == 0) continue;

                    int tx = r.x + x;
                    if ((uint)tx >= (uint)gridW) continue;

                    arr[ty * gridW + tx] = v != 0 ? 1 : 0;
                }
            }
        }


        public static bool FloodFill(int[] arr, int gridW, int gridH, int startX, int startY, int fill01)
        {
            if (arr == null) return false;
            if (!InBounds(startX, startY, gridW, gridH)) return false;

            fill01 = fill01 != 0 ? 1 : 0;

            int startIdx = startY * gridW + startX;
            if ((uint)startIdx >= (uint)arr.Length) return false;

            int target = arr[startIdx];
            if (target == fill01) return false;

            int max = gridW * gridH;
            int[] stack = new int[Mathf.Max(16, Mathf.Min(max, 4096))];
            int sp = 0;

            void Push(int idx)
            {
                if (sp >= stack.Length)
                    Array.Resize(ref stack, stack.Length * 2);

                stack[sp++] = idx;
            }

            Push(startIdx);

            bool changed = false;

            while (sp > 0)
            {
                int idx = stack[--sp];
                if ((uint)idx >= (uint)arr.Length) continue;
                if (arr[idx] != target) continue;

                arr[idx] = fill01;
                changed = true;

                int x = idx % gridW;
                int y = idx / gridW;

                if (x > 0) Push(idx - 1);
                if (x + 1 < gridW) Push(idx + 1);
                if (y > 0) Push(idx - gridW);
                if (y + 1 < gridH) Push(idx + gridW);
            }

            return changed;
        }

        public static RectInt MakeRectFromTwoCells(Vector2Int a, Vector2Int b)
        {
            int xMin = Mathf.Min(a.x, b.x);
            int yMin = Mathf.Min(a.y, b.y);
            int xMax = Mathf.Max(a.x, b.x);
            int yMax = Mathf.Max(a.y, b.y);

            return new RectInt(xMin, yMin, xMax - xMin + 1, yMax - yMin + 1);
        }
        
        public static int[] ResizePreserveWithOffset(
            int[] src, int srcW, int srcH,
            int dstW, int dstH,
            int offsetX, int offsetY,
            int fill01
        )
        {
            var dst = new int[dstW * dstH];
            for (int i = 0; i < dst.Length; i++)
                dst[i] = fill01;

            for (int y = 0; y < srcH; y++)
            {
                for (int x = 0; x < srcW; x++)
                {
                    int nx = x + offsetX;
                    int ny = y + offsetY;
                    if (nx < 0 || ny < 0 || nx >= dstW || ny >= dstH)
                        continue;

                    dst[ny * dstW + nx] = src[y * srcW + x];
                }
            }

            return dst;
        }

    }
}