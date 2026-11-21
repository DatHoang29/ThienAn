using Modules.TMS.Core.Entities;
using Modules.VDS.Core.Dto;

namespace Modules.VDS.Controllers.Segment.Dto;

/// <summary>
/// Author: KienLT
/// Description: Kết quả lưu đoạn A→B và yêu cầu refresh danh mục đoạn.
/// Created date: 08/09/2026
/// </summary>
public class SegmentSaveOutput : VdsSaveOutput
{
    public string ID { get; set; } = "";
}

/// <summary>
/// Author: KienLT
/// Description: Một đoạn tuyến A→B trả ra cho danh sách.
///
/// <para><b>Kế thừa entity theo đúng mẫu <c>{Feature}Output : {Entity}</c> của repo.</b> Bản cũ khai tường
/// minh từng field để khỏi trả <c>TenantId</c>/<c>IsDelete</c>/<c>RowStatus</c>/audit, và đổi cột
/// <c>Status</c> thành <c>Enabled</c> (bool). Cả hai đều là lệch chuẩn tự đặt ra: mọi feature CRUD khác trong
/// repo trả đủ cột và giữ đúng tên cột.</para>
/// <para>Hệ quả cho client: trường <c>enabled</c> KHÔNG còn, dùng <c>status</c> ("1" bật, "0" tắt).</para>
/// Created date: 22/09/2026
/// </summary>
public class SegmentOutput : TmsTrafficSegment { }

/// <summary>
/// Author: KienLT
/// Description: Một dòng kết quả tự sinh/đồng bộ đoạn.
/// Created date: 29/09/2026
/// </summary>
public class SegmentGenerateItemOutput
{
    /// <summary>insert / update / unchanged / disable / locked.</summary>
    public string Action { get; set; } = "";

    /// <summary>ID bản ghi đang có (rỗng khi thêm mới và chưa ghi).</summary>
    public string? SegmentId { get; set; }

    public string? Code { get; set; }
    public string? Name { get; set; }

    /// <summary>S = cùng tuyến, T = rẽ qua nút; rỗng với dòng không còn trong kết quả sinh.</summary>
    public string? Kind { get; set; }
    public string? Turn { get; set; }
    public string? FromCode { get; set; }
    public string? ToCode { get; set; }
    public string? Direction { get; set; }
    public int? DistanceMeter { get; set; }
    public int? OldDistanceMeter { get; set; }
    public int? MinTravelSeconds { get; set; }
    public int? MaxTravelSeconds { get; set; }
    public string? Status { get; set; }

    /// <summary>Diễn giải phép tính khoảng cách.</summary>
    public string? Formula { get; set; }
}

