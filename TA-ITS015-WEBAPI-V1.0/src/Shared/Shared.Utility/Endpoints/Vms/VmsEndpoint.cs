namespace Shared.Utility.Endpoints.Vms
{
    /// <summary>
    /// Vms module endpoint class
    /// </summary>
    public class VmsEndpoint
    {
		/// <summary>
		/// TrafficStatistic endpoint
		/// </summary>
		public class VmsCurrent
		{
			public const string Base = "/api/vms/vmscurrent";
			public static string UpdateByProcess = Base + "/updatebyprocess";
			public static string GetByEquipemnt = Base + "/getbyequipment";
		}
	}
}
