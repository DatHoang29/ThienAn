namespace Module.TOLL.Core.Dto.DoanhThu.DoanhThuTheoQuy
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO output báo cáo doanh thu theo quý (khớp template 1.4_DTTQ.frx)
    /// Created date: 28/07/2026
    /// </summary>
    public class DTTQReportOutput
    {
        /// <summary>
        /// Cấu hình động lấy từ RptInfoConfig, gom theo nhóm: RptInfo.header.title, RptInfo.signature.left_name...
        /// FastReport bind trực tiếp vào node này thay vì set cứng trong file .frx.
        /// </summary>
        public Dictionary<string, Dictionary<string, string?>> RptInfo { get; set; } = new Dictionary<string, Dictionary<string, string?>>();

        public DTTQParameter Parameter { get; set; } = new DTTQParameter();
        public List<DTTQData> ChiTiet { get; set; } = new List<DTTQData>();
        public DTTQCong Cong { get; set; } = new DTTQCong();
        public DTTQSignature Signature { get; set; } = new DTTQSignature();
    }

    public class DTTQParameter
    {
        public string? DonViCapTren { get; set; }
        public string? DonViTrucThuoc { get; set; }
        public string? Hinh { get; set; }
        public string? Tram { get; set; }
        public string? QUY { get; set; }
        public string? Quy { get => QUY; set => QUY = value; }
        public string? Nam { get; set; }
        public string? NgayHienTai { get; set; }
        public string? NgayXacDinh { get; set; }
        public string? DiaDanh { get; set; }
        public string? NhanVienBaoCao { get; set; }
    }

    public class DTTQData
    {
        public int STT { get; set; }
        public string? Ngay { get; set; }
        public long L1_SL { get; set; }
        public decimal L1_TT { get; set; }
        public long L2_SL { get; set; }
        public decimal L2_TT { get; set; }
        public long L3_SL { get; set; }
        public decimal L3_TT { get; set; }
        public long L4_SL { get; set; }
        public decimal L4_TT { get; set; }
        public long L5_SL { get; set; }
        public decimal L5_TT { get; set; }
        public long TongSL { get; set; }
        public decimal TongTT { get; set; }
    }

    public class DTTQCong
    {
        public long L1_SL { get; set; }
        public decimal L1_TT { get; set; }
        public long L2_SL { get; set; }
        public decimal L2_TT { get; set; }
        public long L3_SL { get; set; }
        public decimal L3_TT { get; set; }
        public long L4_SL { get; set; }
        public decimal L4_TT { get; set; }
        public long L5_SL { get; set; }
        public decimal L5_TT { get; set; }
        public long TongSL { get; set; }
        public decimal TongTT { get; set; }
        public decimal TongDoanhThu { get; set; }
        public string? BangChu { get; set; }
    }

    public class DTTQSignature
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

    /// <summary>
    /// DTO nhận kết quả từ stored proc ITS015_Proc_ReportRevenue_Quarterly.
    /// (Tên property = tên cột store trả về; nếu store đặt alias khác thì sửa cho khớp.)
    /// </summary>
    public class DTTQQueryOutput
    {
        public string? ThangNam { get; set; }         // -> ChiTiet.Ngay (khớp alias store)
        public string? NgayThangNam { get => ThangNam; set => ThangNam = value; }
        public long LuuLuong_1 { get; set; }          // -> L1_SL
        public decimal ThanhTien_1 { get; set; }      // -> L1_TT
        public long LuuLuong_2 { get; set; }          // -> L2_SL
        public decimal ThanhTien_2 { get; set; }      // -> L2_TT
        public long LuuLuong_3 { get; set; }          // -> L3_SL
        public decimal ThanhTien_3 { get; set; }      // -> L3_TT
        public long LuuLuong_4 { get; set; }          // -> L4_SL
        public decimal ThanhTien_4 { get; set; }      // -> L4_TT
        public long LuuLuong_5 { get; set; }          // -> L5_SL
        public decimal ThanhTien_5 { get; set; }      // -> L5_TT
        public long LuuLuong_Tong { get; set; }       // -> TongSL
        public decimal ThanhTien_Tong { get; set; }   // -> TongTT
    }
}
