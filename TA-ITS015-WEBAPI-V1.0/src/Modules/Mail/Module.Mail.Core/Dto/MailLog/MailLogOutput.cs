using Shared.Core.Utilities;
using Shared.DTO.Enums;
using Modules.Mail.Core.Constants;
using Modules.Mail.Core.Entities;

namespace Modules.Mail.Core.Dto.LogMail
{
    public class MailLogOutput : MailLog
    {
        [SugarColumn(IsIgnore = true)]
        public string? ScheduleCode { get; set; }

        [SugarColumn(IsIgnore = true)]
        public string? ScheduleName { get; set; }
    }
}
