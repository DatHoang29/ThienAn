using System.Security.Cryptography;
using Modules.Integration.Delivery.Base.Constants;
using Modules.Integration.Delivery.Base.Enums.Encrypt;
using Modules.Integration.Delivery.Base.Helpers;
using Modules.Integration.Delivery.Base.Models.Encrypt;

namespace Modules.Integration.Delivery.Encrypt
{
    /// <summary>
    /// Description: Bộ điều phối mã hoá/giải mã payload hai chiều (EncryptManager), nhận khóa thông qua EncryptKeyReader.
    ///              Thuần BCL theo chuẩn Clean Architecture.
    ///              - Outbound: đọc key "Encryptor" và "EncryptionKeyRef".
    ///              - Inbound: đọc key "InboundEncryptor" và "DecryptionKeyRef".
    /// Created date: 14/10/2026
    /// Modified date: 19/10/2026 (Chuyển ngoại lệ giải mã sang CryptographicException chuẩn BCL)
    /// </summary>
    public class EncryptManager(EncryptKeyReader keyReader)
    {
        private readonly EncryptKeyReader _keyReader = keyReader ?? throw new ArgumentNullException(nameof(keyReader));

        /// <summary>
        /// Description: Lấy bộ mã hóa và ngữ cảnh mã hóa mặc định theo cấu hình kết nối (chiều gửi Outbound).
        /// </summary>
        public (string EncryptorName, EncryptContext Context) GetEncryptor(IDictionary<string, string>? connectionConfig)
            => GetEncryptor(connectionConfig, PayloadDirection.Outbound);

        /// <summary>
        /// Description: Lấy bộ mã hóa và ngữ cảnh mã hóa theo cấu hình kết nối và hướng dữ liệu (Inbound/Outbound).
        /// </summary>
        public (string EncryptorName, EncryptContext Context) GetEncryptor(IDictionary<string, string>? connectionConfig, PayloadDirection direction)
        {
            var reader = new ConnectConfigReader(connectionConfig);

            var (encryptorKeyName, keyRefName) = direction == PayloadDirection.Inbound
                ? (ConnectKeys.InboundEncryptor, ConnectKeys.DecryptionKeyRef)
                : (ConnectKeys.Encryptor, ConnectKeys.EncryptionKeyRef);

            var encryptorName = reader.GetString(encryptorKeyName);
            if (string.IsNullOrWhiteSpace(encryptorName) || string.Equals(encryptorName, EncryptNames.None, StringComparison.OrdinalIgnoreCase))
            {
                return (EncryptNames.None, new EncryptContext(null, null, connectionConfig));
            }

            if (string.Equals(encryptorName, EncryptNames.AesGcm, StringComparison.OrdinalIgnoreCase))
            {
                var keyRef = reader.GetString(keyRefName);
                if (string.IsNullOrWhiteSpace(keyRef))
                {
                    var directionText = direction == PayloadDirection.Inbound ? "chiều nhận (Inbound)" : "chiều gửi (Outbound)";
                    throw new InvalidOperationException($"Thiếu cấu hình [{keyRefName}] cho kênh mã hóa AesGcm ở {directionText}.");
                }

                var keyBytes = _keyReader.GetKey(keyRef);
                if (keyBytes == null || keyBytes.Length == 0)
                {
                    throw new InvalidOperationException($"Không tìm thấy khóa mã hóa bảo mật tương ứng với ref [{keyRef}].");
                }

                if (keyBytes.Length != AesGcmEncrypt.KeySize)
                {
                    throw new InvalidOperationException($"Độ dài khóa mã hóa [{keyRef}] không hợp lệ ({keyBytes.Length} bytes), yêu cầu đúng {AesGcmEncrypt.KeySize} bytes (256 bits).");
                }

                return (EncryptNames.AesGcm, new EncryptContext(keyRef, keyBytes, connectionConfig));
            }

            throw new InvalidOperationException($"Thuật toán mã hóa [{encryptorName}] không được hỗ trợ.");
        }

        /// <summary>
        /// Description: Mã hóa dữ liệu theo cấu hình kết nối đối tác.
        /// </summary>
        public byte[] Encrypt(IDictionary<string, string>? connectConfig, byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return data ?? [];
            }

            var (encryptorName, context) = GetEncryptor(connectConfig, PayloadDirection.Outbound);
            if (encryptorName == EncryptNames.None)
            {
                return data;
            }

            return new AesGcmEncrypt().Encrypt(data, context);
        }

        /// <summary>
        /// Description: Giải mã dữ liệu theo cấu hình kết nối đối tác.
        /// </summary>
        public byte[] Decrypt(IDictionary<string, string>? connectConfig, byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return data ?? [];
            }

            try
            {
                var (encryptorName, context) = GetEncryptor(connectConfig, PayloadDirection.Inbound);
                if (encryptorName == EncryptNames.None)
                {
                    return data;
                }

                return new AesGcmEncrypt().Decrypt(data, context);
            }
            catch (Exception ex) when (ex is not CryptographicException)
            {
                throw new CryptographicException(ex.Message, ex);
            }
        }
    }
}
