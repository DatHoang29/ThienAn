using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Modules.WP.Controllers.Post.Dto;
using Modules.WP.Core.Exceptions;

namespace Modules.WP.Controllers.Post.Validators
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Validate dữ liệu Add/Update/Delete cho bài viết hệ thống WP (Post)
    /// Created date: 03/07/2026
    /// </summary>
    public class ListPostValidator : AbstractValidator<PostInput>
    {
        public ListPostValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.Title).MaximumLength(-1).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Author).MaximumLength(128).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Category).MaximumLength(-1).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Code).MaximumLength(64).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

        }

    }
    /// <summary>
    /// Author: HieuNV
    /// Description: Kiểm tra dữ liệu đầu vào của Page Post
    /// Created date: 14/08/2026
    /// </summary>
    public class PagePostValidator : AbstractValidator<PagePostInput>
    {
        public PagePostValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.Title).MaximumLength(-1).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Author).MaximumLength(128).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Category).MaximumLength(-1).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Code).MaximumLength(64).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

        }

    }
    /// <summary>
    /// Author: HieuNV
    /// Description: Kiểm tra dữ liệu đầu vào của Add Post
    /// Created date: 14/08/2026
    /// </summary>
    public class AddPostValidator:AbstractValidator<AddPostInput>
    {
        public AddPostValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.Title).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.Title]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(-1).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Excerpt).MaximumLength(-1).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Content).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.Content]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
            .MaximumLength(-1).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Thumbnail).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.Thumbnail]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
            .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Category).MaximumLength(-1).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Author).MaximumLength(128).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Remark).MaximumLength(256).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            
        }

    }
    /// <summary>
    /// Author: HieuNV
    /// Description: Kiểm tra dữ liệu đầu vào của Update Post
    /// Created date: 14/08/2026
    /// </summary>

    public class UpdatePostValidator:AbstractValidator<UpdatePostInput>
    {
        public UpdatePostValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.Title).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.Title]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
            .MaximumLength(-1).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Excerpt).MaximumLength(-1).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Content).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.Content]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
            .MaximumLength(-1).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Thumbnail).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.Thumbnail]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
            .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Category).MaximumLength(-1).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Author).MaximumLength(128).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Remark).MaximumLength(256).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
           
        }
    }
    /// <summary>
    /// Author: HieuNV
    /// Description: Kiểm tra dữ liệu đầu vào của Delete Post
    /// Created date: 14/08/2026
    /// </summary>

    public class DeletePostValidator:AbstractValidator<DeletePostInput>
    {
          public DeletePostValidator(IStringLocalizer localizer)
            {
                RuleFor(x => x.ID).NotEmpty()
                    .WithName(x => localizer[BaseMsg.BaseEntity.ID])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);
            }
    }
}
