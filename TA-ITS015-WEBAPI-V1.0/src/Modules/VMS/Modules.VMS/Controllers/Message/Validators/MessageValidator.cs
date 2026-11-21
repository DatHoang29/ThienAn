using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.VMS.Controllers.Message.Dto;
using Modules.VMS.Core.Entities;
using Modules.VMS.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.VMS.Controllers.Message.Validators
{
    //VmsMessage {
    // ID varchar(64) pk unique
    // Code string(64) null
    // Name string(64) null
    // Lang string(16) null
    // Type string(16) null
    // Content1 string(256) null
    // Content2 string(256) null
    // Content3 string(256) null
    // OrderNo integer null
    // Status integer null
    //}

    /// <summary>
    /// Author: ThangNN
    /// Description: Kiểm tra dữ liệu insert-update VmsMessage
    /// Created date: 01/12/2024
    /// </summary>
    public class MessageValidator : AbstractValidator<VmsMessage>
	{
		public MessageValidator(IStringLocalizer lz, bool isCreate = true) {
			RuleFor(x => x.ID)
				.NotEmpty()
					.WithName(x => lz[BaseMsg.BaseEntity.ID])
					.WithMessage(x => lz[BaseMsg.BaseValidation.Required])
					.When(x => !isCreate)
				.MaximumLength(EntityConst.KeyFieldLength)
					.WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

			RuleFor(x => x.Name)
				.NotEmpty()
					.WithName(x => lz[BaseMsg.BaseEntity.Name])
					.WithMessage(x => lz[BaseMsg.BaseValidation.Required])
					.When(x => isCreate || (!isCreate && x.Name != null))
				.MaximumLength(EntityConst.Length64)
					.WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

			RuleFor(x => x.Code)
				.NotEmpty()
					.WithName(x => lz[BaseMsg.BaseEntity.Code])
					.WithMessage(x => lz[BaseMsg.BaseValidation.Required])
					.When(x => isCreate || (!isCreate && x.Code != null))
				.MaximumLength(EntityConst.Length64)
					.WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

			RuleFor(x => x.Lang)
				.NotEmpty()
					.WithName(x => lz[BaseMsg.BaseEntity.Lang])
					.WithMessage(x => lz[BaseMsg.BaseValidation.Required])
					.When(x => isCreate || (!isCreate && x.Lang != null))
				.MaximumLength(EntityConst.ConfigFieldLength)
					.WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

			RuleFor(x => x.Type)
				.NotEmpty()
					.WithName(x => lz[BaseMsg.BaseEntity.Type])
					.WithMessage(x => lz[BaseMsg.BaseValidation.Required])
					.When(x => isCreate || (!isCreate && x.Type != null))
				.MaximumLength(EntityConst.ConfigFieldLength)
					.WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);
            // >=0
            RuleFor(x => x.OrderNo)
                .GreaterThanOrEqualTo(0)
                    .WithName(x => lz[BaseMsg.BaseEntity.OrderNo])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.GreaterThanOrEqual])
                    .When(x => isCreate || (!isCreate && x.OrderNo != null));

            RuleFor(x => x.Status)
                .NotEmpty()
                    .WithName(x => lz[BaseMsg.BaseEntity.Status])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.Required])
                .IsInEnum()
                    .WithMessage(lz[BaseMsg.BaseValidation.MustBeValidValue])
                .When(x => isCreate || (!isCreate && x.Status != null));
        }
	}

    /// <summary>
    /// Author: ThangNN
    /// Created date: 01/12/2024
    /// Description: Kiểm tra dữ liệu insert
    /// </summary>
    public class AddMessageValidator : AbstractValidator<AddVmsMessageInput>
	{
		public AddMessageValidator(IStringLocalizer localizer)
		{
			Include(new MessageValidator(localizer));
		}
	}

    /// <summary>
    /// Author: ThangNN
    /// Created date: 01/12/2024
    /// Description: Kiểm tra dữ liệu update
    /// </summary>
    public class UpdateMessageValidator : AbstractValidator<UpdateVmsMessageInput>
	{
		public UpdateMessageValidator(IStringLocalizer localizer) {
			Include(new MessageValidator(localizer, false));
		}
	}

    /// <summary>
    /// Author: ThangNN
    /// Created date: 01/12/2024
    /// Description: Kiểm tra dữ liệu delete
    /// </summary>
    public class DeleteMessageValidator: AbstractValidator<DeleteVmsMessageInput> {
		public DeleteMessageValidator(IStringLocalizer lz)
		{
			RuleFor(x => x.ID)
				.NotEmpty()
					.WithName(x => lz[BaseMsg.BaseEntity.ID])
					.WithMessage(x => lz[BaseMsg.BaseValidation.Required])
				.MaximumLength(EntityConst.KeyFieldLength)
					.WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);
		}
	}
}
