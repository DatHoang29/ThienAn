

using Modules.Samplev2.Core.Entities;
using Shared.Core.Utilities;

namespace Modules.Samplev2.Controllers.Category.Dto;

public class PageCategoryInput : BasePageInput
{

    public string Name { get; set; }


    public string Description { get; set; }
}

public class CategoryInput
{

    public string Name { get; set; }

    public string Description { get; set; }


}

public class AddCategoryInput : SampCategory
{
}

public class UpdateCategoryInput : AddCategoryInput
{

}

public class DeleteCategoryInput : BaseIdInput
{

}
