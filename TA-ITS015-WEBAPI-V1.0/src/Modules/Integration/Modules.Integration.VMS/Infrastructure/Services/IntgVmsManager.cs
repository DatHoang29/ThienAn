using Modules.Integration.VMS.Base.Core.Constants;
using Modules.Integration.VMS.Base.Core.Dtos;
using Modules.Integration.VMS.Base.Core.Enums;
using Modules.Integration.VMS.Base.Core.Exceptions;
using Modules.Integration.VMS.Base.Core.Helpers;
using Modules.Integration.VMS.Infrastructure.Helpers;

namespace Modules.Integration.VMS.Infrastructure.Services
{
    /// <summary>
    /// Class quản lý chính phần tích hợp thiết bị - VMS
    /// Author: SonTH
    /// Create Date: 17/2/2025
    ///
    /// CẬP NHẬT 22/09/2026 (ChuongLP):
    ///   - Thêm constructor nhận thư mục chứa DLL driver để dùng chung cho WebAPI và dịch vụ ITS.VMS.Worker.
    ///   - InitConfig trả IntgVmsConfigureOutput thay vì bool, để nơi gọi biết LỖI Ở ĐÂU
    ///     (ping không tới / khởi tạo SDK lỗi / khởi tạo API lỗi) — đồng bộ chuẩn IntgVmsBaseOutput của project.
    ///   - Thay BaseHelper.NetworkHp.CheckConnect (Shared.Utility -> kéo Furion) bằng IntgVmsNetHelper ở tầng Base.
    /// </summary>
    public class IntgVmsManager : BaseManager
    {
        #region Events

        public event EventHandler<object> ConnectChanged;

        public event EventHandler<object> ErrorOccurred;

        #endregion

        #region Constructor

        /// <summary>
        /// Khởi tạo manager.
        /// </summary>
        /// <param name="dllDirectory">
        /// Thư mục chứa DLL driver theo hãng.
        /// WebAPI truyền App.WebHostEnvironment.WebRootPath; dịch vụ truyền AppContext.BaseDirectory.
        /// Bỏ trống thì mặc định là thư mục chạy ứng dụng.
        /// </param>
        public IntgVmsManager(string? dllDirectory = null) : base(dllDirectory)
        {
        }

        #endregion

        /// <summary>
        /// Khởi tạo cấu hình kết nối thiết bị
        /// Author: SonTH
        /// Create Date: 17/2/2025
        /// </summary>
        /// <input name="input"></input>
        /// <returns>
        /// Succeeded = true khi thiết bị sẵn sàng nhận lệnh.
        /// Message mô tả rõ bước nào thất bại để nơi gọi ghi log / trả về cho người dùng.
        /// </returns>
        public IntgVmsConfigureOutput InitConfig(IntgVmsConfigureInput input)
        {
            CheckIntegrationInstance();

            var output = new IntgVmsConfigureOutput { Vendor = input.Vendor };

            // Ping trước: sai IP hoặc mất mạng thì báo lỗi rõ ràng, không để SDK treo tới hết timeout.
            var port = input.Port ?? 0;
            if (!IntgVmsNetHelper.CheckConnect(input.Ip, port))
            {
                output.Succeeded = false;
                output.Message = $"{BaseMsg.IntgVms.Exception.CannotConnect}: {input.Ip}:{port}";
                return output;
            }

            switch (input.Vendor)
            {
                case IntgVmsVendor.Chainzone:

                    // Chainzone cần đăng ký CẢ 2 kênh: SDK (gửi/đọc ảnh) và API (độ sáng, thông số).
                    input.Metadata["ConnectionType"] = IntgVmsEnums.IntgVmsIntegrationType.Sdk;
                    var sdk = _integration!.Configure(input);

                    input.Metadata["ConnectionType"] = IntgVmsEnums.IntgVmsIntegrationType.Api;
                    var api = _integration!.Configure(input);

                    // Giữ nguyên hành vi cũ: kết quả của kênh API quyết định thành công/thất bại.
                    output.Succeeded = api.Succeeded;
                    output.Message = api.Succeeded
                        ? $"Init OK (Sdk: {(sdk.Succeeded ? "OK" : "lỗi")}, Api: OK)"
                        : $"Init lỗi (Sdk: {(sdk.Succeeded ? "OK" : "lỗi")}, Api: lỗi). {api.Message}";
                    return output;

                default:

                    input.Metadata["Type"] = IntgVmsEnums.IntgVmsIntegrationType.Sdk;
                    var rs = _integration!.Configure(input);

                    output.Succeeded = rs.Succeeded;
                    output.Message = rs.Succeeded ? "Init OK (Sdk)" : $"Init lỗi (Sdk). {rs.Message}";
                    return output;
            }
        }

