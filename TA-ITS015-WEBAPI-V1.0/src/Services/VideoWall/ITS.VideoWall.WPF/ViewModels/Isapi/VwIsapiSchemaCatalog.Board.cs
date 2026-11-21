namespace Module.VideoWall.WPF.ViewModels.Isapi;

public static partial class VwIsapiSchemaCatalog
{
    private static void RegisterBoardSchemas()
    {
        // 9.7.1.1 Set parameters of a specified sub-board
        Register(new VwIsapiApiSchema("9.7.1.1", "InputBoardCfg", [
            new("BoardID", "ID Board", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true),
            new("slotNo", "Số Slot", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Required: true),
            new("fullFrameEnable", "Bật Full Frame", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Bool, Required: true, Options: ["false", "true"])
        ]));

        // 9.7.1.2 Get sub-board capability
        Register(new VwIsapiApiSchema("9.7.1.2", null, []));

        // 9.7.1.3 Set parameters of all sub-boards
        Register(new VwIsapiApiSchema(
            "9.7.1.3",
            "InputBoardCfg",
            [
                new("slotNo", "Số Slot", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Required: true),
                new("fullFrameEnable", "Bật Full Frame", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Bool, Required: true, Options: ["false", "true"])
            ],
            ListWrapperElement: "InputBoardCfgList"
        ));

        // 9.7.1.4 Get parameters of a specified sub-board
        Register(new VwIsapiApiSchema("9.7.1.4", null, []));

        // W2: System APIs
        Register(new VwIsapiApiSchema("SYS-1", null, []));
        Register(new VwIsapiApiSchema("SYS-2", null, []));
        Register(new VwIsapiApiSchema("SYS-3", null, []));
        Register(new VwIsapiApiSchema("SYS-4", null, []));
        Register(new VwIsapiApiSchema("SYS-5", null, []));
        Register(new VwIsapiApiSchema("SYS-6", null, []));
    }
}
