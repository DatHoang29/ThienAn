using Modules.CCTV.Controllers.Template.Dto;

namespace Module.CCTV.Core.Helper
{
    public static class TemplateItemHelper
    {
        public static string? SerializeItems(List<TemplateSlot> items)
        {
            return items != null && items.Any()
                        ? JsonConvert.SerializeObject(items)
                        : null;
        }

        public static List<TemplateSlot> DeserializeItems(string? json)
        {
            return string.IsNullOrWhiteSpace(json)
                        ? new List<TemplateSlot>()
                        : JsonConvert.DeserializeObject<List<TemplateSlot>>(json) ?? new List<TemplateSlot>();
        }
    }
}
