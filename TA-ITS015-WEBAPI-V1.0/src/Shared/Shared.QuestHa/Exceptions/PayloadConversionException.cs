using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Shared.QuestHa.Exceptions;

/// <summary>
/// Author: KienLT
/// Description: Payload không chuyển được sang kiểu cột ⇒ lỗi DỮ LIỆU (quarantine).
/// Created date: 09/10/2026
/// </summary>
internal sealed class PayloadConversionException : Exception
{
    /// <summary>Tạo lỗi chuyển payload journal sang dòng QuestDB.</summary>
    public PayloadConversionException(string message, Exception? inner = null) : base(message, inner) { }
}
