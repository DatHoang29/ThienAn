namespace Module.VideoWall.WPF.ViewModels.Isapi;

public static partial class VwIsapiSchemaCatalog
{
    private static void RegisterOutputChannelSchemas()
    {
        // 9.7.3.1 GET audio output channels' params
        Register(new VwIsapiApiSchema(
            "9.7.3.1",
            null,
            []
        ));

        // 9.7.3.2 PUT Set parameters of all audio output channels
        Register(new VwIsapiApiSchema(
            "9.7.3.2",
            "AudioOutputChannel",
            [
                new("id", "ID kênh", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("enabled", "Bật", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Bool, Required: true, DefaultValue: "true", Options: ["true", "false"]),
                new("channelName", "Tên kênh", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String),
                new("sourceType", "Loại nguồn", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, Tooltip: "⚠️ Chỉ biết giá trị 'audioInput' — đọc GET /Audio/outputs/channels từ thiết bị để biết các giá trị hợp lệ"),
                new("AudioInputBindID", "ID liên kết Audio Input", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, GroupPath: "AudioWithInput"),
                new("wallID", "ID tường", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, GroupPath: "AudioWithSubWin"),
                new("windowID", "ID cửa sổ", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, GroupPath: "AudioWithSubWin"),
                new("subWinID", "ID cửa sổ con", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, GroupPath: "AudioWithSubWin")
            ],
            ListWrapperElement: "AudioOutputChannelList"
        ));

        // 9.7.3.3 PUT Set parameters of all video outputs
        Register(new VwIsapiApiSchema(
            "9.7.3.3",
            "VideoOutputChannel",
            [
                new("id", "ID kênh", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, Required: true, DefaultValue: "1", Tooltip: "ID 32-bit đóng gói dạng chuỗi"),
                new("portType", "Loại cổng", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum, Required: true, DefaultValue: "HDMI", Options: ["VGA", "CVBS", "HDMI", "Spot", "SDI", "DVI", "TVI", "LEDSendCard"]),
                new("resolution", "Độ phân giải", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, GroupPath: "OutputResolution", Tooltip: "Ví dụ: 1920*1080@60Hz"),
                new("imageWidth", "Chiều rộng (px)", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, GroupPath: "OutputResolution"),
                new("imageHeight", "Chiều cao (px)", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, GroupPath: "OutputResolution"),
                new("boardID", "Board ID", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, GroupPath: "PortInBoard", DefaultValue: "1"),
                new("portID", "Port ID", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, GroupPath: "PortInBoard", DefaultValue: "1"),
                new("boardType", "Loại Board", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, GroupPath: "PortInBoard", DefaultValue: "output"),
                new("deviceID", "ID thiết bị", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int),
                new("useEDIDResolution", "Dùng EDID Resolution", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Bool, Options: ["false", "true"]),
                new("outputMode", "Chế độ xuất", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum, Options: ["HDMI", "DVI", "AUTO"]),
                new("outputType", "Loại xuất", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum, Options: ["main", "sub", "alone"]),
                new("timeSequenceMode", "Chế độ Time Sequence", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum, Options: ["standard", "custom", "advance"], Advanced: true),
                new("name", "Tên", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String),
                new("outputPortEnabled", "Bật cổng xuất", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Bool, Options: ["true", "false"], Tooltip: "⚠️ Không thể tắt HDMI thường — chỉ có tác dụng với LED Send Card (đã xác nhận ở 09B)"),
                new("LEDSendCardResolutionEnabled", "Bật LED Send Card", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Bool, Options: ["false", "true"]),
                new("outputPortAccessType", "Loại truy cập cổng xuất", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum, Options: ["display", "projector", "LCD", "LED", "bigScreen", "conferenceTablet", "recordHost", "universal"], Advanced: true),
                new("outputBackgroundType", "Loại hình nền", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum, Options: ["default", "solidColor"]),
                new("outputBackgroundRGBColor", "Màu nền RGB", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Tooltip: "Chỉ dùng khi outputBackgroundType=solidColor"),
                new("HDMIDialEnabled", "Bật HDMI Dial", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Bool, Options: ["false", "true"])
            ],
            ListWrapperElement: "VideoOutputChannelList"
        ));

        // 9.7.3.4 Get basic parameters of all video outputs
        Register(new VwIsapiApiSchema(
            "9.7.3.4",
            null,
            []
        ));

        // 9.7.3.5 Get parameters of a specific video output
        Register(new VwIsapiApiSchema(
            "9.7.3.5",
            null,
            [
                new("channelID", "ID cổng xuất", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            ]
        ));

        // 9.7.3.6 Set parameters of a specific video output
        Register(new VwIsapiApiSchema(
            "9.7.3.6",
            "VideoOutputChannel",
            [
                new("channelID", "ID cổng xuất (Path)", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("id", "ID cổng xuất (Body)", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, Required: true, DefaultValue: "1"),
                new("portType", "Loại cổng", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum, Required: true, DefaultValue: "HDMI",
                    Options: ["VGA", "CVBS", "HDMI", "Spot", "SDI", "DVI", "TVI", "LEDSendCard"]),
                new("name", "Tên cổng ra", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String),
                new("resolution", "Độ phân giải", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, GroupPath: "OutputResolution", Tooltip: "Ví dụ: 1920*1080@60Hz"),
                new("imageWidth", "Chiều rộng (px)", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, GroupPath: "OutputResolution"),
                new("imageHeight", "Chiều cao (px)", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, GroupPath: "OutputResolution"),
                new("useEDIDResolution", "Dùng EDID Resolution", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Bool, Options: ["false", "true"], DefaultValue: "false"),
                new("outputMode", "Chế độ xuất", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum, Options: ["standard", "custom"]),
                new("outputType", "Loại xuất", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum, Options: ["main", "sub"]),
                new("outputPortEnabled", "Bật cổng xuất", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Bool, Options: ["true", "false"], DefaultValue: "true"),
                new("outputBackgroundType", "Loại hình nền", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum, Options: ["black", "blue", "custom"]),
                new("outputBackgroundRGBColor", "Màu nền RGB", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int),
                new("HDMIDialEnabled", "Bật HDMI Dial", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Bool, Options: ["false", "true"]),
                new("LEDSendCardResolutionEnabled", "Bật LED Send Card", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Bool, Options: ["false", "true"]),
                // Advanced fields
                new("boardID", "Board ID", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, GroupPath: "PortInBoard", Advanced: true),
                new("deviceID", "ID thiết bị", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, GroupPath: "PortInBoard", Advanced: true),
                new("timeSequenceMode", "Chế độ Time Sequence", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, Advanced: true),
                new("CustomMode", "Chế độ Custom", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, Advanced: true),
                new("AdvanceMode", "Chế độ Advance", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, Advanced: true),
            ]
        ));

        // 9.7.3.7 Get the capability of a specific video output
        Register(new VwIsapiApiSchema(
            "9.7.3.7",
            null,
            [
                new("channelID", "ID cổng xuất", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            ]
        ));

        // 9.7.3.8 PUT Set parameters of all video output channels
        Register(new VwIsapiApiSchema(
            "9.7.3.8",
            "VideoOutputChannel",
            [
                new("id", "ID cổng xuất", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Required: true, DefaultValue: "1", Tooltip: "⚠️ Ở đây là kiểu Int (khác 9.7.3.3 là String) — tài liệu không nhất quán"),
                new("portType", "Loại cổng", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum, Options: ["VGA", "CVBS", "HDMI", "Spot", "SDI", "DVI"], Tooltip: "Thiếu TVI/LEDSendCard so với 9.7.3.3 — có thể tài liệu bỏ sót"),
                new("resolution", "Độ phân giải", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, GroupPath: "OutputResolution"),
                new("imageWidth", "Chiều rộng (px)", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, GroupPath: "OutputResolution"),
                new("imageHeight", "Chiều cao (px)", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, GroupPath: "OutputResolution")
            ]
        ));

        // 9.7.3.9 GET configuration capability of all video output channels
        Register(new VwIsapiApiSchema(
            "9.7.3.9",
            null,
            []
        ));
    }
}
