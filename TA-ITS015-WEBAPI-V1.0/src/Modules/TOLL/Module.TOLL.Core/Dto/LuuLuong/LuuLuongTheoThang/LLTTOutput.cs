namespace Module.TOLL.Core.Dto.LuuLuong.LuuLuongTheoThang
{
    public class LLTTReportOutput
    {
        /// <summary>
        /// Cấu hình động lấy từ RptInfoConfig, gom theo nhóm: RptInfo.header.title, RptInfo.signature.left_name...
        /// FastReport bind trực tiếp vào node này thay vì set cứng trong file .frx.
        /// </summary>
        public Dictionary<string, Dictionary<string, string?>> RptInfo { get; set; } = new Dictionary<string, Dictionary<string, string?>>();

        public LLTTParameter Parameter { get; set; } = new LLTTParameter();

        public List<LLTTData> ChiTiet { get; set; } = new List<LLTTData>();

        public LLTTCong Cong { get; set; } = new LLTTCong();

        public LLTTSignature Signature { get; set; } = new LLTTSignature();
    }
    public class LLTTParameter
    {
        public string DonViCapTren { get; set; }  

        public string DonViTrucThuoc { get; set; }  

        public string Hinh { get; set; }  

        public string NgayHienTai { get; set; }  

        public string NgayXacDinh { get; set; }  

        public string DiaDanh { get; set; }  

        public string NhanVienBaoCao { get; set; }

        /// <summary>Tên trạm hiển thị trên header báo cáo (tra từ StationId)</summary>
        public string? Tram { get; set; }

        public int Thang { get; set; }

        public int Nam { get; set; }

        public string TuNgay { get; set; }  

        public string DenNgay { get; set; }  
    }
    public class LLTTData
    {
        public string Ngay { get; set; }  

        public int L1_VL { get; set; }

        public int L1_TQ { get; set; }

        public int L2_VL { get; set; }

        public int L2_TQ { get; set; }

        public int L3_VL { get; set; }

        public int L3_TQ { get; set; }

        public int L4_VL { get; set; }

        public int L4_TQ { get; set; }

        public int L5_VL { get; set; }

        public int L5_TQ { get; set; }

        public int TongVeLuot { get; set; }

        public int TongTQ { get; set; }

        public decimal PhiDBTQ { get; set; }

        public int UT_MotXe { get; set; }

        public int UT_DoanXe { get; set; }

        public int SC_MotXe { get; set; }

        public int SC_DoanXe { get; set; }
    }

    public class LLTTCong
    {
        public string Nhan { get; set; }  

        public int TongXeQuaTram { get; set; }

        public int L1_VL { get; set; }

        public int L1_TQ { get; set; }

        public int L2_VL { get; set; }

        public int L2_TQ { get; set; }

        public int L3_VL { get; set; }

        public int L3_TQ { get; set; }

        public int L4_VL { get; set; }

        public int L4_TQ { get; set; }

        public int L5_VL { get; set; }

        public int L5_TQ { get; set; }

        public int TongVeLuot { get; set; }

        public int TongTQ { get; set; }

        public decimal PhiDBTQ { get; set; }

        public int UT_MotXe { get; set; }

        public int UT_DoanXe { get; set; }

        public int SC_MotXe { get; set; }

        public int SC_DoanXe { get; set; }
    }

    public class LLTTSignature
    {
        public string ChucDanh1 { get; set; }  

        public string ChuKi1 { get; set; }  

        public string NguoiKi1 { get; set; }  

        public string ChucDanh2 { get; set; }  

        public string ChuKi2 { get; set; }  

        public string NguoiKi2 { get; set; }  

        public string ChucDanh3 { get; set; }  

        public string ChuKi3 { get; set; }  

        public string NguoiKi3 { get; set; }  
    }
    public class LLTTQueryOutput
    {
        public DateTime DateValue { get; set; }

        public string NgayThangNam { get; set; }

        public int? LuuLuong_VeLuot_1 { get; set; }

        public int? LuuLuong_VeLuot_2 { get; set; }

        public int? LuuLuong_VeLuot_3 { get; set; }

        public int? LuuLuong_VeLuot_4 { get; set; }

        public int? LuuLuong_VeLuot_5 { get; set; }

        public int? LuuLuong_VeTQ_1 { get; set; }

        public int? LuuLuong_VeTQ_2 { get; set; }

        public int? LuuLuong_VeTQ_3 { get; set; }

        public int? LuuLuong_VeTQ_4 { get; set; }

        public int? LuuLuong_VeTQ_5 { get; set; }

        public int? LL_Tong_VL { get; set; }

        public int? LL_Tong_VTQ { get; set; }

        public decimal? BTC { get; set; }

        public int? UT_1X { get; set; }

        public int? UT_DX { get; set; }

        public int? SC_1X { get; set; }

        public int? SC_DX { get; set; }
    }
}
