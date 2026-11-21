namespace Module.TOLL.Core.Dto.LuuLuong.LuuLuongTheoCa
{
    public class LLTCReportOutput
    {
        /// <summary>
        /// Cấu hình động lấy từ RptInfoConfig, gom theo nhóm: RptInfo.header.title, RptInfo.signature.left_name...
        /// FastReport bind trực tiếp vào node này thay vì set cứng trong file .frx.
        /// </summary>
        public Dictionary<string, Dictionary<string, string?>> RptInfo { get; set; } = new Dictionary<string, Dictionary<string, string?>>();

        public LLTCParameter Parameter { get; set; } = new LLTCParameter();

        public List<LLTCData> ChiTiet { get; set; } = new List<LLTCData>();

        public LLTCCong Cong { get; set; } = new LLTCCong();

        public LLTCSignature Signature { get; set; } = new LLTCSignature();
    }
    public class LLTCParameter
    {
        public string? DonViCapTren { get; set; }
        public string? DonViTrucThuoc { get; set; }

        public string? Hinh { get; set; }

        public string? Ca { get; set; }

        public string? Ngay { get; set; }

        public string? Tram { get; set; }

        public string? Lan { get; set; }

        public string? NgayHienTai { get; set; }

        public string? NgayXacDinh { get; set; }

        public string? DiaDanh { get; set; }

        public string? NhanVienBaoCao { get; set; }
    }
    public class LLTCData
    {
        public int STT { get; set; }

        public string? LoaiVe { get; set; }

        public decimal VeLuot { get; set; }

        public decimal VeThang { get; set; }

        public decimal TongCong { get; set; }
    }
    public class LLTCCong
    {
        public string? Nhan { get; set; }

        public decimal VeLuot { get; set; }

        public decimal VeThang { get; set; }

        public decimal TongCong { get; set; }
    }
    public class LLTCSignature
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
    public class LLTCQueryOutput
    {
        public string? LoaiXe { get; set; }
        public string? TenLoaiXe { get; set; }
        public long LL_VL { get; set; }
        public long LL_VT { get; set; }
        public long LL_Tong { get; set; }
    }
}
