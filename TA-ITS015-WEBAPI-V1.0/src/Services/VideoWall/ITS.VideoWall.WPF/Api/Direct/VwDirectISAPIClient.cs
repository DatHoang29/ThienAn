using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Module.VideoWall.WPF.Api.Direct.Isapi;

namespace Module.VideoWall.WPF.Api.Direct;

/// <summary>
/// Description: Client HTTP trực tiếp gửi các API ISAPI chuẩn XML tới thiết bị VideoWall
/// Created date: 13/09/2026
/// </summary>
public sealed class VwDirectISAPIClient(HttpClient httpClient, VwDirectDeviceCredentials credentials)
{

    public Task<VwISAPIResult<VwISAPIUserCheckResponse>> UserCheck(CancellationToken ct)
    {
        return SendGetXml<VwISAPIUserCheckResponse>("Security/userCheck", ct);
    }

    public Task<VwISAPIResult<VwISAPICapabilitiesResponse>> GetCapabilities(CancellationToken ct)
    {
        return SendGetXml<VwISAPICapabilitiesResponse>("DisplayDev/VideoWall/capabilities", ct);
    }

    public Task<VwISAPIResult<VwISAPIVideoWallList>> GetVideoWalls(CancellationToken ct)
    {
        return SendGetXml<VwISAPIVideoWallList>("DisplayDev/VideoWall", ct);
    }

    public Task<VwISAPIResult<VwISAPIOutputsResponse>> GetOutputs(int wallNo, CancellationToken ct)
    {
        if (wallNo <= 0) return Task.FromResult(VwISAPIResult<VwISAPIOutputsResponse>.Fail("wallNo phải > 0"));
        return SendGetXml<VwISAPIOutputsResponse>($"DisplayDev/VideoWall/{wallNo}/outputs", ct);
    }

    public Task<VwISAPIResult<VwISAPIInputChannelsResponse>> GetInputChannels(CancellationToken ct)
    {
        return SendGetXml<VwISAPIInputChannelsResponse>("DisplayDev/Video/inputs/channels", ct);
    }

    public Task<VwISAPIResult<VwISAPIStreamingChannelsResponse>> GetStreamingChannels(CancellationToken ct)
    {
        return SendGetXml<VwISAPIStreamingChannelsResponse>("DisplayDev/Video/streaming/channels", ct);
    }

    public Task<VwISAPIResult<VwISAPIWallSceneList>> GetScenes(int wallNo, CancellationToken ct)
    {
        if (wallNo <= 0) return Task.FromResult(VwISAPIResult<VwISAPIWallSceneList>.Fail("wallNo phải > 0"));
        return SendGetXml<VwISAPIWallSceneList>($"DisplayDev/VideoWall/{wallNo}/scene", ct);
    }

    public Task<VwISAPIResult<VwISAPIWallWindowList>> GetWindows(int wallNo, CancellationToken ct = default)
    {
        if (wallNo <= 0) return Task.FromResult(VwISAPIResult<VwISAPIWallWindowList>.Fail("wallNo phải > 0"));
        return SendGetXml<VwISAPIWallWindowList>($"DisplayDev/VideoWall/{wallNo}/windows", ct);
    }

    public Task<VwISAPIResult<VwISAPISceneStatusResponse>> GetRunningScene(int wallNo, CancellationToken ct)
    {
        if (wallNo <= 0) return Task.FromResult(VwISAPIResult<VwISAPISceneStatusResponse>.Fail("wallNo phải > 0"));
        return SendGetXml<VwISAPISceneStatusResponse>($"DisplayDev/VideoWall/{wallNo}/scene/isRunning", ct);
    }

    public async Task<VwISAPIResult> DeleteWindow(int wallNo, int windowId, CancellationToken ct = default)
    {
        if (wallNo <= 0)
            return VwISAPIResult.Fail("wallNo phải > 0");
        if (windowId <= 0)
            return VwISAPIResult.Fail("windowId phải > 0");

        _ = await StopDynamicDecode(wallNo, windowId, 1, ct);
        return await SendDeleteStatus($"DisplayDev/VideoWall/{wallNo}/windows/{windowId}", ct);
    }