/// <summary>
/// Author: KienLT
/// Description: Kết quả xem trước/đồng bộ đoạn tự sinh, kèm cảnh báo cấu hình và kết quả refresh.
/// Created date: 29/09/2026
/// </summary>
public class SegmentGenerateOutput : VdsSaveOutput
{
    public List<SegmentGenerateItemOutput> Items { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    /// <summary>Lỗi cấu hình tuyến. Khác rỗng thì GenerateSync KHÔNG ghi gì (Saved = false).</summary>
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Author: KienLT
/// Description: Sơ đồ tuyến cho trang tốc độ trung bình (<c>GET api/vds/vdsSegment/Topology</c>): tuyến, nút giao, camera và
/// đường đi của từng đoạn. Hình học đọc bằng ĐÚNG luật của bộ sinh đoạn nên sơ đồ và đoạn tự sinh hiểu cấu hình như nhau.
///
/// <para><b>Hợp đồng với FE</b> (PLAN-20261020-SO-DO-DUONG-DI-TOC-DO-TB mục 8.2): tên lớp và tên trường không được đổi.
/// Mọi vị trí tính bằng MÉT (<c>Km × 1000 + Met</c>).</para>
/// <para>Chỉ trả camera đủ tuyến/km và nút giao hợp lệ; dữ liệu không vẽ được được ghi vào <c>Warnings</c>.
/// <c>Segments</c> gồm mọi đoạn DB chưa xóa (kể cả tắt), theo thứ tự hiển thị, rồi đến đoạn worker tự sinh chưa có
/// trong DB: <c>Id = Code</c> là mã tất định, <c>Status</c> là null.</para>
/// Created date: 20/10/2026
/// </summary>
public class SegmentTopologyOutput
{
    public List<SegmentTopologyRouteOutput> Routes { get; set; } = [];

    public List<SegmentTopologyJunctionOutput> Junctions { get; set; } = [];

    public List<SegmentTopologyCameraOutput> Cameras { get; set; } = [];

    public List<SegmentTopologySegmentOutput> Segments { get; set; } = [];

    public List<string> Warnings { get; set; } = [];
}

/// <summary>
/// Author: KienLT
/// Description: Một tuyến trên sơ đồ: mã là Location của thiết bị, tên lấy từ danh mục tms_zone_location, thiếu thì dùng mã.
/// Trục h ứng với tuyến a của nút giao, v ứng với tuyến b; tuyến không thuộc nút nào dùng h và có cảnh báo.
/// MinPos/MaxPos là biên vị trí camera và điểm neo nút giao, tính bằng mét.
/// Created date: 20/10/2026
/// </summary>
public class SegmentTopologyRouteOutput
{
    public string Code { get; set; } = "";

    public string Name { get; set; } = "";

    public string Axis { get; set; } = "";

    public int MinPos { get; set; }

    public int MaxPos { get; set; }
}

/// <summary>
/// Author: KienLT
/// Description: Một nút giao hai tuyến (dòng <c>vds_junction_{Code}</c>); Code đã bỏ tiền tố.
/// RouteA/RouteB là mã hai tuyến; PosA/PosB là vị trí neo tương ứng, tính bằng mét.
/// Created date: 20/10/2026
/// </summary>
public class SegmentTopologyJunctionOutput
{
    public string Code { get; set; } = "";

    public string RouteA { get; set; } = "";

    public int PosA { get; set; }

    public string RouteB { get; set; } = "";

    public int PosB { get; set; }
}

/// <summary>
/// Author: KienLT
/// Description: Một camera VDS trên sơ đồ: Id là khóa thiết bị; Code thiếu thì dùng tên, rồi ID; Name thiếu thì dùng Code.
/// Route là Location, Pos là Km × 1000 + Met, Direction là DirectionId của thiết bị.
/// Created date: 20/10/2026
/// </summary>
public class SegmentTopologyCameraOutput
{
    public string Id { get; set; } = "";

    public string Code { get; set; } = "";

    public string Name { get; set; } = "";

    public string Route { get; set; } = "";

    public int Pos { get; set; }

    public int? Direction { get; set; }
}

/// <summary>
/// Author: KienLT
/// Description: Một đoạn A→B kèm đường đi trên sơ đồ. Id là khóa đoạn DB hoặc mã worker tự sinh, khớp SegmentId của hành trình.
/// FromEquipmentId/ToEquipmentId là khóa camera đầu/cuối; DistanceMeter là chiều dài tính tốc độ trung bình.
/// Status: 1 đang dùng, 0 đã tắt, null là đoạn tự sinh chưa có trong DB.
/// Kind/Path: S cùng tuyến [A, B]; T qua nút giao [A, neo tuyến A, neo tuyến B, B] kèm JunctionCode;
/// X không dựng được, Path rỗng.
/// Created date: 20/10/2026
/// </summary>
public class SegmentTopologySegmentOutput
{
    public string Id { get; set; } = "";

    public string? Code { get; set; }
    public string? Name { get; set; }

    public string? FromEquipmentId { get; set; }

    public string? ToEquipmentId { get; set; }

    public string? Direction { get; set; }

    public int? DistanceMeter { get; set; }

    public string? Status { get; set; }

    public string Kind { get; set; } = "";

    public string? JunctionCode { get; set; }

    public List<SegmentTopologyPointOutput> Path { get; set; } = [];
}

/// <summary>
/// Author: KienLT
/// Description: Một điểm của đường đi: vị trí (mét) trên một tuyến.
/// Created date: 20/10/2026
/// </summary>
public class SegmentTopologyPointOutput
{
    public string Route { get; set; } = "";

    public int Pos { get; set; }
}
