using System.Net;
using System.Threading.Tasks;

namespace Tests.Modules.VideoWall.MockServer;

public partial class VwISAPIMockServerHikvision
{
    private async Task<bool> TryHandleBoardAsync(HttpListenerContext context, string method, string path)
    {
        var res = context.Response;

        // 9.7.1.1. Set parameters of a specified sub-board [PUT ISAPI/System/Board/{BoardID}/config]
        if (method == "PUT" && MatchRoute("ISAPI/System/Board/{BoardID}/config", path))
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

        // 9.7.1.2. Get sub-board capability [GET ISAPI/System/Board/capabilities]
        if (method == "GET" && MatchRoute("ISAPI/System/Board/capabilities", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<AllBoardCap xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <BoardCapList>
    <BoardCap>
      <id>
        test
      </id>
      <isSupportBasicParam opt="ture,false">
        true
      </isSupportBasicParam>
      <isSupportSvrParam opt="ture,false">
        true
      </isSupportSvrParam>
      <isSupportNetwork opt="ture,false">
        true
      </isSupportNetwork>
      <isSupportNetAddrTrans opt="true,false">
        true
      </isSupportNetAddrTrans>
      <isSupportStatus opt="true,false">
        true
      </isSupportStatus>
      <isSupportNetworkArea opt="true,false">
        true
      </isSupportNetworkArea>
      <isSupportBatchUpgrade opt="true,false">
        true
      </isSupportBatchUpgrade>
      <isSupportSubSysConfigFileImport opt="true,false">
        true
      </isSupportSubSysConfigFileImport>
    </BoardCap>
  </BoardCapList>
</AllBoardCap>
""");
            return true;
        }

        // 9.7.1.3. Set parameters of all sub-boards [PUT ISAPI/System/Board/config]
        if (method == "PUT" && MatchRoute("ISAPI/System/Board/config", path))
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