    public async Task<VwISAPIResult> DeleteAllWindows(int wallNo, VwISAPIWallWindowList? currentWindows, CancellationToken ct)
    {
        if (wallNo <= 0)
            return VwISAPIResult.Fail("wallNo phải > 0");

        var bulkDeleteRes = await SendDeleteStatus($"DisplayDev/VideoWall/{wallNo}/windows", ct);
        if (bulkDeleteRes.Success)
            return bulkDeleteRes;

        var windows = currentWindows?.WallWindow;
        if (windows is null || windows.Count == 0)
        {
            var winListRes = await GetWindows(wallNo, ct);
            if (winListRes.Success && winListRes.Data?.WallWindow != null)
                windows = winListRes.Data.WallWindow;
        }

        if (windows != null && windows.Count > 0)
        {
            foreach (var window in windows)
            {
                _ = await StopDynamicDecode(wallNo, window.Id, 1, ct);
                await SendDeleteStatus($"DisplayDev/VideoWall/{wallNo}/windows/{window.Id}", ct);
            }
            return VwISAPIResult.Ok();
        }

        return bulkDeleteRes;
    }

    public Task<VwISAPIResult<VwISAPIResponseStatus>> AddWindow(int wallNo, VwISAPIWindowRequest window, CancellationToken ct = default)
    {
        if (wallNo <= 0) return Task.FromResult(VwISAPIResult<VwISAPIResponseStatus>.Fail("wallNo phải > 0"));
        
        string xmlBody = VwIsapiXmlCodec.Serialize(window);
        return SendPostXml($"DisplayDev/VideoWall/{wallNo}/windows", xmlBody, ct);
    }

    public Task<VwISAPIResult> SaveSceneData(int wallNo, int sceneId, CancellationToken ct)
    {
        if (wallNo <= 0) return Task.FromResult(VwISAPIResult.Fail("wallNo phải > 0"));
        const string placeholder = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n<Request xmlns=\"http://www.isapi.org/ver20/XMLSchema\" version=\"2.0\"></Request>";
        return SendPutXml($"DisplayDev/VideoWall/{wallNo}/scene/{sceneId}/saveData", placeholder, ct);
    }

    public Task<VwISAPIResult> SetScene(int wallNo, int sceneId, string sceneName, CancellationToken ct)
    {
        if (wallNo <= 0) return Task.FromResult(VwISAPIResult.Fail("wallNo phải > 0"));
        string xml = $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n<WallScene xmlns=\"http://www.isapi.org/ver20/XMLSchema\" version=\"2.0\">\r\n  <id>{sceneId}</id>\r\n  <name>{System.Security.SecurityElement.Escape(sceneName)}</name>\r\n</WallScene>";
        return SendPutXml($"DisplayDev/VideoWall/{wallNo}/scene/{sceneId}", xml, ct);
    }

    public Task<VwISAPIResult<VwISAPIResponseStatus>> CreateScene(int wallNo, string sceneName, CancellationToken ct)
    {
        if (wallNo <= 0) return Task.FromResult(VwISAPIResult<VwISAPIResponseStatus>.Fail("wallNo phải > 0"));
        string xml = $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n<WallScene xmlns=\"http://www.isapi.org/ver20/XMLSchema\" version=\"2.0\">\r\n  <name>{System.Security.SecurityElement.Escape(sceneName)}</name>\r\n</WallScene>";
        return SendPostXml($"DisplayDev/VideoWall/{wallNo}/scene", xml, ct);
    }

    public Task<VwISAPIResult> ActivateScene(int wallNo, int sceneId, CancellationToken ct)
    {
        if (wallNo <= 0) return Task.FromResult(VwISAPIResult.Fail("wallNo phải > 0"));
        return SendPutStatus($"DisplayDev/VideoWall/{wallNo}/scene/{sceneId}/activate", ct);
    }

    public Task<VwISAPIResult> StartDynamicDecode(int wallNo, int windowId, int subWindowId, CancellationToken ct = default)
    {
        if (wallNo <= 0)
            return Task.FromResult(VwISAPIResult.Fail("wallNo phải > 0"));
        if (windowId <= 0)
            return Task.FromResult(VwISAPIResult.Fail("windowId phải > 0"));
        if (subWindowId <= 0)
            subWindowId = 1;

        return SendPutStatus($"DisplayDev/VideoWall/{wallNo}/windows/{windowId}/sub/{subWindowId}/start", ct);
    }

