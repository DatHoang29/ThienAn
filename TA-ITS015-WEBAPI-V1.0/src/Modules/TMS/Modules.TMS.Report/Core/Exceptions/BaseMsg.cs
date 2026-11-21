using Shared.DTO.Constants.Localization;

namespace Modules.TMS.Report.Core.Exceptions
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 05/12/2024
    /// </summary>
    internal class BaseMsg : BaseLocaleManager
    {
        internal class RptBase
        {
            protected class Prefix
            {
                internal const string Label = "lz.label.base.";
            }

            internal class Label
            {
                internal const string Idx = Prefix.Label + "idx"; // STT
                internal const string Page = Prefix.Label + "page"; // Trang
                internal const string Total = Prefix.Label + "total"; // Tổng
                internal const string Amount = Prefix.Label + "amount"; // Số lượng

                internal const string Time = Prefix.Label + "time"; // Thời gian
                internal const string Day = Prefix.Label + "day"; // Ngày
                internal const string Month = Prefix.Label + "month"; // Tháng
                internal const string Year = Prefix.Label + "year"; // Năm
            }
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: RptIncidentIndex Msg
        /// </summary>
        internal class RptIncidentIndex
        {
            internal class Prefix
            {
                internal const string Exception = "lz.exception.rptIncidentIndex.";
                internal const string Validation = "lz.validation.rptIncidentIndex.";
                internal const string Message = "lz.message.rptIncidentIndex.";
                internal const string Entity = "lz.entity.rptIncidentIndex.";
            }

            internal class Entity
            {
                internal const string EventType = Prefix.Entity + "eventType";
                internal const string Source = Prefix.Entity + "source";
                internal const string FromDate = Prefix.Entity + "fromDate";
                internal const string ToDate = Prefix.Entity + "toDate";
            }
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: RptTrafficVolume Msg
        /// </summary>
        internal class RptTrafficVolume
        {
            internal class Prefix
            {
                internal const string Exception = "lz.exception.rptTrafficVolume.";
                internal const string Validation = "lz.validation.rptTrafficVolume.";
                internal const string Message = "lz.message.rptTrafficVolume.";
                internal const string Entity = "lz.entity.rptTrafficVolume.";
            }

            internal class Entity
            {
                internal const string Type = Prefix.Entity + "type";
                internal const string Shift = Prefix.Entity + "shift";
                internal const string Month = Prefix.Entity + "month";
                internal const string Year = Prefix.Entity + "year";
                internal const string Date = Prefix.Entity + "date";
            }
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: RptTrafficVolume Msg
        /// </summary>
        internal class RptEquipmentHistory
        {
            internal class Prefix
            {
                internal const string Exception = "lz.exception.rptEquipmentHistory.";
                internal const string Validation = "lz.validation.rptEquipmentHistory.";
                internal const string Message = "lz.message.rptEquipmentHistory.";
                internal const string Entity = "lz.entity.rptEquipmentHistory.";
            }

            internal class Entity
            {
                internal const string EquipmentType = Prefix.Entity + "equipmentType";
                internal const string Equipment = Prefix.Entity + "equipment";
                internal const string State = Prefix.Entity + "state";
                internal const string FromDate = Prefix.Entity + "fromDate";
                internal const string ToDate = Prefix.Entity + "toDate";
            }
        }

		/// <summary>
		/// Author: Tuan HA
		/// Description: RptTrafficAnalysis Msg
		/// </summary>
		internal class RptTrafficAnalysis
		{
			internal class Prefix
			{
				internal const string Exception = "lz.exception.tmsTrafficAnalysis.";
				internal const string Validation = "lz.validation.tmsTrafficAnalysis.";
				internal const string Message = "lz.message.tmsTrafficAnalysis.";
				internal const string Entity = "lz.entity.tmsTrafficAnalysis.";
			}

			internal class Entity
			{
				internal const string LengthFrom = Prefix.Entity + "lengthFrom";
				internal const string LengthTo = Prefix.Entity + "lengthTo";
				internal const string LicensePlate = Prefix.Entity + "licensePlate";
			}
		}

        /// <summary>
        /// Author: ThangNN
        /// Description: RptTrafficStatistic Msg
        /// </summary>
        internal class RptTrafficStatistic
        {
            internal class Prefix
            {
                internal const string Exception = "lz.exception.rptTrafficStatistic.";
                internal const string Validation = "lz.validation.rptTrafficStatistic.";
                internal const string Message = "lz.message.rptTrafficStatistic.";
                internal const string Entity = "lz.entity.rptTrafficStatistic.";
                internal const string Label = "lz.label.rptTrafficStatistic.";
            }

            internal class Validation
            {
                internal const string LimitHourRange = Prefix.Validation + "limitHourRange";
                internal const string LimitDateRange = Prefix.Validation + "limitDateRange";
            }

            internal class Entity
            {
                internal const string EquipmentId = Prefix.Entity + "equipmentId";
                internal const string FromTime = Prefix.Entity + "fromTime";
                internal const string AvgSpeed = Prefix.Entity + "avgSpeed";
                internal const string TotalVehicleNumber = Prefix.Entity + "totalVehicleNumber";
                internal const string SpaceOccupy = Prefix.Entity + "spaceOccupy";
            }

            internal class Label
            {
                internal const string SectionRptTitle = Prefix.Label + "sectionRptTitle";
                internal const string ZoneRptTitle = Prefix.Label + "zoneRptTitle";
                internal const string Section = Prefix.Label + "section";
                internal const string Zone = Prefix.Label + "zone";
            }
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: RptIncidentList Msg
        /// </summary>
        internal class RptIncidentList
        {
            internal class Prefix
            {
                internal const string Exception = "lz.exception.rptIncidentList.";
                internal const string Validation = "lz.validation.rptIncidentList.";
                internal const string Message = "lz.message.rptIncidentList.";
                internal const string Entity = "lz.entity.rptIncidentList.";
                internal const string Label = "lz.label.rptIncidentList.";
            }

            internal class Entity
            {
                internal const string Name = Prefix.Entity + "name";
                internal const string Code = Prefix.Entity + "code";
                internal const string StartDate = Prefix.Entity + "startDate";
                internal const string FinishedDate = Prefix.Entity + "finishedDate";
                internal const string EventType = Prefix.Entity + "eventType";
                internal const string Position = Prefix.Entity + "position";
                internal const string Source = Prefix.Entity + "source";
                internal const string TrafficPlan = Prefix.Entity + "trafficPlan";
                internal const string State = Prefix.Entity + "state";
            }

            internal class Label
            {
                internal const string RptTitle = Prefix.Label + "rptTitle";
            }
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: RptIncidentDetail Msg
        /// </summary>
        internal class RptIncidentDetail
        {
            internal class Prefix
            {
                internal const string Exception = "lz.exception.rptIncidentDetail.";
                internal const string Validation = "lz.validation.rptIncidentDetail.";
                internal const string Message = "lz.message.rptIncidentDetail.";
                internal const string Entity = "lz.entity.rptIncidentDetail.";
                internal const string Label = "lz.label.rptIncidentDetail.";
            }

            internal class Entity
            {
                internal const string TemplateId = Prefix.Entity + "templateId";
                internal const string Version = Prefix.Entity + "version";

                internal const string Name = Prefix.Entity + "name";
                internal const string Code = Prefix.Entity + "code";
                internal const string StartDate = Prefix.Entity + "startDate";
                internal const string FinishedDate = Prefix.Entity + "finishedDate";
                internal const string EventType = Prefix.Entity + "eventType";
                internal const string Position = Prefix.Entity + "position";
                internal const string Zone = Prefix.Entity + "zone";
                internal const string Source = Prefix.Entity + "source";
                internal const string TrafficPlan = Prefix.Entity + "trafficPlan";
                internal const string State = Prefix.Entity + "state";

                internal const string WorkName = Prefix.Entity + "workName";
                internal const string WorkStep = Prefix.Entity + "workStep";
                internal const string WorkState = Prefix.Entity + "workState";
                internal const string WorkExecuteOrg = Prefix.Entity + "executeOrg";
            }

            internal class Label
            {
                internal const string RptTitle = Prefix.Label + "rptTitle";
                internal const string RptIncidentTitle = Prefix.Label + "rptIncidentTitle";
                internal const string RptWorkExecuteTitle = Prefix.Label + "rptWorkExecuteTitle";
                internal const string Information = Prefix.Label + "information";
            }
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: RptTrafficVolumeDay Msg
        /// Created date: 12/03/2025
        /// </summary>
        internal class RptTrafficVolumeDay
        {
            internal class Prefix
            {
                internal const string Exception = "lz.exception.rptTrafficVolumeDay.";
                internal const string Validation = "lz.validation.rptTrafficVolumeDay.";
                internal const string Message = "lz.message.rptTrafficVolumeDay.";
                internal const string Entity = "lz.entity.rptTrafficVolumeDay.";
                internal const string Label = "lz.label.rptTrafficVolumeDay.";
            }

            internal class Entity
            {
                internal const string Direction = Prefix.Entity + "direction";
                internal const string ShortLength = Prefix.Entity + "shortLength";
                internal const string MediumLength = Prefix.Entity + "mediumLength";
                internal const string LongLength = Prefix.Entity + "longLength";
            }

            internal class Label
            {
                internal const string RptTitle = Prefix.Label + "rptTitle";
                internal const string RptSubTitle = Prefix.Label + "rptSubTitle";
                internal const string ShiftTotal = Prefix.Label + "shiftTotal";
                internal const string HourTotal = Prefix.Label + "hourTotal";

            }
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: RptTrafficVolumeMonth Msg
        /// Created date: 12/03/2025
        /// </summary>
        internal class RptTrafficVolumeMonth
        {
            internal class Prefix
            {
                internal const string Exception = "lz.exception.rptTrafficVolumeMonth.";
                internal const string Validation = "lz.validation.rptTrafficVolumeMonth.";
                internal const string Message = "lz.message.rptTrafficVolumeMonth.";
                internal const string Entity = "lz.entity.rptTrafficVolumeMonth.";
                internal const string Label = "lz.label.rptTrafficVolumeMonth.";
            }

            internal class Label
            {
                internal const string RptTitle = Prefix.Label + "rptTitle";
                internal const string ShiftTotal = Prefix.Label + "shiftTotal";
                internal const string DirTotal = Prefix.Label + "dirTotal";
                internal const string DayTotal = Prefix.Label + "dayTotal";
                internal const string MonthTotal = Prefix.Label + "monthTotal";
            }
        }

        /// <summary>
        /// Author: KienLT
        /// Description: RptOverSpeed Msg — báo cáo phương tiện vượt quá tốc độ
        /// Created date: 16/10/2026
        /// </summary>
        internal class RptOverSpeed
        {
            internal class Prefix
            {
                internal const string Validation = "lz.validation.rptOverSpeed.";
                internal const string Entity = "lz.entity.rptOverSpeed.";
                internal const string Label = "lz.label.rptOverSpeed.";
            }

            internal class Validation
            {
                /// <summary>Thời gian bắt đầu và kết thúc là bắt buộc, thời gian kết thúc phải sau thời gian bắt đầu.</summary>
                internal const string TimeRangeInvalid = Prefix.Validation + "timeRangeInvalid";

                /// <summary>Báo cáo giới hạn dữ liệu trong {0} ngày.</summary>
                internal const string LimitDateRange = Prefix.Validation + "limitDateRange";

                /// <summary>Mức vượt tối thiểu không được âm.</summary>
                internal const string MinExceedInvalid = Prefix.Validation + "minExceedInvalid";
            }

            internal class Entity
            {
                internal const string Time = Prefix.Entity + "time"; // Thời gian
                internal const string LicensePlate = Prefix.Entity + "licensePlate"; // Biển số
                internal const string VehicleType = Prefix.Entity + "vehicleType"; // Loại xe
                internal const string VehicleColor = Prefix.Entity + "vehicleColor"; // Màu xe
                internal const string Location = Prefix.Entity + "location"; // Lý trình
                internal const string Speed = Prefix.Entity + "speed"; // Tốc độ (km/h)
                internal const string SpeedLimit = Prefix.Entity + "speedLimit"; // Tốc độ cho phép (km/h)
                internal const string Exceed = Prefix.Entity + "exceed"; // Vượt (km/h)
                internal const string Zone = Prefix.Entity + "zone"; // Đoạn lý trình
                internal const string Camera = Prefix.Entity + "camera"; // Camera
                internal const string Direction = Prefix.Entity + "direction"; // Hướng
                internal const string VehicleSize = Prefix.Entity + "vehicleSize"; // Cỡ xe
            }

            internal class Label
            {
                internal const string RptTitle = Prefix.Label + "rptTitle"; // Báo cáo phương tiện vượt quá tốc độ
                internal const string TotalViolation = Prefix.Label + "totalViolation"; // Tổng lượt vi phạm
                internal const string DistinctPlate = Prefix.Label + "distinctPlate"; // Số biển số
                internal const string MaxSpeed = Prefix.Label + "maxSpeed"; // Tốc độ cao nhất (km/h)
                internal const string AvgExceed = Prefix.Label + "avgExceed"; // Mức vượt trung bình (km/h)
                internal const string Violation = Prefix.Label + "violation"; // lượt
                internal const string SpeedLimitNote = Prefix.Label + "speedLimitNote"; // Tốc độ cho phép là ngưỡng lúc vi phạm; dữ liệu cũ dùng ngưỡng hiện hành
                internal const string Filter = Prefix.Label + "filter"; // Bộ lọc
                internal const string All = Prefix.Label + "all"; // Tất cả
                internal const string NotConfigured = Prefix.Label + "notConfigured"; // Chưa cấu hình
                internal const string NoZone = Prefix.Label + "noZone"; // Chưa gán đoạn
            }
        }

        /// <summary>
        /// Author: KienLT
        /// Description: RptVdsImage Msg — đọc ảnh VDS từ ổ đĩa (VdsImageService)
        /// Created date: 13/10/2026
        /// </summary>
        internal class RptVdsImage
        {
            internal class Prefix
            {
                internal const string Exception = "lz.exception.rptVdsImage.";
            }

            internal class Exception
            {
                /// <summary>Có nhiều camera trùng thời gian. Hãy dùng API VdsMedia/File với định danh bản ghi.</summary>
                internal const string MultipleCameraMatch = Prefix.Exception + "multipleCameraMatch";

                /// <summary>ID dữ liệu VDS không hợp lệ.</summary>
                internal const string InvalidId = Prefix.Exception + "invalidId";

                /// <summary>Thời gian phát hiện dữ liệu VDS là bắt buộc.</summary>
                internal const string DetectTimeRequired = Prefix.Exception + "detectTimeRequired";

                /// <summary>PictureType phải là f/p/v/vi/sn.</summary>
                internal const string PictureTypeInvalid = Prefix.Exception + "pictureTypeInvalid";

                /// <summary>Quality phải nằm trong khoảng từ {0} đến {1}.</summary>
                internal const string QualityRange = Prefix.Exception + "qualityRange";

                /// <summary>Không tìm thấy bộ mã hóa ảnh JPEG trên server.</summary>
                internal const string JpegEncoderNotFound = Prefix.Exception + "jpegEncoderNotFound";
            }
        }
    }
}
