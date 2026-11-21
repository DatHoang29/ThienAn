using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Dto.WallTopology;
using Module.VideoWall.Core.Entities;
using Newtonsoft.Json;

namespace Module.VideoWall.Core.Helper
{
    /// <summary>
    /// Description: Dữ liệu quy đổi cửa sổ trên tường (toạ độ trong khối, đơn vị panel 3840×2160)
    ///              sang toạ độ uniform của bộ trung tâm (master).
    /// Created date: 06/11/2026
    /// </summary>
    public sealed class VwMasterProjection
    {
        /// <summary>BlockNo → số cột đứng trước khối (khối xếp ngang theo BlockNo).</summary>
        public Dictionary<int, int> BlockColOffset { get; } = new();

        /// <summary>Canvas bố cục: tổng cột các khối × số hàng lớn nhất (đơn vị màn).</summary>
        public int WallCols { get; set; }
        public int WallRows { get; set; }

        /// <summary>Canvas uniform của master (từ cổng ra của master).</summary>
        public int MasterWidth { get; set; }
        public int MasterHeight { get; set; }

        public List<string> Errors { get; } = new();
    }

    /// <summary>
    /// Description: Hàm thuần (không I/O) cho bố cục:
    ///              - Đọc / chuẩn hoá VwWallBlock.ControllerConfig (ô nào do bộ nào phụ trách).
    ///              - Quy đổi cửa sổ sang master: canvas bố cục (các khối xếp ngang) co giãn tuyến tính lên canvas master.
    ///              Vị trí cổng ra trên ô do người dùng gán tay (VwControllerOutput.WallBlockId / GridCol / GridRow).
    /// Created date: 06/11/2026 — cập nhật 07/11/2026 (bỏ xếp bộ con trái → phải và kiểm khớp tường vật lý)
    /// </summary>
    public static class VwWallPlacement
    {
        #region ControllerConfig

