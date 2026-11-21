using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Helper;
using Xunit;

namespace VideoWallPlacement
{
    /// <summary>
    /// Kiểm VwWallPlacement: master 8 output 1920×1920 xếp 4×2 (canvas 7680×3840);
    /// canvas bố cục = các khối xếp ngang (tổng cột × số hàng lớn nhất), quy đổi tuyến tính lên master.
    /// ControllerConfig = ô nào của khối do bộ nào phụ trách.
    /// </summary>
    public class VwWallPlacementTests
    {
        private const int PanelW = 3840;
        private const int PanelH = 2160;
        private const int Bos = 1920;

        private static List<VwControllerOutput> MasterOutputs()
        {
            var list = new List<VwControllerOutput>();
            for (var c = 0; c < 4; c++)
                for (var r = 0; r < 2; r++)
                    list.Add(new VwControllerOutput { ID = $"m-{c}-{r}", ControllerId = "m", RectX = c * Bos, RectY = r * Bos, RectW = Bos, RectH = Bos });
            return list;
        }

        private static List<VwWallBlock> OneBlock() => [new() { ID = "b0", BlockNo = 0, Cols = 8, Rows = 4 }];

        private static List<VwWallBlock> TwoBlocks() =>
        [
            new() { ID = "b1", BlockNo = 1, Cols = 4, Rows = 4 },
            new() { ID = "b0", BlockNo = 0, Cols = 4, Rows = 4 },
        ];

        #region ControllerConfig

        [Fact]
        public void NormalizeCells_DropsOutsideEmptyAndDuplicate()
        {
            var json = """
                [{"Col":0,"Row":0,"ControllerId":"c2"},{"Col":0,"Row":0,"ControllerId":"c3"},
                 {"Col":5,"Row":0,"ControllerId":"c2"},{"Col":1,"Row":0,"ControllerId":""},{"Col":1,"Row":1,"ControllerId":"c4"}]
                """;
            var cells = VwWallPlacement.ParseCells(VwWallPlacement.NormalizeCells(json, 4, 4));

            Assert.Equal(2, cells.Count);
            Assert.Equal("c2", cells.Single(c => c.Col == 0 && c.Row == 0).ControllerId);
            Assert.Equal("c4", cells.Single(c => c.Col == 1 && c.Row == 1).ControllerId);
        }

        [Fact]
        public void NormalizeCells_ShrinkBlock_DropsCutCells()
        {
            var json = """[{"Col":3,"Row":3,"ControllerId":"c2"}]""";
            Assert.Null(VwWallPlacement.NormalizeCells(json, 2, 2));
        }

        [Fact]
        public void ParseCells_BadJson_IsEmpty() => Assert.Empty(VwWallPlacement.ParseCells("{oops"));

        [Fact]
        public void CellOwner_ManyControllersInOneBlock()
        {
            // Tường chữ L: C2 cột 0–1, C3 cột 2–3 (hàng 0–2), C4 cột 4–5 (hàng 3–4) trong 1 khối 6×5
            var cells = new List<string>();
            for (var r = 0; r < 3; r++)
                for (var c = 0; c < 4; c++)
                    cells.Add($$"""{"Col":{{c}},"Row":{{r}},"ControllerId":"{{(c < 2 ? "c2" : "c3")}}"}""");
            for (var r = 3; r < 5; r++)
                for (var c = 4; c < 6; c++)
                    cells.Add($$"""{"Col":{{c}},"Row":{{r}},"ControllerId":"c4"}""");
            var block = new VwWallBlock { ID = "b0", BlockNo = 0, Cols = 6, Rows = 5, ControllerConfig = "[" + string.Join(",", cells) + "]" };

            Assert.Equal("c2", VwWallPlacement.CellOwner(block, 1, 2));
            Assert.Equal("c3", VwWallPlacement.CellOwner(block, 2, 0));
            Assert.Equal("c4", VwWallPlacement.CellOwner(block, 5, 4));
            Assert.Null(VwWallPlacement.CellOwner(block, 5, 0)); // ô trống
        }

