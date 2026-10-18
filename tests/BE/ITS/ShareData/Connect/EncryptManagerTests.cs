using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Modules.Integration.Delivery.Base.Constants;
using Modules.Integration.Delivery.Base.Enums.Encrypt;
using Modules.Integration.Delivery.Base.Exceptions.Encrypt;
using Modules.Integration.Delivery.Base.Interfaces.Encrypt;
using Modules.Integration.Delivery.Base.Models.Encrypt;
using Modules.Integration.Delivery.Encrypt;
using Xunit;

namespace Tests.ShareData.Connect
{
    /// <summary>
    /// Author: DatHQ
    /// Description: Bộ kiểm thử đơn vị cho EncryptManager và EncryptKeyReader ở tầng kết nối.
    ///              Kiểm chứng các luồng mã hóa, giải mã, bọc ngoại lệ DecryptFailedException và điều phối theo chiều Outbound/Inbound.
    /// Created date: 16/10/2026
    /// Modified date: 18/10/2026 (Chuyển sang IEncryptKeyReader và EncryptKeyReader)
    /// </summary>
    public class EncryptManagerTests
    {
        private static readonly byte[] TestAes256Key =
        [
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10,
            0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18,
            0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20
        ];

        [Fact]
        public void EncryptAndDecrypt_WhenDataNullOrEmpty_ReturnsOriginalOrEmptyBytes_Test()
        {
            // Arrange
            var cfg = new ConfigurationBuilder().Build();
            var manager = new EncryptManager(new EncryptKeyReader(cfg));
            var emptyData = Array.Empty<byte>();

            // Act
            var encryptedNull = manager.Encrypt(null, null!);
            var encryptedEmpty = manager.Encrypt(null, emptyData);
            var decryptedNull = manager.Decrypt(null, null!);
            var decryptedEmpty = manager.Decrypt(null, emptyData);

            // Assert
            Assert.Empty(encryptedNull);
            Assert.Empty(encryptedEmpty);
            Assert.Empty(decryptedNull);
            Assert.Empty(decryptedEmpty);
        }

        [Fact]
        public void EncryptAndDecrypt_WhenConfigNoneOrEmpty_ReturnsOriginalBytes_Test()
        {
            // Arrange
            var cfg = new ConfigurationBuilder().Build();
            var manager = new EncryptManager(new EncryptKeyReader(cfg));
            var data = Encoding.UTF8.GetBytes("hello-plain-world");
            var config = new Dictionary<string, string>
            {
                [ConnectKeys.Encryptor] = EncryptNames.None,
                [ConnectKeys.InboundEncryptor] = EncryptNames.None
            };

            // Act
            var encrypted = manager.Encrypt(config, data);
            var decrypted = manager.Decrypt(config, encrypted);

            // Assert
            Assert.Same(data, encrypted);
            Assert.Same(encrypted, decrypted);
        }

        [Fact]
        public void EncryptAndDecrypt_WhenAesGcmWith256BitKey_RoundtripsSuccessfully_Test()
        {
            // Arrange
            var cfg = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ShareDataSecurity:Keys:my-secret-key"] = Convert.ToBase64String(TestAes256Key)
                })
                .Build();
            var keyReader = new EncryptKeyReader(cfg);
            var manager = new EncryptManager(keyReader);
            var originalBytes = Encoding.UTF8.GetBytes("{\"device\":\"VMS_01\",\"status\":\"Normal\"}");
            var config = new Dictionary<string, string>
            {
                [ConnectKeys.Encryptor] = EncryptNames.AesGcm,
                [ConnectKeys.EncryptionKeyRef] = "my-secret-key",
                [ConnectKeys.InboundEncryptor] = EncryptNames.AesGcm,
                [ConnectKeys.DecryptionKeyRef] = "my-secret-key"
            };

            // Act
            var encryptedBytes = manager.Encrypt(config, originalBytes);
            var decryptedBytes = manager.Decrypt(config, encryptedBytes);

