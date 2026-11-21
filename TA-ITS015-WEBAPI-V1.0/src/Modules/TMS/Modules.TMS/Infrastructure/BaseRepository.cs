using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Infrastructure
{
    public class BaseRepository<T> : SqlSugarRepository<T> where T : class, new()
    {
        public BaseRepository() : base() { }
        /// Author: KhoaND
        /// Description:
        ///     -thêm Insert và update cho vmsCurrent
        /// Created date: 23/10/2024
        ITenant iTenant = SqlSugarSetup.ITenant;



        //public async Task DeletemapDetailContacts(TmsMapDetail tmsDetailContacts)
        //{
        //    base.Context = iTenant.GetConnectionScope("Default");
        //    await base.Context.Deleteable(tmsDetailContacts).ExecuteCommandAsync();

        //}
    }

}
