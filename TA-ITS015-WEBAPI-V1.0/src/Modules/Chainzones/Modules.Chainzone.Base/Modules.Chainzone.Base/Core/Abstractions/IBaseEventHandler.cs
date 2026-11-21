using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modules.Chainzone.Base.Core.Abstractions
{
    public interface IBaseEventHandler
    {
        /// <summary>
        /// Đăng ký channel/topic
        /// </summary>
        /// <param name="keyName"></param>
        /// <returns></returns>
        bool RegisterChannel(string keyName);
        /// <summary>
        /// Gửi dữ liệu lên channel/topic
        /// </summary>
        /// <param name="keyName"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        bool PublishData(string keyName, object data);

        /// <summary>
        /// Nhận dữ liệu từ channel/topic
        /// </summary>
        /// <param name="keyName"></param>
        /// <returns></returns>
        bool SubscribeData(string keyName);

    }
}
