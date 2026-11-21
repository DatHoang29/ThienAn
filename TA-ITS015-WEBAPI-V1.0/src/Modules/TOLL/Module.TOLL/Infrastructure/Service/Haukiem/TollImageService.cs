using Microsoft.Extensions.Configuration;
using Module.TOLL.Core.Dto.HaukiemTransaction;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Text;

namespace Module.TOLL.Infrastructure.Service.Haukiem
{
    /// <summary>
    /// Gọi API ảnh của hệ thống TCS: nhận đường dẫn ảnh (lưu trong DB) và trả về base64 để FE hiển thị.
    /// Author: ChuongLP
    /// Created date: 25/08/2026
    /// </summary>
    public class TollImageService : IScoped
    {
        /// <summary>URL mặc định khi chưa cấu hình "TcsImageApi:Url" trong appsettings.</summary>
        private const string DefaultImageApiUrl = "http://10.10.9.223:9089/api/tcs23quanly/mdsmasterdatatcs/image";

        private readonly IHttpClientFactory _httpClientFactory;

        public TollImageService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Lấy ảnh base64 của một lượt xe theo các đường dẫn ảnh gốc.
        /// Author: ChuongLP
        /// Date: 25/08/2026
        /// </summary>
        public async Task<TransactionImageOutput> GetImagesAsync(TransactionImageInput input, CancellationToken cancellationToken = default)
        {
            var output = new TransactionImageOutput();

            // Không có đường dẫn nào -> khỏi gọi API cho đỡ tốn round-trip
            if (string.IsNullOrWhiteSpace(input.ImageLaneIn)
                && string.IsNullOrWhiteSpace(input.ImageLane)
                && string.IsNullOrWhiteSpace(input.ImagePlate)
                && string.IsNullOrWhiteSpace(input.ImagePlateIn))
            {
                output.Success = true;
                return output;
            }

            // URL + timeout lấy từ cấu hình, có giá trị mặc định để không phụ thuộc cứng vào appsettings
            var url = App.Configuration["TcsImageApi:Url"];
            if (string.IsNullOrWhiteSpace(url)) url = DefaultImageApiUrl;

            var timeoutSeconds = App.Configuration.GetValue<int>("TcsImageApi:TimeoutSeconds", 5);
            if (timeoutSeconds <= 0) timeoutSeconds = 5;

            // Body gửi sang TCS dùng đúng tên trường bên cung cấp (underscore), chuỗi rỗng nếu không cần
            var requestBody = new TcsImageRequest
            {
                ImageLaneIn = input.ImageLaneIn ?? string.Empty,
                ImageLane = input.ImageLane ?? string.Empty,
                ImagePlate = input.ImagePlate ?? string.Empty,
                ImagePlateIn = input.ImagePlateIn ?? string.Empty,
            };

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

                var client = _httpClientFactory.CreateClient();

                var json = JsonConvert.SerializeObject(requestBody);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var response = await client.PostAsync(url, content, cts.Token).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    output.Error = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
                    return output;
                }

                var responseText = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(responseText))
                {
                    output.Error = "Không đọc được dữ liệu ảnh trả về.";
                    return output;
                }

                // TCS bọc dữ liệu trong envelope { code, succeeded, result: {...} } -> bóc "result" trước khi map.
                // Nếu không có "result" thì fallback về root (phòng khi API đổi định dạng).
                var root = JToken.Parse(responseText);
                var payload = root is JObject obj && obj["result"] != null ? obj["result"]! : root;

                var parsed = payload.ToObject<TcsImageResponse>();
                if (parsed == null)
                {
                    output.Error = "Không đọc được dữ liệu ảnh trả về.";
                    return output;
                }

                output.ImageLaneIn = parsed.ImageLaneIn;
                output.ImageLane = parsed.ImageLane;
                output.ImagePlate = parsed.ImagePlate;
                output.ImagePlateIn = parsed.ImagePlateIn;
                output.Success = true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                output.Error = "Đã hủy yêu cầu lấy ảnh.";
            }
            catch (OperationCanceledException)
            {
                output.Error = $"Quá thời gian chờ lấy ảnh ({timeoutSeconds}s).";
            }
            catch (Exception ex)
            {
                output.Error = ex.Message;
            }

            return output;
        }

        /// <summary>
        /// Body request khớp tên trường của API TCS (imageLane_In / imageLane / imagePlate / imagePlate_In).
        /// </summary>
        private sealed class TcsImageRequest
        {
            [JsonProperty("imageLane_In")]
            public string ImageLaneIn { get; set; } = string.Empty;

            [JsonProperty("imageLane")]
            public string ImageLane { get; set; } = string.Empty;

            [JsonProperty("imagePlate")]
            public string ImagePlate { get; set; } = string.Empty;

            [JsonProperty("imagePlate_In")]
            public string ImagePlateIn { get; set; } = string.Empty;
        }

        /// <summary>
        /// Response khớp tên trường của API TCS. Giá trị là chuỗi "data:image/jpeg;base64,...".
        /// </summary>
        private sealed class TcsImageResponse
        {
            [JsonProperty("imageLane_In")]
            public string? ImageLaneIn { get; set; }

            [JsonProperty("imageLane")]
            public string? ImageLane { get; set; }

            [JsonProperty("imagePlate")]
            public string? ImagePlate { get; set; }

            [JsonProperty("imagePlate_In")]
            public string? ImagePlateIn { get; set; }
        }
    }
}
