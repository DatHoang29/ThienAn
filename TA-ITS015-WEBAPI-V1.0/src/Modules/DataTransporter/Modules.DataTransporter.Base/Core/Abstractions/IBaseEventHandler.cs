using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modules.DataTransporter.Base.Core.Abstractions
{
    public delegate void DataReceivedHandler(string keyName, object data);

    public interface IBaseEventHandler
    {
        bool RegisterChannel(string keyName);
        bool PublishData(string keyName, object data);
        bool SubscribeData(string keyName);
        event DataReceivedHandler OnDataReceived;
    }
}
