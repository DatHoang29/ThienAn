using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Services.Vds.Configuration;
using Services.Vds.Sources.Backfill;
using Services.Vds.Pipeline.Logging;
using Services.Vds.Pipeline.Queue;
using Services.Vds.Sources;
using Services.Vds.Pipeline.State;
using Services.Vds.Manager;
using Wolverine;
using Wolverine.Transports;

namespace Services.Vds.Pipeline;

/// <summary>
/// Author: KienLT
/// Description: Composition root riêng của đường dữ liệu thời gian thực (pipeline + nguồn).
///
/// KHÔNG còn <c>AddHostedService</c> nào ở đây: vòng đời của nguồn và của phần bù dữ liệu do
/// <c>VdsServiceManager</c> điều phối tường minh, không phụ thuộc thứ tự đăng ký DI.
/// Created date: 25/08/2026
/// </summary>
public static class PipelineRegistration
{
    public static IServiceCollection AddVdsPipeline(
        this IServiceCollection services, IConfiguration configuration)
    {
        var queueCapacity = configuration
            .GetSection($"{ServiceConfig.SectionName}:Ingestion")
            .Get<IngestionOptions>()?
            .EffectiveQueueCapacity ?? new IngestionOptions().EffectiveQueueCapacity;

        services.AddWolverine(ExtensionDiscovery.ManualOnly, options =>
        {
            // Handler nằm trong class library, không phải entry assembly của Worker.
            options.Discovery.IncludeAssembly(typeof(ProcessVdsFrameCommandHandler).Assembly);

            // Wolverine ghi MỘT dòng Information cho mỗi message xử lý xong. Đường dữ liệu này đẩy một
            // message cho MỖI bản tin xe, nên ở lưu lượng thật log chỉ còn là dòng đó: đo ngày 23/09/2026,
            // khoảng 170.000 dòng loại này chôn mất hai lỗi đang xảy ra — một bản tin bị loại chặn lượt bù
            // suốt 2 ngày, và NATS thiếu file creds báo lỗi mỗi 5 giây.
            //
            // Hạ xuống Debug thay vì lọc theo category: category của log đó là TÊN MESSAGE TYPE
            // (Services.Vds.Pipeline.Queue.ProcessVdsFrameCommand), không phải "Wolverine.*", nên
            // AddFilter("Wolverine", ...) KHÔNG khớp — đã thử và không có tác dụng.
            options.Policies.MessageSuccessLogLevel(LogLevel.Debug);

            options.LocalQueue(VdsQueues.Data)
                .BufferedInMemory(new BufferingLimits(queueCapacity, queueCapacity / 2))
                .Sequential();

            // PublishAsync phải đi đúng endpoint bounded ở trên, không rơi vào queue local mặc định.
            options.PublishMessage<ProcessVdsFrameCommand>()
                .ToLocalQueue(VdsQueues.Data);
        });

        services.AddSingleton<IngestionCheckpointStore>();
        services.AddSingleton<VdsRawLogger>();

        // Vùng chờ giữ bản tin không xử lý được. SINGLETON: nó là một index duy nhất trên đĩa, mỗi frame một
        // instance thì lượt ghi sau xoá mất mục của lượt trước.
        services.AddSingleton<Services.Vds.Pipeline.Quarantine.VdsQuarantineStore>();

        // SINGLETON bắt buộc: cửa chống trùng lượt xe giữ trạng thái "xe nào vừa qua camera nào" trong RAM.
        // Đăng ký scoped/transient thì mỗi frame có một cửa mới rỗng và cửa không chặn được gì.
        services.AddSingleton<VehicleDedupGate>();

        // Lưu trạng thái cửa SKIP ra đĩa để sống qua restart (cùng khuôn với EventDedupStore).
        // Đăng ký KÈM hosted service vì không có ai inject lớp này: singleton không được resolve thì
        // container không dựng, và như vậy lượt nạp lúc khởi động lẫn lượt ghi khi dừng đều không chạy.
        services.AddSingleton<VehicleDedupStateStore>();
        services.AddHostedService(sp => sp.GetRequiredService<VehicleDedupStateStore>());

        services.AddSingleton<VdsFrameProcessor>();
        services.AddSingleton<VdsFrameDispatcher>();

        // History API không có đường ghi riêng: coordinator chỉ enqueue ProcessVdsFrameCommand.
        services.AddSingleton<HistoryBackfillClient>();
        services.AddSingleton<BackfillCompletionTracker>();
        services.AddSingleton<BackfillCoordinator>();

        // Cầu nối "nguồn live yêu cầu bù" — SINGLETON vì hai bên phải dùng CHUNG một tín hiệu; scoped thì
        // yêu cầu của nguồn rơi vào một instance mà worker không bao giờ đọc.
        services.AddSingleton<BackfillRequestTrigger>();

        // Worker bù là chỗ DUY NHẤT gọi coordinator. SseEventSource chỉ đặt yêu cầu, không tự chạy lượt bù —
        // nhờ vậy lỗi khi bù quá khứ không dừng được đường live.
        services.AddHostedService<LiveGapRecoveryWorker>();

        // Nguồn bản tin: đăng ký làm IVdsEventSource để manager gọi, KHÔNG phải hosted service.
        services.AddSingleton<SseEventSource>();
        services.AddSingleton<IVdsEventSource>(sp => sp.GetRequiredService<SseEventSource>());

        return services;
    }
}
