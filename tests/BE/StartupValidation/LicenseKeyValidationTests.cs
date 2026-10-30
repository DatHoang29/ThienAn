using System;
using System.IO;
using Shared.Core.Security;
using Xunit;

namespace Tests.StartupValidation;

/// <summary>
/// Description: Bộ kiểm thử xác thực cơ chế kiểm tra bản quyền và định dạng khóa hệ thống
/// Created date: 30/10/2026
/// </summary>
[Collection("api")]
public class LicenseKeyValidationTests
{
    /// <summary>
    /// Description: Kiểm tra cơ chế GetLicenseStatus khi nạp tệp bản quyền hệ thống hiện hành
    /// Created date: 30/10/2026
    /// </summary>
    [Fact]
    public void GetLicenseStatus_WithCurrentActiveLicense_ReturnsValid_Test()
    {
        // Act
        var status = LicenseService.GetLicenseStatus();

        // Assert
        Assert.NotNull(status);
        Assert.True(status.IsLicensed);
        Assert.Equal("Valid", status.Code);
    }

    /// <summary>
    /// Description: Kiểm tra hàm lấy định danh phần cứng LicenseService.GetHardwareId() không rỗng
    /// Created date: 30/10/2026
    /// </summary>
    [Fact]
    public void LicenseService_GetHardwareId_ReturnsNonEmptyValue_Test()
    {
        // Act
        var hwid = LicenseService.GetHardwareId();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(hwid));
    }

    /// <summary>
    /// Description: Kiểm tra ValidateLicense phát hiện và từ chối các định dạng khóa không hợp lệ
    /// Created date: 30/10/2026
    /// </summary>
    [Theory]
    [InlineData("", "NotLicensed")]
    [InlineData("invalid-single-segment", "Invalid")]
    [InlineData("header.payload", "Invalid")]
    [InlineData("header.payload.corrupted_signature_xyz", "Invalid")]
    public void ValidateLicense_WhenFormatOrSignatureInvalid_ReturnsRejected_Test(string key, string expectedCode)
    {
        // Act
        var status = LicenseService.ValidateLicense(key);

        // Assert
        Assert.NotNull(status);
        Assert.False(status.IsLicensed);
        Assert.Equal(expectedCode, status.Code);
    }

    /// <summary>
    /// Description: Kiểm tra tệp TAKeyData.key không chứa ký tự xuống dòng thừa ở cuối làm vỡ giải mã Base64
    /// Created date: 30/10/2026
    /// </summary>
    [Fact]
    public void LicenseKeyDataFile_DoesNotContainTrailingNewline_Test()
    {
        // Arrange
        var keyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TAKeyData.key");
        if (!File.Exists(keyPath))
            keyPath = "TAKeyData.key";

        // Act
        var bytes = File.ReadAllBytes(keyPath);
        var lastByte = bytes[^1];

        // Assert - byte cuối không được là \n (0x0A) hoặc \r (0x0D)
        Assert.NotEqual((byte)0x0A, lastByte);
        Assert.NotEqual((byte)0x0D, lastByte);
    }
}
