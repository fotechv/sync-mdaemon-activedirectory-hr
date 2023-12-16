using System;

namespace Hpl.HrmDatabase.ViewModels
{
    public class NhanVienViewModelOut
    {
        public int NhanVienID { get; set; }
        public string Ho { get; set; }
        public string Ten { get; set; }
        public string GioiTinh { get; set; }
        public string MaNhanVien { get; set; }
        public string TenDangNhap { get; set; }
        public string Email { get; set; }
        public string EmailCaNhan { get; set; }
        public string DienThoai { get; set; }
        public string CMTND { get; set; }
        public string TenChucVu { get; set; }
        public string TenChucDanh { get; set; }
        public int PhongBanId { get; set; }
        public string TenPhongBan { get; set; }
        public string MaPhongBan { get; set; }
        public string TenPhongBanCap1 { get; set; }
        public string MaCap1 { get; set; }
    }
}