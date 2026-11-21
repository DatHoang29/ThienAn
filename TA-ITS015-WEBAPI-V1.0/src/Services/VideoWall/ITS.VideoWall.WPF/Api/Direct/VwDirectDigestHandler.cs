using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Module.VideoWall.WPF.Api.Direct;

/// <summary>
/// Description: DelegatingHandler tự động xử lý xác thực Digest Authentication với thiết bị ISAPI.
/// Created date: 08/09/2026
/// </summary>
public class VwDirectDigestHandler : DelegatingHandler
{
    internal static readonly HttpRequestOptionsKey<VwDirectDeviceCredentials> CredentialOptionKey = new("VwDirectCredentials");
    
    private const string DigestNonceCount = "00000001";
    private static readonly Regex ChallengeSplitter = new(@"(?:^|,)\s*Digest\s+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await BufferContentAsync(request, cancellationToken);

        HttpResponseMessage firstResponse;
        try
        {
            firstResponse = await base.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            return CreateNetworkErrorResponse(request, ex);
        }

        if (firstResponse.StatusCode != HttpStatusCode.Unauthorized)
            return firstResponse;

        if (!request.Options.TryGetValue(CredentialOptionKey, out var creds) || creds == null)
            return firstResponse;

        var challenge = ParseChallenge(firstResponse);
        if (challenge == null)
            return firstResponse;

        try
        {
            await firstResponse.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Bỏ qua lỗi drain content trên response 401
        }
        firstResponse.Dispose();

        var authorizedRequest = await CloneRequestAsync(request);
        authorizedRequest.Headers.Authorization = BuildAuthorizationHeader(challenge, authorizedRequest.Method, authorizedRequest.RequestUri, creds);

        HttpResponseMessage authorizedResponse;
        try
        {
            authorizedResponse = await base.SendAsync(authorizedRequest, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return CreateNetworkErrorResponse(request, ex);
        }

        if (authorizedResponse.StatusCode == HttpStatusCode.Unauthorized)
        {
            var staleChallenge = ParseChallenge(authorizedResponse);
            if (staleChallenge != null && staleChallenge.Stale)
            {
                try
                {
                    await authorizedResponse.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    // Bỏ qua lỗi drain content trên stale response
                }
                authorizedResponse.Dispose();
                var retryRequest = await CloneRequestAsync(request);
                retryRequest.Headers.Authorization = BuildAuthorizationHeader(staleChallenge, retryRequest.Method, retryRequest.RequestUri, creds);
                
                try
                {
                    return await base.SendAsync(retryRequest, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    return CreateNetworkErrorResponse(request, ex);
                }
            }
        }

        return authorizedResponse;
    }

    private static HttpResponseMessage CreateNetworkErrorResponse(HttpRequestMessage request, Exception ex)
    {
        var innermost = ex;
        while (innermost.InnerException != null)
            innermost = innermost.InnerException;

        var host = request.RequestUri?.Host ?? "127.0.0.1";
        var port = request.RequestUri?.Port ?? 80;
        var errorMsg = $"Không thể kết nối hoặc thiết bị ngắt kết nối đột ngột ({host}:{port}): {innermost.Message}. Vui lòng kiểm tra lại IP, Port (HTTP ISAPI thường là 80/8080/443), hoặc kết nối mạng.";
        var xmlError = $"<ResponseStatus xmlns=\"http://www.isapi.org/ver20/XMLSchema\"><requestURL>{request.RequestUri?.PathAndQuery}</requestURL><statusCode>4</statusCode><statusString>NetworkError</statusString><subStatusCode>socketError</subStatusCode><errorMsg>{errorMsg}</errorMsg></ResponseStatus>";

        return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            RequestMessage = request,
            ReasonPhrase = "Connection Failed",
            Content = new StringContent(xmlError, Encoding.UTF8, "application/xml")
        };
    }

    private static async Task BufferContentAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content == null) 
            return;
            
        var bytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
        var oldHeaders = request.Content.Headers;
        request.Content = new ByteArrayContent(bytes);
        foreach (var header in oldHeaders)
        {
            request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        clone.Version = request.Version;
        clone.VersionPolicy = request.VersionPolicy;

        foreach (var option in request.Options)
        {
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
        }

        if (request.Content != null)
        {
            var bytes = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(bytes);
            foreach (var h in request.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(h.Key, h.Value);
        }

        foreach (var h in request.Headers)
        {
            if (h.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase)) 
                continue;
            clone.Headers.TryAddWithoutValidation(h.Key, h.Value);
        }

        return clone;
    }

    private static VwDigestChallenge? ParseChallenge(HttpResponseMessage response)
    {
        var authenticateHeaders = response.Headers.WwwAuthenticate;
        if (!authenticateHeaders.Any()) 
            return null;

        var fullHeader = string.Join(", ", authenticateHeaders.Select(x => x.ToString()));
        var matches = ChallengeSplitter.Split(fullHeader).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

        VwDigestChallenge? bestChallenge = null;
        foreach (var match in matches)
        {
            var challenge = ParseSingleChallenge(match);
            if (challenge == null) 
                continue;
            
            if (bestChallenge == null)
            {
                bestChallenge = challenge;
            }
            else if (challenge.Algorithm.Equals("SHA-256", StringComparison.OrdinalIgnoreCase) && bestChallenge.Algorithm.Equals("MD5", StringComparison.OrdinalIgnoreCase))
            {
                bestChallenge = challenge;
            }
        }

        return bestChallenge;
    }

