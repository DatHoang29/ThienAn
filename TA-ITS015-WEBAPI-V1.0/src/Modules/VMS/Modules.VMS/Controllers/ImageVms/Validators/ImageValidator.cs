using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.VMS.Controllers.ImageVms.Dto;
using Modules.VMS.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.VMS.Controllers.ImageVms.Validators
{
    //VmsImage {
    // ID varchar(64) pk unique
    // Code string(64) null
    // Name string(64) null
    // Lang string(16) null def(vi-VN)
    // Type string(16) null def(icon)
    // Url string(256) null
    // Size string(16) null
    // OrderNo integer null
    // Status integer null
    // Remark string(256) null
    //}

    public class AddImageValidator : AbstractValidator<AddVmsImageInput>
    {
        public AddImageValidator(IStringLocalizer lz)
        {
            RuleFor(x => x.Code).NotEmpty()
                .WithName(x => lz[BaseMsg.BaseEntity.Code]).WithMessage(x => lz[BaseMsg.BaseValidation.Required])
                .MaximumLength(EntityConst.Length64)
                .WithName(x => lz[BaseMsg.BaseEntity.Code]).WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Name).NotEmpty()
                .WithName(x => lz[BaseMsg.BaseEntity.Name]).WithMessage(x => lz[BaseMsg.BaseValidation.Required])
                .MaximumLength(EntityConst.Length64)
                .WithName(x => lz[BaseMsg.BaseEntity.Name]).WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);
        }
    }

    public class UpdateImageValidator : AbstractValidator<UpdateVmsImageInput>
    {
        public UpdateImageValidator(IStringLocalizer lz)
        {
            Include(new AddImageValidator(lz));
        }
    }

    /// <summary>
    /// Author: ThangNN
    /// Created date: 01/12/2024
    /// </summary>
    public class DeleteImageValidator : AbstractValidator<DeleteVmsImageInput>
    {
        public DeleteImageValidator(IStringLocalizer lz)
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
