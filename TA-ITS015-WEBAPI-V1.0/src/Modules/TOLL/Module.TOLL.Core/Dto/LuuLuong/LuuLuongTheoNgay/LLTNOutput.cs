using System;
using System.Collections.Generic;
using System.Text;

namespace Module.TOLL.Core.Dto.LuuLuong.LuuLuongTheoNgay
{
    public class LLTNReportOutput
    {
        /// <summary>
        /// Cấu hình động lấy từ RptInfoConfig, gom theo nhóm: RptInfo.header.title, RptInfo.signature.left_name...
        /// FastReport bind trực tiếp vào node này thay vì set cứng trong file .frx.
        /// </summary>
        public Dictionary<string, Dictionary<string, string?>> RptInfo { get; set; } = new Dictionary<string, Dictionary<string, string?>>();

        public LLTNParameter Parameter { get; set; } = new LLTNParameter();

        public List<LLTNData> ChiTiet { get; set; } = new List<LLTNData>();

        public LLTNCong Cong { get; set; } = new LLTNCong();

        public LLTNSignature Signature { get; set; } = new LLTNSignature();
    }

    public class LLTNSignature
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

    public class LLTNCong
    {
        public string Nhan { get; set; } = "Tổng cộng";

        public long VeLuot { get; set; }

        public long VeThang { get; set; }

        public long TongCong { get; set; }
    }

    public class LLTNData
    {
        public int STT { get; set; }

        public string? LoaiVe { get; set; }

        public long VeLuot { get; set; }

        public long VeThang { get; set; }

        public long TongCong { get; set; }

        public DateTime? CreateTime { get; set; }

        public string? CreateUId { get; set; }

        public DateTime? UpdateTime { get; set; }

        public string? UpdateUId { get; set; }
    }

    public class LLTNParameter
    {
        public string? DonViCapTren { get; set; }

        public string? DonViTrucThuoc { get; set; }

        public string? Hinh { get; set; }

        public string? Ngay { get; set; }

        public string? Tram { get; set; }

        public string? NgayHienTai { get; set; }

        public string? NgayXacDinh { get; set; }

        public string? DiaDanh { get; set; }

        public string? NhanVienBaoCao { get; set; }
    }
    public class LLTNQueryOutput
    {
        public string? LoaiXe { get; set; }
        public string? TenLoaiXe { get; set; }
        public long LL_VL { get; set; }
        public long LL_VT { get; set; }
        public long LL_Tong { get; set; }
    }
}
