namespace Modules.Integration.Delivery.Base.Enums
{
    /// <summary>
    /// Author: DatHQ
    /// Description: Danh mục các vấn đề (mã lỗi kiểm tra tính hợp lệ) của chuỗi cấu hình kết nối đối tác.
    /// Created date: 16/10/2026
    /// </summary>
    public enum ConnectConfigIssue
    {
        None = 0,
        NotJsonObject,
        SyntaxError,
        EmptyKey,
        ReservedKey,
        ObsoleteKey,
        DuplicateKey,
        NestedValue,
        InvalidTimeout,
        InvalidBoolean,
        UnsupportedAuthType,
        UnsupportedEncryptor,
        FileNamePatternNoToken
    }
}
