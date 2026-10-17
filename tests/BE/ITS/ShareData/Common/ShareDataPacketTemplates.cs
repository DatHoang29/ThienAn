namespace Tests.ShareData.Common;

using Module.ShareData.Core.Entities;
using Module.ShareData.Infrastructure.Persistence.SeedData;

/// <summary>
/// Author: DatHQ
/// Description: Helper cung cấp câu truy vấn SQL trích xuất chuẩn cho các gói tin ShareData (101 - 110)
///              dùng trong môi trường kiểm thử xUnit, lấy nguồn trực tiếp từ ShareDataPacketSeedData.
/// Created date: 16/10/2026
/// </summary>
public static class ShareDataPacketTemplates
{
    public const string Packet110_PublicMessaging = ShareDataPacketSeedData.Sql110_PublicMessaging;

    public static string? GetExtractSql(string? packetCode)
    {
        if (string.IsNullOrWhiteSpace(packetCode)) return null;
        return ShareDataPacketSeedData.GetExtractSql(packetCode);
    }

    public static ShareDataPacket ApplyTemplate(ShareDataPacket packet)
    {
        if (packet == null) return null!;
        ShareDataPacketSeedData.ApplyTemplate(packet, packet.Code ?? string.Empty);
        return packet;
    }
}
