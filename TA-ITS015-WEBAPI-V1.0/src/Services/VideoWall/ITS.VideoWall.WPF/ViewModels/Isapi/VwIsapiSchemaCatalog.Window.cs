namespace Module.VideoWall.WPF.ViewModels.Isapi;

public static partial class VwIsapiSchemaCatalog
{
    private static void RegisterWindowSchemas()
    {
        // 9.7.11.2 Get all windows parameters
        Register(new VwIsapiApiSchema(
            "9.7.11.2",
            null,
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            ]
        ));

        // 9.7.11.3 Delete all windows
        Register(new VwIsapiApiSchema(
            "9.7.11.3",
            null,
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            ]
        ));

        // 9.7.11.5 Get parameters configuration of a specific window
        Register(new VwIsapiApiSchema(
            "9.7.11.5",
            null,
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("VWMWID", "ID cửa sổ", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            ]
        ));

        // 9.7.11.7 Delete a specific window
        Register(new VwIsapiApiSchema(
            "9.7.11.7",
            null,
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("VWMWID", "ID cửa sổ", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            ]
        ));

        // 9.7.11.8 Bottom the window
        Register(new VwIsapiApiSchema(
            "9.7.11.8",
            null,
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("VWMWID", "ID cửa sổ", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            ]
        ));

        // 9.7.11.13 Top the window
        Register(new VwIsapiApiSchema(
            "9.7.11.13",
            null,
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("VWMWID", "ID cửa sổ", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            ]
        ));

        // 9.7.11.14 Get the window configuration capability of the video wall
        Register(new VwIsapiApiSchema(
            "9.7.11.14",
            null,
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            ]
        ));

        // 9.7.11.4 Add a window (DTO VwISAPIWindowRequest)
        Register(new VwIsapiApiSchema(
            "9.7.11.4",
            "WallWindow",
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            ]
        ));

        // 9.7.11.6 Set parameters of a specific window (DTO VwISAPIWindowRequest)
        Register(new VwIsapiApiSchema(
            "9.7.11.6",
            "WallWindow",
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("VWMWID", "ID cửa sổ", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            ]
        ));

        // ── New GET-only endpoints (9.7.11 extension) ──

        // 9.7.11.1 Get LED or LCD areas
        Register(new VwIsapiApiSchema(
            "9.7.11.1",
            null,
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            ]
        ));

        // 9.7.11.9 Get single configuration capabilities of sub-windows
        Register(new VwIsapiApiSchema(
            "9.7.11.9",
            null,
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("VWMWID", "ID cửa sổ chính", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("VWSWID", "ID cửa sổ con", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            ]
        ));

        // 9.7.11.10 Get parameters of decoding delay
        Register(new VwIsapiApiSchema(
            "9.7.11.10",
            null,
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("VWMWID", "ID cửa sổ chính", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("VWSWID", "ID cửa sổ con", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            ]
        ));

        // 9.7.11.11 Get decoding delay capability
        Register(new VwIsapiApiSchema(
            "9.7.11.11",
            null,
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("VWMWID", "ID cửa sổ chính", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("VWSWID", "ID cửa sổ con", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            ]
        ));

        // 9.7.11.12 Get the configuration capability of full-frame-rate fluent video mode
        Register(new VwIsapiApiSchema(
            "9.7.11.12",
            null,
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("VWMWID", "ID cửa sổ chính", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("VWSWID", "ID cửa sổ con", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            ]
        ));

        // 9.7.11.15 Get sub-stream capability in multi-screen mode
        Register(new VwIsapiApiSchema(
            "9.7.11.15",
            null,
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            ]
        ));

        // 9.7.11.16 Get stream type config params
        Register(new VwIsapiApiSchema(
            "9.7.11.16",
            null,
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            ]
        ));

        // 9.7.11.17 Get pre-editing capability of video wall
        Register(new VwIsapiApiSchema("9.7.11.17", null, []));

        // 9.7.11.18 Set SubWindow source
        Register(new VwIsapiApiSchema(
            "9.7.11.18",
            "SubWindow",
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("VWMWID", "ID cửa sổ (VWMWID)", VwIsapiFieldKind.PathParam, VwIsapiFieldType.String, Required: true),
                new("VWSWID", "ID cửa sổ con (VWSWID)", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("videoInputChannelID", "ID nguồn tín hiệu", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String),
            ]
        ));
    }
}
