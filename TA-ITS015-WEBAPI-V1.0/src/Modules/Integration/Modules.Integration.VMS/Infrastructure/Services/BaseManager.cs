using System.Diagnostics;
using System.Reflection;
using Modules.Integration.VMS.Base.Core.Constants;
using Modules.Integration.VMS.Base.Core.Exceptions;
using Modules.Integration.VMS.Base.Core.Interfaces;

namespace Modules.Integration.VMS.Infrastructure.Services
{
    /// <summary>
    /// Abstract class các hàm xử lý chung cho tích hợp thiết bị - VMS
    /// Author: SonTH
    /// Create Date: 17/2/2025
    ///
    /// CẬP NHẬT 22/09/2026 (ChuongLP) — BỎ PHỤ THUỘC FURION để tầng Integration dùng chung được
    /// cho CẢ WebAPI (có Furion) và dịch vụ ITS.VMS.Worker (không có Furion):
    ///   1. Bỏ IStringLocalizer _localizer — biến này trước đây gán rồi không đọc ở đâu (code chết),
    ///      nhưng App.GetService() nằm trong constructor nên làm nổ ngay khi new ở môi trường không có Furion.
    ///   2. Thư mục chứa DLL driver nhận qua constructor thay vì đọc App.WebHostEnvironment.WebRootPath.
    ///      WebAPI truyền WebRootPath, dịch vụ truyền AppContext.BaseDirectory.
    ///   3. Oops.Oh(...) -> throw new Exception(...) — giữ nguyên khóa thông báo, đồng bộ với cách
    ///      ChainzoneIntegration/DystenIntegration đang làm sẵn.
    ///   4. Bỏ Autofac: trước đây chỉ dùng để đăng ký rồi resolve MỘT type, Activator.CreateInstance
    ///      làm được việc tương đương mà không cần thư viện IoC.
    /// </summary>
    public abstract class BaseManager
    {
        #region Events

        public event EventHandler<object> ConnectChanged;

        public event EventHandler<object> ErrorOccurred;

        public event EventHandler<object> Triggered;

        #endregion

        #region Khai báo

        internal IVmsIntegration? _integration;

        /// <summary>
        /// Thư mục chứa DLL driver theo hãng. WebAPI truyền WebRootPath, dịch vụ truyền thư mục chạy exe.
        /// </summary>
        private readonly string _dllDirectory;

        /// <summary>
        /// Danh sách các thiết bị VMS hỗ trợ
        /// Khai báo thêm ở đây khi có thiết bị mới
        /// </summary>
        private readonly Dictionary<string, string> _deviceVendors = new()
        {
            { IntgVmsVendor.Chainzone, "Modules.Integration.VMS.Chainzone.dll" },
            { IntgVmsVendor.Dysten, "Modules.Integration.Vms.Dysten.dll" }
        };

        #endregion

        #region Constructor

        /// <summary>
        /// Khởi tạo manager.
        /// </summary>
        /// <param name="dllDirectory">
        /// Thư mục chứa DLL driver theo hãng. Bỏ trống thì dùng thư mục chạy ứng dụng (AppContext.BaseDirectory).
        /// </param>
        protected BaseManager(string? dllDirectory = null)
        {
            _dllDirectory = string.IsNullOrWhiteSpace(dllDirectory) ? AppContext.BaseDirectory : dllDirectory.Trim();
        }

        #endregion

        /// <summary>
        /// Đăng ký module thiết bị hỗ trợ
        /// Author: SonTH
        /// Create Date: 17/2/2025
        /// </summary>
        /// <param name="vendor">Nhà cung cấp thiết bị</param>
        /// <returns></returns>
        public bool RegisterModule(string vendor)
        {
            if (_integration != null)
            {
                throw new Exception(BaseMsg.IntgVms.Exception.VendorDllAlreadyRegistered);
            }

            // Chặn vendor rỗng trước khi tra Dictionary để tránh lỗi khó hiểu
            // "Value cannot be null. (Parameter 'key')".
            if (string.IsNullOrWhiteSpace(vendor) || !_deviceVendors.ContainsKey(vendor))
            {
                throw new Exception(BaseMsg.IntgVms.Exception.VendorNotSupported);
            }

            string dllPath = Path.Combine(_dllDirectory, _deviceVendors[vendor]);
            if (!File.Exists(dllPath))
            {
                throw new Exception($"{BaseMsg.IntgVms.Exception.VendorDllNotFound}: {dllPath}");
            }

            try
            {
                Assembly assembly = Assembly.LoadFrom(dllPath);

                // Find a concrete class that implements IVmsIntegration
                Type? connectionType = assembly.GetTypes()
                    .FirstOrDefault(t => typeof(IVmsIntegration).IsAssignableFrom(t)
                                       && !t.IsAbstract && !t.IsInterface);

                if (connectionType == null)
                {
                    Debug.WriteLine(BaseMsg.IntgVms.Exception.VendorDllNoConnectionType);
                    return false;
                }

                // Trước đây dùng Autofac để đăng ký + resolve 1 type. Activator làm được việc tương đương
                // mà không cần IoC container, nhờ đó bỏ được tham chiếu Shared.Reference.
                _integration = Activator.CreateInstance(connectionType) as IVmsIntegration;
                if (_integration == null)
                {
                    Debug.WriteLine(BaseMsg.IntgVms.Exception.VendorDllNoConnectionType);
                    return false;
                }

                Debug.WriteLine(string.Format(BaseMsg.IntgVms.Message.VendorDllRegistered));
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"{BaseMsg.IntgVms.Exception.VendorDllRegisteredError}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Hủy đăng ký module
        /// Author: SonTH
        /// Create Date: 17/2/2025
        /// </summary>
        public void UnRegisterModule()
        {
            // Dispose the connection
            try
            {
                _integration?.Disconnect(new { });
            }
            catch
            {
                // Driver đã mất kết nối thì bỏ qua, không để lỗi dọn dẹp che lỗi thật.
            }

            _integration = null;
        }

        /// <summary>
        /// Kiểm tra instace của module có tồn tại không
        /// Author: SonTH
        /// Create Date: 17/2/2025
        /// </summary>
        public void CheckIntegrationInstance()
        {
            if (_integration == null)
            {
                throw new Exception(BaseMsg.IntgVms.Exception.VendorDllNotRegistered);
            }
        }
    }
}
