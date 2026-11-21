namespace Module.VideoWall.WPF.ViewModels.Isapi;

public static partial class VwIsapiSchemaCatalog
{
    private static void RegisterVideoWallSchemas()
    {
        // 9.7.5.1 Get the capability of video wall controller
        Register(new VwIsapiApiSchema(
            "9.7.5.1",
            null,
            []
        ));

        // 9.7.5.2 Get parameters of all video walls (Nguồn copy WallOutputList/WallWindowList)
        Register(new VwIsapiApiSchema(
            "9.7.5.2",
            null,
            []
        ));

        // 9.7.5.4 Get parameters of a specific video wall
        Register(new VwIsapiApiSchema(
            "9.7.5.4",
            null,
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            ]
        ));

        // 9.7.5.5 Get linked screen parameters of all outputs
        Register(new VwIsapiApiSchema(
            "9.7.5.5",
            null,
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            ]
        ));

        // 9.7.5.6 Get video wall capabilities
        Register(new VwIsapiApiSchema(
            "9.7.5.6",
            null,
            []
        ));

        // 9.7.5.3 Set parameters of a specific video wall (HYBRID: field phẳng + 2 ô raw XML + CheckBox)
        Register(new VwIsapiApiSchema(
            "9.7.5.3",
            "VideoWall",
            [
                new("videoWallID", "ID tường ghép (Path)", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("id", "ID tường (Body)", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, DefaultValue: "1"),
                new("name", "Tên tường", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, DefaultValue: "VideoWall 1"),
                new("backgroudColor", "Màu nền", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum, Options: ["black", "blue", "white"], DefaultValue: "black"),
                new("autoSwitchMainSub", "Tự chuyển Main/Sub", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Bool, Options: ["false", "true"], DefaultValue: "false"),
                // Advanced flat fields
                new("ledShowMode", "Chế độ hiển thị LED", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, Advanced: true),
                new("subWndWidth", "Chiều rộng Sub Window", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, GroupPath: "SubStreamAutoSwitch", Advanced: true),
                new("subWndHeight", "Chiều cao Sub Window", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, GroupPath: "SubStreamAutoSwitch", Advanced: true),
                new("wndStaticMode", "Chế độ tĩnh của cửa sổ", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum, Options: ["blackScreen", "lastFrame"], Advanced: true),
                new("streamFailedMode", "Chế độ khi Stream lỗi", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum, Options: ["noSignal", "lastFrame"], Advanced: true),
            ],
            RawBlockElements: ["WallOutputList", "WallWindowList"]
        ));
    }
}
