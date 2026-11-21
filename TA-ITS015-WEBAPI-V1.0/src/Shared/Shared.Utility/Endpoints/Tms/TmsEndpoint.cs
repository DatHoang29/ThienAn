namespace Shared.Utility.Endpoints.Tms
{
    /// <summary>
    /// Tms module endpoint class
    /// </summary>
    public class TmsEndpoint
    {
        /// <summary>
        /// Zone endpoint
        /// </summary>
        public class Zone
        {
            public const string Base = "/api/tms/tmszone";
        }

        /// <summary>
        /// Incident endpoint
        /// </summary>
        public class Incident
        {
            public const string Base = "/api/tms/tmsincident";
        }

        /// <summary>
        /// WorkExecute endpoint
        /// </summary>
        public class WorkExecute
        {
            public const string Base = "/api/tms/tmsworkexecute";
        }

        /// <summary>
        /// WorkExecuteHistory endpoint
        /// </summary>
        public class WorkExecuteHistory
        {
            public const string Base = "/api/tms/tmsworkexecutehistory";
        }

        /// <summary>
        /// TrafficPlan endpoint
        /// </summary>
        public class TrafficPlan
        {
            public const string Base = "/api/tms/tmstrafficplan";
        }

        /// <summary>
        /// Equipment endpoint
        /// </summary>
        public class Equipment
        {
            public const string Base = "/api/tms/tmsequipment";
			public static string List = Base + "/list";
		}

        /// <summary>
        /// EquipmentType endpoint
        /// </summary>
        public class EquipmentType
        {
            public const string Base = "/api/tms/tmsequipmenttype";
        }

        /// <summary>
        /// TrafficData endpoint
        /// </summary>
        public class TrafficData
        {
            public const string Base = "/api/tms/tmstrafficdata";
        }

		/// <summary>
		/// SignalLog endpoint
		/// </summary>
		public class SignalLog
		{
			public const string Base = "/api/tms/tmssignallog";
            public static string Add = Base + "/add";
		}

        /// <summary>
        /// TrafficStatistic endpoint
        /// </summary>
        public class TrafficStatistic
        {
            public const string Base = "/api/tms/tmstrafficstatistic";
        }		
	}
}
