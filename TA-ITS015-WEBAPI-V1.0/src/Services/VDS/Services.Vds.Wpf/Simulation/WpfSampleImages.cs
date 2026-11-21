// Author: Codex
// Description: Ảnh mẫu đóng gói cùng harness; chỉ phục vụ bản tin test do WPF bơm.
// Created date: 02/10/2026

using System.Net;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Services.Vds.Manager;
using Services.Vds.Shared.Domain;

namespace Services.Vds.Wpf.Simulation;

/// <summary>Ảnh mẫu đóng gói cùng harness; chỉ phục vụ bản tin test do WPF bơm.</summary>
public static class WpfSampleImages
{
    private const string UrlPrefix = "/__wpf-test-images__/";
    private static readonly string[] Kinds = ["full", "vehicle", "plate"];

    public static string AttachDefaults(string rawFrame)
    {
        var frame = JObject.Parse(rawFrame);
        if (VdsEventTypes.Canonical(frame.Value<string>("type") ?? "") == VdsEventTypes.Statistic
            || frame["media"] is JArray { Count: > 0 })
            return rawFrame;

        frame["media"] = new JArray(Kinds.Select(kind => new JObject
        {
            ["id"] = frame["id"]?.DeepClone(),
            ["kind"] = kind,
            ["mime"] = "image/jpeg",
            ["url"] = UrlPrefix + kind + ".jpeg"
        }));
        return frame.ToString(Formatting.None);
    }

    public static Stream? Open(Uri? uri)
    {
        if (uri is null) return null;
        var kind = Kinds.FirstOrDefault(value => uri.AbsolutePath.EndsWith(
            UrlPrefix + value + ".jpeg", StringComparison.Ordinal));
        if (kind is null) return null;

        return typeof(WpfSampleImages).Assembly.GetManifestResourceStream(
            $"Services.Vds.Wpf.SampleImages.{kind}.jpeg")
            ?? throw new FileNotFoundException($"Thiếu ảnh mẫu WPF: {kind}.jpeg");
    }
}

/// <summary>Gắn ảnh cho mọi lệnh bơm từ UI và bộ mô phỏng, trước khi đi vào pipeline thật.</summary>
public sealed class WpfSampleFrameInjector(
    IVdsFrameInjector inner, IOptions<SimulatorOptions> options) : IVdsFrameInjector
{
    public Task<bool> InjectAsync(string rawFrame, CancellationToken ct = default)
        => inner.InjectAsync(options.Value.IncludeDefaultImages && !string.IsNullOrWhiteSpace(rawFrame)
            ? WpfSampleImages.AttachDefaults(rawFrame) : rawFrame, ct);
}

/// <summary>
/// Trả ảnh đóng gói cho URL mẫu. VdsMediaStorage vẫn tải và lưu theo cấu hình đang hiệu lực;
/// những URL khác tiếp tục qua HTTP handler gốc.
/// </summary>
public sealed class WpfSampleImageHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var stream = WpfSampleImages.Open(request.RequestUri);
        if (stream is null) return base.SendAsync(request, cancellationToken);

        var content = new StreamContent(stream);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Headers.ContentLength = stream.Length;
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            RequestMessage = request,
            Content = content
        });
    }
}
