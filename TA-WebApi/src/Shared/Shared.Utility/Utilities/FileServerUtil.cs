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
    }
}