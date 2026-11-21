using System.Collections.Generic;

namespace Module.VideoWall.WPF.ViewModels.Isapi;

/// <summary>
/// Description: Danh mục Schema đăng ký cấu trúc các trường tham số cho toàn bộ API ISAPI
/// Created date: 13/09/2026
/// </summary>
public static partial class VwIsapiSchemaCatalog
{
    private static readonly Dictionary<string, VwIsapiApiSchema> Schemas = new();

    static VwIsapiSchemaCatalog()
    {
        RegisterBoardSchemas();
        RegisterDecodingSchemas();
        RegisterOutputChannelSchemas();
        RegisterSignalSourceSchemas();
        RegisterVideoWallSchemas();
        RegisterPlanSchemas();
        RegisterSceneSchemas();
        RegisterScreenSchemas();
        RegisterTextLedSchemas();
        RegisterWallpaperSchemas();
        RegisterWindowSchemas();
    }

    public static VwIsapiApiSchema? GetSchema(string section)
    {
        return Schemas.TryGetValue(section, out var schema) ? schema : null;
    }

    private static void Register(VwIsapiApiSchema schema)
    {
        Schemas[schema.Section] = schema;
    }
}
