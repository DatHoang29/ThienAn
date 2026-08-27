using System.Net;
using System.Threading.Tasks;

namespace Tests.Modules.VideoWall.MockServer;

public partial class VwISAPIMockServerHikvision
{
    private async Task<bool> TryHandleWallpaperAsync(HttpListenerContext context, string method, string path)
    {
        var res = context.Response;

        // 9.7.10.1. Get configuration capability of background picture window [GET ISAPI/DisplayDev/VideoWall/{videoWallID}/baseMap/{mapFileID}/capabilities]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/baseMap/{mapFileID}/capabilities", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<BaseMapOnWall xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <id>
    1
  </id>
  <enabled>
    true
  </enabled>
  <baseMapType opt="picture,video">
    test
  </baseMapType>
  <baseMapID>
    1
  </baseMapID>
  <wndOperateMode opt="uniformCoordinate,resolutionCoordinate">
    uniformCoordinate
  </wndOperateMode>
  <Rect>
    <Coordinate>
      <x min="0" max="1920">
        1
      </x>
      <y min="0" max="1920">
        1
      </y>
    </Coordinate>
    <width min="0" max="1920">
      1
    </width>
    <height min="0" max="1920">
      1
    </height>
  </Rect>
  <Coordinate>
    <x min="0" max="1920">
      1
    </x>
    <y min="0" max="1920">
      1
    </y>
  </Coordinate>
  <ResolutionRect>
    <Coordinate>
      <x min="0" max="1920">
        1
      </x>
      <y min="0" max="1920">
        1
      </y>
    </Coordinate>
    <width min="0" max="1920">
      1
    </width>
    <height min="0" max="1920">
      1
    </height>
  </ResolutionRect>
</BaseMapOnWall>
""");
            return true;
        }

        // 9.7.10.2. Get the capability of all background pictures [GET ISAPI/DisplayDev/VideoWall/{videoWallID}/baseMap/capabilities]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/baseMap/capabilities", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<BaseMapOnWallCap xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <baseMapOnWallNums>
    1
  </baseMapOnWallNums>
</BaseMapOnWallCap>
""");
            return true;
        }

        // 9.7.10.3. Set parameters of all background pictures [PUT ISAPI/DisplayDev/VideoWall/baseMap]
        if (method == "PUT" && MatchRoute("ISAPI/DisplayDev/VideoWall/baseMap", path))
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

        // 9.7.10.4. Delete a specific background picture [DELETE ISAPI/DisplayDev/VideoWall/baseMap/{mapFileID}]
        if (method == "DELETE" && MatchRoute("ISAPI/DisplayDev/VideoWall/baseMap/{mapFileID}", path))
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

        // 9.7.10.5. Set parameters of a specific background picture [PUT ISAPI/DisplayDev/VideoWall/baseMap/{mapFileID}]
        if (method == "PUT" && MatchRoute("ISAPI/DisplayDev/VideoWall/baseMap/{mapFileID}", path))
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

        // 9.7.10.6. Get parameters of a background picture [GET ISAPI/DisplayDev/VideoWall/baseMap/{mapFileID}]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/baseMap/{mapFileID}", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<BaseMap xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <id>
    test
  </id>
  <name>
    test
  </name>
  <fileType>
    test
  </fileType>
  <imageWidth>
    1
  </imageWidth>
  <imageHeight>
    1
  </imageHeight>
</BaseMap>
""");
            return true;
        }

        // 9.7.10.7. Get the background picture configuration capability [GET ISAPI/DisplayDev/VideoWall/baseMap/capabilities]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/baseMap/capabilities", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<BaseMapCap xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <baseMapNums>
    1
  </baseMapNums>
  <supportFileType>
    JPEG
  </supportFileType>
  <maxFileSize>
    1
  </maxFileSize>
  <maxImageWidth>
    1
  </maxImageWidth>
  <maxImageHeight>
    1
  </maxImageHeight>
  <BaseMapAlignUnit>
    <width opt="0,2,4,8,…">
      1
    </width>
    <height opt="0,2,4,8…">
      1
    </height>
  </BaseMapAlignUnit>
  <isSupportBaseMapCircle opt="true">
    true
  </isSupportBaseMapCircle>
  <imageWidth min="0" max="16384">
    1
  </imageWidth>
  <imageHeight min="0" max="8192">
    1
  </imageHeight>
</BaseMapCap>
""");
            return true;
        }

        // 9.7.10.8. Get configuration of all background pictures [GET ISAPI/DisplayDev/VideoWall/baseMap?isGetBaseMapFile={isGetBaseMapFile}]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/baseMap", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<BaseMapList xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <BaseMap>
    <id>
      test
    </id>
    <name>
      test
    </name>
    <fileType>
      test
    </fileType>
    <imageWidth>
      1
    </imageWidth>
    <imageHeight>
      1
    </imageHeight>
    <baseMapFile>
      <filePathType>
        multipart
      </filePathType>
      <filePath>
        test
      </filePath>
    </baseMapFile>
  </BaseMap>
</BaseMapList>
""");
            return true;
        }

        return false;
    }
}
