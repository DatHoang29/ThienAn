using Shared.DTO.Constants.Localization;

namespace Modules.WP.Core.Exceptions
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Định nghĩa các key localization (message/validation/exception/entity) cho module WP
    /// Created date: 03/07/2026
    /// </summary>
    internal class BaseMsg:BaseLocaleManager
    {
        /// <summary>
        /// Author: HieuNV
        /// Description: 
        /// Created date: 14/08/2026
        /// </summary>
        internal class WP
        {
            /// <summary>
            /// Author: HieuNV
            /// Description: Prefix
            /// Created date: 14/08/2026
            /// </summary>

            protected class Prefix
            {
                internal const string Exception = "lz.exception.wp.";
                internal const string Validation = "lz.validation.wp.";
                internal const string Message = "lz.message.wp.";
                internal const string Entity = "lz.entity.wp.";
            }

            /// <summary>
            /// Author: HieuNV
            /// Description: Generate validation messages
            /// Created date: 14/08/2026
            /// </summary>
            internal class Validation
            {
                internal const string MustContainPrefix = Prefix.Validation + "mustContainPrefix";
                internal const string MustBeValidDateRange = Prefix.Validation + "mustBeValidDateRange";
                internal const string DuplicateDate = Prefix.Validation + "duplicateDate";
                internal const string CanNotNull = Prefix.Validation + "canNotBeNull";
                /// <summary>Ngày ban hành phải trong khoảng từ {0} đến {1}.</summary>
                internal const string PublishTimeOutOfRange = Prefix.Validation + "publishTimeOutOfRange";


            }

            /// <summary>
            /// Author: HieuNV
            /// Description: Generate information messages
            /// Created date: 14/08/2026
            /// </summary>
            internal class Message
            {
                internal const string TestMessage = Prefix.Validation + "testMessage";
            }

            /// <summary>
            /// Author: HieuNV
            /// Description: Generate exception messages
            /// Created date: 14/08/2026
            /// </summary>
            internal class Exception
            {
                /// <summary>
                /// Cannot be deleted as it has associated product
                /// </summary>
                internal const string CannotDeleteHasEnable =  Prefix.Exception + "cannotDeleteHasEnable";
                internal const string PriceListInUseCannotDelete = Prefix.Exception + "priceListInUseCannotDelete";

                // ----- Chuyên mục (Category) -----
                /// <summary>
                /// Cannot set as featured. This category has no posts yet.
                /// </summary>
                internal const string CategoryHasNoPost = Prefix.Exception + "categoryHasNoPost";
                /// <summary>
                /// Cannot change the status. There must always be one featured category.
                /// </summary>
                internal const string MustKeepOneHeroCategory = Prefix.Exception + "mustKeepOneHeroCategory";
                /// <summary>
                /// Cannot delete the category that is currently featured.
                /// </summary>
                internal const string CannotDeleteHeroCategory = Prefix.Exception + "cannotDeleteHeroCategory";

                // ----- Bài viết (Post) -----
                /// <summary>
                /// A featured post must be in the published state.
                /// </summary>
                internal const string HeroPostMustBePublished = Prefix.Exception + "heroPostMustBePublished";
                /// <summary>
                /// Only one post can be featured.
                /// </summary>
                internal const string OnlyOneHeroPost = Prefix.Exception + "onlyOneHeroPost";
                /// <summary>
                /// Cannot change the status. There must always be one featured post.
                /// </summary>
                internal const string MustKeepOneHeroPost = Prefix.Exception + "mustKeepOneHeroPost";
                /// <summary>
                /// Cannot delete the post that is currently featured.
                /// </summary>
                internal const string CannotDeleteHeroPost = Prefix.Exception + "cannotDeleteHeroPost";
                /// <summary>
                /// Cannot set an archived post as featured.
                /// </summary>
                internal const string CannotFeatureArchivedPost = Prefix.Exception + "cannotFeatureArchivedPost";

                // ----- Bang gia (Price list) -----
                /// <summary>
                /// The price list is currently in effect and cannot be deactivated.
                /// </summary>
                internal const string ActivePriceListCannotBeDisabled = Prefix.Exception + "activePriceListCannotBeDisabled";
                /// <summary>
                /// There are {0} price lists sharing the same effective start date ({1}). Please review them.
                /// </summary>
                internal const string DuplicatePriceListEffectiveDate = Prefix.Exception + "duplicatePriceListEffectiveDate";
                /// <summary>
                /// No price list is currently in effect.
                /// </summary>
                internal const string NoPriceListInEffect = Prefix.Exception + "noPriceListInEffect";
                /// <summary>
                /// The current price list expires and no replacement covers the period from {0}. Please create a new price list.
                /// </summary>
                internal const string PriceListCoverageGap = Prefix.Exception + "priceListCoverageGap";

                // ----- Quy dinh giao thong (Traffic law) -----
                /// <summary>
                /// Only one traffic regulation can be published.
                /// </summary>
                internal const string OnlyOnePublishedTrafficLaw = Prefix.Exception + "onlyOnePublishedTrafficLaw";
                /// <summary>
                /// Cannot change the status. At least one traffic regulation must remain published.
                /// </summary>
                internal const string MustKeepOnePublishedTrafficLaw = Prefix.Exception + "mustKeepOnePublishedTrafficLaw";
                /// <summary>
                /// Only PDF files are allowed to be uploaded.
                /// </summary>
                internal const string OnlyPdfAllowed = Prefix.Exception + "onlyPdfAllowed";

                // ----- Dung chung trong module WP -----
                /// <summary>
                /// The file is invalid.
                /// </summary>
                internal const string InvalidFile = Prefix.Exception + "invalidFile";
            }

            /// <summary>
            /// Author: HieuNV
            /// Description: Generate entity messages
            /// Created date: 14/08/2026
            /// </summary>
            internal class Entity
            {
                internal const string Name = Prefix.Entity + "name";
                internal const string Remark = Prefix.Entity + "remark";

                internal const string Description = Prefix.Entity + "description";
                internal const string Content = Prefix.Entity + "content";
                internal const string VehicleType = Prefix.Entity + "vehicleType";
                internal const string EntryStation = Prefix.Entity + "entryStation";
                internal const string ExitStation = Prefix.Entity + "exitStation";
                internal const string Price = Prefix.Entity + "price";
                internal const string RefId = Prefix.Entity + "refId";
                internal const string Title = Prefix.Entity + "title";
                internal const string PublishTime = Prefix.Entity + "publishTime";
                internal const string Author = Prefix.Entity + "author";
                internal const string Thumbnail = Prefix.Entity + "thumbnail";
                internal const string OrderNo = Prefix.Entity + "orderNo";
                internal const string SysWebTitle = Prefix.Entity + "sysWebTitle";
                internal const string SysWebAddress = Prefix.Entity + "sysWebAddress";
                internal const string SysWebDescription = Prefix.Entity + "sysWebDescription";
                internal const string SysWebTelephone = Prefix.Entity + "sysWebTelephone";
                internal const string SysWebCopyright = Prefix.Entity + "sysWebCopyright";
                internal const string SysWebLogo = Prefix.Entity + "sysWebLogo";
                internal const string SysWebRepresent = Prefix.Entity + "sysWebRepresent";
                internal const string SysWebEmail = Prefix.Entity + "sysWebEmail";
                internal const string SysWebImg1 = Prefix.Entity + "sysWebImg1";
                internal const string SysWebImg2 = Prefix.Entity + "sysWebImg2";
                internal const string SysWebImg3 = Prefix.Entity + "sysWebImg3";
                internal const string SysWebImgTitle1 = Prefix.Entity + "sysWebImgTitle1";
                internal const string SysWebImgTitle2 = Prefix.Entity + "sysWebImgTitle2";
                internal const string SysWebImgTitle3 = Prefix.Entity + "sysWebImgTitle3";
                internal const string SysWebOffice = Prefix.Entity + "sysWebOffice";
                internal const string SysOfficeAddress = Prefix.Entity + "sysOfficeAddress";
                internal const string SysOfficeTelephone = Prefix.Entity + "sysOfficeTelephone";
                internal const string SysBusinessLicense = Prefix.Entity + "sysBusinessLicense";



            }

        }

    }

}
