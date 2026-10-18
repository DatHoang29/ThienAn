using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;

namespace Tests;

/// <summary>
/// Author: DatHQ
/// Description: Hàm mở rộng IServiceProvider mô phỏng luồng gọi API thật qua MVC:
///              chạy FluentValidation lấy từ DI trước khi chuyển input sang IMessageBus.
/// Created date: 18/10/2026
/// </summary>
public static class ApiCallExtensions
{
    public static async Task CallApiAsync(this IServiceProvider sp, object input)
    {
        ArgumentNullException.ThrowIfNull(sp);
        ArgumentNullException.ThrowIfNull(input);

        var validatorType = typeof(IValidator<>).MakeGenericType(input.GetType());
        if (sp.GetService(validatorType) is IValidator validator)
        {
            var result = validator.Validate(new ValidationContext<object>(input));
            if (!result.IsValid)
            {
                throw new ValidationException(result.Errors);
            }
        }

        var bus = sp.GetRequiredService<IMessageBus>();
        await bus.InvokeAsync(input);
    }

    public static async Task<TOutput> CallApiAsync<TOutput>(this IServiceProvider sp, object input)
    {
        ArgumentNullException.ThrowIfNull(sp);
        ArgumentNullException.ThrowIfNull(input);

        var validatorType = typeof(IValidator<>).MakeGenericType(input.GetType());
        if (sp.GetService(validatorType) is IValidator validator)
        {
            var result = validator.Validate(new ValidationContext<object>(input));
            if (!result.IsValid)
            {
                throw new ValidationException(result.Errors);
            }
        }

        var bus = sp.GetRequiredService<IMessageBus>();
        return await bus.InvokeAsync<TOutput>(input);
    }
}
