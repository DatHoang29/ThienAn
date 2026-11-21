using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Modules.Integration.Delivery.Base.Constants;
using Modules.Integration.Delivery.Base.Models.Encrypt;

namespace Modules.Integration.Delivery.Encrypt
{
    /// <summary>
    /// Bộ mã hóa chuẩn AES-256-GCM với Nonce 12 bytes, Tag 16 bytes và đóng gói JSON envelope:
    /// {"alg":"AES256-GCM","kid":"...","iv":"...","tag":"...","data":"..."}
    /// </summary>
    public class AesGcmEncrypt
    {
        public const string AlgorithmName = "AES256-GCM";
        public const int NonceSize = 12;
        public const int TagSize = 16;
        public const int KeySize = 32; // 256 bits

        public string Name => EncryptNames.AesGcm;

        /// <summary>
        /// Author: DatHQ
        /// Description: Mã hóa AES-GCM với nonce ngẫu nhiên, trả JSON gồm alg, kid, iv, tag, data. Khóa phải đúng 256 bit.
        /// Created date: 19/10/2026
        /// </summary>
        public byte[] Encrypt(byte[] payload, EncryptContext context)
        {
            ArgumentNullException.ThrowIfNull(payload);
            if (context.KeyBytes == null || context.KeyBytes.Length != KeySize)
            {
                throw new InvalidOperationException($"AesGcmEncrypt yêu cầu khóa 256-bit ({KeySize} bytes). Độ dài thực tế: {context.KeyBytes?.Length ?? 0} bytes.");
            }

            var nonce = new byte[NonceSize];
            RandomNumberGenerator.Fill(nonce);

            var tag = new byte[TagSize];
            var cipherText = new byte[payload.Length];

            using (var aesGcm = new AesGcm(context.KeyBytes, TagSize))
            {
                aesGcm.Encrypt(nonce, payload, cipherText, tag);
            }

            var envelope = new
            {
                alg = AlgorithmName,
                kid = context.KeyRef ?? "default",
                iv = Convert.ToBase64String(nonce),
                tag = Convert.ToBase64String(tag),
                data = Convert.ToBase64String(cipherText)
            };

            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope));
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Giải mã JSON do Encrypt tạo, đọc iv, tag, data. Khóa phải đúng 256 bit.
        /// Created date: 19/10/2026
        /// </summary>
        public byte[] Decrypt(byte[] encryptedPayload, EncryptContext context)
        {
            ArgumentNullException.ThrowIfNull(encryptedPayload);
            if (context.KeyBytes == null || context.KeyBytes.Length != KeySize)
            {
                throw new InvalidOperationException($"AesGcmEncrypt yêu cầu khóa 256-bit ({KeySize} bytes).");
            }

            using var doc = JsonDocument.Parse(encryptedPayload);
            var root = doc.RootElement;

            if (!root.TryGetProperty("iv", out var ivProp) ||
                !root.TryGetProperty("tag", out var tagProp) ||
                !root.TryGetProperty("data", out var dataProp))
            {
                throw new InvalidOperationException("Payload không đúng định dạng envelope của AesGcmEncrypt (thiếu iv, tag hoặc data).");
            }

            var nonce = Convert.FromBase64String(ivProp.GetString()!);
            var tag = Convert.FromBase64String(tagProp.GetString()!);
            var cipherText = Convert.FromBase64String(dataProp.GetString()!);

            var plainText = new byte[cipherText.Length];
            using (var aesGcm = new AesGcm(context.KeyBytes, TagSize))
            {
                aesGcm.Decrypt(nonce, cipherText, tag, plainText);
            }

            return plainText;
        }
    }
}
