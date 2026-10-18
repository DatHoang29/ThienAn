using System.Security.Cryptography;
using System.Text;
using Modules.Integration.ShareData.Base.Constants;
using Modules.Integration.ShareData.Base.Enums.Encryption;
using Modules.Integration.ShareData.Base.Exceptions.Encryption;
using Modules.Integration.ShareData.Base.Interfaces.Encryption;
using Modules.Integration.ShareData.Base.Models.Encryption;
using Modules.Integration.ShareData.Encryption;
using Xunit;

namespace Tests.ShareData.Connect
{
    /// <summary>
    /// Author: DatHQ
    /// Description: Bộ kiểm thử đơn vị cho EncryptManager ở tầng kết nối.
    ///              Kiểm chứng các luồng mã hóa, giải mã, bọc ngoại lệ DecryptFailedException và điều phối theo chiều Outbound/Inbound.
    /// Created date: 16/10/2026
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
            var manager = new EncryptManager();
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
            var manager = new EncryptManager();
            var data = Encoding.UTF8.GetBytes("hello-plain-world");
            var config = new Dictionary<string, string>
            {
                [ConnectKeys.Encryptor] = EncryptorNames.None,
                [ConnectKeys.InboundEncryptor] = EncryptorNames.None
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
            var keyResolver = new SimpleKeyResolver("my-secret-key", TestAes256Key);
            var manager = new EncryptManager(keyResolver);
            var originalBytes = Encoding.UTF8.GetBytes("{\"device\":\"VMS_01\",\"status\":\"Normal\"}");
            var config = new Dictionary<string, string>
            {
                [ConnectKeys.Encryptor] = EncryptorNames.AesGcm,
                [ConnectKeys.EncryptionKeyRef] = "my-secret-key",
                [ConnectKeys.InboundEncryptor] = EncryptorNames.AesGcm,
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
            var keyResolver = new SimpleKeyResolver("key-ref", TestAes256Key);
            var manager = new EncryptManager(keyResolver);
            var corruptedBytes = Encoding.UTF8.GetBytes("not-a-valid-json-envelope");
            var config = new Dictionary<string, string>
            {
                [ConnectKeys.InboundEncryptor] = EncryptorNames.AesGcm,
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
            var wrongKey = new byte[AesGcmEncryptor.KeySize];
            RandomNumberGenerator.Fill(wrongKey);

            var keyResolverOutbound = new SimpleKeyResolver("key-ref", TestAes256Key);
            var managerOutbound = new EncryptManager(keyResolverOutbound);

            var keyResolverInbound = new SimpleKeyResolver("key-ref", wrongKey);
            var managerInbound = new EncryptManager(keyResolverInbound);

            var originalBytes = Encoding.UTF8.GetBytes("{\"secret\":\"confidential\"}");
            var config = new Dictionary<string, string>
            {
                [ConnectKeys.Encryptor] = EncryptorNames.AesGcm,
                [ConnectKeys.EncryptionKeyRef] = "key-ref",
                [ConnectKeys.InboundEncryptor] = EncryptorNames.AesGcm,
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
            var keyResolver = new MultiKeyResolver(new Dictionary<string, byte[]>
            {
                ["out-key"] = TestAes256Key,
                ["in-key"] = TestAes256Key
            });
            var manager = new EncryptManager(keyResolver);

            var config1 = new Dictionary<string, string>
            {
                [ConnectKeys.Encryptor] = EncryptorNames.AesGcm,
                [ConnectKeys.EncryptionKeyRef] = "out-key",
                [ConnectKeys.InboundEncryptor] = EncryptorNames.None
            };

            // Act 1
            var (outEncryptor1, outCtx1) = manager.GetEncryptor(config1, PayloadDirection.Outbound);
            var (inEncryptor1, _) = manager.GetEncryptor(config1, PayloadDirection.Inbound);

            // Assert 1
            Assert.IsType<AesGcmEncryptor>(outEncryptor1);
            Assert.Equal("out-key", outCtx1.KeyRef);
            Assert.IsType<PlainEncryptor>(inEncryptor1);

            // Arrange 2 (ngược lại)
            var config2 = new Dictionary<string, string>
            {
                [ConnectKeys.Encryptor] = EncryptorNames.None,
                [ConnectKeys.InboundEncryptor] = EncryptorNames.AesGcm,
                [ConnectKeys.DecryptionKeyRef] = "in-key"
            };

            // Act 2
            var (outEncryptor2, _) = manager.GetEncryptor(config2, PayloadDirection.Outbound);
            var (inEncryptor2, inCtx2) = manager.GetEncryptor(config2, PayloadDirection.Inbound);

            // Assert 2
            Assert.IsType<PlainEncryptor>(outEncryptor2);
            Assert.IsType<AesGcmEncryptor>(inEncryptor2);
            Assert.Equal("in-key", inCtx2.KeyRef);
        }

        private class SimpleKeyResolver(string expectedRef, byte[] key) : IKeyResolver
        {
            public byte[]? ResolveKey(string keyRef)
            {
                return string.Equals(keyRef, expectedRef, StringComparison.OrdinalIgnoreCase) ? key : null;
            }
        }

        private class MultiKeyResolver(IDictionary<string, byte[]> keys) : IKeyResolver
        {
            public byte[]? ResolveKey(string keyRef)
            {
                return keys.TryGetValue(keyRef, out var key) ? key : null;
            }
        }
    }
}
