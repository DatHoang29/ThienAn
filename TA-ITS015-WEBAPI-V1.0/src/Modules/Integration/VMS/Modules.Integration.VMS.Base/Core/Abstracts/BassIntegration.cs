using Modules.Integration.VMS.Base.Core.Dtos;
using Modules.Integration.VMS.Base.Core.Interfaces;

namespace Modules.Integration.VMS.Base.Core.Abstracts
{
    /// <summary>
    /// Abstract class cho việc tích hợp thiết bị
    /// Author: SonTH
    /// Create Date: 17/2/2025
    /// Lưu ý: Tất cả hàm, event ở đây đều là virtual, để override thực hiện lại ở class con
    /// Mục đích: Không cần implement tất cả các hàm, event khi kế thừa
    /// </summary>
    public abstract class BaseIntegration: IVmsIntegration
    {
        /// <summary>
        /// Event khi trạng thái kết nối thay đổi
        /// </summary>
        public virtual event EventHandler<object>? ConnectionStatusChanged;

        /// <summary>
        /// Event khi có sự kiện thực hiện
        /// </summary>
        public virtual event EventHandler<object>? EventTriggered;

        /// <summary>
        /// Event khi có lỗi xảy ra
        /// </summary>
        public virtual event EventHandler<object>? ErrorOccurred;


        
        public virtual IntgVmsConfigureOutput Configure(IntgVmsConfigureInput input)
        {
            throw new NotImplementedException();
        }

        public virtual object Connect(object? connectParams)
        {
            throw new NotImplementedException();
        }

        public virtual object Disconnect(object? disconnectParams)
        {
            return "NotImplement";
        }

        public virtual IntgVmsSendImageOutput SendImage(IntgVmsSendImageInput input)
        {
            throw new NotImplementedException();
        }

        public virtual IntgVmsReadImageOutput ReadImage(IntgVmsReadImageInput input)
        {
            throw new NotImplementedException();
        }

        public virtual IntgVmsSetBrightnessOutput SetBrightness(IntgVmsSetBrightnessInput input)
        {
            throw new NotImplementedException();
        }

        public virtual IntgVmsGetBrightnessOutput GetBrightness(IntgVmsGetBrightnessInput input)
        {
            throw new NotImplementedException();
        }

        public virtual IntgVmsGetStatOutput GetStat(IntgVmsGetStatInput input)
        {
            throw new NotImplementedException();
        }

       
        public object ResetDevice(object? input = null)
        {
            throw new NotImplementedException();
        }


        public virtual object SetText(object? textParams)
        {
            throw new NotImplementedException();
        }

        public virtual object GetText(object? textParams)
        {
            throw new NotImplementedException();
        }
    }
}
