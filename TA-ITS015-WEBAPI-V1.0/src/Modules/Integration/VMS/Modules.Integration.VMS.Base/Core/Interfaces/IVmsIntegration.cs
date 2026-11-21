using Modules.Integration.VMS.Base.Core.Dtos;

namespace Modules.Integration.VMS.Base.Core.Interfaces
{
    public interface IVmsIntegration
    {

        #region Events

        /// <summary>
        /// Event khi trạng thái kết nối thay đổi
        /// </summary>
        event EventHandler<object>? ConnectionStatusChanged;

        /// <summary>
        /// Event khi có sự kiện thực hiện
        /// </summary>
        event EventHandler<object>? EventTriggered;

        /// <summary>
        /// Event khi có lỗi xảy ra
        /// </summary>
        event EventHandler<object>? ErrorOccurred;


        //event EventHandler<object>? OperationCompleted;
        //event EventHandler<object>? OperationFailed;

        #endregion

        #region Functions

        /// <summary>
        /// Cấu hình/ khởi tạo thiết bị
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        IntgVmsConfigureOutput Configure(IntgVmsConfigureInput input);


        /// <summary>
        /// Kết nối
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        object Connect(object? input);

        /// <summary>
        /// Ngắt kết nối
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        object Disconnect(object? input);

        /// <summary>
        /// Gửi hình
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        IntgVmsSendImageOutput SendImage(IntgVmsSendImageInput input);


        /// <summary>
        /// Đọc hình
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        IntgVmsReadImageOutput ReadImage(IntgVmsReadImageInput input);

        /// <summary>
        /// Thiết lập độ sáng
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        IntgVmsSetBrightnessOutput SetBrightness(IntgVmsSetBrightnessInput input);

        /// <summary>
        /// Đọc độ sáng
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        IntgVmsGetBrightnessOutput GetBrightness(IntgVmsGetBrightnessInput input);


        /// <summary>
        /// Đọc thông số hoạt động
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        IntgVmsGetStatOutput GetStat(IntgVmsGetStatInput input);


        /// <summary>
        /// Reset thiết bị
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        object ResetDevice(object? input = null);


        //object GetConfig(object? configParams = null);

        //object SetConfig(object? configParams = null);

        

        object SetText(object? input = null);

        object GetText(object? input = null);
        #endregion
        



    }
}
