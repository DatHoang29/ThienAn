namespace Module.VideoWall.Core.Interfaces
{
    /// <summary>
    /// Description: Giao diện phát thông điệp NATS cho VideoWall, cho phép dùng chung giữa WebAPI và Worker
    /// Created date: 11/09/2026
    /// </summary>
    public interface IVwNatsPublisher
    {
        Task<bool> PublishAsync(string subject, object payload, CancellationToken ct = default);
    }
}
