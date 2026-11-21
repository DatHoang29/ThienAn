namespace Module.VideoWall.WPF.ViewModels.Isapi;

public static partial class VwIsapiSchemaCatalog
{
    private static void RegisterScreenSchemas()
    {
        // 9.7.8.1 Close all screens
        Register(new VwIsapiApiSchema(
            "9.7.8.1",
            "ScreenCtrl",
            [
                new("VideoWallID", "ID tường ghép", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int,
                    Tooltip: "ID tường ghép cần đóng (để trống = đóng toàn bộ). ⚠️ Chỉ test trên MockServer, cấm gửi lên thiết bị thật đang chiếu."),
                new("OutputID", "ID cổng xuất", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int,
                    Tooltip: "ID cổng xuất cần đóng (để trống = đóng toàn bộ)."),
            ]
        ));
    }
}
