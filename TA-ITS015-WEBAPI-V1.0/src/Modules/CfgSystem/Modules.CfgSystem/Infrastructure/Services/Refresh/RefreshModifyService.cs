using System.Globalization;
using Microsoft.Extensions.Logging;
using Modules.CfgSystem.Core.Dto.ConfigData;
using Modules.CfgSystem.Core.Refresh;
using Modules.DataTransporter.ITS.Infrastructure.Services;
using Wolverine;

namespace Modules.CfgSystem.Infrastructure.Services.Refresh
{
    /// <summary>
    /// Author: KienLT
    /// Description: Đọc rule <c>refresh_modify_route</c> trong SysConfigData, rồi bắn payload legacy
    /// <c>{ code }</c> lên subject <see cref="ItsRefresh.Subject"/>.
    ///
    /// Đặt ở CfgSystem (không phải tầng transporter) vì logic dựa trên cấu hình của CfgSystem. Chiều phụ
    /// thuộc: CfgSystem → DataTransporter.ITS (một chiều). Transporter chỉ là kênh publish.
    /// Created date: 18/09/2026
    /// </summary>
    public sealed class RefreshModifyService : IScoped
    {
        public const string RouteConfigTypeCode = "refresh_modify_route";

        private readonly IMessageBus _bus;
        private readonly ILogger<RefreshModifyService> _logger;

        public RefreshModifyService(
            IMessageBus bus,
            ILogger<RefreshModifyService> logger)
        {
            _bus = bus;
            _logger = logger;
        }

        public async Task<RefreshDispatchResult> NotifyAsync(
            RefreshModifyRequest request, CancellationToken cancellationToken = default)
        {
            var source = request.Source?.Trim().ToLowerInvariant() ?? string.Empty;
            var scope = request.Scope?.Trim().ToLowerInvariant() ?? string.Empty;
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(scope))
            {
                _logger.LogWarning("[RefreshModify] Bỏ refresh vì source/scope rỗng. Source={Source}, Scope={Scope}.", request.Source, request.Scope);
                return Pending();
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var routes = await _bus.InvokeAsync<List<SysConfigDataOutput>>(
                    new SysConfigDataInput { ConfigTypeCode = RouteConfigTypeCode });
                var routeKey = $"{source}:{scope}";
                var route = routes?.FirstOrDefault(x => string.Equals(x.Code, routeKey, StringComparison.OrdinalIgnoreCase))
                    ?? routes?.FirstOrDefault(x => string.Equals(x.Code, $"{source}:*", StringComparison.OrdinalIgnoreCase));

                if (route is null)
                {
                    _logger.LogInformation("[RefreshModify] Không có route {RouteKey}; không publish.", routeKey);
                    return Pending();
                }

                if (!TryParseCode(route.Value, out var code))
                {
                    _logger.LogWarning("[RefreshModify] Route {RouteKey} có Value không hợp lệ; phải là code nguyên không âm.", route.Code);
                    return Pending();
                }

                var transport = ItsRefresh.Send(code);
                var published = transport.Count > 0 && transport.Values.All(x => x);
                _logger.LogInformation("[RefreshModify] {Action} {Source}:{Scope}, key={Key} => code={Code}, published={Published}.",
                    request.Action, source, scope, request.Key, code, published);
                return new RefreshDispatchResult(published, code, transport);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RefreshModify] Không resolve/publish được {Source}:{Scope}; dữ liệu đã commit vẫn giữ nguyên.", source, scope);
                return Pending();
            }
        }

        internal static bool TryParseCode(string? value, out int code) =>
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out code) && code >= 0;

        private static RefreshDispatchResult Pending() => new(false, null, new Dictionary<string, bool>());
    }
}
