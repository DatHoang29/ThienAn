namespace Modules.Integration.Delivery.Base.Constants
{
    /// <summary>
    /// Description: Danh mục các hằng số khóa cấu hình kết nối chuẩn hóa giữa hai chiều gửi và nhận.
    ///              Thuần BCL, dùng chung cho cả WebAPI và Worker theo chuẩn Layer Separation.
    /// Created date: 14/10/2026
    /// </summary>
    public static class ConnectKeys
    {
        // ── Chiều nhận (Inbound) ────────────────────────────────────────────────
        public const string InboundEncryptor = "InboundEncryptor";
        public const string DecryptionKeyRef = "DecryptionKeyRef";

        // ── Chiều gửi (Outbound) & Dùng chung ────────────────────────────────────
        public const string Encryptor = "Encryptor";
        public const string EncryptionKeyRef = "EncryptionKeyRef";
        public const string Format = "Format";
        public const string DirectoryPath = "DirectoryPath";
        public const string FileNamePattern = "FileNamePattern";

        // Chung / Dispatcher
        public const string Enabled = "Enabled";
        public const string ConnectType = "ConnectionType";
        public const string RawConfig = "RawConfig";
        public const string PartnerCode = "PartnerCode";

        // REST / HTTP API
        public const string TimeoutSeconds = "TimeoutSeconds";
        public const string HttpTimeoutSec = "HttpTimeoutSec";
        public const string TimeoutSec = "TimeoutSec";

        public const string BypassSsl = "BypassSsl";
        public const string IgnoreSsl = "IgnoreSsl";
        public const string Insecure = "Insecure";

        public const string AuthType = "AuthType";
        public const string Token = "Token";
        public const string HeaderName = "HeaderName";
        public const string ApiKeyHeader = "ApiKeyHeader";
        public const string ApiKey = "ApiKey";
        public const string Username = "Username";
        public const string Password = "Password";

        public const string SuccessField = "SuccessField";
        public const string ErrorMessageField = "ErrorMessageField";
        public const string ErrorCodeField = "ErrorCodeField";
        public const string ResponseContract = "ResponseContract";

        // File
        public const string OutputPath = "OutputPath";
        public const string BasePath = "BasePath";

        // FTP
        public const string RemotePath = "RemotePath";
        public const string UseSsl = "UseSsl";
        public const string EnableSsl = "EnableSsl";
        public const string User = "User";
        public const string Pass = "Pass";
        public const string CsvIncludeBom = "CsvIncludeBom";
        public const string FileName = "FileName";

        // Kafka
        public const string TimeoutMs = "TimeoutMs";
        public const string ClientId = "ClientId";
        public const string SecurityProtocol = "SecurityProtocol";
        public const string SaslMechanism = "SaslMechanism";
        public const string SaslUsername = "SaslUsername";
        public const string SaslPassword = "SaslPassword";
        public const string SslCaLocation = "SslCaLocation";
        public const string Topic = "Topic";
        public const string Channel = "Channel";
        public const string BootstrapServers = "BootstrapServers";

        // ── Danh sách Aliases theo thứ tự ưu tiên ──────────────────────────────
        public static readonly string[] TimeoutSecondsAliases = [HttpTimeoutSec, TimeoutSec];
        public static readonly string[] BypassSslAliases = [IgnoreSsl, Insecure];
        public static readonly string[] HeaderNameAliases = [ApiKeyHeader];
        public static readonly string[] FileDirectoryAliases = [BasePath];
        public static readonly string[] UseSslAliases = [EnableSsl];
        public static readonly string[] UsernameAliases = [User];
        public static readonly string[] PasswordAliases = [Pass];
        public static readonly string[] TopicAliases = [Channel];
    }
}
