using Modules.Notification.Core.Constants;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.Notification.Infrastructure
{
    /// <summary>
    /// Author: SonTH
    /// Description: Repository dùng chung của phân hệ Notification.
    ///
    ///              Phân hệ nằm trên CSDL RIÊNG (ConfigId "NotiDefault", khai trong Database.json),
    ///              nên ctor trỏ Context về connection đó — cùng pattern được dùng trong
    ///              Modules.TMS/Infrastructure/BaseRepository (GetConnectionScope).
    ///              Mọi BaseRepository<T> của module Inject về đây đều tự động đúng DB,
    ///              caller không phải chọn connection.
    /// Created date: 17/09/2026
    /// </summary>
    public class BaseRepository<T> : SqlSugarRepository<T> where T : class, new()
    {
        public BaseRepository() : base(NotiConst.DbConfigId)
        {
        }
    }

    public class BaseDefaultRepository<T> : SqlSugarRepository<T> where T : class, new()
    {
        public BaseDefaultRepository() : base()
        {
        }
    }
}
