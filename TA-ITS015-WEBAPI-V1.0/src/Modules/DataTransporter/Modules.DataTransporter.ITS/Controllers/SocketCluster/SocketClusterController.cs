using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Modules.DataTransporter.ITS.Infrastructure.Services;

namespace Modules.DataTransporter.ITS.Controllers.SocketCluster
{
    public class SocketClusterController:BaseController
    {

        public SocketClusterController()
        {

        }

        public string GetTest()
        {
            ItsDataTransporter.BroadcastData(ItsDataTransporterChannel.Test,"test");
            return "ok";
        }
        public string GetCount()
        {
            ItsDataTransporter.BroadcastData(ItsDataTransporterChannel.CountVehicle,new Random().NextInt64(0,100000000));
            return "ok";
        }
    }
}
