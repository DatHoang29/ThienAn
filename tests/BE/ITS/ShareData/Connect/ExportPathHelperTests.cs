using Modules.Integration.ShareData.Base.Helpers;
using System.Text;

namespace Tests.ShareData.Connect
{
    /// <summary>
    /// Author: DatHQ
    /// Description: Bộ kiểm thử đơn vị cho ExportPathHelper.SaveFile (tầng kết nối ShareData).
    ///              Kiểm chứng phân giải đường dẫn tương đối, đường dẫn tuyệt đối, tạo thư mục cha và hủy CancellationToken.
    /// Created date: 16/10/2026
    /// </summary>
    public class ExportPathHelperTests
    {
        [Fact]
        public async Task SaveFile_WhenRelativePath_SavesUnderBaseDirectoryAndMatchesContent_Test()
        {
            // Arrange
            var testBasePath = "test_export_relative/" + Guid.NewGuid().ToString("N");
            var relativePath = "subdir/test_file.json";
            var contentBytes = Encoding.UTF8.GetBytes("{\"test\":\"relative_content\"}");

            // Act
            var fullPath = await ExportPathHelper.SaveFile(
                testBasePath,
                relativePath,
                contentBytes,
                CancellationToken.None);

            // Assert
            var expectedRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, testBasePath));
            var expectedPath = Path.GetFullPath(Path.Combine(expectedRoot, relativePath));

            Assert.Equal(expectedPath, fullPath);
            Assert.True(File.Exists(fullPath));
            var readBytes = await File.ReadAllBytesAsync(fullPath);
            Assert.Equal(contentBytes, readBytes);

            // Cleanup
            if (File.Exists(fullPath)) File.Delete(fullPath);
            var parentDir = Path.GetDirectoryName(fullPath);
            if (Directory.Exists(parentDir)) Directory.Delete(parentDir, true);
            if (Directory.Exists(expectedRoot)) Directory.Delete(expectedRoot, true);
        }

        [Fact]
        public async Task SaveFile_WhenAbsolutePath_SavesDirectlyToExactPathAndCreatesDirectories_Test()
        {
            // Arrange
            var tempBase = Path.Combine(Path.GetTempPath(), "test_export_abs_" + Guid.NewGuid().ToString("N"));
            var absolutePath = Path.Combine(tempBase, "deep", "nested", "proof.json");
            var contentBytes = Encoding.UTF8.GetBytes("{\"test\":\"absolute_content\"}");

            // Act
            var fullPath = await ExportPathHelper.SaveFile(
                "any_base_path",
                absolutePath,
                contentBytes,
                CancellationToken.None);

            // Assert
            Assert.Equal(Path.GetFullPath(absolutePath), fullPath);
            Assert.True(File.Exists(fullPath));
            var readBytes = await File.ReadAllBytesAsync(fullPath);
            Assert.Equal(contentBytes, readBytes);

            // Cleanup
            if (Directory.Exists(tempBase)) Directory.Delete(tempBase, true);
        }

        [Fact]
        public async Task SaveFile_WhenCanceledToken_ThrowsOperationCanceledExceptionAndDoesNotLeaveFile_Test()
        {
            // Arrange
            var testBasePath = "test_export_cancel/" + Guid.NewGuid().ToString("N");
            var relativePath = "cancel_test.json";
            var contentBytes = Encoding.UTF8.GetBytes("{\"test\":\"should_not_exist\"}");
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var expectedRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, testBasePath));
            var expectedPath = Path.GetFullPath(Path.Combine(expectedRoot, relativePath));

            // Act & Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await ExportPathHelper.SaveFile(
                    testBasePath,
                    relativePath,
                    contentBytes,
                    cts.Token);
            });

            Assert.False(File.Exists(expectedPath));

            // Cleanup
            if (Directory.Exists(expectedRoot)) Directory.Delete(expectedRoot, true);
        }
    }
}
