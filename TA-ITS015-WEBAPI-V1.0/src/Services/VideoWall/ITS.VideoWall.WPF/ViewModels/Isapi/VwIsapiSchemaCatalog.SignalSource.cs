namespace Module.VideoWall.WPF.ViewModels.Isapi;

public static partial class VwIsapiSchemaCatalog
{
    private static void RegisterSignalSourceSchemas()
    {
        // 9.7.4.1 GET audio capabilities
        Register(new VwIsapiApiSchema(
            "9.7.4.1",
            null,
            []
        ));

        // 9.7.4.2 GET capability of adding signal source group
        Register(new VwIsapiApiSchema(
            "9.7.4.2",
            null,
            []
        ));

        // 9.7.4.3 POST Get signal source groups
        Register(new VwIsapiApiSchema(
            "9.7.4.3",
            null,
            []
        ));

        // 9.7.4.4 GET capability editing signal source group
        Register(new VwIsapiApiSchema(
            "9.7.4.4",
            null,
            []
        ));

        // 9.7.4.5 GET capability no signal params
        Register(new VwIsapiApiSchema(
            "9.7.4.5",
            null,
            []
        ));

        // 9.7.4.6 GET no signal params
        Register(new VwIsapiApiSchema(
            "9.7.4.6",
            null,
            []
        ));

        // 9.7.4.7 GET video capabilities
        Register(new VwIsapiApiSchema(
            "9.7.4.7",
            null,
            []
        ));

        // 9.7.4.8 Get parameters of all video input channels
        Register(new VwIsapiApiSchema(
            "9.7.4.8",
            null,
            []
        ));

        // 9.7.4.9 PUT Set params of all video input channels
        Register(new VwIsapiApiSchema(
            "9.7.4.9",
            "VideoInputChannel",
            [
                new("id", "ID kênh vào (Body)", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("name", "Tên nguồn tín hiệu", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, DefaultValue: "Dashboard GT", Tooltip: "Use-case chính: đổi tên nguồn tín hiệu hiển thị"),
                new("portType", "Loại cổng", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum, DefaultValue: "HDMI", Options: ["VGA", "CVBS", "HDMI", "DVI", "SDI", "YPbPr", "RGB", "HDBaseT", "DP", "Joint"]),
                // Advanced fields
                new("useSelfdefineResolution", "Tự định nghĩa độ phân giải", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Bool, Options: ["false", "true"], Advanced: true),
                new("selfdefineResolutionNo", "Số độ phân giải tự định nghĩa", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Advanced: true),
                new("imageWidth", "Chiều rộng", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Advanced: true),
                new("imageHeight", "Chiều cao", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Advanced: true),
                new("FPS", "FPS", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Advanced: true),
                new("boardID", "Board ID", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, GroupPath: "PortInBoard", Advanced: true),
                new("portID", "Port ID", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, GroupPath: "PortInBoard", Advanced: true),
                new("deviceID", "Device ID", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, GroupPath: "PortInBoard", Advanced: true),
                new("horizontalCount", "Ghép ngang (chỉ Joint)", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, GroupPath: "JointPort", Advanced: true, Tooltip: "Số lượng ghép ngang (chỉ khi portType=Joint)"),
                new("verticalCount", "Ghép dọc (chỉ Joint)", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, GroupPath: "JointPort", Advanced: true, Tooltip: "Số lượng ghép dọc (chỉ khi portType=Joint)"),
                new("jointMatrixEnabled", "Bật ma trận ghép", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Bool, Options: ["false", "true"], Advanced: true),
                new("videoCodecType", "Loại Codec video", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, Advanced: true),
                new("enabled", "Bật SmartCodec", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Bool, Options: ["false", "true"], GroupPath: "SmartCodec", Advanced: true),
                new("audioCompressionType", "Loại nén Audio", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, Advanced: true),
                new("inputPortEnabled", "Bật cổng vào", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Bool, Options: ["true", "false"], Advanced: true),
                new("timeSequenceResolution", "Độ phân giải Time Sequence", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, Advanced: true),
                new("inputContent", "Nội dung đầu vào", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, Advanced: true),
                new("enabled", "Bật OptimumResolution", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Bool, Options: ["false", "true"], GroupPath: "OptimumResolution", Advanced: true),
                new("imageWidth", "Optimum Width", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, GroupPath: "OptimumResolution", Advanced: true),
                new("imageHeight", "Optimum Height", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, GroupPath: "OptimumResolution", Advanced: true),
                new("inputPortAccessType", "Loại truy cập cổng vào", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, Advanced: true),
                new("streamType", "Loại Stream", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, Advanced: true),
                new("bitRateType", "Loại BitRate", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, Advanced: true),
                new("intervalFrame", "Interval Frame (1-400)", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Min: 1, Max: 400, Advanced: true),
                new("picQuality", "Chất lượng ảnh (1-6)", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Min: 1, Max: 6, Advanced: true),
                new("resolution", "Độ phân giải", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, Advanced: true),
                new("videoType", "Loại Video", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, Advanced: true),
                new("videoBitRate", "Video BitRate", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, Advanced: true),
                new("customBitRate", "Custom BitRate (128-16384)", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Min: 128, Max: 16384, Advanced: true),
            ],
            ListWrapperElement: "VideoInputChannelList"
        ));

        // 9.7.4.10 Set parameters of a specified signal source
        Register(new VwIsapiApiSchema(
            "9.7.4.10",
            "VideoInputChannel",
            [
                new("channelID", "ID kênh vào (Path)", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("id", "ID kênh vào (Body)", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("name", "Tên nguồn tín hiệu", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, DefaultValue: "Dashboard GT",
                    Tooltip: "Use-case chính: đổi tên nguồn tín hiệu hiển thị"),
                new("RelateScreenServer", "RelateScreenServer", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Bool, Required: true,
                    Options: ["true", "false"], DefaultValue: "true"),
                new("portType", "Loại cổng", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum, DefaultValue: "HDMI",
                    Options: ["VGA", "CVBS", "HDMI", "DVI", "SDI", "YPbPr", "RGB", "HDBaseT", "DP", "Joint"]),
                // Advanced fields
                new("useSelfdefineResolution", "Tự định nghĩa độ phân giải", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Bool, Options: ["false", "true"], Advanced: true),
                new("selfdefineResolutionNo", "Số độ phân giải tự định nghĩa", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Advanced: true),
                new("imageWidth", "Chiều rộng", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Advanced: true),
                new("imageHeight", "Chiều cao", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Advanced: true),
                new("FPS", "FPS", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Advanced: true),
                new("boardID", "Board ID", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, GroupPath: "PortInBoard", Advanced: true),
                new("portID", "Port ID", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, GroupPath: "PortInBoard", Advanced: true),
                new("deviceID", "Device ID", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, GroupPath: "PortInBoard", Advanced: true),
                new("horizontalCount", "Ghép ngang (chỉ Joint)", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, GroupPath: "JointPort", Advanced: true, Tooltip: "Số lượng ghép ngang (chỉ khi portType=Joint)"),
                new("verticalCount", "Ghép dọc (chỉ Joint)", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, GroupPath: "JointPort", Advanced: true, Tooltip: "Số lượng ghép dọc (chỉ khi portType=Joint)"),
                new("jointMatrixEnabled", "Bật ma trận ghép", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Bool, Options: ["false", "true"], Advanced: true),
                new("videoCodecType", "Loại Codec video", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, Advanced: true),
                new("enabled", "Bật SmartCodec", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Bool, Options: ["false", "true"], GroupPath: "SmartCodec", Advanced: true),
                new("audioCompressionType", "Loại nén Audio", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, Advanced: true),
                new("inputPortEnabled", "Bật cổng vào", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Bool, Options: ["true", "false"], Advanced: true),
                new("timeSequenceResolution", "Độ phân giải Time Sequence", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, Advanced: true),
                new("inputContent", "Nội dung đầu vào", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, Advanced: true),
                new("enabled", "Bật OptimumResolution", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Bool, Options: ["false", "true"], GroupPath: "OptimumResolution", Advanced: true),
                new("imageWidth", "Optimum Width", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, GroupPath: "OptimumResolution", Advanced: true),
                new("imageHeight", "Optimum Height", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, GroupPath: "OptimumResolution", Advanced: true),
                new("inputPortAccessType", "Loại truy cập cổng vào", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, Advanced: true),
                new("streamType", "Loại Stream", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, Advanced: true),
                new("bitRateType", "Loại BitRate", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, Advanced: true),
                new("intervalFrame", "Interval Frame (1-400)", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Min: 1, Max: 400, Advanced: true),
                new("picQuality", "Chất lượng ảnh (1-6)", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Min: 1, Max: 6, Advanced: true),
                new("resolution", "Độ phân giải", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, Advanced: true),
                new("videoType", "Loại Video", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, Advanced: true),
                new("videoBitRate", "Video BitRate", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, Advanced: true),
                new("customBitRate", "Custom BitRate (128-16384)", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Min: 128, Max: 16384, Advanced: true),
            ]
        ));

        // 9.7.4.11 GET params of specific signal source
        Register(new VwIsapiApiSchema(
            "9.7.4.11",
            null,
            [
                new("channelID", "ID kênh", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true)
            ]
        ));

        // 9.7.4.12 GET color params
        Register(new VwIsapiApiSchema(
            "9.7.4.12",
            null,
            [
                new("channelID", "ID kênh", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true)
            ]
        ));

        // 9.7.4.13 PUT Set color params
        Register(new VwIsapiApiSchema(
            "9.7.4.13",
            "Color",
            [
                new("channelID", "ID kênh", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("brightnessLevel", "Độ sáng", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Min: 0, Max: 100),
                new("contrastLevel", "Độ tương phản", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Min: 0, Max: 100),
                new("saturationLevel", "Độ bão hòa", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Min: 0, Max: 100),
                new("hueLevel", "Màu sắc", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Min: 0, Max: 100),
                new("sharpnessLevel", "Độ sắc nét", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Min: 0, Max: 100)
            ]
        ));

        // 9.7.4.14 GET color config capability
        Register(new VwIsapiApiSchema(
            "9.7.4.14",
            null,
            [
                new("channelID", "ID kênh", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true)
            ]
        ));

        // 9.7.4.15 Get picture cropping parameters of a specific signal source
        Register(new VwIsapiApiSchema(
            "9.7.4.15",
            null,
            [
                new("channelID", "ID kênh vào", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            ]
        ));

        // 9.7.4.16 Set picture cropping parameters of a specified signal source
        Register(new VwIsapiApiSchema(
            "9.7.4.16",
            "InputCutOff",
            [
                new("channelID", "ID kênh vào", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("leftCutOff", "Cắt lề trái (0-30)", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Min: 0, Max: 30, DefaultValue: "0"),
                new("rightCutOff", "Cắt lề phải (0-30)", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Min: 0, Max: 30, DefaultValue: "0"),
                new("topCutOff", "Cắt lề trên (0-30)", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Min: 0, Max: 30, DefaultValue: "0"),
                new("bottomCutOff", "Cắt lề dưới (0-30)", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Min: 0, Max: 30, DefaultValue: "0"),
            ]
        ));

        // 9.7.4.17 GET cropping capability
        Register(new VwIsapiApiSchema(
            "9.7.4.17",
            null,
            [
                new("channelID", "ID kênh", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true)
            ]
        ));

        // 9.7.4.18 Get captured pictures (binary response)
        Register(new VwIsapiApiSchema(
            "9.7.4.18",
            null,
            [
                new("channelID", "ID kênh vào", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1",
                    Tooltip: "⚠️ Phản hồi từ thiết bị là dữ liệu nhị phân ảnh JPEG, không phải văn bản."),
            ]
        ));

        // 9.7.4.19 GET position capability
        Register(new VwIsapiApiSchema(
            "9.7.4.19",
            null,
            [
                new("channelID", "ID kênh", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true)
            ]
        ));

        // 9.7.4.20 PUT Set custom resolution
        Register(new VwIsapiApiSchema(
            "9.7.4.20",
            "Resolution",
            [
                new("channelID", "ID kênh", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("id", "ID", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Required: true),
                new("resolutionName", "Tên độ phân giải", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String),
                new("imageWidth", "Chiều rộng", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Required: true),
                new("imageHeight", "Chiều cao", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Required: true),
                new("refreshRate", "Tần số quét", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Tooltip: "Giá trị điển hình: 25/30/60 — đọc capability 9.7.4.21 để biết giá trị hợp lệ"),
                new("colorDepth", "Độ sâu màu", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum, Options: ["8", "16", "32"], Tooltip: "Giá trị điển hình: 8/16/32", Advanced: true),
                new("scanType", "Loại quét", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum, Options: ["progressiveScan", "intervalScan"], Tooltip: "Sai alignment → lỗi theResolutionDoesNotMeetTheAlignmentConstraints", Advanced: true)
            ]
        ));

        // 9.7.4.21 GET custom resolution capability
        Register(new VwIsapiApiSchema(
            "9.7.4.21",
            null,
            [
                new("channelID", "ID kênh", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true)
            ]
        ));

        // 9.7.4.22 GET OSD config capability
        Register(new VwIsapiApiSchema(
            "9.7.4.22",
            null,
            [
                new("channelID", "ID kênh", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true)
            ]
        ));

        // 9.7.4.23 GET video input capability
        Register(new VwIsapiApiSchema(
            "9.7.4.23",
            null,
            []
        ));

        // 9.7.4.24 GET splicing config of all
        Register(new VwIsapiApiSchema(
            "9.7.4.24",
            null,
            []
        ));

        // 9.7.4.25 PUT Set jointing parameters
        Register(new VwIsapiApiSchema(
            "9.7.4.25",
            "JoinSignal",
            [
                new("channelID", "ID kênh", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("id", "ID", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Required: true, Min: 0, Max: 100),
                new("enabled", "Bật", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Bool, Required: true, Options: ["true", "false"]),
                new("joinName", "Tên ghép", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, Required: true, Tooltip: "Tối đa 10 ký tự"),
                new("camMode", "Chế độ Cam", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Required: true),
                new("rows", "Số hàng", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Required: true, Min: 0, Max: 100),
                new("columns", "Số cột", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Required: true, Min: 0, Max: 100),
                new("joinSignalNo", "Số hiệu tín hiệu ghép", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, DefaultValue: "1"),
                new("signalNoListRaw", "Danh sách kênh ghép (cách nhau bởi dấu phẩy, tối đa 10)", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, Advanced: true, Tooltip: "Nhập các channelID cần ghép, phân tách bởi dấu phẩy, vd: 16842753,16842754")
            ]
        ));

        // 9.7.4.26 GET splicing params
        Register(new VwIsapiApiSchema(
            "9.7.4.26",
            null,
            [
                new("channelID", "ID kênh", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true)
            ]
        ));

        // 9.7.4.27 GET splicing capability
        Register(new VwIsapiApiSchema(
            "9.7.4.27",
            null,
            []
        ));

        // 9.7.4.28 GET all video streams
        Register(new VwIsapiApiSchema(
            "9.7.4.28",
            null,
            []
        ));

        // 9.7.4.29 PUT Set all video stream params
        Register(new VwIsapiApiSchema(
            "9.7.4.29",
            null,
            []
        ));

        // 9.7.4.30 PUT Set params of specific video stream
        Register(new VwIsapiApiSchema(
            "9.7.4.30",
            null,
            [
                new("channelID", "ID kênh", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1")
            ]
        ));

        // 9.7.4.31 DELETE params of specific video stream
        Register(new VwIsapiApiSchema(
            "9.7.4.31",
            null,
            [
                new("channelID", "ID kênh", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1")
            ]
        ));

        // 9.7.4.32 GET params of specified video stream
        Register(new VwIsapiApiSchema(
            "9.7.4.32",
            null,
            [
                new("channelID", "ID kênh", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true)
            ]
        ));

        // 9.7.4.33 GET video stream capability
        Register(new VwIsapiApiSchema(
            "9.7.4.33",
            null,
            []
        ));

        // 9.7.4.34 GET capability searching network input
        Register(new VwIsapiApiSchema(
            "9.7.4.34",
            null,
            []
        ));
    }
}
