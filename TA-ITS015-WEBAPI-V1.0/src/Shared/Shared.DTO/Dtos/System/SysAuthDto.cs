using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.DTO.Dtos.Base;
using Shared.DTO.Enums;

namespace Shared.DTO.Dtos.System
{
    public class SysAuthDto : BaseEntityDto
    {
        public string Id { get; set; }

        /// <summary>
        /// Account Name
        /// </summary>
        public string Account { get; set; }

        /// <summary>
        /// Real Name
        /// </summary>
        public string RealName { get; set; }

        /// <summary>
        /// Phone Number
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// ID Card Number
        /// </summary>
        public string IdCardNum { get; set; }

        /// <summary>
        /// Email
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Account Type
        /// </summary>
        public string AccountType { get; set; } = BaseEnums.AccountTypeEnum.Guest.ToString();

        /// <summary>
        /// Avatar
        /// </summary>
        public string Avatar { get; set; }

        /// <summary>
        /// Introduction
        /// </summary>
        public string Introduction { get; set; }

        /// <summary>
        /// Address
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Signature
        /// </summary>
        public string Signature { get; set; }

        /// <summary>
        /// Organization Id
        /// </summary>
        public string OrgId { get; set; }

        /// <summary>
        /// Organization Name
        /// </summary>
        public string OrgName { get; set; }

        /// <summary>
        /// Organization Type
        /// </summary>
        public string OrgType { get; set; }

        /// <summary>
        /// Organization Code
        /// </summary>
        public string OrgCode { get; set; }

        /// <summary>
        /// Position Name
        /// </summary>
        public string PosName { get; set; }

        /// <summary>
        /// Button Permissions Collection
        /// </summary>
        public List<string> Buttons { get; set; }

        /// <summary>
        /// Role Collection
        /// </summary>
        public List<string> RoleIds { get; set; }

        /// <summary>
        /// Watermark Text
        /// </summary>
        public string WatermarkText { get; set; }
        /// <summary>
        /// Default menu
        /// </summary>
        public string DefaultMenu { get; set; }
    }
}
