namespace Shared.Utility.Endpoints.Sample
{
    /// <summary>
    /// Sample endpoint class
    /// </summary>
    public class SampleEndpoint
    {
        /// <summary>
        /// Sample Category endpoint
        /// </summary>
        public class Category
        {
            public const string Base = "/api/sample/sampcategory";
            public const string Version2 = "/api/v2/sample/sampcategory";
        }

        public class Product
        {
            public const string Base = "/api/sample/sampproduct";
        }

    }
}
