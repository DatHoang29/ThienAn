namespace Modules.VDS.Core.Dto;

/// <summary>
/// Author: KienLT
/// Description: Sự cố TMS được nối vào một bản tin / hành trình VDS.
///
/// <para>Khai tường minh từng trường thay vì trả entity <c>TmsIncident</c>: entity kế thừa
/// <c>EntityTenant</c> nên kéo theo <c>TenantId</c>, <c>IsDelete</c>, <c>RowStatus</c>, <c>CreateUId</c>,
/// <c>UpdateUId</c> — hạ tầng lưu trữ, không phải dữ liệu nghiệp vụ, và không có việc gì ở màn hình VDS.</para>
///
/// <para><b>Giữ ĐỦ trường nghiệp vụ ĐỌC ĐƯỢC TỪ DB của entity</b> (chỉ bỏ phần hạ tầng nói trên) thay vì
/// tự chọn một tập hẹp: màn hình nào của FE đang đọc trường nào thì chưa đo được từ phía WebAPI, nên cắt bớt
/// là rủi ro không cần thiết. Mục đích của DTO này là bỏ nhiễu hạ tầng, không phải thu hẹp dữ liệu.</para>
///
/// <para><b>KHÔNG chuẩn hoá các cột thời gian ở đây — và đó là quyết định có chủ ý.</b> Bản đầu (22/09/2026)
/// gắn <c>ServerTimeToUtcConverter</c> cho <c>StartDate</c>/<c>CreatedDate</c>/... với lập luận "chỉ worker
/// ghi <c>EventId</c> nên mọi dòng VDS đọc đều do worker tạo, tức là giờ server". <b>Lập luận đó sai</b>: nó
/// suy nguồn giờ từ NGƯỜI TẠO, trong khi cột có thể bị ghi lại suốt đời bản ghi.
/// <c>TmsIncident/Update</c> nhận <c>UpdateTmsIncidentInput : AddTmsIncidentInput : TmsIncident</c> nên mang
/// theo <c>StartDate</c>, <c>IncidentUpdateMapping.Apply</c> copy nguyên không quy đổi, và Update chạy trên
/// BẤT KỲ incident theo ID — kể cả bản ghi do worker tạo. Nên cùng một cột, cùng tập dòng mà VDS đọc, vẫn
/// trộn giờ server (worker, và TMS khi FE không gửi) với UTC (giá trị FE gửi được lưu nguyên).</para>
/// <para>Chuẩn hoá một chiều trong hoàn cảnh đó sẽ làm SAI một nửa số dòng. Giá trị vì vậy được trả NGUYÊN
/// như trong DB, không hậu tố <c>Z</c>, để không công bố một thời điểm mà hệ thống không bảo đảm được. Muốn
/// đóng thật thì phải thống nhất phía GHI của TMS trước — cụ thể là <c>IncidentCommandHandler</c>,
/// <c>IncidentExternalCommandHandler</c> và đường Update phải quy đổi giá trị FE gửi bằng
/// <c>BaseUtil.TimeServer.GetTimeServer(utc)</c> như <c>IncidentPromoter</c> đang làm.</para>
///
/// <para><c>EventGroup</c> thì GIỮ vì là cột DB thật; bản ghi legacy có thể null và đó là dữ liệu, không
/// phải thiếu sót của mapping.</para>
///
/// <para>Đặt ở <c>Modules.VDS.Core/Dto</c> vì có HAI feature dùng: chi tiết bản tin sự kiện (Event) và
/// chi tiết hành trình (Journey).</para>
/// Created date: 22/09/2026
/// </summary>
public class VdsLinkedIncidentOutput
{
    public string ID { get; set; } = "";
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? EventTypeId { get; set; }
    public string? EventGroup { get; set; }
    public DateTime? StartDate { get; set; }
    public string? Source { get; set; }
    public string? Location { get; set; }
    public string? ZoneId { get; set; }
    public int? KmNumber { get; set; }
    public int? MetNumber { get; set; }
    public string? InfluenceScope { get; set; }
    public int? InjuredNumber { get; set; }
    public int? VehicleNumber { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string? ValidatedBy { get; set; }
    public DateTime? ValidatedDate { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? FinishedBy { get; set; }
    public DateTime? FinishedDate { get; set; }
    public string? TrafficPlanId { get; set; }
    public string? EquipmentId { get; set; }
    public string? State { get; set; }
    public string? ProcessMode { get; set; }
    public string? Description { get; set; }
    public string? Metadata { get; set; }
    public string? Remark { get; set; }

    /// <summary>Khóa bản tin đã sinh ra sự cố — chính là chỗ nối sang VDS.</summary>
    public string? EventId { get; set; }

    /// <summary>Khóa SỰ VIỆC (nhiều bản tin của cùng một sự việc dùng chung giá trị này).</summary>
    public string? ConditionKey { get; set; }
}
