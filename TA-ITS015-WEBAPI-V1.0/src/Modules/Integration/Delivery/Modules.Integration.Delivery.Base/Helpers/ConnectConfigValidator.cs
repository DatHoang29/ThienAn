using System.Text.Json;
using Modules.Integration.Delivery.Base.Constants;
using Modules.Integration.Delivery.Base.Enums;

namespace Modules.Integration.Delivery.Base.Helpers
{
    /// <summary>
    /// Author: DatHQ
    /// Description: Bộ kiểm tra cấu hình kết nối đối tác (ConnectConfig),
    ///              thuần BCL, dùng chung cho cả WebAPI và Worker theo chuẩn Layer Separation.
    /// Created date: 16/10/2026
    /// </summary>
    public static class ConnectConfigValidator
    {
        public static readonly HashSet<string> ColumnReservedKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "BaseUrl", "Url", "Host", "Server", "Address", "IP", "Port", "Endpoint", "Path", "EndPointApiUrl"
        };

        /// <summary>
        /// Author: DatHQ
        /// Description: Kiểm ConnectionConfig, chỉ trả loại lỗi.
        /// Created date: 16/10/2026
        /// </summary>
        public static ConnectConfigIssue Check(string? configStr) => Check(configStr, out _);

        /// <summary>
        /// Author: DatHQ
        /// Description: Kiểm ConnectionConfig gồm cả giá trị, detail cho biết khóa hoặc câu lỗi cụ thể.
        /// Created date: 16/10/2026
        /// </summary>
        public static ConnectConfigIssue Check(string? configStr, out string? detail)
            => Validate(configStr, validateValues: true, out detail);

        /// <summary>
        /// Author: DatHQ
        /// Description: Trống là hợp lệ. Có nội dung thì phải là JSON object, khóa không trống, không trùng nhau, không trùng cột của đối tác, giá trị không lồng mảng hay object.
        ///              validateValues = true thì kiểm thêm khóa đã bỏ như OutputPath và giá trị từng khóa.
        /// Created date: 16/10/2026
        /// </summary>
        private static ConnectConfigIssue Validate(string? configStr, bool validateValues, out string? detail)
        {
            detail = null;
            if (string.IsNullOrWhiteSpace(configStr))
            {
                return ConnectConfigIssue.None;
            }

            var trimmed = configStr.Trim();
            if (!trimmed.StartsWith('{') || !trimmed.EndsWith('}'))
            {
                return ConnectConfigIssue.NotJsonObject;
            }

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(trimmed);
            }
            catch (JsonException ex)
            {
                detail = ex.Message;
                return ConnectConfigIssue.SyntaxError;
            }

            using (doc)
            {
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return ConnectConfigIssue.NotJsonObject;
                }

                var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    var name = prop.Name;
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        return ConnectConfigIssue.EmptyKey;
                    }

                    name = name.Trim();

                    if (ColumnReservedKeys.Contains(name))
                    {
                        detail = name;
                        return ConnectConfigIssue.ReservedKey;
                    }

                    if (validateValues && string.Equals(name, ConnectKeys.OutputPath, StringComparison.OrdinalIgnoreCase))
                    {
                        detail = name;
                        return ConnectConfigIssue.ObsoleteKey;
                    }

                    if (!seenKeys.Add(name))
                    {
                        detail = name;
                        return ConnectConfigIssue.DuplicateKey;
                    }

                    if (prop.Value.ValueKind is JsonValueKind.Array or JsonValueKind.Object)
                    {
                        detail = name;
                        return ConnectConfigIssue.NestedValue;
                    }

                    if (!validateValues)
                    {
                        continue;
                    }

                    var valStr = prop.Value.ValueKind switch
                    {
                        JsonValueKind.String => prop.Value.GetString(),
                        JsonValueKind.Number => prop.Value.GetRawText(),
                        JsonValueKind.True => "true",
                        JsonValueKind.False => "false",
                        JsonValueKind.Null => null,
                        _ => null
                    };

                    if ((string.Equals(name, ConnectKeys.TimeoutSeconds, StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(name, ConnectKeys.HttpTimeoutSec, StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(name, ConnectKeys.TimeoutSec, StringComparison.OrdinalIgnoreCase)) && !string.IsNullOrWhiteSpace(valStr))
                    {
                        if (!int.TryParse(valStr, out var timeout) || timeout <= 0)
                        {
                            detail = valStr;
                            return ConnectConfigIssue.InvalidTimeout;
                        }
                    }
                    else if ((string.Equals(name, ConnectKeys.BypassSsl, StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(name, ConnectKeys.IgnoreSsl, StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(name, ConnectKeys.Insecure, StringComparison.OrdinalIgnoreCase)) && !string.IsNullOrWhiteSpace(valStr))
                    {
                        if (!bool.TryParse(valStr, out _))
                        {
                            detail = $"{name} [{valStr}]";
                            return ConnectConfigIssue.InvalidBoolean;
                        }
                    }
                    else if (string.Equals(name, ConnectKeys.AuthType, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(valStr))
                    {
                        if (!AuthTypes.IsSupported(valStr))
                        {
                            detail = valStr;
                            return ConnectConfigIssue.UnsupportedAuthType;
                        }
                    }
                    else if ((string.Equals(name, ConnectKeys.InboundEncryptor, StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(name, ConnectKeys.Encryptor, StringComparison.OrdinalIgnoreCase)) && !string.IsNullOrWhiteSpace(valStr))
                    {
                        if (!EncryptNames.IsSupported(valStr))
                        {
                            detail = $"{name} [{valStr}]";
                            return ConnectConfigIssue.UnsupportedEncryptor;
                        }
                    }
                    else if (string.Equals(name, ConnectKeys.FileNamePattern, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(valStr))
                    {
                        if (!FileNameTokens.HasRequiredToken(valStr))
                        {
                            detail = valStr;
                            return ConnectConfigIssue.FileNamePatternNoToken;
                        }
                    }
                }
            }

            return ConnectConfigIssue.None;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Kiểm cấu trúc JSON phẳng của ConnectionConfig (6 lỗi chặn lưu); không kiểm giá trị từng khóa.
        /// Created date: 18/10/2026
        /// </summary>
        public static ConnectConfigIssue CheckStructure(string? configStr)
            => Validate(configStr, validateValues: false, out _);
    }
}
