namespace Modules.FMS.Controllers.EquipmentHistoryChart.Dto
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 22/01/2025
    /// </summary>
    public class EquipmentStateInfo
    {
        public string Name { get; set; } = "";
        public string Color { get; set; } = "";

        public List<string> StateList { get; set; } = new List<string>();

        public bool HasState(string state)
        {
            if (this.StateList.Count > 0 && this.StateList.Contains(state))
                return true;

            return false;
        }
    }
}
