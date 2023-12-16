using System.Linq;

namespace Hpl.SaleOnlineDatabase
{
    public class SaleOnlineServices
    {
        public static string LockUser(string userName)
        {
            string strOut = "N/A";

            var db = new SaleOnlineDbContext();
            var lstNvs = db.NhanViens.Where(x => x.MaSo == userName);
            if (lstNvs.Any())
            {
                var user = lstNvs.FirstOrDefault();
                if (user?.Lock != null && !user.Lock.Value)
                {
                    user.Lock = true;
                    db.SaveChanges();

                    strOut = "Đã lock";
                }
            }
            else
            {
                strOut = "Không tồn tại";
            }

            return strOut;
        }

        public static string UnlockUser(string userName)
        {
            string strOut = "N/A";

            var db = new SaleOnlineDbContext();
            var lstNvs = db.NhanViens.Where(x => x.MaSo == userName);
            if (lstNvs.Any())
            {
                var user = lstNvs.FirstOrDefault();
                if (user?.Lock != null && !user.Lock.Value)
                {
                    user.Lock = false;
                    db.SaveChanges();

                    strOut = "Đã unlock";
                }
            }
            else
            {
                strOut = "Không tồn tại";
            }

            return strOut;
        }

        public static NhanVien GetNhanVienByUserName(string userName)
        {
            var db = new SaleOnlineDbContext();
            var lstNvs = db.NhanViens.Where(x => x.MaSo == userName);
            if (lstNvs.Any())
            {
                return lstNvs.FirstOrDefault();
            }

            return null;
        }

        //public static Branch GetBranchId(string maPhongBan)
        //{
        //    var db = new SaleOnlineDbContext();

        //    var branch = db.Branches.FirstOrDefault(x => x.BranchCode.Equals(maPhongBan) &
        //                                                 x.IsAgentLink == false);
        //    if (branch != null)
        //    {
        //        return branch;
        //    }

        //    db.Dispose();

        //    return null;
        //}

        public static Branch GetBranch(string maPhongBan)
        {
            var db = new SaleOnlineDbContext();

            return db.Branches.FirstOrDefault(x => x.BranchCode.Equals(maPhongBan) &
                                                   x.IsAgentLink == false);
        }

        public static PhongBan GetPhongBan(string maPhongBan)
        {
            var db = new SaleOnlineDbContext();
            return db.PhongBans.FirstOrDefault(x => x.TenPb.Equals(maPhongBan));
        }

        public static void UpdateUserSale(NhanVien nhanVien)
        {
            var db = new SaleOnlineDbContext();
            db.NhanViens.Update(nhanVien);
            db.SaveChanges();
        }

        public static void CreateUserSale(NhanVien nhanVien)
        {
            var db = new SaleOnlineDbContext();
            db.NhanViens.Add(nhanVien);
            db.SaveChanges();
        }
        //objNV
        //MaNV: 4475,
        //---MaSo: "nv.tuan",
        //---HoTen: "Nguyễn Văn Tuấn",
        //---Ho: "Nguyễn Văn",
        //---DienThoai: "0904710719",
        //----Email: "nv.tuan@haiphatland.com.vn",
        //MatKhau: "MqY2b87/BgZgDxJA5zHg83Jfay2pN+nctXk4Lk8KeKGNIHcmBwVa/6XO2pREbq3MDXMmE5NKW+k4ojobR3nVrB4u9FFY282fBo8PeTNkgcBnu4IUfsySFVg0lsYI7IX3g1T38xBJ+2y1yxozb0fX/0S0TXW3fZBsYKYxYPzJDTI=",
        //---NgaySinh: {11/08/1981 12:00:00 SA
        //},
        //PerID: 4,
        //----UserType: 1,
        //CusID: null,
        //---Lock: false,
        //---SoCMND: "12990838",
        //NgayCap: null,
        //NoiCap: "CA Hà Nội",
        //DiaChi: "",
        //HoKhau: "",
        //MaPB: 622,
        //MaCV: 126,
        //MaNKD: 7,
        //MaTTNCN: "",
        //SoTKNH: "",
        //MaNH: 2,
        //MaQD: 2,
        //----KeyCode: "VP-77",
        //HDHT: null,
        //MaQL: null,
        //MaQL2: null,
        //MaDM: null,
        //MaTT: null,
        //IsCDT: true,
        //Rose: null,
        //DienThoaiNB: null,
        //DiaChiLL: "",
        //---MaNVCN: 1, //mã Nhân viên cập nhật
        //NgayCN: {18/10/2020 2:07:38 CH
        //},
        //ImgAvatar: "",
        //ImgSignature: "",
        //Description: "",
        //----BranchID: 209,
        //---IsDeleted: false,
        //LevelID: null,
        //---Gender: null,
        //IDLS: 115
    }
}