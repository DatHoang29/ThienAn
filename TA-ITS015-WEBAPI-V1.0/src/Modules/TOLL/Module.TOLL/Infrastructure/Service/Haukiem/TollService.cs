using NATS.Client.JetStream;
using Shared.DTO.Constants.Localization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Module.TOLL.Infrastructure.Service.Haukiem
{
    public class TollService
    {
        public TollService() { }

        /// <summary>
        /// Kiểm tra DateFrom và DateTo hợp lệ cho add và update - Kế toán
        /// Author: ChuongLP
        /// Date: 24/07/2026
        /// </summary>
        public void CheckTimeIsValid(DateTime? dateFrom, DateTime? dateTo)
        {
            if (dateTo?.Date < DateTime.Now.Date)
                throw Oops.Oh(TcsManager.Message.ExpirationDateMustBeGreaterThanToday);

            if (dateTo?.Date <= dateFrom?.Date)
                throw Oops.Oh(TcsManager.Message.ExpirationDateMustBeGreaterThanStartDate);
        }

        /// <summary>
        /// Kiểm tra DateFrom và DateTo hợp lệ để tìm kiếm
        /// Author: ChuongLP
        /// Date: 24/07/2026
        /// </summary>
        public void CheckDateForSearch(DateTime? dateFrom, DateTime? dateTo, bool isCheck7Date = false, string trangThaiRa = "")
        {
            if (dateTo.HasValue && dateFrom.HasValue && dateTo < dateFrom)
                throw Oops.Oh(TcsManager.Message.EndDateMustBeGreaterThanStartDate);

            if (isCheck7Date)
            {
                if ((dateTo?.Date - dateFrom?.Date)?.TotalDays > 7)
                    throw Oops.Oh(TcsManager.Exception.DateRangeMustNotExceed7Days);
            }

            if (trangThaiRa == "1")
            {
                if ((dateTo?.Date - dateFrom?.Date)?.TotalDays > 31)
                    throw Oops.Oh(TcsManager.Exception.DateRangeMustNotExceed30Days);
            }
        }

        ///// <summary>
        ///// Lấy text trạng thái hậu kiểm xuất ra excel
        ///// Author: ChuongLP
        ///// Date: 24/07/2026
        ///// </summary>
        ///// <param name="status"></param>
        ///// <returns></returns>
        //public string GetTrangThaiHauKiemText(string status)
        //{
        //    switch (status)
        //    {
        //        case AcsEnums.AcsTrangThaiHauKiemConstants.DaHauKiemID:
        //            return AcsEnums.AcsTrangThaiHauKiemConstants.DaHauKiemText;
        //        default:
        //            return AcsEnums.AcsTrangThaiHauKiemConstants.ChuaHauKiemText;
        //    }
        //}
    }
}
