/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

using Application.Commands;

namespace Application.Validators
{
    /// <summary>
    /// Description: Trình kiểm tra tính hợp lệ của lệnh tạo người dùng (FluentValidation Validator)
    /// Author: Antigravity
    /// Create Date: 22/07/2026
    /// </summary>
    public class cls_CreateUserCommandValidator : AbstractValidator<cls_CreateUserCommand>
    {
        public cls_CreateUserCommandValidator()
        {
            RuleFor(x => x.strUserName)
                .NotEmpty().WithMessage("Tên đăng nhập không được để trống!")
                .MinimumLength(3).WithMessage("Tên đăng nhập phải chứa ít nhất 3 ký tự!")
                .MaximumLength(50).WithMessage("Tên đăng nhập không được vượt quá 50 ký tự!");

            RuleFor(x => x.strEmail)
                .EmailAddress().WithMessage("Địa chỉ Email không đúng định dạng!")
                .When(x => !string.IsNullOrEmpty(x.strEmail));
        }
    }

    public class cls_CreateUserCommandValidator_v1 : AbstractValidator<cls_CreateUserCommand>
    {
        public cls_CreateUserCommandValidator_v1()
        {
            RuleFor(x => x.strUserName)
                .NotEmpty().WithMessage("Tên đăng nhập không được để trống!")
                .MinimumLength(3).WithMessage("Tên đăng nhập phải chứa ít nhất 3 ký tự!")
                .MaximumLength(50).WithMessage("Tên đăng nhập không được vượt quá 50 ký tự!");

            RuleFor(x => x.strEmail)
                .EmailAddress().WithMessage("Địa chỉ Email không đúng định dạng!")
                .When(x => !string.IsNullOrEmpty(x.strEmail));
        }
    }

    public class cls_CreateUserCommandValidator_v2 : AbstractValidator<cls_CreateUserCommand>
    {
        public cls_CreateUserCommandValidator_v2()
        {
            RuleFor(x => x.strUserName)
                .NotEmpty().WithMessage("Tên đăng nhập không được để trống!")
                .MinimumLength(3).WithMessage("Tên đăng nhập phải chứa ít nhất 3 ký tự!")
                .MaximumLength(50).WithMessage("Tên đăng nhập không được vượt quá 50 ký tự!");

            RuleFor(x => x.strEmail)
                .EmailAddress().WithMessage("Địa chỉ Email không đúng định dạng!")
                .When(x => !string.IsNullOrEmpty(x.strEmail));
        }
    }
}