        #endregion

        #region Quy đổi sang master

        [Fact]
        public void MasterCanvas_FromMasterOutputs()
            => Assert.Equal((7680, 3840), VwWallPlacement.MasterCanvas(MasterOutputs()));

        [Theory]
        [InlineData(0, 0, 3840, 2160, 0, 0, 960, 960)]             // ô đầu
        [InlineData(19200, 2160, 3840, 2160, 4800, 960, 960, 960)] // cột 6, hàng 2
        [InlineData(26880, 6480, 3840, 2160, 6720, 2880, 960, 960)] // góc dưới phải
        [InlineData(11520, 0, 7680, 4320, 2880, 0, 1920, 1920)]    // vắt qua 2 cụm (chấp nhận)
        [InlineData(0, 0, 30720, 8640, 0, 0, 7680, 3840)]          // toàn tường
        [InlineData(0, 0, 3840, 1000, 0, 0, 960, 444)]             // làm tròn Y
        public void MapWindow_OneBlock(int x, int y, int w, int h, int ex, int ey, int ew, int eh)
        {
            var p = VwWallPlacement.BuildProjection(OneBlock(), MasterOutputs());
            Assert.Empty(p.Errors);
            Assert.Equal((8, 4), (p.WallCols, p.WallRows));

            var rect = VwWallPlacement.MapWindow(p, 0, x, y, w, h, PanelW, PanelH, out var error);
            Assert.Null(error);
            Assert.Equal((ex, ey, ew, eh), rect);
        }

        [Theory]
        [InlineData(0, 0, 3840, 2160, 3840, 0, 960, 960)]
        [InlineData(7680, 6480, 3840, 2160, 5760, 2880, 960, 960)]
        public void MapWindow_SecondBlock_AddsBlockOffset(int x, int y, int w, int h, int ex, int ey, int ew, int eh)
        {
            var p = VwWallPlacement.BuildProjection(TwoBlocks(), MasterOutputs());
            var rect = VwWallPlacement.MapWindow(p, 1, x, y, w, h, PanelW, PanelH, out var error);
            Assert.Null(error);
            Assert.Equal((ex, ey, ew, eh), rect);
        }

        [Fact]
        public void LayoutCanvas_UsesTallestBlock()
        {
            var p = VwWallPlacement.BuildProjection(
                [new() { ID = "a", BlockNo = 0, Cols = 4, Rows = 3 }, new() { ID = "b", BlockNo = 1, Cols = 2, Rows = 5 }],
                MasterOutputs());
            Assert.Equal((6, 5), (p.WallCols, p.WallRows));
        }

        [Fact]
        public void AdjacentWindows_ShareEdge()
        {
            var p = VwWallPlacement.BuildProjection(OneBlock(), MasterOutputs());
            var a = VwWallPlacement.MapWindow(p, 0, 0, 0, 1000, 1000, PanelW, PanelH, out _)!.Value;
            var b = VwWallPlacement.MapWindow(p, 0, 1000, 0, 1000, 1000, PanelW, PanelH, out _)!.Value;
            Assert.Equal(a.X + a.W, b.X);
        }

        [Fact]
        public void Error_NotSyncedOrNoLayout()
        {
            var p = VwWallPlacement.BuildProjection([], []);
            Assert.Contains(p.Errors, e => e.Contains("bố cục"));
            Assert.Contains(p.Errors, e => e.Contains("bộ điều khiển trung tâm"));
            Assert.Null(VwWallPlacement.MapWindow(p, 0, 0, 0, 3840, 2160, PanelW, PanelH, out var error));
            Assert.NotNull(error);
        }

        [Fact]
        public void Error_WindowOutsideWall()
        {
            var p = VwWallPlacement.BuildProjection(OneBlock(), MasterOutputs());
            Assert.Null(VwWallPlacement.MapWindow(p, 0, 30000, 0, 3840, 2160, PanelW, PanelH, out var error));
            Assert.Equal("Cửa sổ nằm ngoài tường.", error);
        }

        #endregion
    }
}
