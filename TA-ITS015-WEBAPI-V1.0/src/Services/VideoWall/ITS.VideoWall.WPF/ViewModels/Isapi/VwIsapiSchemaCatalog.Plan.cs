namespace Module.VideoWall.WPF.ViewModels.Isapi;

public static partial class VwIsapiSchemaCatalog
{
    private static void RegisterPlanSchemas()
    {
        // 9.7.6.1 Add a plan (complex nested — flat subset only, no PlanDetail/TimeBlock sub-array form)
        Register(new VwIsapiApiSchema(
            "9.7.6.1",
            "WallPlan",
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("id", "ID plan (1-8)", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Required: true, DefaultValue: "1", Min: 1, Max: 8),
                new("name", "Tên plan", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, Tooltip: "1-32 ký tự"),
                new("actTimeMode", "Chế độ kích hoạt", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum, GroupPath: "ActTimeDetail",
                    Options: ["at once", "on day", "weekly"], Tooltip: "Chế độ thời điểm kích hoạt"),
                new("actCount", "Số đếm Act", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Advanced: true,
                    Tooltip: "Ý nghĩa chưa rõ trong tài liệu"),
            ]
        ));

        // 9.7.6.2 Get configuration capability of a specific plan
        Register(new VwIsapiApiSchema(
            "9.7.6.2",
            null,
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("planTemplateID", "ID plan template", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            ]
        ));

        // 9.7.6.3 Get plan configuration capability
        Register(new VwIsapiApiSchema(
            "9.7.6.3",
            null,
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            ]
        ));

        // 9.7.6.4 Get the current plan
        Register(new VwIsapiApiSchema(
            "9.7.6.4",
            null,
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            ]
        ));

        // W4: Plan — GET list / PUT / DELETE / start / stop
        Register(new VwIsapiApiSchema("9.7.6.X1", null, [
            new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
        ]));
        Register(new VwIsapiApiSchema(
            "9.7.6.X2",
            "WallPlan",
            [
                new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("planTemplateID", "ID plan", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
                new("name", "Tên plan", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String),
            ]
        ));
        Register(new VwIsapiApiSchema("9.7.6.X3", null, [
            new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            new("planTemplateID", "ID plan", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
        ]));
        Register(new VwIsapiApiSchema("9.7.6.X4", null, [
            new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            new("planTemplateID", "ID plan", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
        ]));
        Register(new VwIsapiApiSchema("9.7.6.X5", null, [
            new("videoWallID", "ID tường ghép", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
            new("planTemplateID", "ID plan", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true, DefaultValue: "1"),
        ]));
    }
}
