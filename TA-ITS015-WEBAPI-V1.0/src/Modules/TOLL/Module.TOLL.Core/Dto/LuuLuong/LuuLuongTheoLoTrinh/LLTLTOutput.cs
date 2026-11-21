namespace Module.TOLL.Core.Dto.LuuLuong.LuuLuongTheoLoTrinh
{
    public class LLTLTReportOutput
    {
        /// <summary>
        /// Cấu hình động lấy từ RptInfoConfig, gom theo nhóm: RptInfo.header.title, RptInfo.signature.left_name...
        /// FastReport bind trực tiếp vào node này thay vì set cứng trong file .frx.
        /// </summary>
        public Dictionary<string, Dictionary<string, string?>> RptInfo { get; set; } = new Dictionary<string, Dictionary<string, string?>>();

        public LLTLTParameter Parameter { get; set; } = new LLTLTParameter();

        public List<LLTLTData> ChiTiet { get; set; } = new List<LLTLTData>();

        public LLTLTCong Cong { get; set; } = new LLTLTCong();

        public LLTLTSignature Signature { get; set; } = new LLTLTSignature();
    }
    public class LLTLTParameter
    {
        public string? DonViCapTren { get; set; }

        public string? DonViTrucThuoc { get; set; }

        public string? Hinh { get; set; }

        public string? NgayBatDau { get; set; }

        public string? NgayKetThuc { get; set; }

        public string? Tram { get; set; }

        public string? Lan { get; set; }

        public string? NgayHienTai { get; set; }

        public string? NgayXacDinh { get; set; }

        public string? DiaDanh { get; set; }

        public string? NhanVienBaoCao { get; set; }
    }
    public class LLTLTData
    {
        public int STT { get; set; }

        public string? LoTrinh { get; set; }

        public string? LoaiXe { get; set; }

        public decimal LuuLuong { get; set; }
    }
    public class LLTLTCong
    {
        public decimal LuuLuong { get; set; }
    }
    public class LLTLTSignature
    {
        public string? ChucDanh1 { get; set; }

        public string? ChuKi1 { get; set; }

        public string? NguoiKi1 { get; set; }

        public string? ChucDanh2 { get; set; }

        public string? ChuKi2 { get; set; }

        public string? NguoiKi2 { get; set; }

        public string? ChucDanh3 { get; set; }

        public string? ChuKi3 { get; set; }

        public string? NguoiKi3 { get; set; }
    }
    public class LLTLTQueryOutput
    {
        public string? LoTrinh { get; set; }
        public string? TenLoTrinh { get; set; }
        public string? LoaiXe { get; set; }
        public string? TenLoaiXe { get; set; }
        public decimal LuuLuong { get; set; }
    }
}
