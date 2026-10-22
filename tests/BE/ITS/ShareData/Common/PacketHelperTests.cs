namespace Tests.ShareData.Common;

using Module.ShareData.Core.Entities;
using Shared.DTO.Enums;
using Xunit;

/// <summary>
/// Description: Kiểm thử đơn vị cho lớp tiện ích PacketHelper:
///              - Chuẩn hoá mã gói tin (NormalizePacketCode)
///              - Phân giải số hiệu gói tin (TryGetPacketNumber)
///              - Phân giải chính sách đồng bộ (GetPacketPolicy)
///              - Phân giải enum kiểu dữ liệu (TryGetDatatypeEnum)
/// </summary>
public class PacketHelperTests
{
    [Theory]
    [InlineData("SELECT TOP (@pageSize) ID AS __lastKey, Time AS __lastTime FROM T WHERE Time > @lastTime ORDER BY Time", ShareDataEnum.PacketPolicy.AlwaysIncremental)]
    [InlineData("SELECT TOP (@pageSize) ID AS __lastKey, Time AS __lastTime FROM T WHERE Time > @lastTime AND ID > @lastKey ORDER BY Time, ID", ShareDataEnum.PacketPolicy.AlwaysIncremental)]
    [InlineData("SELECT ID, Code, Name FROM TmsZone", ShareDataEnum.PacketPolicy.Snapshot)]
    [InlineData("SELECT ID, Code FROM TmsZone -- @lastTime in comment", ShareDataEnum.PacketPolicy.Snapshot)]
    [InlineData(null, ShareDataEnum.PacketPolicy.Snapshot)]
    [InlineData("", ShareDataEnum.PacketPolicy.Snapshot)]
    public void GetPolicyFromSql_ResolvesCorrectly(string? sql, ShareDataEnum.PacketPolicy expected)
    {
        var policy = PacketHelper.GetPolicyFromSql(sql);
        Assert.Equal(expected, policy);
    }

    [Theory]
    [InlineData("101", "101_commonData")]
    [InlineData("101_commonData", "101_commonData")]
    [InlineData("102", "102_cctvData")]
    [InlineData("102_cctvData", "102_cctvData")]
    [InlineData("103", "103_vdsData")]
    [InlineData("103_vdsData", "103_vdsData")]
    [InlineData("104", "104_weatherData")]
    [InlineData("104_weatherData", "104_weatherData")]
    [InlineData("105", "105_rfidData")]
    [InlineData("105_rfidData", "105_rfidData")]
    [InlineData("106", "106_wimData")]
    [InlineData("106_wimData", "106_wimData")]
    [InlineData("107", "107_incidentData")]
    [InlineData("107_incidentData", "107_incidentData")]
    [InlineData("108", "108_vmsInfo")]
    [InlineData("108_vmsInfo", "108_vmsInfo")]
    [InlineData("109", "109_etcData")]
    [InlineData("109_etcData", "109_etcData")]
    [InlineData("110", "110_wpData")]
    [InlineData("110_wpData", "110_wpData")]
    [InlineData("111", "111")]
    public void NormalizePacketCode_ResolvesShortAndCanonicalCorrectly(string input, string expected)
    {
        var normalized = PacketHelper.NormalizePacketCode(input);
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("101", 101)]
    [InlineData("103_vdsData", 103)]
    [InlineData("105_rfidData", 105)]
    [InlineData("111", 111)]
    public void TryGetPacketNumber_ValidCodes_ReturnsTrueAndNumber(string input, int expectedNumber)
    {
        var success = PacketHelper.TryGetPacketNumber(input, out var num);
        Assert.True(success);
        Assert.Equal(expectedNumber, num);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("99")]
    [InlineData("200")]
    [InlineData("invalid")]
    public void TryGetPacketNumber_InvalidCodes_ReturnsFalse(string? input)
    {
        var success = PacketHelper.TryGetPacketNumber(input, out var num);
        Assert.False(success);
        Assert.Equal(0, num);
    }

    [Theory]
    [InlineData("TrafficFlow", true)]
    [InlineData("Weather", true)]
    [InlineData("invalid", false)]
    [InlineData(null, false)]
    public void TryGetDatatypeEnum_ResolvesCorrectly(string? input, bool expectedSuccess)
    {
        var success = PacketHelper.TryGetDatatypeEnum(input, out _);
        Assert.Equal(expectedSuccess, success);
    }
}
