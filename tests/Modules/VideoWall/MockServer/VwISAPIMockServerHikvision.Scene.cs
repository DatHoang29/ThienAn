using System.Net;
using System.Threading.Tasks;

namespace Tests.Modules.VideoWall.MockServer;

public partial class VwISAPIMockServerHikvision
{
    private async Task<bool> TryHandleSceneAsync(HttpListenerContext context, string method, string path)
    {
        var res = context.Response;

        // GET export ALL scenes [GET .../scene/export]  (KB-15, 09A ~32451)
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/scene/export", path, out var expParams))
        {
            var wallId = int.TryParse(expParams["videoWallID"], out var w) ? w : 1;
            var store = GetSceneStore(wallId);
            var scenesJson = string.Join(",", store.Select(kv => $"{{\"id\":{kv.Key},\"name\":\"{kv.Value}\"}}"));
            await WriteJsonResponseAsync(res, HttpStatusCode.OK, $"{{\"SceneExport\":{{\"scenes\":[{scenesJson}]}}}}");
            return true;
        }

        // POST import scenes [POST .../scene/import]  (KB-15, 09A ~32448)
        if (method == "POST" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/scene/import", path, out var impParams))
        {
            var wallId = int.TryParse(impParams["videoWallID"], out var w) ? w : 1;
            var store = GetSceneStore(wallId);
            var id = NextSceneId++;
            store[id] = "Imported Scene";
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                <?xml version="1.0" encoding="UTF-8"?>
                <ResponseStatus version="1.0" xmlns="{{Ns}}">
                  <requestURL>{{path}}</requestURL>
                  <statusCode>1</statusCode>
                  <statusString>OK</statusString>
                  <subStatusCode>ok</subStatusCode>
                  <ID>{{id}}</ID>
                </ResponseStatus>
                """);
            return true;
        }

        // PUT copy scene [PUT .../scene/{SID}/copy]
        if (method == "PUT" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/scene/{SID}/copy", path, out var copyParams))
        {
            var wallId = int.TryParse(copyParams["videoWallID"], out var w) ? w : 1;
            var store = GetSceneStore(wallId);
            if (int.TryParse(copyParams["SID"], out var srcSid) && store.TryGetValue(srcSid, out var srcName))
            {
                var id = NextSceneId++;
                store[id] = srcName + " (Copy)";
            }
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                <?xml version="1.0" encoding="UTF-8"?>
                <ResponseStatus version="1.0" xmlns="{{Ns}}">
                  <requestURL>{{path}}</requestURL>
                  <statusCode>1</statusCode>
                  <statusString>OK</statusString>
                  <subStatusCode>ok</subStatusCode>
                </ResponseStatus>
                """);
            return true;
        }

        // POST: Create scene [POST ISAPI/DisplayDev/VideoWall/{videoWallID}/scene]
        if (method == "POST" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/scene", path, out var postParams))
        {
            var wallId = int.TryParse(postParams["videoWallID"], out var w) ? w : 1;
            var store = GetSceneStore(wallId);
            var name = "New Scene";
            if (!string.IsNullOrWhiteSpace(LastReceivedBody))
            {
                var match = System.Text.RegularExpressions.Regex.Match(LastReceivedBody, @"<name>(.*?)</name>");
                if (match.Success)
                    name = match.Groups[1].Value;
            }
            var id = NextSceneId++;
            store[id] = name;
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                <?xml version="1.0" encoding="UTF-8"?>
                <ResponseStatus version="1.0" xmlns="{{Ns}}">
                  <requestURL>{{path}}</requestURL>
                  <statusCode>1</statusCode>
                  <statusString>OK</statusString>
                  <subStatusCode>ok</subStatusCode>
                  <ID>{{id}}</ID>
                </ResponseStatus>
                """);
            return true;
        }

        // DELETE all scenes [DELETE ISAPI/DisplayDev/VideoWall/{videoWallID}/scene]
        if (method == "DELETE" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/scene", path, out var delAllParams))
        {
            var wallId = int.TryParse(delAllParams["videoWallID"], out var w) ? w : 1;
            var store = GetSceneStore(wallId);
            store.Clear();
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                <?xml version="1.0" encoding="UTF-8"?>
                <ResponseStatus version="1.0" xmlns="{{Ns}}">
                  <requestURL>{{path}}</requestURL>
                  <statusCode>1</statusCode>
                  <statusString>OK</statusString>
                  <subStatusCode>ok</subStatusCode>
                </ResponseStatus>
                """);
            return true;
        }

        // DELETE specific scene [DELETE ISAPI/DisplayDev/VideoWall/{videoWallID}/scene/{SID}]
        if (method == "DELETE" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/scene/{SID}", path, out var delParams))
        {
            var wallId = int.TryParse(delParams["videoWallID"], out var w) ? w : 1;
            var store = GetSceneStore(wallId);
            if (int.TryParse(delParams["SID"], out var delId))
                store.Remove(delId);
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                <?xml version="1.0" encoding="UTF-8"?>
                <ResponseStatus version="1.0" xmlns="{{Ns}}">
                  <requestURL>{{path}}</requestURL>
                  <statusCode>1</statusCode>
                  <statusString>OK</statusString>
                  <subStatusCode>ok</subStatusCode>
                </ResponseStatus>
                """);
            return true;
        }

        // GET scene info JSON [GET .../scene/{SID}/sceneInfo]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/scene/{SID}/sceneInfo", path, out var infoParams))
        {
            var wallId = int.TryParse(infoParams["videoWallID"], out var w) ? w : 1;
            var store = GetSceneStore(wallId);
            var sid = int.TryParse(infoParams["SID"], out var parsedSid) ? parsedSid : 1;
            var sceneName = store.TryGetValue(sid, out var n) ? n : "Unknown";
            await WriteJsonResponseAsync(res, HttpStatusCode.OK, $$"""{"id":{{sid}},"name":"{{sceneName}}"}""");
            return true;
        }

        // 9.7.7.1. Get all scenes' parameters [GET ISAPI/DisplayDev/VideoWall/{videoWallID}/scene]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/scene", path, out var listParams))
        {
            var wallId = int.TryParse(listParams["videoWallID"], out var w) ? w : 1;
            var store = GetSceneStore(wallId);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("""<WallSceneList xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">""");
            foreach (var kvp in store)
            {
                sb.AppendLine($"  <WallScene><id>{kvp.Key}</id><name>{kvp.Value}</name></WallScene>");
            }
            sb.AppendLine("</WallSceneList>");
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, sb.ToString());
            return true;
        }

