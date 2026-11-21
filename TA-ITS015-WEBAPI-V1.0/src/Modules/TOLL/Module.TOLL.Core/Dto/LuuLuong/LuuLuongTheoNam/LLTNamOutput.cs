namespace Module.TOLL.Core.Dto.LuuLuong.LuuLuongTheoNam
{
    public class LLTNamReportOutput
    {
        /// <summary>
        /// Cấu hình động lấy từ RptInfoConfig, gom theo nhóm: RptInfo.header.title, RptInfo.signature.left_name...
        /// FastReport bind trực tiếp vào node này thay vì set cứng trong file .frx.
        /// </summary>
        public Dictionary<string, Dictionary<string, string?>> RptInfo { get; set; } = new Dictionary<string, Dictionary<string, string?>>();

        public LLTNamParameter Parameter { get; set; } = new LLTNamParameter();

        public List<LLTNamData> ChiTiet { get; set; } = new List<LLTNamData>();

        public LLTNamCong Cong { get; set; } = new LLTNamCong();

        public LLTNamSignature Signature { get; set; } = new LLTNamSignature();
    }
    public class LLTNamParameter
    {
        public string DonViCapTren { get; set; } = string.Empty;

        public string DonViTrucThuoc { get; set; } = string.Empty;

        public string Hinh { get; set; } = string.Empty;

        public string NgayHienTai { get; set; } = string.Empty;

        public string NgayXacDinh { get; set; } = string.Empty;

        public string DiaDanh { get; set; } = string.Empty;

        public string NhanVienBaoCao { get; set; } = string.Empty;

        /// <summary>Tên trạm hiển thị trên header báo cáo (tra từ StationId)</summary>
        public string? Tram { get; set; }

        public int Nam { get; set; }
    }

    public class LLTNamData
    {
        public int Nam { get; set; }

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

    public class LLTNamCong
    {
        public string Nhan { get; set; } = string.Empty;
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

    public class LLTNamSignature
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
    public class LLTNamQueryOutput
    {
        public int YearNo { get; set; }

        public int MonthNo { get; set; }

        public string ThangNam { get; set; } = string.Empty;

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
