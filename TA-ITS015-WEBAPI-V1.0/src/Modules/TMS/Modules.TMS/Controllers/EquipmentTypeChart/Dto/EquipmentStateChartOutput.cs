using Shared.Utility.Helpers;

namespace Modules.TMS.Controllers.EquipmentTypeChart.Dto
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 09/08/2024
    /// </summary>
    public class EquipmentStateChartOutput : BaseHelper.EChartHp.EChartResponse
    {
        public object[] SeriesData { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: Thông tin trạng thái thiết bị (tên, màu, danh sách mã trạng thái) dùng cho biểu đồ trạng thái thiết bị.
    /// Created date: 09/08/2024
    /// </summary>
    public class EquipmentStateInfo
    {
        public string Name { get; set; } = "";
        public string Color { get; set; } = "";

        public List<string> StateList { get; set; } = new List<string>();

        public bool HasState(string state)
        {
            if (this.StateList != null && this.StateList.Count > 0 && this.StateList.Contains(state))
                return true;

            return false;
        }
    }
}