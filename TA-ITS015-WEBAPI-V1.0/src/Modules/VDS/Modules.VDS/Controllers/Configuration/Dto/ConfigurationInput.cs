namespace Modules.VDS.Controllers.Configuration.Dto;

/// <summary>
/// Author: KienLT
/// Description: Dữ liệu lưu một tham số vận hành VDS — chỉ cần mã và giá trị, không cần biết ID.
///
/// <para>Luật cấu trúc (bắt buộc, độ dài) nằm ở <c>ConfigurationSaveValidator</c> thay vì DataAnnotations
/// để câu thông báo được localize như phần còn lại của module; luật riêng theo từng mã nằm ở
/// <c>ConfigurationCommandHandler</c>.</para>
/// Created date: 08/09/2026
/// </summary>
public class VdsConfigValueInput
{
    public string Code { get; set; } = "";
    public string Value { get; set; } = "";
}

/// <summary>
/// Author: KienLT
/// Description: Yêu cầu phát lại bản tin refresh theo mã đã khai.
///
/// <para>DTO này KHÔNG lộ ra swagger: action <c>Refresh</c> giữ nguyên body là một số nguyên trần để
/// contract HTTP không đổi, controller tự dựng DTO rồi dispatch qua message bus.</para>
/// <para>Dải 0..6 là dải mã mà <c>ItsRefresh.Send</c> chấp nhận. Vì DTO không phải tham số action nên
/// FluentValidation không chạy cho nó — chỗ chặn thật là <c>[FromBody, Range(0, 6)]</c> trên tham số của
/// action <c>Refresh</c>, giữ ở đây thêm <c>[Range]</c> chỉ để tài liệu hóa dải.</para>
/// Created date: 21/09/2026
/// </summary>
public class ConfigurationRefreshInput
{
    [Range(0, 6)] public int Code { get; set; }
}


/// <summary>
/// Author: KienLT
/// Description: Message truy vấn danh sách tham số cấu hình VDS (nhóm <c>vds_config</c>).
///
/// <para>DTO này KHÔNG lộ ra swagger: action <c>GetList</c> không nhận tham số, controller tự dựng message
/// rồi dispatch qua message bus để query handler có kiểu định tuyến.</para>
/// Created date: 16/09/2026
/// </summary>
public class VdsConfigurationListInput { }
