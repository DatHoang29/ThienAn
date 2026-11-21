using Microsoft.Extensions.Hosting;
using Modules.TMS.Controllers.Weather.Dto;
using Shared.DTO.Dtos.System;
using Shared.Infrastructure.Utilities.BaseApiClient;
using Shared.Utility.Apis.System;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;

namespace Modules.TMS.Infrastructure.Services.Weather
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 10/05/2024
    /// </summary>
    public class WeatherService : IHostedService
    {
        private IMessageBus _messBus;

        public WeatherService(IMessageBus messageBus)
        {
            this._messBus = messageBus;
        }

        /// <summary>
        /// Author: ThangNN
        /// Created Date: 10/05/2024
        /// </summary>
        public async Task InsertTmsWeatherFromAPI()
        {
            try
            {
                // Reset Hangfire local nếu có thay đổi tên hàm
                //drop table[HangFire].[AggregatedCounter]
                //drop table[HangFire].[Counter]
                //drop table[HangFire].[Hash]
                //drop table[HangFire].[List]
                //drop table[HangFire].[Schema]
                //drop table[HangFire].[Server]
                //drop table[HangFire].[Set]
                //drop table[HangFire].[State]
                //drop table[HangFire].[JobParameter]
                //drop table[HangFire].[JobQueue]
                //drop table[HangFire].[Job] -- Sau cùng

                // ERROR: Wolverine = null nếu gọi tại thời điểm khởi chạy AddHostedService | IHostedService này
                // Wolverine hiện tại được khởi tạo sau cùng trong Progame.cs: IHostBuilder CreateHostBuilder(...)
                if (this._messBus is null)
                    this._messBus = App.GetService<IMessageBus>();

                if (this._messBus is null)
                    return;

                var query = new SysOpConfigListRequest() { };
                var response = await BaseApiClient<IOpConfigApi>.Client().List(query);
                var opConfigList = response.Content?.Result?.Where(config => config.GroupCode != null && config.GroupCode.Equals("operation_config", StringComparison.OrdinalIgnoreCase)).ToList();

                string? openApi = opConfigList?.Find(oc => oc.Code != null && oc.Code.Equals("weather_open_api", StringComparison.OrdinalIgnoreCase))?.Value;
                string? equiApi = opConfigList?.Find(oc => oc.Code != null && oc.Code.Equals("weather_equi_api", StringComparison.OrdinalIgnoreCase))?.Value;

                string? weatherApi = !string.IsNullOrEmpty(equiApi) ? equiApi : openApi;

                weatherApi = weatherApi ?? "https://api.openweathermap.org/data/2.5/weather?lat=10.762622&lon=106.660172&units=metric&lang=vi&appid=e11a766a0980adfb44e8f106aa7d0689";

                // Api request timeout là 10 giây
                string responseContent = await this.FetchJsonFromAPI(NormalizeUrl(weatherApi), 10 * 1000);

                List<KeyValuePair<string, string>> keyValueList = new List<KeyValuePair<string, string>>();
                ParseJsonElement(JsonDocument.Parse(responseContent).RootElement, keyValueList);

                if (keyValueList is null || !keyValueList.Any())
                    return;

                AddTmsWeatherInput? weatherInfo = null;
                if (!string.IsNullOrEmpty(equiApi))
                {

                }
                else
                {
                    weatherInfo = new AddTmsWeatherInput()
                    {
                        Temperature = StringToFloat(keyValueList.Find(kv => kv.Key.Contains("temp", StringComparison.OrdinalIgnoreCase)).Value),
                        Hudmidity = StringToFloat(keyValueList.Find(kv => kv.Key.Contains("humidity", StringComparison.OrdinalIgnoreCase)).Value),
                        WindDirection = keyValueList.Find(kv => kv.Key.Contains("wind", StringComparison.OrdinalIgnoreCase) && kv.Key.Contains("deg", StringComparison.OrdinalIgnoreCase)).Value,
                        WindSpeed = StringToFloat(keyValueList.Find(kv => kv.Key.Contains("wind", StringComparison.OrdinalIgnoreCase) && kv.Key.Contains("speed", StringComparison.OrdinalIgnoreCase)).Value),
                        Rain = StringToFloat(keyValueList.Find(kv => kv.Key.Contains("rain", StringComparison.OrdinalIgnoreCase) && kv.Key.Contains("1h", StringComparison.OrdinalIgnoreCase)).Value),
                        Foresight = StringToFloat(keyValueList.Find(kv => kv.Key.Contains("visibility", StringComparison.OrdinalIgnoreCase)).Value),
                        TimeDetect = DateTime.Now,
                        Description = string.Concat(
                                "|Longitude:", " ", keyValueList.Find(kv => kv.Key.Contains("lon", StringComparison.OrdinalIgnoreCase)).Value,
                                "|Latitude:", " ", keyValueList.Find(kv => kv.Key.Contains("lat", StringComparison.OrdinalIgnoreCase)).Value,
                                "|Cloud:", " ", keyValueList.Find(kv => kv.Key.Contains("cloud", StringComparison.OrdinalIgnoreCase)).Value,
                                "|Pressure:", " ", keyValueList.Find(kv => kv.Key.Contains("pressure", StringComparison.OrdinalIgnoreCase)).Value,
                                "|Tempmin:", " ", keyValueList.Find(kv => kv.Key.Contains("temp", StringComparison.OrdinalIgnoreCase) && kv.Key.Contains("min", StringComparison.OrdinalIgnoreCase)).Value,
                                 "|Tempmax:", " ", keyValueList.Find(kv => kv.Key.Contains("temp", StringComparison.OrdinalIgnoreCase) && kv.Key.Contains("max", StringComparison.OrdinalIgnoreCase)).Value,
                                "|Timezone: ", " ", keyValueList.Find(kv => kv.Key.Contains("timezone", StringComparison.OrdinalIgnoreCase)).Value
                            ),
                        LocationDetail = string.Concat(
                            keyValueList.Find(kv => kv.Key.Contains("name", StringComparison.OrdinalIgnoreCase)).Value,
                            "|", DateTime.Now.ToString("G"),
                            "|Longitude:", " ", keyValueList.Find(kv => kv.Key.Contains("lon", StringComparison.OrdinalIgnoreCase)).Value,
                            "|Latitude:", " ", keyValueList.Find(kv => kv.Key.Contains("lat", StringComparison.OrdinalIgnoreCase)).Value,
                            "|Timezone: ", " ", keyValueList.Find(kv => kv.Key.Contains("timezone", StringComparison.OrdinalIgnoreCase)).Value
                        ),
                        ShortDescription = keyValueList.Find(kv => kv.Key.Contains("weather", StringComparison.OrdinalIgnoreCase) && kv.Key.Contains("description", StringComparison.OrdinalIgnoreCase)).Value,
                        Type = keyValueList.Find(kv => kv.Key.Contains("weather", StringComparison.OrdinalIgnoreCase) && kv.Key.Contains("main", StringComparison.OrdinalIgnoreCase)).Value
                    };

                    if (weatherInfo is not null)
                    {
                        if (weatherInfo.Type.Contains("clear", StringComparison.OrdinalIgnoreCase))
                            weatherInfo.Type = "clear";
                        else if (weatherInfo.Type.Contains("rain", StringComparison.OrdinalIgnoreCase))
                            weatherInfo.Type = "rain";
                        else if (weatherInfo.Type.Contains("cloud", StringComparison.OrdinalIgnoreCase))
                            weatherInfo.Type = "cloud";
                        else if (weatherInfo.Type.Contains("storm", StringComparison.OrdinalIgnoreCase))
                            weatherInfo.Type = "storm";
                        else if (weatherInfo.Type.Contains("snow", StringComparison.OrdinalIgnoreCase))
                            weatherInfo.Type = "snow";
                        else
                            weatherInfo.Type = "weather";
                    }
                }

                if (weatherInfo is null)
                    return;

                this._messBus?.InvokeAsync(weatherInfo);
            }
            catch (Exception ex) { }
        }

        /// <summary>
        /// Description: Hàm truy xuất json bằng api
        /// Author: ThangNN
        /// Created Date: 10/05/2024
        /// </summary>
        public async Task<string> FetchJsonFromAPI(string apiUrl, int timeout)
        {
            if (string.IsNullOrWhiteSpace(apiUrl))
                return string.Empty;

            var result = string.Empty;
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var cancelTS = new CancellationTokenSource(timeout);
                    result = await httpClient.GetStringAsync(apiUrl, cancelTS.Token);
                }
            }
            catch { }

            return result;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Hàm chuẩn hóa url
        /// Created date: 10/05/2024
        /// </summary>
        /// <param name="urlFull"></param>
        /// <returns></returns>
        public static string NormalizeUrl(string urlFull)
        {
            try
            {
                Uri uri = new Uri(urlFull);
                string baseUrl = uri.GetLeftPart(UriPartial.Path);
                string queryString = uri.Query;

                string? encodedQuery = HttpUtility.ParseQueryString(queryString).ToString();
                if (encodedQuery is null)
                    return string.Empty;

                UriBuilder uriBuilder = new UriBuilder(baseUrl) { Query = encodedQuery };

                return uriBuilder.Uri.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Author: ThangNN
        /// Created Date: 10/05/2024
        /// </summary>
        public static float StringToFloat(string? input)
        {
            try
            {
                if (string.IsNullOrEmpty(input))
                    return 0;

                var numFormat = new NumberFormatInfo { NumberDecimalSeparator = ".", NumberGroupSeparator = "," };
                return float.TryParse(input, NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands, numFormat, out var result) ? result : 0;
            }
            catch { return 0; }
        }

        /// <summary>
        /// Author: ThangNN
        /// Created Date: 10/05/2024
        /// </summary>
        public static void ParseJsonElement(JsonElement element, List<KeyValuePair<string, string>> keyValueList, string prefix = "")
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        ParseJsonElement(property.Value, keyValueList, Regex.Replace($"{prefix}{property.Name}.", @"\.{2,}", "."));
                    }
                    break;
                case JsonValueKind.Array:
                    int index = 0;
                    foreach (var item in element.EnumerateArray())
                    {
                        ParseJsonElement(item, keyValueList, Regex.Replace($"{prefix}.", @"\.{2,}", "."));
                        index++;
                    }
                    break;
                default:
                    keyValueList.Add(new KeyValuePair<string, string>(Regex.Replace(prefix.Trim('.'), @"\.{2,}", "."), element.ToString()));
                    break;
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
