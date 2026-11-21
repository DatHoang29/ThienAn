using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.DTO.Dtos.Base;

namespace Shared.DTO.Dtos.CfgSystem.CfgSysTerminology
{
    public class CfgSysTerminologyDto: BaseEntityDto
    {

        public string Name { get; set; }

        public string Code { get; set; }

        public string Value { get; set; }

        public string Lang { get; set; }

        public string Language { get; set; }

        public string Status { get; set; }

    }
}