    public Task<VwISAPIResult> StopDynamicDecode(int wallNo, int windowId, int subWindowId, CancellationToken ct = default)
    {
        if (wallNo <= 0)
            return Task.FromResult(VwISAPIResult.Fail("wallNo phải > 0"));
        if (windowId <= 0)
            return Task.FromResult(VwISAPIResult.Fail("windowId phải > 0"));
        if (subWindowId <= 0)
            subWindowId = 1;

        return SendPutStatus($"DisplayDev/VideoWall/{wallNo}/windows/{windowId}/sub/{subWindowId}/stop", ct);
    }

    public Task<VwISAPIResult> UpdateDeviceWindowRect(int wallNo, int windowId, int x, int y, int w, int h, CancellationToken ct = default)
    {
        if (wallNo <= 0)
            return Task.FromResult(VwISAPIResult.Fail("wallNo phải > 0"));
        if (windowId <= 0)
            return Task.FromResult(VwISAPIResult.Fail("windowId phải > 0"));

        var xml = $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <WallWindow xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
              <id>{windowId}</id>
              <wndOperateMode>uniformCoordinate</wndOperateMode>
              <Rect>
                <Coordinate>
                  <x>{x}</x>
                  <y>{y}</y>
                </Coordinate>
                <width>{w}</width>
                <height>{h}</height>
              </Rect>
            </WallWindow>
            """;

        return SendPutXml($"DisplayDev/VideoWall/{wallNo}/windows/{windowId}", xml, ct);
    }

    public Task<VwISAPIResult> UpdateDeviceWindow(int wallNo, VwISAPIWindowRequest request, CancellationToken ct = default)
    {
        if (wallNo <= 0)
            return Task.FromResult(VwISAPIResult.Fail("wallNo phải > 0"));
        if (request.Id <= 0)
            return Task.FromResult(VwISAPIResult.Fail("windowId phải > 0"));

        var xml = VwIsapiXmlCodec.Serialize(request);
        return SendPutXml($"DisplayDev/VideoWall/{wallNo}/windows/{request.Id}", xml, ct);
    }

    public Task<VwISAPIResult> SetDeviceWindowTop(int wallNo, int windowId, CancellationToken ct = default)
    {
        if (wallNo <= 0)
            return Task.FromResult(VwISAPIResult.Fail("wallNo phải > 0"));
        if (windowId <= 0)
            return Task.FromResult(VwISAPIResult.Fail("windowId phải > 0"));

        return SendPutStatus($"DisplayDev/VideoWall/{wallNo}/windows/{windowId}/top", ct);
    }

    public Task<VwISAPIResult> SetDeviceWindowBottom(int wallNo, int windowId, CancellationToken ct = default)
    {
        if (wallNo <= 0)
            return Task.FromResult(VwISAPIResult.Fail("wallNo phải > 0"));
        if (windowId <= 0)
            return Task.FromResult(VwISAPIResult.Fail("windowId phải > 0"));

        return SendPutStatus($"DisplayDev/VideoWall/{wallNo}/windows/{windowId}/bottom", ct);
    }

    public async Task<VwISAPIResult<string>> SendRaw(string method, string relativePath, string? body, string? contentType, CancellationToken ct)
    {
        try
        {
            var httpMethod = new HttpMethod(method);
            using var request = CreateRequest(httpMethod, relativePath);
            
            if (!string.IsNullOrEmpty(body))
            {
                request.Content = new StringContent(body, Encoding.UTF8, contentType ?? "application/xml");
            }

            using var response = await httpClient.SendAsync(request, ct);
            var mediaType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();
            byte[] rawBytes = await response.Content.ReadAsByteArrayAsync(ct);
            string responseData;

            bool isImageOrBinary = (mediaType != null && (mediaType.StartsWith("image/") || mediaType.Contains("octet-stream")))
                || IsBinaryImage(rawBytes);

            if (isImageOrBinary)
            {
                var effectiveType = mediaType?.StartsWith("image/") == true ? mediaType : DetectImageType(rawBytes) ?? "image/jpeg";
                var base64 = Convert.ToBase64String(rawBytes);
                responseData = $"<!-- [BINARY IMAGE DATA: {effectiveType}, Size: {rawBytes.Length:N0} bytes] -->\n[data:{effectiveType};base64,{base64}]";
            }
            else
            {
                responseData = Encoding.UTF8.GetString(rawBytes);
            }

            if (!response.IsSuccessStatusCode)
            {
                var status = ReadStatus(responseData, (int)response.StatusCode, body);
                return VwISAPIResult<string>.Fail(status.ErrorMessage ?? "Lỗi HTTP " + response.StatusCode, response.StatusCode, body, responseData);
            }

            return VwISAPIResult<string>.Ok(responseData, response.StatusCode, body, responseData);
        }
        catch (Exception ex)
        {
            return VwISAPIResult<string>.Fail(FormatException(ex));
        }
    }

    private async Task<VwISAPIResult<T>> SendGetXml<T>(string path, CancellationToken ct)
    {
        try
        {
            using var request = CreateRequest(HttpMethod.Get, path);
            using var response = await httpClient.SendAsync(request, ct);
            string responseData = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                var status = ReadStatus(responseData, (int)response.StatusCode, null);
                return VwISAPIResult<T>.Fail(status.ErrorMessage ?? "Lỗi HTTP " + response.StatusCode, response.StatusCode, null, responseData);
            }

            var data = VwIsapiXmlCodec.Deserialize<T>(responseData);
            return data != null 
                ? VwISAPIResult<T>.Ok(data, response.StatusCode, null, responseData) 
                : VwISAPIResult<T>.Fail("Lỗi parse XML", response.StatusCode, null, responseData);
        }
        catch (Exception ex)
        {
            return VwISAPIResult<T>.Fail(FormatException(ex));
        }
    }

    private async Task<VwISAPIResult<VwISAPIResponseStatus>> SendPostXml(string path, string xmlBody, CancellationToken ct)
    {
        try
        {
            using var request = CreateRequest(HttpMethod.Post, path);
            request.Content = new StringContent(xmlBody, Encoding.UTF8, "application/xml");
            
            using var response = await httpClient.SendAsync(request, ct);
            string responseData = await response.Content.ReadAsStringAsync(ct);

            var status = ReadStatus(responseData, (int)response.StatusCode, xmlBody);
            
            if (!response.IsSuccessStatusCode)
            {
                return VwISAPIResult<VwISAPIResponseStatus>.Fail(status.ErrorMessage ?? "Lỗi HTTP " + response.StatusCode, response.StatusCode, xmlBody, responseData);
            }

            return VwISAPIResult<VwISAPIResponseStatus>.Ok(status.Data!, response.StatusCode, xmlBody, responseData);
        }
        catch (Exception ex)
        {
            return VwISAPIResult<VwISAPIResponseStatus>.Fail(FormatException(ex));
        }
    }

    private async Task<VwISAPIResult> SendPutStatus(string path, CancellationToken ct)
    {
        try
        {
            using var request = CreateRequest(HttpMethod.Put, path);
            request.Content = new StringContent(string.Empty, Encoding.UTF8, "application/xml");
            
            using var response = await httpClient.SendAsync(request, ct);
            string responseData = await response.Content.ReadAsStringAsync(ct);

            var status = ReadStatus(responseData, (int)response.StatusCode, null);
            
            if (!response.IsSuccessStatusCode || !status.Success)
            {
                return VwISAPIResult.Fail(status.ErrorMessage ?? "Lỗi HTTP " + response.StatusCode, response.StatusCode, null, responseData);
            }

            return VwISAPIResult.Ok(response.StatusCode, null, responseData);
        }
        catch (Exception ex)
        {
            return VwISAPIResult.Fail(FormatException(ex));
        }
    }

    private async Task<VwISAPIResult> SendPutXml(string path, string xmlBody, CancellationToken ct)
    {
        try
        {
            using var request = CreateRequest(HttpMethod.Put, path);
            request.Content = new StringContent(xmlBody, Encoding.UTF8, "application/xml");
            
            using var response = await httpClient.SendAsync(request, ct);
            string responseData = await response.Content.ReadAsStringAsync(ct);

            var status = ReadStatus(responseData, (int)response.StatusCode, xmlBody);
            
            if (!response.IsSuccessStatusCode || !status.Success)
            {
                return VwISAPIResult.Fail(status.ErrorMessage ?? "Lỗi HTTP " + response.StatusCode, response.StatusCode, xmlBody, responseData);
            }

            return VwISAPIResult.Ok(response.StatusCode, xmlBody, responseData);
        }
        catch (Exception ex)
        {
            return VwISAPIResult.Fail(FormatException(ex));
        }
    }

    private async Task<VwISAPIResult> SendDeleteStatus(string path, CancellationToken ct)
    {
        try
        {
            using var request = CreateRequest(HttpMethod.Delete, path);
            
            using var response = await httpClient.SendAsync(request, ct);
            string responseData = await response.Content.ReadAsStringAsync(ct);

            var status = ReadStatus(responseData, (int)response.StatusCode, null);
            
            if (!response.IsSuccessStatusCode || !status.Success)
            {
                return VwISAPIResult.Fail(status.ErrorMessage ?? "Lỗi HTTP " + response.StatusCode, response.StatusCode, null, responseData);
            }

            return VwISAPIResult.Ok(response.StatusCode, null, responseData);
        }
        catch (Exception ex)
        {
            return VwISAPIResult.Fail(FormatException(ex));
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativePath)
    {
        string path = relativePath.TrimStart('/');
        if (!path.StartsWith("ISAPI/", StringComparison.OrdinalIgnoreCase)
            && !path.StartsWith("SDK/", StringComparison.OrdinalIgnoreCase))
        {
            path = "ISAPI/" + path;
        }
        var request = new HttpRequestMessage(method, "/" + path);
        request.Headers.ConnectionClose = true;
        request.Options.Set(VwDirectDigestHandler.CredentialOptionKey, credentials);
        return request;
    }

    private string FormatException(Exception ex)
    {
        var innermost = ex;
        while (innermost.InnerException != null)
            innermost = innermost.InnerException;

        var msg = innermost.Message;
        var endpoint = $"{credentials.Ip}:{credentials.Port}";
        if (innermost is HttpRequestException || innermost is System.IO.IOException || innermost is System.Net.Sockets.SocketException || msg.Contains("ResponseEnded", StringComparison.OrdinalIgnoreCase) || msg.Contains("prematurely", StringComparison.OrdinalIgnoreCase))
        {
            return $"Không thể kết nối hoặc thiết bị ngắt kết nối đột ngột ({endpoint}): {msg}. Vui lòng kiểm tra lại địa chỉ IP, cổng Port (HTTP ISAPI thường là 80/8080/443), hoặc kết nối mạng.";
        }

        return $"Lỗi kết nối ({endpoint}): {msg}";
    }

    private static VwISAPIResult<VwISAPIResponseStatus> ReadStatus(string xml, int httpStatus, string? rawRequest)
    {
        var status = VwIsapiXmlCodec.Deserialize<VwISAPIResponseStatus>(xml);
        if (status != null)
        {
            string msg;
            if (!string.IsNullOrWhiteSpace(status.ErrorMsg))
            {
                msg = status.ErrorMsg;
            }
            else if (!string.IsNullOrWhiteSpace(status.Description))
            {
                msg = status.Description;
            }
            else if (string.Equals(status.SubStatusCode, "invalidOperation", StringComparison.OrdinalIgnoreCase) || status.StatusCode == 4)
            {
                msg = "Thiết bị trả về Invalid Operation (Mã 4): Thao tác không hợp lệ trên phần cứng (kịch bản SID chưa tồn tại hoặc tường chưa gán màn hình/unbound).";
            }
            else
            {
                msg = $"Status: {status.StatusString}, ErrorCode: {status.StatusCode}, SubStatusCode: {status.SubStatusCode}";
            }

            if (status.StatusCode != 1 && status.StatusCode != 0)
            {
                return VwISAPIResult<VwISAPIResponseStatus>.Fail(msg, (HttpStatusCode)httpStatus, rawRequest, xml);
            }
            return VwISAPIResult<VwISAPIResponseStatus>.Ok(status, (HttpStatusCode)httpStatus, rawRequest, xml);
        }
        return VwISAPIResult<VwISAPIResponseStatus>.Fail($"Lỗi HTTP {httpStatus}", (HttpStatusCode)httpStatus, rawRequest, xml);
    }

    private static bool IsBinaryImage(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 4)
            return false;
        if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            return true;
        if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
            return true;
        if (bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46 && (bytes[3] == 0x38 || bytes[3] == 0x37))
            return true;
        if (bytes[0] == 0x42 && bytes[1] == 0x4D)
            return true;
        return false;
    }

    private static string? DetectImageType(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 4)
            return null;
        if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            return "image/jpeg";
        if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
            return "image/png";
        if (bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46)
            return "image/gif";
        if (bytes[0] == 0x42 && bytes[1] == 0x4D)
            return "image/bmp";
        return "application/octet-stream";
    }
}
