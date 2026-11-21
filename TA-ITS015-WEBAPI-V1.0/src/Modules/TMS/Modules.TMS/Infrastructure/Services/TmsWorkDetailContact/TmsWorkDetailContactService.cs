using Microsoft.AspNetCore.Hosting;
using Modules.TMS.Core.Entities;

namespace Modules.TMS.Infrastructure.Services.TmsWorkDetailContact
{
    public class TmsWorkDetailContactService : IScoped
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private BaseRepository<TmsWorkDetailContacts> _TmsWorkDetailContact;
        public TmsWorkDetailContactService(IWebHostEnvironment hostingEnvironment, BaseRepository<TmsWorkDetailContacts> TmsWorkDetailContact
            )
        {
            _hostingEnvironment = hostingEnvironment;
            _TmsWorkDetailContact = TmsWorkDetailContact;
        }

        public async Task deleteworkDetailContacts(TmsWorkDetailContacts workDetailContacts)
        {
            var result = await _TmsWorkDetailContact.DeleteAsync(c => c.ID == workDetailContacts.ID);

        }



    }
}
