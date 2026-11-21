using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.VMS.Controllers.Template.Dto;
using Modules.VMS.Core.Entities;
using Modules.VMS.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.VMS.Controllers.Template.Validators
{
	/*
	 
    VmsTemplate {
	    ID varchar(64) pk unique
	    Code string(64) null unique
	    Name string(64) null
	    Lang string(16) null def(vi-VN)
	    JsonData string(256) null
	    Size string(16) null
	    RowNo integer null
	    ImageNo integer null
	    OrderNo integer null
	    Preview string(256) null
	    Status integer null
	    Remark string(256)
    }
	 */

	/// <summary>
	/// Author: SonTH
	/// Description: Lớp kiểm tra dữ liệu đầu vào trước khi insert AddTemplateInput
	/// Created date: 01/10/2024
	/// </summary>
	public class TemplateValidator : AbstractValidator<VmsTemplate>
	{
		public TemplateValidator(IStringLocalizer<VmsTemplate> localizer, bool isCreate = true) {
			
            

            RuleFor(data => data.Size)
                .MaximumLength(EntityConst.ConfigFieldLength)
                    .WithName(BaseMsg.VmsTemplate.Entity.Size)
                    .WithMessage(BaseMsg.BaseValidation.MaxLength);
            
            
            RuleFor(x => x.RowNo)
                .GreaterThanOrEqualTo(0)
                    .WithName(x => localizer[BaseMsg.VmsTemplate.Entity.RowNo])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual]);

            RuleFor(x => x.ImageNo)
                .GreaterThanOrEqualTo(0)
                    .WithName(x => localizer[BaseMsg.VmsTemplate.Entity.ImageNo])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual]);

            RuleFor(x => x.OrderNo)
                .GreaterThanOrEqualTo(0)
                    .WithName(x => localizer[BaseMsg.BaseEntity.OrderNo])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual]);


            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.Length256)
                    .WithName(x => localizer[BaseMsg.BaseEntity.Remark])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);


            //When
            RuleFor(x => x.Code)
                .NotEmpty()
                    .WithName(x => localizer[BaseMsg.BaseEntity.Code])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .When(x => (x.Code != null && !isCreate) || isCreate)
                .MaximumLength(EntityConst.Length64)
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Name)
                .NotEmpty()
                    .WithName(x => localizer[BaseMsg.BaseEntity.Name])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .When(x => (x.Name != null && !isCreate) || isCreate)
                .MaximumLength(EntityConst.Length128)
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            
            RuleFor(x => x.Lang)
                .NotEmpty()
                    .WithName(x => localizer[BaseMsg.BaseEntity.Lang])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .When(x => (x.Lang != null && !isCreate) || isCreate)
                .MaximumLength(EntityConst.ConfigFieldLength)
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Status)
                .NotEmpty()
                    .WithName(x => localizer[BaseMsg.BaseEntity.Status])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .When(x => (x.Status != null && !isCreate) || isCreate)
                .IsInEnum()
                    .WithMessage(localizer[BaseMsg.BaseValidation.MustBeValidValue]);

		}
	}


	/// <summary>
	/// Author: SonTH
	/// Description: AddTemplateInput validator
	/// Created date: 01/10/2024
	/// </summary>
	public class AddTemplateInputValidator : AbstractValidator<AddTemplateInput>
	{
		public AddTemplateInputValidator(IStringLocalizer<AddTemplateInput> localizer) 
        {
			Include(new TemplateValidator(localizer));
		}
	}

	/// <summary>
	/// Author: SonTH
	/// Description: UpdateTemplateInput validator
	/// Created date: 01/10/2024
	/// </summary>
	public class UpdateTemplateInputValidator : AbstractValidator<UpdateTemplateInput>
	{
		public UpdateTemplateInputValidator(IStringLocalizer<UpdateTemplateInput> localizer) {
			Include(new TemplateValidator(localizer,false));
		}
	}

	/// <summary>
	/// Author: KhoaND SonTH
	/// Description: Lớp kiểm tra dữ liệu đầu vào trước khi delete DeleteTemplateInput
	/// Created date: 01/10/2024
	/// </summary>
	public class DeleteTemplateInputValidator: AbstractValidator<DeleteTemplateInput> {
		public DeleteTemplateInputValidator(IStringLocalizer localizer)
		{
			RuleFor(data => data.ID)
				.NotEmpty()
                    .WithName(x => localizer[BaseMsg.BaseEntity.ID])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);
		}
	}
}
