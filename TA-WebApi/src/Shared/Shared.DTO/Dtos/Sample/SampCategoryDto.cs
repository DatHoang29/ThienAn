using Shared.DTO.Dtos.Base;

namespace Shared.DTO.Dtos.Sample
{
    public class SampCategoryDto: BaseEntityDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int? Status { get; set; }
    }
}
