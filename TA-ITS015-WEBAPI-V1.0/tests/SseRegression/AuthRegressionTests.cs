using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Core.Utilities.Constants;
using Shared.Utility.Sse;



internal static class AuthRegression
{
    public static async Task RunAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthentication("Bearer")
            .AddScheme<AuthenticationSchemeOptions, TestBearerHandler>("Bearer", _ => { });
        services.AddSnapshotSse();
        services.AddSnapshotSse();
        Check(services.Count(x => x.ServiceType == typeof(SnapshotSseHub)) == 1,
            "repeated module registration preserves one singleton hub");
        Check(services.Single(x => x.ServiceType == typeof(SnapshotSseHub)).Lifetime == ServiceLifetime.Singleton,
            "hub uses singleton lifetime");
        await using var provider = services.BuildServiceProvider();
        var policy = await provider.GetRequiredService<IAuthorizationPolicyProvider>()
            .GetPolicyAsync(SnapshotSseAuthorization.Policy)
            ?? throw new Exception("Missing snapshot SSE authorization policy");
        Check(policy.AuthenticationSchemes.SequenceEqual(new[] { "Bearer" }),
            "policy accepts only the existing JWT Bearer scheme");
        Check(ReferenceEquals(provider.GetRequiredService<SnapshotSseHub>(), provider.GetRequiredService<SnapshotSseHub>()),
            "all stream modules resolve the same hub");

        await Case("anonymous request rejected", null, null, false);
        await Case("JWT identity without tenant rejected", Principal("Bearer"), null, false);
        await Case("JWT identity with empty tenant rejected", Principal("Bearer", ""), null, false);
        await Case("JWT identity with whitespace tenant rejected", Principal("Bearer", " \t"), null, false);
        await Case("JWT with tenant accepted without AccountType", Principal("Bearer", "tenant-a"), null, true);
        await Case("unauthenticated identity with tenant rejected", Principal(null, "tenant-a"), null, false);
        await Case("API key identity cannot replace a missing JWT", null, Principal("SseApiKey", "tenant-a"), false);
        await Case("valid JWT wins over unrelated API key context identity", Principal("Bearer", "tenant-b"), Principal("SseApiKey"), true);

        async Task Case(string name, ClaimsPrincipal? bearerPrincipal, ClaimsPrincipal? existingPrincipal, bool allowed)
        {
            await using var scope = provider.CreateAsyncScope();
            var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
            if (bearerPrincipal is not null) context.Items[TestBearerHandler.PrincipalKey] = bearerPrincipal;
            if (existingPrincipal is not null) context.User = existingPrincipal;
            var evaluator = scope.ServiceProvider.GetRequiredService<IPolicyEvaluator>();
            var authenticated = await evaluator.AuthenticateAsync(policy, context);
            var result = await evaluator.AuthorizeAsync(policy, authenticated, context, null);
            Check(result.Succeeded == allowed, name);
        }
    }

    private static ClaimsPrincipal Principal(string? authenticationType, string? tenant = null)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "synthetic-user") };
        if (tenant is not null) claims.Add(new Claim(ClaimConst.TenantId, tenant));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType));
    }

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("FAIL: " + name);
        Console.WriteLine("PASS: " + name);
    }
}

internal sealed class TestBearerHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string PrincipalKey = "synthetic-bearer-principal";
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        => Task.FromResult(Context.Items.TryGetValue(PrincipalKey, out var value) && value is ClaimsPrincipal principal
            && principal.Identity?.IsAuthenticated == true
            ? AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name))
            : AuthenticateResult.NoResult());
}
