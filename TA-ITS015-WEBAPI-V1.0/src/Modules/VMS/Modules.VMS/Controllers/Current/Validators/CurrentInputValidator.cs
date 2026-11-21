#region reference

// For: AbstractValidator
using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.VMS.Controllers.Current.Dto;
using Modules.VMS.Core.Entities;
using Modules.VMS.Core.Exceptions;
using Shared.DTO.Constants.Application;

// For: Entity

// For: Dto

// For: VMSMsgs

#endregion

namespace Modules.VMS.Controllers.Current.Validators
{
	/*
     VmsCurrent {
	    ID varchar(64) pk unique
	    Code string(64) null
	    Name string(64) null
	    Lang string(16) null def(vi-VN)
	    Type string(16) null def(icon)
	    Url string(256) null
	    Size string null
	    OrderNo integer null
	    Status integer null
    }
     */
	public class VmsCurrentValidator : AbstractValidator<VmsCurrent>
	{
		public VmsCurrentValidator(IStringLocalizer localizer, bool isCreate = true)
		{
			RuleFor(x => x.Name)
			   .NotEmpty()
			   .WithName(x => localizer[BaseMsg.BaseEntity.Name])
			   .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
			   .When(x => (x.Name != null && !isCreate) || isCreate)
			   .MaximumLength(EntityConst.Length64)
			   .WithName(x => localizer[BaseMsg.BaseEntity.Name])
			   .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);


			RuleFor(x => x.RowData)
			   .MaximumLength(EntityConst.Length1024)
			   .WithName(x => localizer[BaseMsg.VmsCurrent.Entity.RowData])
			   .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

			RuleFor(x => x.DefaultRowData)
			   .MaximumLength(EntityConst.Length1024)
			   .WithName(x => localizer[BaseMsg.VmsCurrent.Entity.DefaultRowData])
			   .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);


			RuleFor(x => x.Url)
			   .MaximumLength(EntityConst.Length512)
			   .WithName(x => localizer[BaseMsg.VmsCurrent.Entity.Url])
			   .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);


			RuleFor(x => x.DefaultUrl)
			   .MaximumLength(EntityConst.Length512)
			   .WithName(x => localizer[BaseMsg.VmsCurrent.Entity.DefaultUrl])
			   .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

			RuleFor(x => x.Size)
			   .MaximumLength(EntityConst.ConfigFieldLength)
			   .WithName(x => localizer[BaseMsg.VmsCurrent.Entity.Size])
			   .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

			RuleFor(x => x.Font)
			   .MaximumLength(EntityConst.ConfigFieldLength)
			   .WithName(x => localizer[BaseMsg.VmsCurrent.Entity.Font])
			   .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

			RuleFor(x => x.DefaultFont)
			   .MaximumLength(EntityConst.ConfigFieldLength)
			   .WithName(x => localizer[BaseMsg.VmsCurrent.Entity.DefaultFont])
			   .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

			RuleFor(x => x.FontSize)
			   .MaximumLength(EntityConst.ConfigFieldLength)
			   .WithName(x => localizer[BaseMsg.VmsCurrent.Entity.FontSize])
			   .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

			RuleFor(x => x.Color)
			   .MaximumLength(EntityConst.ConfigFieldLength)
			   .WithName(x => localizer[BaseMsg.VmsCurrent.Entity.Color])
			   .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

			RuleFor(x => x.DefaultColor)
			   .MaximumLength(EntityConst.ConfigFieldLength)
			   .WithName(x => localizer[BaseMsg.VmsCurrent.Entity.DefaultColor])
			   .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

			RuleFor(x => x.BackColor)
			   .MaximumLength(EntityConst.ConfigFieldLength)
			   .WithName(x => localizer[BaseMsg.VmsCurrent.Entity.BackColor])
			   .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

			RuleFor(x => x.DefaultBackColor)
			   .MaximumLength(EntityConst.ConfigFieldLength)
			   .WithName(x => localizer[BaseMsg.VmsCurrent.Entity.DefaultBackColor])
			   .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

			RuleFor(x => x.ExecutedBy)
			   .MaximumLength(EntityConst.Length64)
			   .WithName(x => localizer[BaseMsg.VmsCurrent.Entity.ExecutedBy])
			   .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

			// >=0
			RuleFor(x => x.Priority)
				.GreaterThanOrEqualTo(0)
					.WithName(x => localizer[BaseMsg.VmsCurrent.Entity.Priority])
					.WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual]);
		}
	}


	/// <summary>
	/// Author: KhoaND
	/// Description: Lớp kiểm tra dữ liệu đầu vào trước khi insert AddVmsCurrentInput
	/// Created date: 01/10/2024
	/// </summary>
	public class AddVmsCurrentInputValidator : AbstractValidator<AddVmsCurrentInput>
    {
        public AddVmsCurrentInputValidator(IStringLocalizer localizer) 
		{
			Include(new VmsCurrentValidator(localizer));
		}
    }

    /// <summary>
    /// Author: KhoaND
    /// Description: Lớp kiểm tra dữ liệu đầu vào trước khi update UpdateVmsCurrentInput
    /// Created date: 01/10/2024
    /// </summary>
    public class UpdateVmsCurrentInputValidator : AbstractValidator<UpdateVmsCurrentInput>
    {
        public UpdateVmsCurrentInputValidator(IStringLocalizer localizer) 
		{
			Include(new VmsCurrentValidator(localizer, false));
			RuleFor(data => data.ID)
                //.NotEmpty().WithName(VmsCurrentMsg.BaseEntity.ID).WithMessage(VmsCurrentMsg.BaseValidation.Required)
                .MaximumLength(EntityConst.KeyFieldLength).WithName(BaseMsg.BaseEntity.ID).WithMessage(BaseMsg.BaseValidation.MaxLength);           
        }
    }

    /// <summary>
    /// Author: KhoaND
    /// Description: Lớp kiểm tra dữ liệu đầu vào trước khi delete DeleteVmsCurrentInput
    /// Created date: 01/10/2024
    /// </summary>
    public class DeleteVmsCurrentInputValidator: AbstractValidator<DeleteVmsCurrentInput> {
        public DeleteVmsCurrentInputValidator(IStringLocalizer localizer)
        {
           RuleFor(x => x.ID)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.BaseEntity.ID])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);
        }
    }
}