        /// <summary>Đọc mảng ô của khối; JSON hỏng / trống ⇒ rỗng.</summary>
        public static List<VwBlockControllerCell> ParseCells(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return [];
            try
            {
                return JsonConvert.DeserializeObject<List<VwBlockControllerCell>>(json) ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }

        /// <summary>
        /// Chuẩn hoá: bỏ ô ngoài khối, ô không có bộ, ô trùng (giữ ô đầu tiên); sắp theo hàng rồi cột.
        /// Trả null nếu không còn ô nào.
        /// </summary>
        public static string? NormalizeCells(string? json, int cols, int rows)
        {
            var cells = ParseCells(json)
                .Where(c => c.Col >= 0 && c.Row >= 0 && c.Col < cols && c.Row < rows && !string.IsNullOrWhiteSpace(c.ControllerId))
                .GroupBy(c => (c.Col, c.Row))
                .Select(g => g.First())
                .OrderBy(c => c.Row).ThenBy(c => c.Col)
                .ToList();
            return cells.Count == 0 ? null : JsonConvert.SerializeObject(cells);
        }

        /// <summary>Bộ phụ trách ô (col, row) của khối; không có ⇒ null.</summary>
        public static string? CellOwner(VwWallBlock block, int col, int row)
            => ParseCells(block.ControllerConfig).FirstOrDefault(c => c.Col == col && c.Row == row)?.ControllerId;

        #endregion

        #region Vị trí cổng trong tường bộ con (WallOutputList)

        /// <summary>
        /// Description: Bảng cổng ra → rect trong tường của MỘT bộ con, tính từ Bố cục gán cổng:
        ///              - Vùng của bộ = các ô gán cho bộ (ControllerConfig) trong 1 khối; ô góc trên-trái của vùng = (0, 0) của tường bộ con.
        ///              - Cổng gán cho màn ở ô (col, row) ⇒ rect ((col − cột đầu) × baseSize, (row − hàng đầu) × baseSize, baseSize, baseSize).
        ///              - Cổng không gán màn / cổng mất trên thiết bị / chưa có outputID ⇒ không có trong bảng (bỏ khỏi tường).
        ///              Vùng không phải hình chữ nhật vẫn tính theo ô góc trên-trái (phụ thuộc cấu hình trên web).
        /// Created date: 07/11/2026
        /// </summary>
        public static (List<VwWallOutputTarget> Targets, string? Error) BuildWallOutputs(
            string controllerId, IEnumerable<VwWallBlock> blocks, IEnumerable<VwScreen> screens, IEnumerable<VwControllerOutput> outputs, int baseSize)
        {
            var areas = blocks
                .Select(b => (Block: b, Cells: ParseCells(b.ControllerConfig).Where(c => c.ControllerId == controllerId).ToList()))
                .Where(x => x.Cells.Count > 0)
                .ToList();
            if (areas.Count == 0)
                return ([], "Bộ chưa được gán ô nào trong Bố cục tường.");
            if (areas.Count > 1)
                return ([], "Bộ được gán ô ở nhiều khối — tường của một bộ con chỉ nằm trong một khối.");

            var (block, cells) = areas[0];
            var minCol = cells.Min(c => c.Col);
            var minRow = cells.Min(c => c.Row);
            var ports = outputs
                .Where(o => o.ControllerId == controllerId && o.DeviceOutputId != null && o.DeviceState != VwOutputDeviceState.Missing)
                .ToDictionary(o => o.ID);

            var targets = screens
                .Where(s => s.ControllerOutputId != null && ports.ContainsKey(s.ControllerOutputId)
                            && s.WallBlockId == block.ID && s.GridCol != null && s.GridRow != null)
                .Select(s => new VwWallOutputTarget(
                    ports[s.ControllerOutputId!].ID,
                    ports[s.ControllerOutputId!].DeviceOutputId!.Value,
                    (s.GridCol!.Value - minCol) * baseSize,
                    (s.GridRow!.Value - minRow) * baseSize,
                    baseSize,
                    baseSize))
                .OrderBy(t => t.Y).ThenBy(t => t.X)
                .ToList();
            return (targets, null);
        }

        #endregion

        #region Quy đổi sang master

        /// <summary>Canvas uniform của master = hình bao các ô cổng ra của master.</summary>
        public static (int W, int H) MasterCanvas(IEnumerable<VwControllerOutput> masterOutputs)
        {
            var list = masterOutputs.Where(o => o.DeviceState != VwOutputDeviceState.Missing && o.RectW > 0 && o.RectH > 0).ToList();
            return list.Count == 0
                ? (0, 0)
                : (list.Max(o => (o.RectX ?? 0) + o.RectW!.Value), list.Max(o => (o.RectY ?? 0) + o.RectH!.Value));
        }

        /// <summary>Dựng dữ liệu quy đổi: canvas bố cục từ các khối, canvas master từ cổng ra của master.</summary>
        public static VwMasterProjection BuildProjection(IEnumerable<VwWallBlock> blocks, IEnumerable<VwControllerOutput> masterOutputs)
        {
            var p = new VwMasterProjection();
            var blockList = blocks.Where(b => b.BlockNo != null && b.Cols > 0 && b.Rows > 0).ToList();
            foreach (var r in BlockRanges(blockList))
                p.BlockColOffset[r.Block.BlockNo!.Value] = r.Offset;

            p.WallCols = blockList.Sum(b => b.Cols!.Value);
            p.WallRows = blockList.Count == 0 ? 0 : blockList.Max(b => b.Rows!.Value);
            (p.MasterWidth, p.MasterHeight) = MasterCanvas(masterOutputs);

            if (blockList.Count == 0)
                p.Errors.Add("Chưa có bố cục đang áp dụng.");
            if (p.MasterWidth == 0)
                p.Errors.Add("Chưa đồng bộ cổng ra của bộ điều khiển trung tâm.");

            return p;
        }

        /// <summary>
        /// Quy đổi cửa sổ (toạ độ trong khối blockNo, mỗi màn panelW × panelH) sang hình chữ nhật uniform trên master.
        /// Tính theo hai mép rồi làm tròn để hai cửa sổ sát nhau không hở / chồng 1 đơn vị.
        /// </summary>
        public static (int X, int Y, int W, int H)? MapWindow(
            VwMasterProjection p, int? blockNo, int x, int y, int w, int h,
            int panelW, int panelH, out string? error)
        {
            error = null;
            if (p.Errors.Count > 0)
            {
                error = string.Join("; ", p.Errors);
                return null;
            }
            if (w <= 0 || h <= 0)
            {
                error = "Cửa sổ có kích thước không hợp lệ.";
                return null;
            }

            var offset = blockNo is int b && p.BlockColOffset.TryGetValue(b, out var o) ? o : 0;
            long wallW = (long)p.WallCols * panelW;
            long wallH = (long)p.WallRows * panelH;
            long gx = (long)offset * panelW + x;

            var left = Scale(gx, p.MasterWidth, wallW);
            var right = Scale(gx + w, p.MasterWidth, wallW);
            var top = Scale(y, p.MasterHeight, wallH);
            var bottom = Scale((long)y + h, p.MasterHeight, wallH);

            if (left < 0 || top < 0 || right > p.MasterWidth || bottom > p.MasterHeight)
            {
                error = "Cửa sổ nằm ngoài tường.";
                return null;
            }
            if (right <= left || bottom <= top)
            {
                error = "Cửa sổ quá nhỏ sau khi quy đổi sang master.";
                return null;
            }

            return (left, top, right - left, bottom - top);
        }

        private static List<(VwWallBlock Block, int Offset)> BlockRanges(IEnumerable<VwWallBlock> blocks)
        {
            var result = new List<(VwWallBlock, int)>();
            var offset = 0;
            foreach (var b in blocks.Where(b => b.BlockNo != null && b.Cols > 0 && b.Rows > 0).OrderBy(b => b.BlockNo))
            {
                result.Add((b, offset));
                offset += b.Cols!.Value;
            }
            return result;
        }

        private static int Scale(long v, int target, long source)
            => (int)Math.Round((double)v * target / source, MidpointRounding.AwayFromZero);

        #endregion
    }

    /// <summary>Một cổng ra trong tường bộ con: OutputId = VwControllerOutput.ID, DeviceOutputId = outputID trên thiết bị, rect uniform.</summary>
    public sealed record VwWallOutputTarget(string OutputId, int DeviceOutputId, int X, int Y, int W, int H);
}
