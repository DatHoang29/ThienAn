namespace Module.VideoWall.WPF.ViewModels.Isapi;

public static partial class VwIsapiSchemaCatalog
{
    private static void RegisterDecodingSchemas()
    {
        // 9.7.2.1 Get decoding device status
        Register(new VwIsapiApiSchema("9.7.2.1", null, []));

        // 9.7.2.5 Start dynamic decoding
        Register(new VwIsapiApiSchema("9.7.2.5", null, [
            new("videoWallID", "ID tường", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            new("VWMWID", "ID cửa sổ chính", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            new("VWSWID", "ID cửa sổ con", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
        ]));

        // 9.7.2.6
        Register(new VwIsapiApiSchema("9.7.2.6", null, [
            new("videoWallID", "ID tường", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            new("VWMWID", "ID cửa sổ chính", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            new("VWSWID", "ID cửa sổ con", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
        ]));

        // 9.7.2.7
        Register(new VwIsapiApiSchema("9.7.2.7", null, [
            new("videoWallID", "ID tường", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            new("VWMWID", "ID cửa sổ chính", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            new("VWSWID", "ID cửa sổ con", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
        ]));

        // 9.7.2.8 Get decoding status of all sub-windows
        Register(new VwIsapiApiSchema("9.7.2.8", null, [
            new("videoWallID", "ID tường", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
        ]));

        // 9.7.2.2 GET network pre-monitor of a video wall
        Register(new VwIsapiApiSchema("9.7.2.2", null, [
            new("videoWallID", "ID tường", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
        ]));

        // 9.7.2.3 PUT Set network pre-monitor parameters
        Register(new VwIsapiApiSchema("9.7.2.3", "WallNPreMonitor", [
            new("videoWallID", "ID tường", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            new("id", "ID", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            new("resolution", "Độ phân giải", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, Required: true, Tooltip: "Đọc GET .../nPreMonitor/capabilities (9.7.2.16) để lấy giá trị hợp lệ cho thiết bị này"),
            new("frameRate", "Tốc độ khung hình", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Required: true, Tooltip: "Đọc capabilities để lấy giá trị hợp lệ"),
            new("bitRateType", "Loại Bitrate", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum, Required: true, Options: ["CBR", "VBR"]),
            new("bitRate", "Bitrate", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Required: false, Tooltip: "Chỉ dùng khi bitRateType=CBR"),
            new("maxBitRate", "Bitrate tối đa", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Required: false, Tooltip: "Chỉ dùng khi bitRateType=VBR"),
            new("intervalFrameI", "Khoảng cách I-Frame", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Required: true, Tooltip: "Khoảng cách giữa 2 I-Frame (ms)"),
        ]));

        // 9.7.2.4 GET sub window configuration capability
        Register(new VwIsapiApiSchema("9.7.2.4", null, [
            new("videoWallID", "ID tường", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            new("VWMWID", "ID cửa sổ chính", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            new("VWSWID", "ID cửa sổ con", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
        ]));

        // 9.7.2.9 GET sub-board stream exporting configurations
        Register(new VwIsapiApiSchema("9.7.2.9", null, []));

        // 9.7.2.10 PUT Set sub-board stream exporting (JSON body)
        Register(new VwIsapiApiSchema("9.7.2.10", null, [
            new("enabled", "Bật", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Bool, Required: false, DefaultValue: "true", Options: ["true", "false"]),
        ], BodyFormat: VwIsapiBodyFormat.Json));

        // 9.7.2.11 GET capability of default decoding delay
        Register(new VwIsapiApiSchema("9.7.2.11", null, []));

        // 9.7.2.12 GET default decoding delay parameters
        Register(new VwIsapiApiSchema("9.7.2.12", null, []));

        // 9.7.2.13 PUT Set default decoding delay parameters (JSON body)
        Register(new VwIsapiApiSchema("9.7.2.13", null, [
            new("defaultDecodeDelayParam", "Tham số trễ giải mã mặc định", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum, Required: true, Options: ["mostRealTime", "moreRealTime", "balance", "moreFluency", "mostFluency"]),
        ], BodyFormat: VwIsapiBodyFormat.Json));

        // 9.7.2.14 GET network pre-monitoring parameters of all video walls
        Register(new VwIsapiApiSchema("9.7.2.14", null, []));

        // 9.7.2.15 PUT Set network pre-monitoring parameters of all video walls (List-wrapper)
        Register(new VwIsapiApiSchema(
            "9.7.2.15",
            "WallNPreMonitor",
            [
                new("id", "ID", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("resolution", "Độ phân giải", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, Required: true, Tooltip: "Đọc GET .../nPreMonitor/capabilities (9.7.2.16) để lấy giá trị hợp lệ"),
                new("frameRate", "Tốc độ khung hình", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Required: true),
                new("bitRateType", "Loại Bitrate", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum, Required: true, Options: ["CBR", "VBR"]),
                new("bitRate", "Bitrate", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Required: false, Tooltip: "Chỉ dùng khi bitRateType=CBR"),
                new("maxBitRate", "Bitrate tối đa", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Required: false, Tooltip: "Chỉ dùng khi bitRateType=VBR"),
                new("intervalFrameI", "Khoảng cách I-Frame", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Required: true, Tooltip: "Khoảng cách giữa 2 I-Frame (ms)"),
            ],
            ListWrapperElement: "WallNPreMonitorList"
        ));

        // 9.7.2.16 GET capability of network pre-monitoring
        Register(new VwIsapiApiSchema("9.7.2.16", null, []));
    }
}
