// Author: Codex
// Description: Authorize tenant-scoped SSE with the application's JWT identity.
// Created date: 02/10/2026

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Core.Utilities.Constants;

namespace Shared.Utility.Sse;

/// <summary>Authorize tenant-scoped SSE with the application's JWT identity.</summary>
public static class SnapshotSseAuthorization
{
    public const string Policy = "SnapshotSseAccess";

    public static IServiceCollection AddSnapshotSse(this IServiceCollection services)
    {
        services.TryAddSingleton<SnapshotSseHub>();
        services.AddAuthorization(options => options.AddPolicy(Policy, policy => policy
            .AddAuthenticationSchemes("Bearer")
            .RequireAuthenticatedUser()
            .RequireAssertion(context => !string.IsNullOrWhiteSpace(
                context.User.FindFirst(ClaimConst.TenantId)?.Value))));
        return services;
    }
}
