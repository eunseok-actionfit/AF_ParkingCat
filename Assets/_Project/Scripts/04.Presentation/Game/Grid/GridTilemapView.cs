using UnityEngine;
using UnityEngine.Tilemaps;


namespace ParkingCat.Game.Grid
{
    /// <summary>
    /// 논리 그리드 데이터를 타일맵으로 시각화한다.<br/>
    /// 잔디/도로 레이어를 분리해 그린다.
    /// </summary>
    /// <remarks>
    /// 입력은 0/1 값의 2차원 배열을 전제로 한다.<br/>
    /// 경계 셀은 도로 타일을 한 칸 확장해 외곽을 보강한다.
    /// </remarks>
    public sealed class GridTilemapView : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField]
        [Tooltip("잔디 타일을 그릴 타일맵")]
        Tilemap grassTilemap;

        [SerializeField]
        [Tooltip("도로 타일을 그릴 타일맵")]
        Tilemap roadTilemap;

        [Header("Tiles")]
        [SerializeField]
        [Tooltip("기본 배경으로 사용할 잔디 타일")]
        TileBase grassTile;

        [SerializeField]
        [Tooltip("도로 셀(값 1)에 사용할 도로 타일")]
        TileBase roadTile;

        [Header("Options")]
        [SerializeField]
        [Tooltip("논리 셀 (0,0)에 대응하는 타일맵 셀 오프셋")]
        Vector3Int origin = Vector3Int.zero;

        [SerializeField]
        [Tooltip("그리기 전에 기존 타일을 모두 지울지 여부")]
        bool clearBeforeDraw = true;

        /// <summary>
        /// 0/1 그리드를 타일맵에 그린다.<br/>
        /// 1은 도로, 그 외 값은 잔디로 처리한다.
        /// </summary>
        /// <param name="grid01">0/1 논리 그리드</param>
        public void Draw(int[,] grid01)
        {
            if (roadTilemap == null || grassTilemap == null) return;
            if (grid01 == null) return;

            int h = grid01.GetLength(0);
            int w = grid01.GetLength(1);

            if (clearBeforeDraw)
            {
                roadTilemap.ClearAllTiles();
                grassTilemap.ClearAllTiles();
            }

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var pos = new Vector3Int(origin.x + x, origin.y + y, 0);

                    if (grid01[y, x] == 1)
                        roadTilemap.SetTile(pos, roadTile);

                    if (grid01[y, x] != 0)
                        continue;

                    grassTilemap.SetTile(pos, grassTile);


                    if (x == 0)
                        grassTilemap.SetTile(new Vector3Int(origin.x + x - 1, origin.y + y, 0), grassTile);

                    if (x == w - 1)
                        grassTilemap.SetTile(new Vector3Int(origin.x + x + 1, origin.y + y, 0), grassTile);

                    if (y == 0)
                        grassTilemap.SetTile(new Vector3Int(origin.x + x, origin.y + y - 1, 0), grassTile);

                    if (y == h - 1)
                        grassTilemap.SetTile(new Vector3Int(origin.x + x, origin.y + y + 1, 0), grassTile);
                }

            grassTilemap.SetTile(new Vector3Int(w, h, 0), grassTile);
            grassTilemap.SetTile(new Vector3Int(-1, h, 0), grassTile);
            grassTilemap.SetTile(new Vector3Int(w, -1, 0), grassTile);
            grassTilemap.SetTile(new Vector3Int(-1, -1, 0), grassTile);
            roadTilemap.RefreshAllTiles();
        }

        /// <summary>
        /// 논리 셀 좌표를 월드 좌표의 셀 중심으로 변환한다.
        /// </summary>
        /// <param name="cell">논리 셀 좌표</param>
        /// <returns>해당 셀 중심의 월드 좌표</returns>
        public Vector3 CellCenterWorld(Vector2Int cell)
        {
            if (roadTilemap == null)
                return new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);

            var c = new Vector3Int(origin.x + cell.x, origin.y + cell.y, 0);
            return roadTilemap.GetCellCenterWorld(c);
        }

        public bool TryWorldToCell(Vector3 world, int width, int height, out Vector2Int cell)
        {
            cell = default;

            if (roadTilemap == null)
                return false;

            // 월드 → 타일맵 셀
            var tileCell = roadTilemap.WorldToCell(world);

            int x = tileCell.x - origin.x;
            int y = tileCell.y - origin.y;

            if (x < 0 || y < 0 || x >= width || y >= height)
                return false;

            cell = new Vector2Int(x, y);
            return true;
        }

        public void AdjustOriginForCenterResize(int oldW, int oldH, int newW, int newH)
        {
            int dx = newW - oldW;
            int dy = newH - oldH;

            origin += new Vector3Int(-HalfFloor(dx), -HalfFloor(dy), 0);
        }

        static int HalfFloor(int d)
        {
            if (d >= 0) return d / 2;
            int a = -d;
            return -((a + 1) / 2);
        }


        public Vector3 CellCornerWorld(int x, int y)
        {
            // 논리 셀(x,y)의 "좌하단 코너" 월드좌표
            var tileCell = new Vector3Int(origin.x + x, origin.y + y, 0);
            return roadTilemap.CellToWorld(tileCell);
        }

        public void RectCornerWorlds(RectInt r, Vector3[] cornersOut)
        {
            // cornersOut: 길이 4 이상
            // 0: LB, 1: RB, 2: RT, 3: LT
            float cw = roadTilemap.cellSize.x;
            float ch = roadTilemap.cellSize.y;

            var lb = CellCornerWorld(r.x, r.y);
            var rb = lb + new Vector3(r.width * cw, 0f, 0f);
            var rt = lb + new Vector3(r.width * cw, r.height * ch, 0f);
            var lt = lb + new Vector3(0f, r.height * ch, 0f);

            cornersOut[0] = lb;
            cornersOut[1] = rb;
            cornersOut[2] = rt;
            cornersOut[3] = lt;
        }



    }
}