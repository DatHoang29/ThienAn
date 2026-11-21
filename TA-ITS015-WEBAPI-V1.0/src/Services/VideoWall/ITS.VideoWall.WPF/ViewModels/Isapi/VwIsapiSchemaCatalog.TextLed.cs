namespace Module.VideoWall.WPF.ViewModels.Isapi;

public static partial class VwIsapiSchemaCatalog
{
    // Shared LED body fields reused by 9.7.9.1, 9.7.9.3, 9.7.9.6
    private static VwIsapiFieldDef[] BuildLedBodyFields() =>
    [
        new("id", "LED ID", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int),
        new("enabled", "Bật", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Bool, Required: true, Options: ["true", "false"]),
        new("wndOperateMode", "Chế độ toạ độ", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum,
            Options: ["uniformCoordinate", "resolutionCoordinate"]),
        new("x", "X", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, GroupPath: "Rect.Coordinate", Min: 0, Max: 1920),
        new("y", "Y", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, GroupPath: "Rect.Coordinate", Min: 0, Max: 1920),
        new("width", "Chiều rộng", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, GroupPath: "Rect", Min: 0, Max: 1920),
        new("height", "Chiều cao", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, GroupPath: "Rect", Min: 0, Max: 1920),
        new("text", "Nội dung chữ", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, Tooltip: "Max 1024 ký tự"),
        new("RGB", "RGB màu chữ", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, GroupPath: "foregroundColor",
            Tooltip: "Giá trị RGB của màu chữ"),
        new("RGB", "RGB màu nền", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, GroupPath: "backgroundColor",
            Tooltip: "Giá trị RGB của màu nền"),
        new("transparencyMode", "Chế độ trong suốt", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum,
            Options: ["opaque", "half-transparent", "transparent"]),
        new("moveDirection", "Hướng di chuyển", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum,
            Options: ["left to right", "right to left", "top to bottom", "bottom to top"]),
        new("moveSpeed", "Tốc độ (1-4)", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Min: 1, Max: 4),
        new("moveMode", "Chế độ di chuyển", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum,
            Options: ["normal", "smooth", "static"],
            Tooltip: "⚠️ Thiếu trong tài liệu 9.7.9.1 — lấy nguồn từ 9.7.9.3/.6 và capability"),
        new("ledType", "Loại LED", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum,
            Options: ["text", "time", "clock", "weather"], Advanced: true),
        new("ledDirection", "Hướng LED", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum,
            Options: ["horizontal", "vertical"], Advanced: true),
        new("timeType", "Kiểu thời gian", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum,
            Options: ["timeOnly", "dateTime"], Advanced: true),
        new("dateFormat", "Định dạng ngày", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum,
            Options: ["yyyy-mm-dd", "mm-dd-yyyy", "dd-mm-yyyy", "xxxxYxxMxxD", "xxMxxDxxxxY", "xxDxxMxxxxY", "yyyy/mm/dd", "yy/mm/dd"], Advanced: true),
        new("timeFormat", "Định dạng giờ", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum,
            Options: ["h:mm:ss", "hh:mm:ss", "hh:mm", "hhHmmMssS", "hhHmmM"], Advanced: true),
        new("hourFormat", "12h / 24h", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum,
            Options: ["12h", "24h"], Advanced: true),
        new("amFormat", "Định dạng AM", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum,
            Options: ["am", "morning"], Advanced: true),
        new("pmFormat", "Định dạng PM", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum,
            Options: ["pm", "afternoon"], Advanced: true),
        new("alignmentX", "Canh ngang", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum,
            Options: ["left", "middle", "right"], Advanced: true),
        new("alignmentY", "Canh dọc", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum,
            Options: ["up", "middle", "down"], Advanced: true),
        new("fontType", "Font", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum,
            Options: ["simSun", "simHei", "kaiTi", "default", "custom", "LiSu", "custom2", "custom3"], Advanced: true),
        new("fontSize", "Cỡ chữ", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum,
            Options: ["0.5times", "1times", "1.5times", "2times", "2.5times", "3times", "3.5times", "4times", "4.5times", "5times", "6times", "7times", "8times"],
            Advanced: true, Tooltip: "13 mức cỡ chữ"),
        new("fontDisplayMode", "Chế độ hiển thị Font", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum,
            Options: ["0", "1", "2"], Advanced: true),
        new("fontBold", "In đậm", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Bool,
            Options: ["true", "false"], Advanced: true),
    ];

    private static void RegisterTextLedSchemas()
    {
        // 9.7.9.1 Set parameters of all virtual LEDs (List-wrapper, single element)
        Register(new VwIsapiApiSchema(
            "9.7.9.1",
            "VirtualLEDOnWall",
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                .. BuildLedBodyFields()
            ],
            ListWrapperElement: "VirtualLEDOnWallList"
        ));

        // 9.7.9.2 Get parameters of all virtual LEDs
        Register(new VwIsapiApiSchema(
            "9.7.9.2",
            null,
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            ]
        ));

        // 9.7.9.3 Add all virtual LEDs (single element despite name)
        Register(new VwIsapiApiSchema(
            "9.7.9.3",
            "VirtualLEDOnWall",
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                .. BuildLedBodyFields()
            ]
        ));

        // 9.7.9.4 Get parameters of a specified LED
        Register(new VwIsapiApiSchema(
            "9.7.9.4",
            null,
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("SubtitlesID", "Subtitles ID", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            ]
        ));

        // 9.7.9.5 Delete a specific virtual LED
        Register(new VwIsapiApiSchema(
            "9.7.9.5",
            null,
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("SubtitlesID", "Subtitles ID", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            ]
        ));

        // 9.7.9.6 Set parameters of a specific virtual LED
        Register(new VwIsapiApiSchema(
            "9.7.9.6",
            "VirtualLEDOnWall",
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("SubtitlesID", "Subtitles ID", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                .. BuildLedBodyFields()
            ]
        ));

        // 9.7.9.7 Get the virtual LED configuration capability
        Register(new VwIsapiApiSchema(
            "9.7.9.7",
            null,
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("SubtitlesID", "Subtitles ID", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            ]
        ));

        // 9.7.9.8 Get configuration capability of all virtual LEDs
        Register(new VwIsapiApiSchema(
            "9.7.9.8",
            null,
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            ]
        ));
    }
}
