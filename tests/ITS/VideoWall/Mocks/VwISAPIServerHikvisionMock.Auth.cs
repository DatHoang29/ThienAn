using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Tests.VideoWall.Mocks
{
    public partial class VwISAPIServerHikvisionMock
    {
        /// <summary>
        /// Description: Xử lý xác thực Digest cho một request. Trả về true nếu được phép đi tiếp;
        ///              trả về false nghĩa là đã ghi xong phản hồi 401 và bên gọi phải return ngay.
        /// Created date: 30/09/2026
        /// </summary>
        private bool HandleDigestAuth(HttpListenerRequest req, HttpListenerResponse res, string method, string path)
        {
            var authHeader = req.Headers["Authorization"];
            var clientUsername = ReadAuthDirective(authHeader, "username");
            var clientRealm = ReadAuthDirective(authHeader, "realm") ?? Realm;
            var clientNonce = ReadAuthDirective(authHeader, "nonce");
            var clientUri = ReadAuthDirective(authHeader, "uri") ?? path;
            var clientQop = ReadAuthDirective(authHeader, "qop");
            var clientNc = ReadAuthDirective(authHeader, "nc");
            var clientCnonce = ReadAuthDirective(authHeader, "cnonce");
            var clientResponse = ReadAuthDirective(authHeader, "response");

            if (!string.IsNullOrWhiteSpace(clientNonce))
                LastReceivedAuthNonce = clientNonce;

            bool hasValidAuth;
            if (VerifyDigestResponseHash)
            {
                if (clientUsername == DefaultUser && !string.IsNullOrWhiteSpace(clientNonce) && !string.IsNullOrWhiteSpace(clientResponse))
                {
                    var ha1 = ComputeMd5Hex($"{DefaultUser}:{clientRealm}:{DefaultPassword}");
                    var ha2 = ComputeMd5Hex($"{method}:{clientUri}");
                    string expectedResponse;
                    if (!string.IsNullOrWhiteSpace(clientQop) && !string.IsNullOrWhiteSpace(clientNc) && !string.IsNullOrWhiteSpace(clientCnonce))
                    {
                        expectedResponse = ComputeMd5Hex($"{ha1}:{clientNonce}:{clientNc}:{clientCnonce}:{clientQop}:{ha2}");
                    }
                    else
                    {
                        expectedResponse = ComputeMd5Hex($"{ha1}:{clientNonce}:{ha2}");
                    }

                    hasValidAuth = string.Equals(clientResponse, expectedResponse, StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    hasValidAuth = false;
                }
            }
            else
            {
                hasValidAuth = !string.IsNullOrWhiteSpace(authHeader)
                    && authHeader.StartsWith("Digest", StringComparison.OrdinalIgnoreCase)
                    && authHeader.Contains($"username=\"{DefaultUser}\"", StringComparison.OrdinalIgnoreCase);
            }

            // Kịch bản Nonce Expiry: lần đầu trả stale="true" để bắt client tái cấp nonce mới
            if (SimulateNonceExpiry && hasValidAuth && NonceExpiryTriggerCount == 0)
            {
                NonceExpiryTriggerCount++;
                var staleNonce = Guid.NewGuid().ToString("N");
                LastIssuedMd5Nonce = staleNonce;
                res.StatusCode = (int)HttpStatusCode.Unauthorized;
                res.Headers.Add("WWW-Authenticate", $"Digest realm=\"{Realm}\", qop=\"auth\", nonce=\"{staleNonce}\", opaque=\"{Guid.NewGuid():N}\", algorithm=MD5, stale=\"true\"");
                res.Close();
                return false;
            }

            if (!hasValidAuth)
            {
                if (!string.IsNullOrWhiteSpace(authHeader))
                {
                    ConsecutiveFailedAuthCount++;
                    if (FailedAuthLockoutThreshold > 0 && ConsecutiveFailedAuthCount >= FailedAuthLockoutThreshold)
                    {
                        IsLockedOut = true;
                    }
                }

                var nonceMd5 = Guid.NewGuid().ToString("N");
                LastIssuedMd5Nonce = nonceMd5;
                var opaque = Guid.NewGuid().ToString("N");

                res.StatusCode = (int)HttpStatusCode.Unauthorized;
                if (SimulateDualChallengeHeader)
                {
                    var nonceSha = Guid.NewGuid().ToString("N");
                    res.Headers.Add("WWW-Authenticate", $"Digest realm=\"{Realm}\", qop=\"auth\", nonce=\"{nonceSha}\", algorithm=SHA-256, stale=\"false\", Digest realm=\"{Realm}\", qop=\"auth\", nonce=\"{nonceMd5}\", algorithm=MD5, stale=\"false\"");
                }
                else if (SimulateChallengeWithoutAlgorithm)
                {
                    res.Headers.Add("WWW-Authenticate", $"Digest realm=\"{Realm}\", qop=\"auth\", nonce=\"{nonceMd5}\", opaque=\"{opaque}\", stale=\"false\"");
                }
                else
                {
                    res.Headers.Add("WWW-Authenticate", $"Digest realm=\"{Realm}\", qop=\"auth\", nonce=\"{nonceMd5}\", opaque=\"{opaque}\", algorithm=MD5, stale=\"false\"");
                }
                res.Close();
                return false;
            }

            // Xác thực thành công: reset chuỗi sai liên tiếp
            ConsecutiveFailedAuthCount = 0;
            return true;
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Đọc một directive từ header Authorization Digest
        /// Created date: 24/08/2026
        /// </summary>
        private static string? ReadAuthDirective(string? header, string key)
        {
            if (string.IsNullOrWhiteSpace(header))
                return null;

            var quoted = Regex.Match(header, key + @"\s*=\s*""([^""]*)""", RegexOptions.IgnoreCase);
            if (quoted.Success)
                return quoted.Groups[1].Value;

            var bare = Regex.Match(header, key + @"\s*=\s*([^\s,""]+)", RegexOptions.IgnoreCase);
            return bare.Success ? bare.Groups[1].Value : null;
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Tính mã hash MD5 trả về chuỗi hex chữ thường
        /// Created date: 24/08/2026
        /// </summary>
        private static string ComputeMd5Hex(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            return Convert.ToHexString(MD5.HashData(bytes)).ToLowerInvariant();
        }
    }
}
