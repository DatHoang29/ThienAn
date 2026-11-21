using Module.VideoWall.Core.Constants;

namespace Module.VideoWall.Core.Helper
{
    /// <summary>
    /// Description: Cac ham toan hoc quy doi toa do va luoi hien thi Video Wall
    /// Created date: 10/09/2026
    /// </summary>
    public static class VwGeometryHelper
    {
        /// <summary>
        /// Author: Đạt
        /// Description: Phần lõi thuần (không I/O) của phép quy đổi toạ độ — tách riêng để test
        ///              trực tiếp bằng số liệu, không cần Host/DB.
        /// Created date: 16/08/2026
        /// </summary>
        public static (int X, int Y, int W, int H) ToIsapiLocalRect(
            int originCol,
            int originRow,
            int x,
            int y,
            int w,
            int h,
            int panelWidthPx = VwWallProfile.PanelWidthPx,
            int panelHeightPx = VwWallProfile.PanelHeightPx,
            int baseOutputSize = VwWallProfile.BaseOutputSize)
        {
            var localX = x - originCol * panelWidthPx;
            var localY = y - originRow * panelHeightPx;

            return (
                localX * baseOutputSize / panelWidthPx,
                localY * baseOutputSize / panelHeightPx,
                w * baseOutputSize / panelWidthPx,
                h * baseOutputSize / panelHeightPx
            );
        }

        /// <summary>
        /// Quy doi toa do tuyet doi toan tuong sang uniformCoordinate cho bo trung tam
        /// cascade theo ti le truc tiep giua wallCanvas va centerCanvas.
        /// </summary>
        public static (int X, int Y, int W, int H)? ToCenterUniformRect(
            int x,
            int y,
            int w,
            int h,
            (int W, int H) wallCanvas,
            (int W, int H) centerCanvas)
        {
            if (w <= 0 || h <= 0 || wallCanvas.W <= 0 || wallCanvas.H <= 0 || centerCanvas.W <= 0 || centerCanvas.H <= 0)
                return null;

            var rx = (int)((long)x * centerCanvas.W / wallCanvas.W);
            var ry = (int)((long)y * centerCanvas.H / wallCanvas.H);
            var rw = (int)((long)w * centerCanvas.W / wallCanvas.W);
            var rh = (int)((long)h * centerCanvas.H / wallCanvas.H);

            if (rw <= 0) rw = 1;
            if (rh <= 0) rh = 1;

            return (rx, ry, rw, rh);
        }

        /// <summary>
        /// Overload quy doi toa do bo trung tam cascade theo so cot / hang tuong.
        /// </summary>
        public static (int X, int Y, int W, int H) ToCenterUniformRect(
            int x,
            int y,
            int w,
            int h,
            int wallCols,
            int wallRows,
            int panelWidthPx = VwWallProfile.PanelWidthPx,
            int panelHeightPx = VwWallProfile.PanelHeightPx,
            int baseOutputSize = VwWallProfile.BaseOutputSize)
        {
            if (wallCols <= 0 || wallRows <= 0)
                return (0, 0, 0, 0);

            var wallCanvas = (wallCols * panelWidthPx, wallRows * panelHeightPx);
            var centerCanvas = (baseOutputSize, baseOutputSize);
            var result = ToCenterUniformRect(x, y, w, h, wallCanvas, centerCanvas);
            return result ?? (0, 0, 0, 0);
        }
    }
}
