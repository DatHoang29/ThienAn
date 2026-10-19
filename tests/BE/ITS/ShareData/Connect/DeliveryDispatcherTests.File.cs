using System.Text;
using Modules.Integration.Delivery;
using Modules.Integration.Delivery.Base.Constants;
using Modules.Integration.Delivery.Base.Helpers;
using Modules.Integration.Delivery.Base.Models;
using Modules.Integration.Delivery.Formats;

namespace Tests.ShareData.Connect;

public partial class DeliveryDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_WhenPartnerConnectionTypeIsFile_RoutesToFileChannel_Test()
    {
        // Arrange
        var fileSender = CreateFileSender();
        var dispatcher = new DeliveryDispatcher([fileSender]);
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

    [Fact]
    public async Task FileSender_WhenDirectoryPathHasInvalidCharacters_ReturnsInvalidPathError_Test()
    {
        // Arrange
        var fileSender = CreateFileSender();
        var ctx = CreateDummyContext("103", "PARTNER_FILE_INVALID", ConnectionType.File);
        var invalidDir = "Out/Invalid|Path\0";
        var request = new DeliveryRequest
        {
            JsonPayload = "{}",
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
        Assert.Contains("không hợp lệ", result.ErrorMessage);
    }

    [Fact]
    public void CsvFormat_ConvertsJsonArrayToRfc4180Csv_EscapesProperly_Test()
    {
        // Arrange
        var json = """
            [
                {"id": 1, "name": "Station A", "remark": "Clear, dry"},
                {"id": 2, "name": "Station \"B\"", "remark": "Rainy, wet\r\nSlippery road"}
            ]
            """;

        // Act
        var csv = CsvFormat.ConvertJsonToCsv(json);

        // Assert
        Assert.Contains("id,name,remark", csv);
        Assert.Contains("1,Station A,\"Clear, dry\"", csv);
        Assert.Contains("2,\"Station \"\"B\"\"\",\"Rainy, wet\r\nSlippery road\"", csv);
    }

    [Fact]
    public async Task FileSender_WhenDirectoryPathHasPathTraversal_ReturnsInvalidConfiguration_Test()
    {
        var fileSender = CreateFileSender();
        var ctx = CreateDummyContext("103", "PARTNER_FILE_TRAVERSAL", ConnectionType.File);
        var request = new DeliveryRequest
        {
            PayloadBytes = Encoding.UTF8.GetBytes("test-content"),
            ConnectType = ConnectionType.File,
            ConnectConfig = new Dictionary<string, string>
            {
                ["DirectoryPath"] = "safe/../../etc/passwd"
            }
        };

        var result = await fileSender.Send(request, ctx, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("File", result.Channel);
        Assert.Equal("InvalidConfiguration", result.ErrorCode);
        Assert.Contains("traversal", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FileSender_WhenDirectoryPathAndFileNamePatternConfigured_SavesToExpectedCustomPath_Test()
    {
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

        var result = await fileSender.Send(request, ctx, CancellationToken.None);

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

    [Fact]
    public async Task FileSender_WhenFileNamePatternMissingRequiredTokens_EmitsWarning_Test()
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
                [ConnectKeys.FileNamePattern] = "static_export_file.json"
            }
        };

        // Act
        var result = await handler.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Warnings);
        Assert.Contains(result.Warnings, w => w.Contains("thiếu tham số biến đổi {serial} và {timestamp}"));

        // Dọn dẹp tệp sinh ra
        if (!string.IsNullOrWhiteSpace(result.TargetAddressOrPath))
        {
            var fullPath = Path.Combine(ExportPathHelper.ResolveRootPath(ExportPathHelper.DefaultBasePath), result.TargetAddressOrPath);
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }
    }

    [Fact]
    public async Task FileSender_WhenFileNamePatternHasSerialToken_DoesNotEmitMissingTokenWarning_Test()
    {
        // Arrange
        var handler = CreateFileSender();
        var ctx = CreateSendContext(connectionType: "File");
        var request = new DeliveryRequest
        {
            PayloadBytes = Encoding.UTF8.GetBytes("{\"file\": 2}"),
            ConnectType = ConnectionType.File,
            ConnectConfig = new Dictionary<string, string>
            {
                [ConnectKeys.DirectoryPath] = "test_warn_out",
                [ConnectKeys.FileNamePattern] = "export_{serial}.json"
            }
        };

        // Act
        var result = await handler.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Warnings);
        Assert.DoesNotContain(result.Warnings, w => w.Contains("thiếu tham số biến đổi {serial} và {timestamp}"));

        // Dọn dẹp tệp sinh ra
        if (!string.IsNullOrWhiteSpace(result.TargetAddressOrPath))
        {
            var fullPath = Path.Combine(ExportPathHelper.ResolveRootPath(ExportPathHelper.DefaultBasePath), result.TargetAddressOrPath);
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }
    }
}
