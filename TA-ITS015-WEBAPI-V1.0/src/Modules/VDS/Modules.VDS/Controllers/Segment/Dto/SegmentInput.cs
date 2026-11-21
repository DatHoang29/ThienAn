using Modules.TMS.Core.Entities;
using Shared.Core.Utilities;

namespace Modules.VDS.Controllers.Segment.Dto;

/// <summary>
/// Author: KienLT
/// Description: Dữ liệu thêm một đoạn tuyến VDS.
///
/// <para><b>Tách khỏi <c>Update</c> từ 22/09/2026.</b> Trước đó chỉ có một <c>SegmentSaveInput</c> gộp cả
/// thêm và sửa, phân biệt bằng <c>ID</c> rỗng hay không. Hệ quả là không tầng nào nói được "ID bắt buộc" —
/// validator phải nhận ID rỗng để còn thêm mới được, nên nhánh sửa mất lớp chặn và chỉ báo lỗi khi handler
/// truy DB không thấy bản ghi.</para>
///
/// <para><b>Kế thừa thẳng entity từ 23/09/2026</b> theo đúng mẫu <c>Add{Feature}Input : {Entity}</c> của repo.
/// Bản trước khai tường minh từng field như một danh sách trắng; kiểm bằng reflection thì
/// <c>EntityBaseId.ID</c> chỉ có <c>[SugarColumn]</c> chứ không có <c>[Required]</c>, nên kế thừa entity
/// không hề chặn thêm mới — lý do duy nhất để không kế thừa đã không còn.</para>
///
/// <para><b>Đổi wire format:</b> bản cũ phơi <c>Enabled</c> (bool) thay cho cột <c>Status</c> (chuỗi
/// "1"/"0"). Nay dùng đúng <c>Status</c> của entity như mọi feature khác. Client phải gen lại; rà ngày
/// 22/09/2026 thì FE chưa gọi API này nên không có chỗ nào vỡ.</para>
///
/// <para><b>Cột hệ thống vẫn không do client quyết:</b> <c>ID</c>/<c>TenantId</c>/<c>IsDelete</c> và audit bị
/// <c>VdsSystemColumns</c> đặt lại trong handler, không phải bị cắt khỏi DTO.</para>
/// Created date: 22/09/2026
/// </summary>
public class AddSegmentInput : TmsTrafficSegment { }

/// <summary>
/// Author: KienLT
/// Description: Dữ liệu cập nhật một đoạn tuyến VDS.
///
/// <para>Kế thừa <see cref="AddSegmentInput"/> theo mẫu <c>Update{Feature}Input : Add{Feature}Input</c>: tập
/// field y hệt, kiểu riêng chỉ để message bus định tuyến đúng nhánh sửa.</para>
/// <para><c>ID</c> BẮT BUỘC, và luật đó nằm ở <c>SegmentValidator</c> với <c>isCreate: false</c>
/// (FluentValidation) chứ không ở attribute kế thừa, để thông báo đi qua localizer.</para>
/// <para>Nhánh sửa GHI ĐÈ toàn bộ field nghiệp vụ (không phải patch từng field), nên body phải gửi đủ như
/// khi thêm.</para>
/// Created date: 22/09/2026
/// </summary>
public class UpdateSegmentInput : AddSegmentInput { }

/// <summary>
/// Author: KienLT
/// Description: Message truy vấn danh sách đoạn tuyến VDS.
///
/// <para>DTO này KHÔNG lộ ra swagger: action <c>GetList</c> không nhận tham số, controller tự dựng message
/// rồi dispatch qua message bus để query handler có kiểu định tuyến.</para>
/// Created date: 16/09/2026
/// </summary>
public class SegmentListInput { }

/// <summary>
/// Author: KienLT
/// Description: Xóa đoạn tuyến VDS theo ID.
///
/// <para>Kế thừa <see cref="BaseIdInput"/> theo quy ước <c>Delete{Feature}Input</c> — ở đây ID BẮT BUỘC nên
/// <c>[Required]</c> của base là đúng chỗ.</para>
/// <para><b>Từ 22/09/2026 đây là THAM SỐ ACTION</b>, không còn là DTO nội bộ dựng từ một chuỗi trần. Nhờ vậy
/// FluentValidation chạy được cho nó và lỗi "thiếu ID" được báo ngay tại cửa nhập thay vì để handler truy DB
/// rồi trả "không tìm thấy đoạn tuyến" — hai nguyên nhân khác nhau không nên gộp một thông báo.</para>
/// Created date: 16/09/2026
/// </summary>
public class DeleteSegmentInput : BaseIdInput { }

/// <summary>
/// Author: KienLT
/// Description: Message xem trước kết quả tự sinh đoạn A→B (không ghi DB).
/// Created date: 29/09/2026
/// </summary>
public class SegmentPreviewInput { }

/// <summary>
/// Author: KienLT
/// Description: Message đồng bộ kết quả tự sinh đoạn A→B vào <c>TmsTrafficSegment</c>.
/// Created date: 29/09/2026
/// </summary>
public class SegmentSyncInput { }

/// <summary>
/// Author: KienLT
/// Description: Message truy vấn sơ đồ tuyến (tuyến, nút giao, camera, đoạn kèm đường đi) cho sơ đồ hành trình trên trang
/// tốc độ trung bình.
///
/// <para>DTO này KHÔNG lộ ra swagger: action <c>Topology</c> không nhận tham số, controller tự dựng message rồi dispatch
/// qua message bus để query handler có kiểu định tuyến (cùng cách <see cref="SegmentListInput"/>).</para>
/// Created date: 20/10/2026
/// </summary>
public class SegmentTopologyInput { }
