namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Một file trong manifest backup nguội: đường dẫn tương đối trong thư mục backup, kích thước byte và
/// SHA-256 (hex) đo lúc tạo backup. Restore/preview băm lại theo danh sách này trước khi dừng/đổi root node.
/// Created date: 30/10/2026
/// </summary>
internal sealed record BackupManifestEntryOutput(string Path, long Length, string Sha256);