        /// <summary>
        /// Ghi hình ảnh lên thiết bị
        /// Author: SonTH
        /// Create Date: 17/2/2025
        /// </summary>
        /// <input name="input">Url: Nội dung gửi lên là Url hình ảnh</input>
        /// <returns></returns>
        public object? SendImage(IntgVmsSendImageInput input)
        {
            CheckIntegrationInstance();

            switch (input.Vendor)
            {
                case IntgVmsVendor.Chainzone:

                    //var config = IntgVmsinput.GenerateSendImageinput(imageinput) as IntgChainzoneSendImageInput;
                    input.Metadata["ConnectionType"] = IntgVmsEnums.IntgVmsIntegrationType.Sdk;
                    var rs = _integration!.SendImage(input);
                    input.Metadata["ConnectionType"] = IntgVmsEnums.IntgVmsIntegrationType.Api;
                    var rs2 = _integration!.SendImage(input);
                    return rs.Succeeded && rs2.Succeeded;

                default:
                    input.Metadata["Type"] = IntgVmsEnums.IntgVmsIntegrationType.Sdk;
                    return _integration!.SendImage(input).Succeeded;
            }
        }

        /// <summary>
        /// Đoc hình ảnh từ thiết bị
        /// Author: SonTH
        /// Create Date: 17/2/2025
        /// </summary>
        /// <input name="input"></input>
        /// <returns>Base64: Dữ liệu trả về là Base64</returns>
        public object? ReadImage(IntgVmsReadImageInput input)
        {
            CheckIntegrationInstance();
            switch (input.Vendor)
            {
                default:
                    input.Metadata["ConnectionType"] = IntgVmsEnums.IntgVmsIntegrationType.Api;
                    // Dysten đọc khóa "Type" thay vì "ConnectionType" -> set cả 2 để dùng chung 1 nhánh.
                    input.Metadata["Type"] = IntgVmsEnums.IntgVmsIntegrationType.Api;
                    return _integration!.ReadImage(input);
            }
        }

        /// <summary>
        /// Đọc các thông số hoạt động
        /// Các thông số hoạt động sử dụng qua IntgVmsStatType
        /// Author: SonTH
        /// Create Date: 17/2/2025
        /// </summary>
        /// <input name="input"></input>
        /// <returns></returns>
        public object? GetStats(IntgVmsGetStatInput input)
        {
            CheckIntegrationInstance();
            switch (input.Vendor)
            {
                case IntgVmsVendor.Chainzone:

                    // LƯU Ý: với Chainzone, Metadata["Type"] mang MẢNG mã thông số của hãng,
                    // KHÔNG phải Sdk/Api như các lệnh khác.
                    var type = input.Type.Select(l => IntgVmsStatHelper.Chainzone.ToChainzoneStatType(l)).ToArray();
                    input.Metadata["Type"] = type;
                    var output = _integration!.GetStat(input);
                    output.Value = output.Value.ToDictionary(l => IntgVmsStatHelper.Chainzone.ToVmsStatType(int.Parse(l.Key)), l => l.Value);

                    return output;

                default:
                    var output2 = _integration!.GetStat(input);
                    return output2;
            }
        }

        /// <summary>
        /// Đọc độ sáng
        /// Author: SonTH
        /// Create Date: 17/2/2025
        /// </summary>
        /// <input name="input"></input>
        /// <returns>Độ sáng</returns>
        public object? GetBrightness(IntgVmsGetBrightnessInput input)
        {
            CheckIntegrationInstance();
            return _integration!.GetBrightness(input);
        }

        /// <summary>
        /// Thiết lập độ sáng
        /// Author: SonTH
        /// Create Date: 17/2/2025
        /// </summary>
        /// <input name="input">Brightness: độ sáng thiết lập</input>
        /// <returns></returns>
        public object? SetBrightness(IntgVmsSetBrightnessInput input)
        {
            CheckIntegrationInstance();
            return _integration!.SetBrightness(input);
        }
    }
}
