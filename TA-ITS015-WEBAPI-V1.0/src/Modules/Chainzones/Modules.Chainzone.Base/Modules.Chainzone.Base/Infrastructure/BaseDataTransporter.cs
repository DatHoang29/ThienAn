using Modules.Chainzone.Base.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modules.Chainzone.Base.Infrastructure
{
    public class BaseChainzone
    {
        /// <summary>
        /// Description: 
        ///Author:
        ///Create Date:
        ///</summary>
        static public List<IBaseEventHandler> handlers = new List<IBaseEventHandler>();
        public static void RegisterHandler(IBaseEventHandler itsEventHandler)
        {
            handlers.Add(itsEventHandler);
        }
        /// <summary>
        /// Description: 
        ///Author:
        ///Create Date:
        ///</summary>
        public static void UnRegisterHandler(IBaseEventHandler itsEventHandler)
        {
            handlers.Remove(itsEventHandler);
        }
        /// <summary>
        /// Description: 
        ///Author:
        ///Create Date:
        ///</summary>
        public static void UnRegisterAllHandler()
        {
            handlers.Clear();
        }
       
    }
}
