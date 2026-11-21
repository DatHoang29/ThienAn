using Shared.DTO.Constants.Localization;

namespace Modules.VMS.Core.Exceptions
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 01/12/2024
    /// </summary>
    public class BaseMsg : BaseLocaleManager
    {
        public class Vms
        {
            public class Prefix
            {
                public const string Message = "lz.message.vms.";
                public const string Validation = "lz.validation.vms.";
                public const string Exception = "lz.exception.vms.";
                public const string Entity = "lz.entity.vms.";
            }

            public class Entity
            {
                /// <summary>Hãng thiết bị (Vendor)</summary>
                public const string Vendor = Prefix.Entity + "vendor";
            }

            public class Validation
            {
                public const string NoContent = Prefix.Validation + "noContent";
            }
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: VmsImage Msg
        /// Created date: 01/12/2024
        /// </summary>
        public class VmsImage
        {
            public class Prefix
            {
                public const string Message = "lz.message.vmsImage.";
                public const string Validation = "lz.validation.vmsImage.";
                public const string Exception = "lz.exception.vmsImage.";
                public const string Entity = "lz.entity.vmsImage.";
            }

            public class Entity
            {
                public const string Size = Prefix.Entity + "size";
            }

            public class Validation
            {
                public const string SizeIsRequired = Prefix.Entity + "sizeIsRequired";
                public const string SizeIsInvalid = Prefix.Entity + "sizeIsInvalid";
            }
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: VmsMessage Msg
        /// Created date: 01/12/2024
        /// </summary>
        public class VmsMessage
        {
            public class Prefix
            {
                public const string Message = "lz.message.vmsMessage.";
                public const string Validation = "lz.validation.vmsMessage.";
                public const string Exception = "lz.exception.vmsMessage.";
                public const string Entity = "lz.entity.vmsMessage.";
            }

            public class Entity
            {
                public const string Content1 = Prefix.Entity + "content1";
                public const string Content2 = Prefix.Entity + "content2";
                public const string Content3 = Prefix.Entity + "content3";
            }
        }

        /// <summary>
        /// VMS Template Message
        /// Author: SonTH
        /// </summary>
        public class VmsTemplate
        {
            protected class Prefix
            {
                public const string Entity = "lz.entity.vmsTemplate.";
                public const string Validation = "lz.validation.vmsTemplate.";
                public const string Exception = "lz.exception.vmsTemplate.";
                public const string Message = "lz.message.vmsTemplate.";
            }
            public class Entity
            {
                public const string Size = Prefix.Entity + "size";
                public const string JsonData = Prefix.Entity + "jsonData";
                public const string RowNo = Prefix.Entity + "rowNo";
                public const string ImageNo = Prefix.Entity + "imageNo";
            }

            public class Exception
            {
                //Invalid template size format. Expected format: widthxheight
                public const string SizeIncorrectFormat = Prefix.Exception + "sizeIncorrectFormat";
                //Invalid template size values. Width and height must be integers
                public const string SizeIncorrectValue = Prefix.Exception + "sizeIncorrectValue";
                //Failed to parse template JSON.
                public const string JsonIncorrect = Prefix.Exception + "jsonIncorrect";
                //Error generating image: {0}
                public const string GenerateError = Prefix.Exception + "generateError";
            }
        }


		public class VmsCurrent : BaseMsg
		{
			protected class Prefix
			{
				public const string Message = "lz.message.vmsCurrent.";
				public const string Validation = "lz.validation.vmsCurrent.";
				public const string Exception = "lz.exception.vmsCurrent.";
				public const string Entity = "lz.entity.vmsCurrent.";
			}

			public class Entity
			{
				public const string RowData = Prefix.Entity + "rowData";
				public const string DefaultRowData = Prefix.Entity + "defaultRowData";
				public const string Url = Prefix.Entity + "url";
				public const string DefaultUrl = Prefix.Entity + "defaultUrl";
				public const string Size = Prefix.Entity + "size";
				public const string Font = Prefix.Entity + "font";
				public const string DefaultFont = Prefix.Entity + "defaultFont";
				public const string FontSize = Prefix.Entity + "fontSize";
				public const string Color = Prefix.Entity + "color";
				public const string DefaultColor = Prefix.Entity + "defaultColor";
                public const string BackColor = Prefix.Entity + "backColor";
                public const string DefaultBackColor = Prefix.Entity + "defaultBackColor";
				public const string ExecutedBy = Prefix.Entity + "executedBy";
				public const string ExecutedDate = Prefix.Entity + "executedDate";
				public const string Priority = Prefix.Entity + "priority";
			}
			
		}

		public class VmsEventDefault : BaseMsg
		{
			protected class Prefix
			{
				public const string Message = "lz.message.vmsEventDefault.";
				public const string Validation = "lz.validation.vmsEventDefault.";
				public const string Exception = "lz.exception.vmsEventDefault.";
				public const string Entity = "lz.entity.vmsEventDefault.";
			}

			public class Entity
			{
				public const string EventTypeId = Prefix.Entity + "eventTypeId";
				public const string EquipmentTypeId = Prefix.Entity + "equipmentTypeId";
				public const string RowData = Prefix.Entity + "rowData";
				public const string Url = Prefix.Entity + "url";
				public const string DefaultConfig = Prefix.Entity + "defaultConfig";
			}

			public class Validation
			{
				public const string EquipmentTypeInvalid = Prefix.Validation + "equipmentTypeInvalid";
			}
		}

		public class VmsProcess : BaseMsg
		{
			protected class Prefix
			{
				public const string Message = "lz.message.vmsProcess.";
				public const string Validation = "lz.validation.vmsProcess.";
				public const string Exception = "lz.exception.vmsProcess.";
				public const string Entity = "lz.entity.vmsProcess.";
			}

			public class Entity
			{
				public const string Brightness = Prefix.Entity + "brightness";
				public const string State = Prefix.Entity + "state";
				public const string Type = Prefix.Entity + "type";
				public const string Priority = Prefix.Entity + "priority";
                public const string UrlImage = Prefix.Entity + "urlImage";
			    public const string VmsId = Prefix.Entity + "vmsId";
				public const string TimeStart = Prefix.Entity + "timeStart";
				public const string TimeEnd = Prefix.Entity + "timeEnd";
			}

			public class Validation
			{
				public const string TimeStartAndTimeEndCanNotBeTheSame = Prefix.Validation + "timeStartAndTimeEndCanNotBeTheSame";
				public const string NoDeviceSelected = Prefix.Validation + "noDeviceSelected";
			}
		}

		public class VmsProcessImg : BaseMsg
		{
			protected class Prefix
			{
				public const string Message = "lz.message.vmsProcessImg.";
				public const string Validation = "lz.validation.vmsProcessImg.";
				public const string Exception = "lz.exception.vmsProcessImg.";
				public const string Entity = "lz.entity.vmsProcessImg.";
			}

			public class Entity
			{
				public const string Url = Prefix.Entity + "url";
				public const string TimeToLive = Prefix.Entity + "timeToLive";
			}

		}
	}

    // TODO: CODE NGƯỜI KHÁC => CHECK SAU ========================================================

    public class VmsTemplateMsg : BaseMsg
    {
        //VmsImage {
        // ID varchar(64) pk unique
        // Code string(64) null
        // Name string(64) null
        // Lang string(16) null def(vi-VN)
        // Type string(16) null def(icon)
        // Url string(256) null
        // Size string(16) null
        // OrderNo integer null
        // Status integer null
        //}

        public const string _Root = "be.";

        public static class Entity
        {
            public const string JsonData = _Root + "vmsMessage.jsonData";
            public const string Size = _Root + "vmsMessage.size";
        }
    }
    public class VmsProcessMsg : BaseMsg
    {
        //VmsImage {
        // ID varchar(64) pk unique
        // Code string(64) null
        // Name string(64) null
        // Lang string(16) null def(vi-VN)
        // Type string(16) null def(icon)
        // Url string(256) null
        // Size string(16) null
        // OrderNo integer null
        // Status integer null
        //}

        public const string _Root = "be.";

        public static class Entity
        {
            public const string VmsId = _Root + "vmsMessage.vmsId";
        }
    }
    public class VmsProcessImgMsg : BaseMsg
    {
        //VmsImage {
        // ID varchar(64) pk unique
        // Code string(64) null
        // Name string(64) null
        // Lang string(16) null def(vi-VN)
        // Type string(16) null def(icon)
        // Url string(256) null
        // Size string(16) null
        // OrderNo integer null
        // Status integer null
        //}

        public const string _Root = "be.";

        public static class Entity
        {
            public const string ProcesId = _Root + "vmsMessage.procesId";
        }
    }
  
}