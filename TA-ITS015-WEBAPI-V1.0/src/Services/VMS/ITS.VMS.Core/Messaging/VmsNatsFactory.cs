using NATS.Client.Core;

namespace ITS.VMS.Core.Messaging
{
    /// <summary>
    /// Dựng tham số kết nối NATS từ cấu hình. Tách riêng để cả bên NHẬN lệnh
    /// (<see cref="VmsControlNatsListener"/>) và bên GỬI lệnh (<see cref="VmsControlSender"/>) dùng chung,
    /// tránh lặp phần xác thực JWT/NKEY/TOKEN/USERPASS ở 2 nơi.
    /// Author: ChuongLP
    /// Created date: 22/09/2026
    /// </summary>
    internal static class VmsNatsFactory
    {
        /// <summary>Tham số kết nối; tự kết nối lại vô hạn khi NATS server khởi động lại.</summary>
        public static NatsOpts BuildOpts(VmsControlNatsOption options, string? clientNameSuffix = null)
        {
            var rawUrl = string.IsNullOrWhiteSpace(options.Url) ? "nats://127.0.0.1:4222" : options.Url.Trim();
            if (rawUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                rawUrl = "tls://" + rawUrl["https://".Length..];
                options.Url = rawUrl;
            }
            else if (rawUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                rawUrl = "nats://" + rawUrl["http://".Length..];
                options.Url = rawUrl;
            }

            var name = string.IsNullOrWhiteSpace(options.ClientName) ? "ITS_VMS_SERVICE" : options.ClientName.Trim();
            if (!string.IsNullOrWhiteSpace(clientNameSuffix)) name = $"{name}_{clientNameSuffix}";

            return new NatsOpts
            {
                Url = rawUrl,
                Name = name,
                AuthOpts = BuildAuthOpts(options),
                TlsOpts = BuildTlsOpts(options),
                WebSocketOpts = BuildWebSocketOpts(options),
                MaxReconnectRetry = -1,
                ConnectTimeout = TimeSpan.FromSeconds(10),
            };
        }

        /// <summary>Cấu hình WebSocket cho kết nối NATS qua ws:// hoặc wss://.</summary>
        public static NatsWebSocketOpts? BuildWebSocketOpts(VmsControlNatsOption options)
        {
            var url = options.Url?.Trim();
            var isWs = url?.StartsWith("ws://", StringComparison.OrdinalIgnoreCase) == true
                       || url?.StartsWith("wss://", StringComparison.OrdinalIgnoreCase) == true;

            if (!isWs && !options.InsecureSkipVerify)
                return null;

            return new NatsWebSocketOpts
            {
                ConfigureClientWebSocketOptions = (uri, clientWsOpts, ct) =>
                {
                    if (options.InsecureSkipVerify)
                    {
                        clientWsOpts.RemoteCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                    }
                    return ValueTask.CompletedTask;
                }
            };
        }

        /// <summary>Cấu hình TLS cho kết nối NATS, hỗ trợ InsecureSkipVerify và client certs cho cả tls:// và wss://.</summary>
        public static NatsTlsOpts BuildTlsOpts(VmsControlNatsOption options)
        {
            var url = options.Url?.Trim();
            var isTls = url?.StartsWith("tls://", StringComparison.OrdinalIgnoreCase) == true
                        || url?.StartsWith("wss://", StringComparison.OrdinalIgnoreCase) == true;
            var hasTlsAuth = string.Equals(options.AuthMode?.Trim(), "TLS", StringComparison.OrdinalIgnoreCase);
            var hasCaFile = !string.IsNullOrWhiteSpace(options.CaFile);
            var hasCert = !string.IsNullOrWhiteSpace(options.TlsCertFile);

            if (!isTls && !hasTlsAuth && !hasCaFile && !hasCert && !options.InsecureSkipVerify)
                return NatsTlsOpts.Default;

            string? caFile = null;
            if (hasCaFile)
            {
                var path = ResolvePath(options.CaFile!);
                if (File.Exists(path))
                    caFile = path;
            }

            string? certFile = null;
            if (hasCert)
            {
                var path = ResolvePath(options.TlsCertFile!);
                if (File.Exists(path))
                    certFile = path;
            }

            string? keyFile = null;
            if (!string.IsNullOrWhiteSpace(options.TlsKeyFile))
            {
                var path = ResolvePath(options.TlsKeyFile!);
                if (File.Exists(path))
                    keyFile = path;
            }

            return new NatsTlsOpts
            {
                CaFile = caFile,
                CertFile = certFile,
                KeyFile = keyFile,
                InsecureSkipVerify = options.InsecureSkipVerify
            };
        }

        /// <summary>Kênh điều khiển, có giá trị mặc định khi cấu hình để trống.</summary>
        public static string ResolveSubject(VmsControlNatsOption options)
            => string.IsNullOrWhiteSpace(options.ControlSubject) ? "ta.its.control.vms" : options.ControlSubject.Trim();

        /// <summary>Dựng thông tin xác thực theo AuthMode (hạ tầng thật dùng JWT + creds file).</summary>
        public static NatsAuthOpts BuildAuthOpts(VmsControlNatsOption options)
        {
            var mode = options.AuthMode?.Trim().ToUpperInvariant() ?? "NONE";

            switch (mode)
            {
                case "JWT":
                    if (!string.IsNullOrWhiteSpace(options.Jwt) && !string.IsNullOrWhiteSpace(options.Seed))
                        return new NatsAuthOpts { Jwt = options.Jwt!.Trim(), Seed = options.Seed!.Trim() };

                    var creds = ResolvePath(options.CredsFile);
                    if (string.IsNullOrWhiteSpace(creds) || !File.Exists(creds))
                        throw new FileNotFoundException(
                            $"Không tìm thấy file creds NATS. Nats:CredsFile='{options.CredsFile}', " +
                            $"đường dẫn tra cứu thực tế='{creds}', thư mục chạy (BaseDirectory)='{AppContext.BaseDirectory}'. " +
                            "Hãy đặt file nats.creds đúng vào '<thư mục chứa .exe>\\Configuration\\nats.creds' " +
                            "(hoặc khai báo Nats:CredsFile là đường dẫn tuyệt đối).");

                    return new NatsAuthOpts { CredsFile = creds };

                case "NKEY":
                    if (string.IsNullOrWhiteSpace(options.Seed))
                        throw new InvalidOperationException("NKEY auth cần Nats:Seed.");
                    return new NatsAuthOpts { NKey = options.NKey?.Trim(), Seed = options.Seed!.Trim() };

                case "TOKEN":
                    if (string.IsNullOrWhiteSpace(options.Token))
                        throw new InvalidOperationException("TOKEN auth cần Nats:Token.");
                    return new NatsAuthOpts { Token = options.Token!.Trim() };

                case "USERPASS":
                    if (string.IsNullOrWhiteSpace(options.Username))
                        throw new InvalidOperationException("USERPASS auth cần Nats:Username.");
                    return new NatsAuthOpts { Username = options.Username!.Trim(), Password = options.Password };

                case "TLS":
                    return NatsAuthOpts.Default;

                default:
                    return NatsAuthOpts.Default;
            }
        }

        /// <summary>Chuẩn hóa đường dẫn tương đối về tuyệt đối tính từ thư mục chạy ứng dụng.</summary>
        public static string? ResolvePath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            return Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
        }
    }
}
