using Microsoft.AspNetCore.Authorization;
using Module.ShareData.Core.Dto.Inbound;
using System.Text;
using Module.ShareData.Core.Exceptions;

namespace Module.ShareData.Controllers.Inbound;

/// <summary>
/// Author: HieuNV
/// Description: API tiếp nhận gói tin từ đối tác (luồng NHẬN).
///              Chỉ lưu thô vào ShareDataInboundPacket (CSDL Inbound) — việc mapping ngược
///              và ghi CSDL đích do DataInboundWorker thực hiện sau.
/// Created date: 15/09/2026
/// Updated date: 20/09/2026
/// </summary>
public class ShareDataInboundController : BaseController
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Nhận một gói tin — thêm mới 1 bản ghi ShareDataInboundPacket.
    /// Created date: 15/09/2026
    /// Updated date: 20/09/2026
    /// </summary>
    [AllowAnonymous]
    [ApiDescriptionSettings(Name = "Receive"), HttpPost]
    [DisplayName("Receive ShareData inbound packet")]
    public async Task AddShareDataInbound()
    {
        var httpRequest = App.HttpContext?.Request
            ?? throw Oops.Oh(BaseMsg.ShareData.Exception.HttpContextUnavailable);

        // Đọc header thủ công, KHÔNG dùng [FromHeader] trên class phức hợp: HeaderModelBinderProvider
        // của ASP.NET Core chỉ bind được string / collection string, gắn lên một class thì các property
        // lặng lẽ để null — gói tin vào tới handler là mất sạch định danh.
        var header = new ShareDataInboundIdentityHeader
        {
            PartnerCode = httpRequest.Headers["PartnerCode"].FirstOrDefault(),
            PacketCode = httpRequest.Headers["PacketCode"].FirstOrDefault(),
            SerialNbr = int.TryParse(httpRequest.Headers["SerialNbr"].FirstOrDefault(), out var serialNbr)
                ? serialNbr
                : null
        };

        string rawContent;
        using (var reader = new StreamReader(httpRequest.Body, Encoding.UTF8, leaveOpen: true))
        {
            rawContent = await reader.ReadToEndAsync();
        }

        var input = new ShareDataAddInboundInput
        {
            PartnerCode = header.PartnerCode?.Trim(),
            PacketCode = header.PacketCode?.Trim(),
            SerialNbr = header.SerialNbr,
            RawContent = rawContent
        };

        await MessBus.InvokeAsync(input);
    }
}
