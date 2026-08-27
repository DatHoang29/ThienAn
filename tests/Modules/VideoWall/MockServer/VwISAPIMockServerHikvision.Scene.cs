using System.Net;
using System.Threading.Tasks;

namespace Tests.Modules.VideoWall.MockServer;

public partial class VwISAPIMockServerHikvision
{
    private async Task<bool> TryHandleSceneAsync(HttpListenerContext context, string method, string path)
    {
        var res = context.Response;

        // 9.7.7.1. Get all scenes' parameters [GET ISAPI/DisplayDev/VideoWall/{videoWallID}/scene]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/scene", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<WallSceneList xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <WallScene>
    <id>
      test
    </id>
    <name>
      test
    </name>
  </WallScene>
</WallSceneList>
""");
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
