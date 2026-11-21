using Furion;

namespace Shared.Utility.Utilities
{
    /// <summary>
    /// File Server Utility
    /// </summary>
    public class FileServerUtil
    {

        
        public class PathTypeEnum
        {
            public const string Absolute = "absolute";
            public const string WebRoot = "webroot";
            public const string Cdn = "cdn";
        }

        public static string GetFileServer(string path)
        {
            var httpContext = App.HttpContext;
            var request = httpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";
            return Path.Combine(baseUrl, path?.TrimStart('\\', '/') ?? "");
        }

        /// <summary>
        /// Lấy đường dẫn truy cập file lưu trữ
        /// </summary>
        /// <param name="pathType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static string GetPath(string pathType = PathTypeEnum.WebRoot)
        {

            var rootPath = App.Configuration["Upload:RootPath"];

            string baseUrl;
            var httpContext = App.HttpContext;
            switch (pathType)
            {
                case PathTypeEnum.Absolute:
                    //TODO: Implement this
                    baseUrl = "";
                    break;
                case PathTypeEnum.WebRoot:
                    if (httpContext == null)
                    {
                        throw new ArgumentNullException(nameof(httpContext), "HttpContext cannot be null for WebRoot path type");
                    }
                    var request = httpContext.Request;
                    baseUrl = $"{request.Scheme}://{request.Host}";
                    if (!string.IsNullOrEmpty(rootPath))
                        baseUrl = Path.Combine(baseUrl, "share");
                    break;
                case PathTypeEnum.Cdn:
                    //TODO implement this
                    baseUrl = "https://cdn.example.com/";
                    break;
                default:
                    throw new ArgumentException("Invalid path type");
            }

            return baseUrl;
        }


		/// <summary>
		/// Lấy đường dẫn truy cập file lưu trữ
		/// </summary>
        public static async Task<bool> UrlFileExistAsync(string url)
        {
            using(var httpClient = new HttpClient())
            {
                try
                {
                    var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
                    return response.IsSuccessStatusCode;
                }
				catch (HttpRequestException)
				{
					// Handle network errors or invalid URLs
					return false;
				}
				catch (Exception)
				{
					return false;
				}
			}
        }

	}
}