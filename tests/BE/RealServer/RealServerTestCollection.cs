namespace Tests.RealServer
{
    /// <summary>
    /// Description: Collection chuyên biệt cho các bài kiểm thử kết nối trực tiếp tới hạ tầng / server thật bên ngoài.
    ///              Tách biệt hoàn toàn khỏi [Collection("api")] (không khởi tạo WebApplicationFactory, không đụng CSDL local hay nghiệp vụ).
    /// Created date: 13/10/2026
    /// </summary>
    [CollectionDefinition("real-server", DisableParallelization = true)]
    public class RealServerTestCollection
    {
    }
}
