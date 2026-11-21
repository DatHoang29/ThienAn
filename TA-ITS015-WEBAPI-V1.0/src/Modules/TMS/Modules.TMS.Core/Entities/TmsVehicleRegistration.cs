using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Core.Entities
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 25/11/2024
    /// </summary>
    [SugarTable("TmsVehicleRegistration")]
    public class TmsVehicleRegistration : EntityTenant
    {
        //TmsVehicleRegistration {
        // ID varchar(64) pk
        // LicensePlate string(32) null
        // VehicleType string(64) null
        // Brand string(64) null
        // YearManufacture string(64) null
        // Owner string(64) null
        // Address string(128) null
        // EngineNumber string(64) null
        // ChassisNumber string(64) null
        // OverallDimensions string(64) null
        // ContainerDimensions string(64) null
        // Seat string(32) null
        // InspectionStamp string(64) null
        // DateRegistrations datetime null
        // ExpirationDate datetime null
        // DataType string(16) null
        //}

        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? LicensePlate { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? VehicleType { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? Brand { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? YearManufacture { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? Owner { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? Address { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? EngineNumber { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? ChassisNumber { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? OverallDimensions { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? ContainerDimensions { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? Seat { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? InspectionStamp { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? DateRegistrations { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? ExpirationDate { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? DataType { get; set; }
    }
}
