using System;

namespace Shared.DTO.ResponseWrapper
{
    public interface IWBaseResult
    {
        string Message { get; set; }
        int Code { get; set; }
        bool Succeeded { get; set; }
        DateTime Time { get; set; }

    }
    public interface IWResult<out T> : IWBaseResult
    {
        T Result { get; }
    }


}