    private static VwDigestChallenge? ParseSingleChallenge(string challengeStr)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        int pos = 0;
        while (pos < challengeStr.Length)
        {
            pos = SkipWhitespace(challengeStr, pos);
            if (pos >= challengeStr.Length) break;
            
            int eqPos = challengeStr.IndexOf('=', pos);
            if (eqPos < 0) break;

            string key = challengeStr.Substring(pos, eqPos - pos).TrimEnd();
            pos = eqPos + 1;
            pos = SkipWhitespace(challengeStr, pos);

            string value;
            if (pos < challengeStr.Length && challengeStr[pos] == '"')
            {
                pos++;
                int endQuote = challengeStr.IndexOf('"', pos);
                if (endQuote < 0) break;
                value = challengeStr.Substring(pos, endQuote - pos);
                pos = endQuote + 1;
            }
            else
            {
                int commaPos = challengeStr.IndexOf(',', pos);
                if (commaPos < 0) commaPos = challengeStr.Length;
                value = challengeStr.Substring(pos, commaPos - pos).TrimEnd();
                pos = commaPos;
            }

            dict[key] = value;
            pos = SkipWhitespace(challengeStr, pos);
            if (pos < challengeStr.Length && challengeStr[pos] == ',') pos++;
        }

        if (!dict.TryGetValue("realm", out var realm) || !dict.TryGetValue("nonce", out var nonce))
            return null;

        dict.TryGetValue("qop", out var qop);
        dict.TryGetValue("opaque", out var opaque);
        dict.TryGetValue("algorithm", out var algorithm);
        dict.TryGetValue("stale", out var staleStr);

        bool stale = staleStr?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
        if (string.IsNullOrEmpty(algorithm)) 
            algorithm = "MD5";

        return new VwDigestChallenge(realm, nonce, qop, opaque, algorithm, stale);
    }

    private static int SkipWhitespace(string s, int pos)
    {
        while (pos < s.Length && char.IsWhiteSpace(s[pos])) pos++;
        return pos;
    }

    private static AuthenticationHeaderValue BuildAuthorizationHeader(VwDigestChallenge challenge, HttpMethod method, Uri? uri, VwDirectDeviceCredentials creds)
    {
        var uriStr = uri?.PathAndQuery ?? "/";
        var cnonce = GenerateCnonce();
        var qop = SelectQop(challenge.Qop);

        var ha1 = Hash(challenge.Algorithm, $"{creds.Account}:{challenge.Realm}:{creds.Password}");
        var ha2 = Hash(challenge.Algorithm, $"{method.Method}:{uriStr}");

        string response;
        if (!string.IsNullOrEmpty(qop))
        {
            response = Hash(challenge.Algorithm, $"{ha1}:{challenge.Nonce}:{DigestNonceCount}:{cnonce}:{qop}:{ha2}");
        }
        else
        {
            response = Hash(challenge.Algorithm, $"{ha1}:{challenge.Nonce}:{ha2}");
        }

        var sb = new StringBuilder();
        sb.Append($"username=\"{creds.Account}\", ");
        sb.Append($"realm=\"{challenge.Realm}\", ");
        sb.Append($"nonce=\"{challenge.Nonce}\", ");
        sb.Append($"uri=\"{uriStr}\", ");
        sb.Append($"algorithm={challenge.Algorithm}, ");
        sb.Append($"response=\"{response}\"");

        if (!string.IsNullOrEmpty(challenge.Opaque))
        {
            sb.Append($", opaque=\"{challenge.Opaque}\"");
        }
        if (!string.IsNullOrEmpty(qop))
        {
            sb.Append($", qop={qop}, nc={DigestNonceCount}, cnonce=\"{cnonce}\"");
        }

        return new AuthenticationHeaderValue("Digest", sb.ToString());
    }

    private static string SelectQop(string? qop)
    {
        if (string.IsNullOrEmpty(qop)) return string.Empty;
        var parts = qop.Split(',').Select(x => x.Trim());
        if (parts.Contains("auth", StringComparer.OrdinalIgnoreCase)) return "auth";
        if (parts.Contains("auth-int", StringComparer.OrdinalIgnoreCase)) return "auth-int";
        return string.Empty;
    }

    private static string Hash(string algorithm, string input)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes;

        if (algorithm.Equals("MD5", StringComparison.OrdinalIgnoreCase))
        {
            hashBytes = MD5.HashData(inputBytes);
        }
        else if (algorithm.Equals("SHA-256", StringComparison.OrdinalIgnoreCase))
        {
            hashBytes = SHA256.HashData(inputBytes);
        }
        else
        {
            hashBytes = MD5.HashData(inputBytes);
        }

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string GenerateCnonce()
    {
        var bytes = new byte[8];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Description: Thông tin challenge xác thực Digest trả về từ header Www-Authenticate của thiết bị.
    /// Created date: 08/09/2026
    /// </summary>
    private sealed class VwDigestChallenge
    {
        /// <summary>Vùng bảo mật Realm từ thiết bị.</summary>
        public string Realm { get; }

        /// <summary>Chuỗi Nonce ngẫu nhiên do thiết bị sinh ra.</summary>
        public string Nonce { get; }

        /// <summary>Chế độ bảo vệ chất lượng Qop (auth hoặc auth-int).</summary>
        public string? Qop { get; }

        /// <summary>Chuỗi Opaque trả về từ thiết bị để gửi lại nguyên vẹn.</summary>
        public string? Opaque { get; }

        /// <summary>Thuật toán mã hóa băm (MD5 hoặc SHA-256).</summary>
        public string Algorithm { get; }

        /// <summary>Cho biết Nonce trước đó đã cũ (stale) và cần làm mới.</summary>
        public bool Stale { get; }

        public VwDigestChallenge(string realm, string nonce, string? qop, string? opaque, string algorithm, bool stale)
        {
            Realm = realm;
            Nonce = nonce;
            Qop = qop;
            Opaque = opaque;
            Algorithm = algorithm;
            Stale = stale;
        }
    }
}
