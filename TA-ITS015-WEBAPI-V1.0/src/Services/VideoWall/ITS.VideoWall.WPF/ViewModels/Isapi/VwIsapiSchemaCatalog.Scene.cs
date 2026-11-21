namespace Module.VideoWall.WPF.ViewModels.Isapi;

public static partial class VwIsapiSchemaCatalog
{
    private static void RegisterSceneSchemas()
    {
        // 9.7.7.1 Get all scenes' parameters
        Register(new VwIsapiApiSchema(
            "9.7.7.1",
            null,
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            ]
        ));

        // 9.7.7.2 Set parameters of a specific scene
        Register(new VwIsapiApiSchema(
            "9.7.7.2",
            "WallScene",
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("SID", "Scene ID", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("id", "ID (body)", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String),
                new("name", "Tên scene", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String),
            ]
        ));

        // 9.7.7.3 Switch to a specific scene (action only, no body)
        Register(new VwIsapiApiSchema(
            "9.7.7.3",
            null,
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("SID", "Scene ID", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1",
                    Tooltip: "⚠️ Báo lỗi inSceneSwitchingPleaseDoNotOperate nếu scene đang trong quá trình chuyển"),
            ]
        ));

        // 9.7.7.4 Save the current scene (action only, no meaningful body)
        Register(new VwIsapiApiSchema(
            "9.7.7.4",
            null,
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("SID", "Scene ID", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1",
                    Tooltip: "⚠️ saveData KHÔNG lưu LED ảo hay ảnh nền (isSupportSaveSceneVirLed=false, isSupportSaveSceneBaseMap=false trên thiết bị đã test)"),
            ]
        ));

        // 9.7.7.5 Get scene configuration capability
        Register(new VwIsapiApiSchema(
            "9.7.7.5",
            null,
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            ]
        ));

        // 9.7.7.6 Get the current scene
        Register(new VwIsapiApiSchema(
            "9.7.7.6",
            null,
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            ]
        ));

        // 9.7.7.7 Get scene control parameters capability
        Register(new VwIsapiApiSchema("9.7.7.7", null, []));

        // 9.7.7.8 Get scene control parameters
        Register(new VwIsapiApiSchema("9.7.7.8", null, []));

        // 9.7.7.9 Set scene control parameters (JSON body)
        Register(new VwIsapiApiSchema(
            "9.7.7.9",
            null,
            [
                new("switchSubtitlesEnabled", "Chuyển chữ chạy khi đổi scene", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Bool,
                    Required: true, DefaultValue: "false", Options: ["true", "false"]),
                new("switchPictureEnabled", "Chuyển ảnh nền khi đổi scene", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Bool,
                    DefaultValue: "false", Options: ["true", "false"]),
            ],
            BodyFormat: VwIsapiBodyFormat.Json
        ));

        // W3: Scene — POST / DELETE / hidden APIs
        Register(new VwIsapiApiSchema(
            "9.7.7.X1",
            "WallScene",
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("name", "Tên scene", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, Required: true),
            ]
        ));
        Register(new VwIsapiApiSchema(
            "9.7.7.X2",
            null,
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("SID", "Scene ID", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            ]
        ));
        Register(new VwIsapiApiSchema(
            "9.7.7.X3",
            null,
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            ]
        ));
        Register(new VwIsapiApiSchema(
            "9.7.7.X4",
            null,
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("SID", "Scene ID", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            ]
        ));
        Register(new VwIsapiApiSchema("9.7.7.X5", null, [
            new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
        ]));
        Register(new VwIsapiApiSchema("9.7.7.X6", null, [
            new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
        ], BodyFormat: VwIsapiBodyFormat.Json));
        Register(new VwIsapiApiSchema("9.7.7.X7", null, [
            new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            new("SID", "Scene ID", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
        ]));
    }
}
