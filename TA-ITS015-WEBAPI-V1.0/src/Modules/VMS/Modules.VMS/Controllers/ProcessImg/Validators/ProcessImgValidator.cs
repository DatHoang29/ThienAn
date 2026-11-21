using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.VMS.Controllers.ProcessImg.Dto;
using Modules.VMS.Core.Entities;
using Modules.VMS.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.VMS.Controllers.ProcessImg.Validators
{
	public class VmsProcessImgValidator : AbstractValidator<VmsProcessImg>
	{
		public VmsProcessImgValidator(IStringLocalizer localizer, bool isCreate = true)
		{
			RuleFor(x => x.Code)
			  .NotEmpty()
				  .WithName(x => localizer[BaseMsg.BaseEntity.Code])
				  .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
			  .When(x => (x.Code != null && !isCreate) || isCreate)
			  .MaximumLength(EntityConst.Length64)
				  .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

			RuleFor(x => x.Url)
				.MaximumLength(EntityConst.Length256)
					.WithName(BaseMsg.VmsProcessImg.Entity.Url)
					.WithMessage(VmsProcessImgMsg.BaseValidation.MaxLength);

			// >=0
			RuleFor(x => x.TimeToLive)
				.GreaterThanOrEqualTo(0)
					.WithName(x => localizer[BaseMsg.VmsProcessImg.Entity.TimeToLive])
					.WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual]);
		}
	}

	/// <summary>
	/// Author: KhoaND
	/// Description: Lớp kiểm tra dữ liệu đầu vào trước khi insert AddVmsProcessImgInput
	/// Created date: 14/10/2024
	/// </summary>
	public class AddVmsProcessImgInputValidator : AbstractValidator<AddVmsProcessImgInput>
	{
		public AddVmsProcessImgInputValidator(IStringLocalizer localizer) 
		{
			Include(new VmsProcessImgValidator(localizer));
		}
	}

	/// <summary>
	/// Author: KhoaND
	/// Description: Lớp kiểm tra dữ liệu đầu vào trước khi update UpdateVmsProcessImgInput
	/// Created date: 14/10/2024
	/// </summary>
	public class UpdateVmsProcessImgInputValidator : AbstractValidator<UpdateVmsProcessImgInput>
	{
		public UpdateVmsProcessImgInputValidator(IStringLocalizer localizer) 
		{
			Include(new VmsProcessImgValidator(localizer, false));
		}
	}

	/// <summary>
	/// Author: KhoaND
	/// Description: Lớp kiểm tra dữ liệu đầu vào trước khi delete DeleteVmsProcessImgInput
	/// Created date: 14/10/2024
	/// </summary>
	public class DeleteVmsProcessImgInputValidator: AbstractValidator<DeleteVmsProcessImgInput> {
		public DeleteVmsProcessImgInputValidator()
		{
			RuleFor(data => data.ID)
				.NotEmpty()
				.WithName(BaseMsg.BaseEntity.ID)
				.WithMessage(BaseMsg.BaseValidation.Required);
		}
	}
}