        // 9.7.7.2. Set parameters of a specific scene [PUT ISAPI/DisplayDev/VideoWall/{videoWallID}/scene/{SID}]
        if (method == "PUT" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/scene/{SID}", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                <?xml version="1.0" encoding="UTF-8"?>
                <ResponseStatus version="1.0" xmlns="{{Ns}}">
                  <requestURL>{{path}}</requestURL>
                  <statusCode>1</statusCode>
                  <statusString>OK</statusString>
                  <subStatusCode>ok</subStatusCode>
                </ResponseStatus>
                """);
            return true;
        }

        // 9.7.7.3. Switch to a specific scene [PUT ISAPI/DisplayDev/VideoWall/{videoWallID}/scene/{SID}/activate]
        if (method == "PUT" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/scene/{SID}/activate", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                <?xml version="1.0" encoding="UTF-8"?>
                <ResponseStatus version="1.0" xmlns="{{Ns}}">
                  <requestURL>{{path}}</requestURL>
                  <statusCode>1</statusCode>
                  <statusString>OK</statusString>
                  <subStatusCode>ok</subStatusCode>
                </ResponseStatus>
                """);
            return true;
        }

        // 9.7.7.4. Save the current scene [PUT ISAPI/DisplayDev/VideoWall/{videoWallID}/scene/{SID}/saveData]
        if (method == "PUT" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/scene/{SID}/saveData", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                <?xml version="1.0" encoding="UTF-8"?>
                <ResponseStatus version="1.0" xmlns="{{Ns}}">
                  <requestURL>{{path}}</requestURL>
                  <statusCode>1</statusCode>
                  <statusString>OK</statusString>
                  <subStatusCode>ok</subStatusCode>
                </ResponseStatus>
                """);
            return true;
        }

        // 9.7.7.5. Get scene configuration capability [GET ISAPI/DisplayDev/VideoWall/{videoWallID}/scene/capabilities]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/scene/capabilities", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<SceneCap xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <maxSceneNums>
    1
  </maxSceneNums>
  <isSupportSceneCopy>
    true
  </isSupportSceneCopy>
  <isSupportSceneInfo>
    true
  </isSupportSceneInfo>
  <isSupportSceneImport>
    true
  </isSupportSceneImport>
  <isSupportSceneExport>
    true
  </isSupportSceneExport>
  <isSupportSaveSceneLogo>
    true
  </isSupportSaveSceneLogo>
  <isSupportSaveSceneAudio>
    true
  </isSupportSaveSceneAudio>
  <isSupportSaveSceneVirLed>
    true
  </isSupportSaveSceneVirLed>
  <isSupportSaveSceneSmartDec>
    true
  </isSupportSaveSceneSmartDec>
  <isSupportSaveSceneBaseMap>
    true
  </isSupportSaveSceneBaseMap>
  <isSupportSaveSceneDecOsd>
    true
  </isSupportSaveSceneDecOsd>
  <isSupportSaveSceneDecDelay>
    true
  </isSupportSaveSceneDecDelay>
  <name min="1" max="10">
    test
  </name>
</SceneCap>
""");
            return true;
        }

        // 9.7.7.7. Get scene control parameters capability [GET ISAPI/DisplayDev/VideoWallScene/SceneControlParams/capabilities?format=json]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWallScene/SceneControlParams/capabilities", path))
        {
            await WriteJsonResponseAsync(res, HttpStatusCode.OK, """
{
  "SceneControlParamsCap": {
    "switchSubtitlesEnabled": {
      "@opt": [],
      "@def": "false"
    },
    "switchPictureEnabled": {
      "@opt": [],
      "@def": "false"
    }
  }
}
""");
            return true;
        }

        // 9.7.7.8. Get scene control parameters [GET ISAPI/DisplayDev/VideoWallScene/SceneControlParams?format=json]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWallScene/SceneControlParams", path))
        {
            await WriteJsonResponseAsync(res, HttpStatusCode.OK, """
{
  "switchSubtitlesEnabled": "false",
  "switchPictureEnabled": "false"
}
""");
            return true;
        }

        // 9.7.7.9. Set scene control parameters [PUT ISAPI/DisplayDev/VideoWallScene/SceneControlParams?format=json]
        if (method == "PUT" && MatchRoute("ISAPI/DisplayDev/VideoWallScene/SceneControlParams", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                <?xml version="1.0" encoding="UTF-8"?>
                <ResponseStatus version="1.0" xmlns="{{Ns}}">
                  <requestURL>{{path}}</requestURL>
                  <statusCode>1</statusCode>
                  <statusString>OK</statusString>
                  <subStatusCode>ok</subStatusCode>
                </ResponseStatus>
                """);
            return true;
        }

        return false;
    }
}