            // Assert
            Assert.NotEqual(originalBytes, encryptedBytes);
            Assert.Equal(originalBytes, decryptedBytes);
            var jsonEnvelope = Encoding.UTF8.GetString(encryptedBytes);
            Assert.Contains("AES256-GCM", jsonEnvelope);
            Assert.Contains("my-secret-key", jsonEnvelope);
        }

        [Fact]
        public void Decrypt_WhenCipherCorrupted_ThrowsDecryptFailedException_Test()
        {
            // Arrange
            var cfg = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ShareDataSecurity:Keys:key-ref"] = Convert.ToBase64String(TestAes256Key)
                })
                .Build();
            var keyReader = new EncryptKeyReader(cfg);
            var manager = new EncryptManager(keyReader);
            var corruptedBytes = Encoding.UTF8.GetBytes("not-a-valid-json-envelope");
            var config = new Dictionary<string, string>
            {
                [ConnectKeys.InboundEncryptor] = EncryptNames.AesGcm,
                [ConnectKeys.DecryptionKeyRef] = "key-ref"
            };

            // Act & Assert
            var ex = Assert.Throws<DecryptFailedException>(() => manager.Decrypt(config, corruptedBytes));
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public void Decrypt_WhenWrongKey_ThrowsDecryptFailedException_Test()
        {
            // Arrange
            var wrongKey = new byte[AesGcmEncrypt.KeySize];
            RandomNumberGenerator.Fill(wrongKey);

            var cfgOutbound = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ShareDataSecurity:Keys:key-ref"] = Convert.ToBase64String(TestAes256Key)
                })
                .Build();
            var keyReaderOutbound = new EncryptKeyReader(cfgOutbound);
            var managerOutbound = new EncryptManager(keyReaderOutbound);

            var cfgInbound = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ShareDataSecurity:Keys:key-ref"] = Convert.ToBase64String(wrongKey)
                })
                .Build();
            var keyReaderInbound = new EncryptKeyReader(cfgInbound);
            var managerInbound = new EncryptManager(keyReaderInbound);

            var originalBytes = Encoding.UTF8.GetBytes("{\"secret\":\"confidential\"}");
            var config = new Dictionary<string, string>
            {
                [ConnectKeys.Encryptor] = EncryptNames.AesGcm,
                [ConnectKeys.EncryptionKeyRef] = "key-ref",
                [ConnectKeys.InboundEncryptor] = EncryptNames.AesGcm,
                [ConnectKeys.DecryptionKeyRef] = "key-ref"
            };

            var encryptedBytes = managerOutbound.Encrypt(config, originalBytes);

            // Act & Assert
            Assert.Throws<DecryptFailedException>(() => managerInbound.Decrypt(config, encryptedBytes));
        }

        [Fact]
        public void GetEncryptor_OutboundReadsEncryptorAndEncryptionKeyRef_InboundReadsInboundEncryptorAndDecryptionKeyRef_Test()
        {
            // Arrange
            var cfg = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ShareDataSecurity:Keys:out-key"] = Convert.ToBase64String(TestAes256Key),
                    ["ShareDataSecurity:Keys:in-key"] = Convert.ToBase64String(TestAes256Key)
                })
                .Build();
            var keyReader = new EncryptKeyReader(cfg);
            var manager = new EncryptManager(keyReader);

            var config1 = new Dictionary<string, string>
            {
                [ConnectKeys.Encryptor] = EncryptNames.AesGcm,
                [ConnectKeys.EncryptionKeyRef] = "out-key",
                [ConnectKeys.InboundEncryptor] = EncryptNames.None
            };

            // Act 1
            var (outEncrypt1, outCtx1) = manager.GetEncryptor(config1, PayloadDirection.Outbound);
            var (inEncrypt1, _) = manager.GetEncryptor(config1, PayloadDirection.Inbound);

            // Assert 1
            Assert.IsType<AesGcmEncrypt>(outEncrypt1);
            Assert.Equal("out-key", outCtx1.KeyRef);
            Assert.IsType<PlainEncrypt>(inEncrypt1);

            // Arrange 2 (ngược lại)
            var config2 = new Dictionary<string, string>
            {
                [ConnectKeys.Encryptor] = EncryptNames.None,
                [ConnectKeys.InboundEncryptor] = EncryptNames.AesGcm,
                [ConnectKeys.DecryptionKeyRef] = "in-key"
            };

            // Act 2
            var (outEncrypt2, _) = manager.GetEncryptor(config2, PayloadDirection.Outbound);
            var (inEncrypt2, inCtx2) = manager.GetEncryptor(config2, PayloadDirection.Inbound);

            // Assert 2
            Assert.IsType<PlainEncrypt>(outEncrypt2);
            Assert.IsType<AesGcmEncrypt>(inEncrypt2);
            Assert.Equal("in-key", inCtx2.KeyRef);
        }

        [Fact]
        public void GetEncryptor_WhenKeyMissingInConfig_ThrowsInvalidOperationException_Test()
        {
            // Arrange
            var cfg = new ConfigurationBuilder().Build();
            var keyReader = new EncryptKeyReader(cfg);
            var manager = new EncryptManager(keyReader);
            var config = new Dictionary<string, string>
            {
                [ConnectKeys.Encryptor] = EncryptNames.AesGcm,
                [ConnectKeys.EncryptionKeyRef] = "missing-key"
            };

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => manager.GetEncryptor(config, PayloadDirection.Outbound));
            Assert.Contains("Không tìm thấy khóa mã hóa bảo mật", ex.Message);
        }

        [Fact]
        public void EncryptKeyReader_GetKey_WhenKeyExists_ReturnsDecodedBytes_WhenMissingOrEmpty_ReturnsNull_Test()
        {
            // Arrange
            var cfg = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ShareDataSecurity:Keys:valid-key"] = Convert.ToBase64String(TestAes256Key),
                    ["ShareDataSecurity:Keys:empty-key"] = "   "
                })
                .Build();
            var reader = new EncryptKeyReader(cfg);

            // Act
            var validKey = reader.GetKey("valid-key");
            var emptyKey = reader.GetKey("empty-key");
            var missingKey = reader.GetKey("missing-key");

            // Assert
            Assert.Equal(TestAes256Key, validKey);
            Assert.Null(emptyKey);
            Assert.Null(missingKey);
        }
    }
}
