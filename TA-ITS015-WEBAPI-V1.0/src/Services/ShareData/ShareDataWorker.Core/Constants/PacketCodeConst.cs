namespace ShareDataWorker.Core.Constants
{
    /// <summary>
    /// Description: Danh mục mã gói tin chuẩn (Packet Code) dùng chung trong ShareDataWorker.
    /// Created date: 25/09/2026
    /// </summary>
    public static class PacketCodeConst
    {
        /// <summary>Gói 101: Dữ liệu thông tin chung / luồng giao thông</summary>
        public const string CommonData = "101_commonData";

        /// <summary>Gói 102: Dữ liệu hình ảnh giao thông CCTV</summary>
        public const string CctvData = "102_cctvData";

        /// <summary>Gói 103: Dữ liệu đếm xe VDS</summary>
        public const string VdsData = "103_vdsData";

        /// <summary>Gói 104: Dữ liệu khí tượng / thời tiết</summary>
        public const string WeatherData = "104_weatherData";

        /// <summary>Gói 105: Dữ liệu định danh phương tiện AVI / RFID</summary>
        public const string RfidData = "105_rfidData";

        /// <summary>Gói 106: Dữ liệu cân tải trọng xe tự động WIM</summary>
        public const string WimData = "106_wimData";

        /// <summary>Gói 107: Dữ liệu thông tin sự cố / sự kiện giao thông</summary>
        public const string IncidentData = "107_incidentData";

        /// <summary>Gói 108: Dữ liệu thông tin biển báo điện tử VMS</summary>
        public const string VmsInfo = "108_vmsInfo";

        /// <summary>Gói 109: Dữ liệu giao dịch thu phí ETC</summary>
        public const string EtcData = "109_etcData";

        /// <summary>Gói 110: Dữ liệu trao đổi thông tin với người tham gia giao thông</summary>
        public const string WpData = "110_wpData";

        /// <summary>Gói 111: Dữ liệu trao đổi thông tin giữa các TT QLĐHGT tuyến</summary>
        public const string InterCenter = "111";

        /// <summary>
        /// Description: Danh mục số hiệu gói tin ngắn (101 - 111) dùng khi đối chiếu hoặc tương thích fallback.
        /// </summary>
        public static class Short
        {
            public const string P101 = "101";
            public const string P102 = "102";
            public const string P103 = "103";
            public const string P104 = "104";
            public const string P105 = "105";
            public const string P106 = "106";
            public const string P107 = "107";
            public const string P108 = "108";
            public const string P109 = "109";
            public const string P110 = "110";
            public const string P111 = "111";
        }
    }
}
