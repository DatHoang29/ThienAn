using System.Text;
using Modules.Integration.Delivery;
using Modules.Integration.Delivery.Base.Constants;
using Modules.Integration.Delivery.Base.Helpers;
using Modules.Integration.Delivery.Base.Models;

namespace Tests.ShareData.Connect;

public partial class DeliveryDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_WhenPartnerConnectionTypeIsFile_RoutesToFileChannel_Test()
    {
        // Arrange
        var fileSender = CreateFileSender();
        var dispatcher = CreateDispatcher(file: fileSender);
        var ctx = CreateDummyContext("103", "PARTNER_FILE", ConnectionType.File);
        var jsonPayload = "{\"vdsCount\": 12, \"speed\": 80}";
        var customDir = "Out/TestPartner";
        var fileNamePattern = "vds_export_{serial}.json";
        var request = new DeliveryRequest
        {
            JsonPayload = jsonPayload,
            ConnectType = ConnectionType.File,
            ConnectConfig = new Dictionary<string, string>
            {
                ["DirectoryPath"] = customDir,
                ["FileNamePattern"] = fileNamePattern
            }
        };

        // Act
        var result = await dispatcher.Dispatch(request, ctx, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("File", result.Channel);
        Assert.Equal(Path.Combine(customDir, "vds_export_0.json"), result.TargetAddressOrPath);
        Assert.Equal(Encoding.UTF8.GetByteCount(jsonPayload), result.SentByteSize);
    }

    /// <summary>
    /// Description: Kiểm thử FileDeliverySender từ chối đường dẫn thư mục không hợp lệ hoặc chứa path traversal.
    /// Created date: 17/10/2026
    /// Modified date: 24/10/2026
    /// </summary>
    [Theory]
    [InlineData("Out/Invalid|Path\0", "không hợp lệ")]
    [InlineData("safe/../../etc/passwd", "traversal")]
    public async Task FileSender_WhenDirectoryPathInvalidOrTraversal_ReturnsInvalidConfiguration_Test(string invalidDir, string expectedErrorKeyword)
    {
        // Arrange
        var fileSender = CreateFileSender();
        var ctx = CreateDummyContext("103", "PARTNER_FILE_INVALID", ConnectionType.File);
        var request = new DeliveryRequest
        {
            PayloadBytes = Encoding.UTF8.GetBytes("test-content"),
            ConnectType = ConnectionType.File,
            ConnectConfig = new Dictionary<string, string>
            {
                ["DirectoryPath"] = invalidDir
            }
        };

        // Act
        var result = await fileSender.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("File", result.Channel);
        Assert.Equal("InvalidConfiguration", result.ErrorCode);
        Assert.Contains(expectedErrorKeyword, result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FileSender_WhenDirectoryPathAndFileNamePatternConfigured_SavesToExpectedCustomPath_Test()
    {
        // Arrange
        var fileSender = CreateFileSender();
        var ctx = CreateDummyContext("103", "PARTNER_FILE_CUSTOM", ConnectionType.File);
        var expectedBytes = Encoding.UTF8.GetBytes("custom-file-payload-content");
        var request = new DeliveryRequest
        {
            PayloadBytes = expectedBytes,
            FileExtension = ".csv",
            ConnectType = ConnectionType.File,
            ConnectConfig = new Dictionary<string, string>
            {
                ["DirectoryPath"] = "custom_export_dir",
                ["FileNamePattern"] = "export_{partner}_{packet}_{timestamp}"
            }
        };

        // Act
        var result = await fileSender.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("File", result.Channel);
        Assert.NotNull(result.TargetAddressOrPath);
        Assert.Contains("custom_export_dir", result.TargetAddressOrPath);
        Assert.Contains("PARTNER_FILE_CUSTOM", result.TargetAddressOrPath);
        Assert.EndsWith(".csv", result.TargetAddressOrPath);

        var fullPath = Path.Combine(ExportPathHelper.ResolveRootPath(ExportPathHelper.DefaultBasePath), result.TargetAddressOrPath);
        Assert.True(File.Exists(fullPath));
        var savedBytes = await File.ReadAllBytesAsync(fullPath);
        Assert.Equal(expectedBytes, savedBytes);

        // Clean up
        if (File.Exists(fullPath)) File.Delete(fullPath);
    }

    /// <summary>
    /// Description: Kiểm thử FileDeliverySender phát hiện cảnh báo khi FileNamePattern thiếu tham số biến đổi {serial} hoặc {timestamp}.
    /// Created date: 17/10/2026
    /// Modified date: 24/10/2026
    /// </summary>
    [Theory]
    [InlineData("static_export_file.json", true)]
    [InlineData("export_{serial}.json", false)]
    public async Task FileSender_WhenFileNamePatternTokensChecked_EmitsWarningAsExpected_Test(string fileNamePattern, bool shouldWarn)
    {
        // Arrange
        var handler = CreateFileSender();
        var ctx = CreateSendContext(connectionType: "File");
        var request = new DeliveryRequest
        {
            PayloadBytes = Encoding.UTF8.GetBytes("{\"file\": 1}"),
            ConnectType = ConnectionType.File,
            ConnectConfig = new Dictionary<string, string>
            {
                [ConnectKeys.DirectoryPath] = "test_warn_out",
                [ConnectKeys.FileNamePattern] = fileNamePattern
            }
        };

        // Act
        var result = await handler.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Warnings);
        if (shouldWarn)
        {
            Assert.Contains(result.Warnings, w => w.Contains("thiếu tham số biến đổi {serial} và {timestamp}"));
        }
        else
        {
            Assert.DoesNotContain(result.Warnings, w => w.Contains("thiếu tham số biến đổi {serial} và {timestamp}"));
        }

        // Dọn dẹp tệp sinh ra
        if (!string.IsNullOrWhiteSpace(result.TargetAddressOrPath))
        {
            var fullPath = Path.Combine(ExportPathHelper.ResolveRootPath(ExportPathHelper.DefaultBasePath), result.TargetAddressOrPath);
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }
    }
}
