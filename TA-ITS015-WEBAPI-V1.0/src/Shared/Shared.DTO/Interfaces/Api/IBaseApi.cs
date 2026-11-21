using System.Net.Http;

namespace Shared.DTO.Interfaces.Api
{
    public  interface IBaseApi
    {
         HttpClient Client { get;  }

    }
}
