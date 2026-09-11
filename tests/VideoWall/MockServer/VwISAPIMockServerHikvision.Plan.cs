using System.Net;
using System.Threading.Tasks;

namespace Tests.Modules.VideoWall.MockServer;

public partial class VwISAPIMockServerHikvision
{
    private async Task<bool> TryHandlePlanAsync(HttpListenerContext context, string method, string path)
    {
        var res = context.Response;

        // 9.7.6.1. Add a plan [POST ISAPI/DisplayDev/VideoWall/{videoWallID}/plan]
        if (method == "POST" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/plan", path))
        {
            var name = "New Plan";
            if (!string.IsNullOrWhiteSpace(LastReceivedBody))
            {
                var match = System.Text.RegularExpressions.Regex.Match(LastReceivedBody, @"<name>(.*?)</name>");
                if (match.Success)
                    name = match.Groups[1].Value;
            }
            var id = NextPlanId++;
            PlanStore[id] = name;
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

        // M5: GET plan list [GET ISAPI/DisplayDev/VideoWall/{videoWallID}/plan]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/plan", path))
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("""<WallPlanList xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">""");
            foreach (var kvp in PlanStore)
            {
                sb.AppendLine($"  <WallPlan><id>{kvp.Key}</id><name>{kvp.Value}</name></WallPlan>");
            }
            sb.AppendLine("</WallPlanList>");
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, sb.ToString());
            return true;
        }

        // M5: PUT update plan [PUT ISAPI/DisplayDev/VideoWall/{videoWallID}/plan/{planTemplateID}]
        if (method == "PUT" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/plan/{planTemplateID}", path, out var putParams))
        {
            if (int.TryParse(putParams["planTemplateID"], out var putId) && PlanStore.ContainsKey(putId))
            {
                var match = System.Text.RegularExpressions.Regex.Match(LastReceivedBody ?? "", @"<name>(.*?)</name>");
                if (match.Success)
                    PlanStore[putId] = match.Groups[1].Value;
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

        // M5: DELETE plan [DELETE ISAPI/DisplayDev/VideoWall/{videoWallID}/plan/{planTemplateID}]
        if (method == "DELETE" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/plan/{planTemplateID}", path, out var delParams))
        {
            if (int.TryParse(delParams["planTemplateID"], out var delId))
                PlanStore.Remove(delId);
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

        // M5: PUT start plan [PUT .../plan/{planTemplateID}/start]
        if (method == "PUT" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/plan/{planTemplateID}/start", path, out var startParams))
        {
            if (int.TryParse(startParams["planTemplateID"], out var startId))
                ActivePlanId = startId;
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

        // M5: PUT stop plan [PUT .../plan/{planTemplateID}/stop]
        if (method == "PUT" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/plan/{planTemplateID}/stop", path))
        {
            ActivePlanId = null;
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

        // 9.7.6.2. Get configuration capability of a specific plan [GET ISAPI/DisplayDev/VideoWall/{videoWallID}/plan/{planTemplateID}/capabilities]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/plan/{planTemplateID}/capabilities", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<WallPlan xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <id min="1" max="8">
    1
  </id>
  <name min="1" max="32">
    test
  </name>
  <ActTimeDetail>
    <actTimeMode opt=" at once,on day,weekly">
      test
    </actTimeMode>
    <OnDayTime>
      <datetime>
        1970-01-01T00:00:00+08:00
      </datetime>
    </OnDayTime>
    <WeeklyTime>
      <TimeBlockList>
        <TimeBlock>
          <dayOfWeek>
            1
          </dayOfWeek>
          <beginTime>
            00:00:00+08:00
          </beginTime>
          <cycleSum min="1" max="128">
            1
          </cycleSum>
        </TimeBlock>
      </TimeBlockList>
    </WeeklyTime>
  </ActTimeDetail>
  <PlanDetailList>
    <PlanDetail>
      <operationType>
        activateScene
      </operationType>
      <sceneID min="1" max="128">
        1
      </sceneID>
      <duration min="1" max="86400">
        1
      </duration>
      <baseMapType>
        baseMap
      </baseMapType>
      <baseMapWndNo>
        1
      </baseMapWndNo>
      <baseMapNo>
        1
      </baseMapNo>
    </PlanDetail>
  </PlanDetailList>
  <actCount>
    1
  </actCount>
</WallPlan>
""");
            return true;
        }

        // 9.7.6.3. Get plan configuration capability [GET ISAPI/DisplayDev/VideoWall/{videoWallID}/plan/capabilities]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/plan/capabilities", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<PlanCap xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <maxPlanNums>
    1
  </maxPlanNums>
  <isSupportBaseMapCycleSwitch>
    true
  </isSupportBaseMapCycleSwitch>
</PlanCap>
""");
            return true;
        }

        // 9.7.6.4. Get the current plan [GET ISAPI/DisplayDev/VideoWall/{videoWallID}/plan/isRunning]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/plan/isRunning", path))
        {
            var planIdXml = ActivePlanId.HasValue ? $"<planID>{ActivePlanId.Value}</planID>" : "<planID>0</planID>";
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, $"""
                <?xml version="1.0" encoding="UTF-8"?>
                <RunningPlan xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
                  {planIdXml}
                </RunningPlan>
                """);
            return true;
        }

        return false;
    }
}
