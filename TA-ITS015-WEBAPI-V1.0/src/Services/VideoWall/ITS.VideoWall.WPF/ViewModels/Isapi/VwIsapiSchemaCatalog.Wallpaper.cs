namespace Module.VideoWall.WPF.ViewModels.Isapi;

public static partial class VwIsapiSchemaCatalog
{
    private static void RegisterWallpaperSchemas()
    {
        // 9.7.10.1 Get configuration capability of background picture window
        Register(new VwIsapiApiSchema(
            "9.7.10.1",
            null,
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("mapFileID", "ID File ảnh nền", VwIsapiFieldKind.PathParam, VwIsapiFieldType.String, Required: true, DefaultValue: "1"),
            ]
        ));

        // 9.7.10.2 Get the capability of all background pictures
        Register(new VwIsapiApiSchema(
            "9.7.10.2",
            null,
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            ]
        ));

        // 9.7.10.3 Set parameters of all background pictures (List-wrapper, single element)
        Register(new VwIsapiApiSchema(
            "9.7.10.3",
            "BaseMap",
            [
                new("id", "ID", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, DefaultValue: "1"),
                new("name", "Tên ảnh nền", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, DefaultValue: "Wallpaper 1"),
                new("fileType", "Loại File", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, DefaultValue: "jpeg", Advanced: true),
                new("imageWidth", "Chiều rộng", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, DefaultValue: "1920", Advanced: true),
                new("imageHeight", "Chiều cao", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, DefaultValue: "1080", Advanced: true),
            ],
            ListWrapperElement: "BaseMapList"
        ));

        // 9.7.10.4 Delete a specific background picture
        Register(new VwIsapiApiSchema(
            "9.7.10.4",
            null,
            [
                new("mapFileID", "ID File ảnh nền", VwIsapiFieldKind.PathParam, VwIsapiFieldType.String, Required: true, DefaultValue: "1"),
            ]
        ));

        // 9.7.10.5 Set parameters of a specific background picture
        Register(new VwIsapiApiSchema(
            "9.7.10.5",
            "BaseMap",
            [
                new("mapFileID", "ID File ảnh nền", VwIsapiFieldKind.PathParam, VwIsapiFieldType.String, Required: true, DefaultValue: "1"),
                new("id", "ID", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, DefaultValue: "1"),
                new("name", "Tên ảnh nền", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, DefaultValue: "Wallpaper 1"),
                new("fileType", "Loại File", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, DefaultValue: "jpeg", Advanced: true),
                new("imageWidth", "Chiều rộng", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, DefaultValue: "1920", Advanced: true),
                new("imageHeight", "Chiều cao", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, DefaultValue: "1080", Advanced: true),
            ]
        ));

        // 9.7.10.6 Get parameters of a background picture
        Register(new VwIsapiApiSchema(
            "9.7.10.6",
            null,
            [
                new("mapFileID", "ID File ảnh nền", VwIsapiFieldKind.PathParam, VwIsapiFieldType.String, Required: true, DefaultValue: "1"),
            ]
        ));

        // 9.7.10.7 Get the background picture configuration capability
        Register(new VwIsapiApiSchema("9.7.10.7", null, []));

        // 9.7.10.8 Get configuration of all background pictures
        Register(new VwIsapiApiSchema(
            "9.7.10.8",
            null,
            [
                new("isGetBaseMapFile", "Lấy Base Map File?", VwIsapiFieldKind.PathParam, VwIsapiFieldType.String,
                    Required: true, DefaultValue: "true", Tooltip: "Tham số query trên URL"),
            ]
        ));
    }
}
