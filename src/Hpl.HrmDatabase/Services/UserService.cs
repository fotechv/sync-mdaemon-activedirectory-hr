using Hpl.Common.Helper;
using Hpl.HrmDatabase.ViewModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Hpl.SaleOnlineDatabase;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Hpl.HrmDatabase.Services
{
    public class UserService
    {
        private readonly IHrmDbContext _hrmDb;
        private static IAbpHplDbContext _abpHplDb;
        private readonly ISaleOnlineDbContext _saleOnlineDb;

        public UserService(IHrmDbContext hrmDb, IAbpHplDbContext abpHplDb, ISaleOnlineDbContext saleOnlineDb)
        {
            _hrmDb = hrmDb;
            _abpHplDb = abpHplDb;
            _saleOnlineDb = saleOnlineDb;
        }

        #region XỬ LÝ TRONG BẢNG TẠM [BaoNXMaNhanVienEmail]
        public static BaoNxMaNhanVienEmail GetEmail(string email)
        {
            var db = new HrmDbContext();
            return db.BaoNxMaNhanVienEmails.FirstOrDefault(x => x.Email == email);
        }

        public static List<BaoNxMaNhanVienEmail> GetAllEmailImport()
        {
            var db = new HrmDbContext();
            return db.BaoNxMaNhanVienEmails.ToList();
        }

        public static List<string> GetAllEmailHrm()
        {
            var db = new HrmDbContext();
            return db.NhanViens.Where(x => !string.IsNullOrEmpty(x.Email))
                                .Select(x => x.Email)
                                .OrderBy(x => x)
                                .ToList();
        }

        public static List<string> UpdateEmailAndUserHrm()
        {
            List<string> lstStr = new List<string>();
            var db = new HrmDbContext();
            var listNv = db.BaoNxMaNhanVienEmails.ToList();

            foreach (var email in listNv)
            {
                try
                {
                    string strOut = "";
                    bool check = false;
                    var nv = db.NhanViens.FirstOrDefault(x => x.MaNhanVien == email.MaNhanVien);
                    if (nv != null)
                    {
                        if (!nv.Email.Equals(email.Email))
                        {
                            check = true;
                            nv.Email = email.Email;
                            strOut = email.MaNhanVien + " - HCNS email: " + email.Email;
                            strOut += ". HRM Email: " + nv.Email;

                            email.LogUser = "Email";
                        }

                        var nd = db.SysNguoiDungs.FirstOrDefault(x => x.NhanVienId == nv.NhanVienId);
                        if (nd != null)
                        {
                            var str = email.Email.Split("@");
                            if (str.Length == 2)
                            {
                                if (!nd.TenDangNhap.Equals(str[0].Trim()) & str[1].Trim().Equals("haiphatland.com.vn"))
                                {
                                    check = true;
                                    strOut += "User cũ: " + nd.TenDangNhap;
                                    nd.TenDangNhap = str[0].Trim();
                                    strOut += ". User mới: " + str[0].Trim();
                                    if (!string.IsNullOrEmpty(email.LogUser))
                                    {
                                        email.LogUser = ",Username";
                                    }
                                    else
                                    {
                                        email.LogUser = "Username";
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        email.LogUser = "Not found";
                    }

                    if (check)
                    {
                        lstStr.Add(strOut);
                    }

                }
                catch (Exception e)
                {
                    lstStr.Add(email.MaNhanVien + "-" + email.Email + "=>" + e.Message);
                }

            }

            db.SaveChanges();
            db.Dispose();

            return lstStr;
        }
        #endregion

        public static void InactiveUserHrm(string userName)
        {
            var db = new HrmDbContext();

            //Fix username
            var user = db.SysNguoiDungs.FirstOrDefault(x => x.TenDangNhap == userName);
            if (user != null)
            {
                user.Active = false;
                user.ModifyDate = DateTime.Now;
                db.SaveChanges();
                db.Dispose();
            }
        }

        public static void ReactiveUserHrm(string userName)
        {
            var db = new HrmDbContext();

            //Fix username
            var user = db.SysNguoiDungs.FirstOrDefault(x => x.TenDangNhap == userName);
            if (user != null)
            {
                user.Active = true;
                user.ModifyDate = DateTime.Now;
                db.SaveChanges();
                db.Dispose();
            }
        }
        public static NhanVienViewModel2 CreateUserHrm3(NhanVienViewModel2 model, string userName)
        {
            var db = new HrmDbContext();

            //Fix username
            var user = db.SysNguoiDungs.FirstOrDefault(x => x.TenDangNhap == userName);
            if (user != null) //Fix lai user name
            {
                //Fix lai email va user name
                var newEmployee = db.NhanViens.FirstOrDefault(x => x.NhanVienId == model.NhanVienId);
                if (newEmployee != null)
                {
                    //Update lai Email ca nhan
                    if (!string.IsNullOrEmpty(newEmployee.Email))
                    {
                        if (string.IsNullOrEmpty(newEmployee.EmailCaNhan))
                        {
                            newEmployee.EmailCaNhan = newEmployee.Email;
                            model.EmailCaNhan = user.TenDangNhap.ToLower();
                        }
                    }

                    //Cap nhat lai email cty cua User
                    newEmployee.Email = userName + "@haiphatland.com.vn";
                    model.Email = userName + "@haiphatland.com.vn";
                }

                //Update lại userName
                user.TenDangNhap = userName;
                user.IsAdAccount = true;

                db.SaveChanges();
                db.Dispose();
            }
            else//TẠO MỚI USER
            {
                //Tạo người dùng
                user = new SysNguoiDung();
                //int NguoiDungId // NguoiDungID (Primary key)
                //int? NhanVienId // NhanVienId
                user.NhanVienId = model.NhanVienId;
                //string TenDangNhap // TenDangNhap (length: 50)
                user.TenDangNhap = userName;
                //string MatKhau // MatKhau (length: 50)
                user.MatKhau = "[81DC9BDB52D04DC20036DBD8313ED055]";
                //DateTime? LastLogin // LastLogin
                //DateTime? LastLogout // LastLogout
                //bool? Active // Active
                user.Active = true;
                //bool? IsPortalAccount // IsPortalAccount
                user.IsPortalAccount = true;
                //bool? IsAdAccount // IsADAccount
                user.IsAdAccount = true;
                //byte[] Settings // Settings (length: 2147483647)
                user.Settings = new byte[] { 0, 0 };
                //int? NhomNguoiDungId // NhomNguoiDungID
                user.NhomNguoiDungId = 0;
                //string ActiveModule // ActiveModule (length: 50)
                user.ActiveModule = "";
                //bool? IsDeleted // IsDeleted
                user.IsDeleted = false;
                //int? NhomQuyenId // NhomQuyenID
                user.NhomQuyenId = 7;
                //int? CapBacDanhGia // CapBacDanhGia
                user.CapBacDanhGia = 0;
                //bool? DanhGiaReadOnly // DanhGia_ReadOnly
                user.DanhGiaReadOnly = false;
                //int? CreatedById // CreatedByID
                user.CreatedById = 1561;
                //DateTime? CreatedDate // CreatedDate
                user.CreatedDate = DateTime.Now;
                //int? ModifyById // ModifyByID
                //DateTime? ModifyDate // ModifyDate
                //string Aid // AID (length: 50)
                //DateTime? DueDate // DueDate
                //int? HrisTuCapBac // HRIS_TuCapBac
                user.HrisTuCapBac = 0;
                //int? CbTuCapBac // CB_TuCapBac
                user.CbTuCapBac = 0;

                //string EmailAccount // EmailAccount (length: 250)
                //=>KHÔNG TẠO CÁI NÀY, VÌ BỊ LỖI KHÔNG SỬA THÔNG TIN USER ĐƯỢC
                //user.EmailAccount = userName + "@haiphatland.com.vn"; 
                //string EmailPassword // EmailPassword (length: 250)
                //user.EmailPassword = "[81DC9BDB52D04DC20036DBD8313ED055]";

                //string NdHoVaTen // ND_HoVaTen (length: 50)
                user.NdHoVaTen = model.Ho + " " + model.Ten;
                //string NdMaNhanVien // ND_MaNhanVien (length: 50)
                user.NdMaNhanVien = model.MaNhanVien;
                //string DeviceId // DeviceId (length: 500)
                //string Token // Token (length: 500)
                user.Token = "";
                //string RedirectUrl // RedirectURL (length: 250)

                //Cập nhật lại email vào hồ sơ nhân sự
                var newNv = db.NhanViens.FirstOrDefault(x => x.NhanVienId == model.NhanVienId);
                if (newNv != null)
                {
                    if (!string.IsNullOrEmpty(newNv.Email))
                    {
                        if (string.IsNullOrEmpty(newNv.EmailCaNhan))
                        {
                            newNv.EmailCaNhan = newNv.Email;
                            model.EmailCaNhan = newNv.Email;
                        }
                    }

                    newNv.Email = userName + "@haiphatland.com.vn";
                    model.Email = userName + "@haiphatland.com.vn";
                }

                db.SysNguoiDungs.Add(user);
                db.SaveChanges();//TODO
                db.Dispose();//=>Dispose de connection ben trong ham TaoQuyenNguoiDung dc mo

                //CẬP NHẬT QUYỀN CƠ BẢN NGƯỜI DÙNG
                TaoQuyenNguoiDung(user.NguoiDungId);//TODO
            }

            //Cap nhat lai userName moi cho Nhan Vien va return
            model.TenDangNhap = userName;

            return model;
        }

        public static GetAllNhanVienTheoListMaNvReturnModel CreateUserHrm4(GetAllNhanVienTheoListMaNvReturnModel model, string userName)
        {
            var db = new HrmDbContext();

            //Fix username
            var user = db.SysNguoiDungs.FirstOrDefault(x => x.TenDangNhap == userName);
            if (user != null) //Fix lai user name
            {
                //Fix lai email va user name
                var newEmployee = db.NhanViens.FirstOrDefault(x => x.NhanVienId == model.NhanVienId);
                if (newEmployee != null)
                {
                    //Update lai Email ca nhan
                    if (!string.IsNullOrEmpty(newEmployee.Email))
                    {
                        if (string.IsNullOrEmpty(newEmployee.EmailCaNhan))
                        {
                            newEmployee.EmailCaNhan = newEmployee.Email;
                            model.EmailCaNhan = user.TenDangNhap.ToLower();
                        }
                    }

                    //Cap nhat lai email cty cua User
                    newEmployee.Email = userName + "@haiphatland.com.vn";
                    model.Email = userName + "@haiphatland.com.vn";
                }

                //Update lại userName
                user.TenDangNhap = userName;
                user.IsAdAccount = true;

                db.SaveChanges();
                db.Dispose();
            }
            else//TẠO MỚI USER
            {
                //Tạo người dùng
                user = new SysNguoiDung();
                //int NguoiDungId // NguoiDungID (Primary key)
                //int? NhanVienId // NhanVienId
                user.NhanVienId = model.NhanVienId;
                //string TenDangNhap // TenDangNhap (length: 50)
                user.TenDangNhap = userName;
                //string MatKhau // MatKhau (length: 50)
                user.MatKhau = "[81DC9BDB52D04DC20036DBD8313ED055]";
                //DateTime? LastLogin // LastLogin
                //DateTime? LastLogout // LastLogout
                //bool? Active // Active
                user.Active = true;
                //bool? IsPortalAccount // IsPortalAccount
                user.IsPortalAccount = true;
                //bool? IsAdAccount // IsADAccount
                user.IsAdAccount = true;
                //byte[] Settings // Settings (length: 2147483647)
                user.Settings = new byte[] { 0, 0 };
                //int? NhomNguoiDungId // NhomNguoiDungID
                user.NhomNguoiDungId = 0;
                //string ActiveModule // ActiveModule (length: 50)
                user.ActiveModule = "";
                //bool? IsDeleted // IsDeleted
                user.IsDeleted = false;
                //int? NhomQuyenId // NhomQuyenID
                user.NhomQuyenId = 7;
                //int? CapBacDanhGia // CapBacDanhGia
                user.CapBacDanhGia = 0;
                //bool? DanhGiaReadOnly // DanhGia_ReadOnly
                user.DanhGiaReadOnly = false;
                //int? CreatedById // CreatedByID
                user.CreatedById = 1561;
                //DateTime? CreatedDate // CreatedDate
                user.CreatedDate = DateTime.Now;
                //int? ModifyById // ModifyByID
                //DateTime? ModifyDate // ModifyDate
                //string Aid // AID (length: 50)
                //DateTime? DueDate // DueDate
                //int? HrisTuCapBac // HRIS_TuCapBac
                user.HrisTuCapBac = 0;
                //int? CbTuCapBac // CB_TuCapBac
                user.CbTuCapBac = 0;

                //string EmailAccount // EmailAccount (length: 250)
                //=>KHÔNG TẠO CÁI NÀY, VÌ BỊ LỖI KHÔNG SỬA THÔNG TIN USER ĐƯỢC
                //user.EmailAccount = userName + "@haiphatland.com.vn"; 
                //string EmailPassword // EmailPassword (length: 250)
                //user.EmailPassword = "[81DC9BDB52D04DC20036DBD8313ED055]";

                //string NdHoVaTen // ND_HoVaTen (length: 50)
                user.NdHoVaTen = model.Ho + " " + model.HoTen;
                //string NdMaNhanVien // ND_MaNhanVien (length: 50)
                user.NdMaNhanVien = model.MaNhanVien;
                //string DeviceId // DeviceId (length: 500)
                //string Token // Token (length: 500)
                user.Token = "";
                //string RedirectUrl // RedirectURL (length: 250)

                //Cập nhật lại email vào hồ sơ nhân sự
                var newNv = db.NhanViens.FirstOrDefault(x => x.NhanVienId == model.NhanVienId);
                if (newNv != null)
                {
                    if (!string.IsNullOrEmpty(newNv.Email))
                    {
                        if (string.IsNullOrEmpty(newNv.EmailCaNhan))
                        {
                            newNv.EmailCaNhan = newNv.Email;
                            model.EmailCaNhan = newNv.Email;
                        }
                    }

                    newNv.Email = userName + "@haiphatland.com.vn";
                    model.Email = userName + "@haiphatland.com.vn";
                }

                db.SysNguoiDungs.Add(user);
                db.SaveChanges();//TODO
                db.Dispose();//=>Dispose de connection ben trong ham TaoQuyenNguoiDung dc mo

                //CẬP NHẬT QUYỀN CƠ BẢN NGƯỜI DÙNG
                TaoQuyenNguoiDung(user.NguoiDungId);//TODO
            }

            //Cap nhat lai userName moi cho Nhan Vien va return
            model.TenDangNhap = userName;

            return model;
        }

        public static NhanVienViewModel CreateUserHrm2(NhanVienViewModel model, string userName)
        {
            var db = new HrmDbContext();

            //Fix username
            var user = db.SysNguoiDungs.FirstOrDefault(x => x.TenDangNhap == model.TenDangNhap);
            if (user != null) //Fix lai user name
            {
                //Fix lai email va user name
                var newEmployee = db.NhanViens.FirstOrDefault(x => x.NhanVienId == model.NhanVienId);
                if (newEmployee != null)
                {
                    //Update lai Email ca nhan
                    if (!string.IsNullOrEmpty(newEmployee.Email))
                    {
                        if (string.IsNullOrEmpty(newEmployee.EmailCaNhan))
                        {
                            newEmployee.EmailCaNhan = newEmployee.Email;
                            model.EmailCaNhan = user.TenDangNhap.ToLower();
                        }
                    }

                    //Cap nhat lai email cty cua User
                    newEmployee.Email = userName + "@haiphatland.com.vn";
                    model.Email = userName + "@haiphatland.com.vn";
                }

                //Update lại userName
                user.TenDangNhap = userName;
                user.IsAdAccount = true;

                db.SaveChanges();
                db.Dispose();
            }
            else//TẠO MỚI USER
            {
                //Tạo người dùng
                user = new SysNguoiDung();
                //int NguoiDungId // NguoiDungID (Primary key)
                //int? NhanVienId // NhanVienId
                user.NhanVienId = model.NhanVienId;
                //string TenDangNhap // TenDangNhap (length: 50)
                user.TenDangNhap = userName;
                //string MatKhau // MatKhau (length: 50)
                user.MatKhau = "[81DC9BDB52D04DC20036DBD8313ED055]";
                //DateTime? LastLogin // LastLogin
                //DateTime? LastLogout // LastLogout
                //bool? Active // Active
                user.Active = true;
                //bool? IsPortalAccount // IsPortalAccount
                user.IsPortalAccount = true;
                //bool? IsAdAccount // IsADAccount
                user.IsAdAccount = true;
                //byte[] Settings // Settings (length: 2147483647)
                user.Settings = new byte[] { 0, 0 };
                //int? NhomNguoiDungId // NhomNguoiDungID
                user.NhomNguoiDungId = 0;
                //string ActiveModule // ActiveModule (length: 50)
                user.ActiveModule = "";
                //bool? IsDeleted // IsDeleted
                user.IsDeleted = false;
                //int? NhomQuyenId // NhomQuyenID
                user.NhomQuyenId = 7;
                //int? CapBacDanhGia // CapBacDanhGia
                user.CapBacDanhGia = 0;
                //bool? DanhGiaReadOnly // DanhGia_ReadOnly
                user.DanhGiaReadOnly = false;
                //int? CreatedById // CreatedByID
                user.CreatedById = 1561;
                //DateTime? CreatedDate // CreatedDate
                user.CreatedDate = DateTime.Now;
                //int? ModifyById // ModifyByID
                //DateTime? ModifyDate // ModifyDate
                //string Aid // AID (length: 50)
                //DateTime? DueDate // DueDate
                //int? HrisTuCapBac // HRIS_TuCapBac
                user.HrisTuCapBac = 0;
                //int? CbTuCapBac // CB_TuCapBac
                user.CbTuCapBac = 0;

                //string EmailAccount // EmailAccount (length: 250)
                //=>KHÔNG TẠO CÁI NÀY, VÌ BỊ LỖI KHÔNG SỬA THÔNG TIN USER ĐƯỢC
                //user.EmailAccount = userName + "@haiphatland.com.vn"; 
                //string EmailPassword // EmailPassword (length: 250)
                //user.EmailPassword = "[81DC9BDB52D04DC20036DBD8313ED055]";

                //string NdHoVaTen // ND_HoVaTen (length: 50)
                user.NdHoVaTen = model.Ho + " " + model.Ten;
                //string NdMaNhanVien // ND_MaNhanVien (length: 50)
                user.NdMaNhanVien = model.MaNhanVien;
                //string DeviceId // DeviceId (length: 500)
                //string Token // Token (length: 500)
                user.Token = "";
                //string RedirectUrl // RedirectURL (length: 250)

                //Cập nhật lại email vào hồ sơ nhân sự
                var newNv = db.NhanViens.FirstOrDefault(x => x.NhanVienId == model.NhanVienId);
                if (newNv != null)
                {
                    if (!string.IsNullOrEmpty(newNv.Email))
                    {
                        if (string.IsNullOrEmpty(newNv.EmailCaNhan))
                        {
                            newNv.EmailCaNhan = newNv.Email;
                            model.EmailCaNhan = newNv.Email;
                        }
                    }

                    newNv.Email = userName + "@haiphatland.com.vn";
                    model.Email = userName + "@haiphatland.com.vn";
                }

                db.SysNguoiDungs.Add(user);
                db.SaveChanges();
                db.Dispose();//=>Dispose de connection ben trong ham TaoQuyenNguoiDung dc mo

                //CẬP NHẬT QUYỀN CƠ BẢN NGƯỜI DÙNG
                TaoQuyenNguoiDung(user.NguoiDungId);
            }

            //Cap nhat lai userName moi cho Nhan Vien va return
            model.TenDangNhap = userName;

            return model;
        }

        public static NhanVienViewModel CreateUserHrm(string maNhanVien, string userName)
        {
            var db = new HrmDbContext();
            var nhanVien = new NhanVienViewModel();
            //var qr1 = db.SysNguoiDungs.Where(x => x.TenDangNhap == username);

            var query = from nv in db.NhanViens
                        join nd in db.SysNguoiDungs on nv.NhanVienId equals nd.NhanVienId into table1
                        from nd in table1.DefaultIfEmpty()
                        join cv in db.NsDsChucVus on nv.ChucVuId equals cv.ChucVuId into table2
                        from cv in table2.DefaultIfEmpty()
                        join cd in db.NsDsChucDanhs on nv.ChucDanhId equals cd.ChucDanhId into table3
                        from cd in table3.DefaultIfEmpty()
                        join p in db.PhongBans on nv.PhongBanId equals p.PhongBanId into table4
                        from p in table4.DefaultIfEmpty()
                        where nv.MaNhanVien == maNhanVien
                        select new NhanVienViewModel
                        {
                            NhanVienId = nv.NhanVienId,
                            Ho = nv.Ho,
                            Ten = nv.HoTen,
                            GioiTinh = nv.GioiTinh,
                            MaNhanVien = nv.MaNhanVien,
                            TenDangNhap = nd.TenDangNhap,
                            Email = nv.Email,
                            EmailCaNhan = nv.EmailCaNhan,
                            DienThoai = nv.DienThoai,
                            CMTND = nv.Cmtnd,
                            TenChucVu = cv.TenChucVu,
                            TenChucDanh = cd.TenChucDanh,
                            PhongBanId = p.PhongBanId,
                            TenPhongBan = p.Ten,
                            MaPhongBan = p.MaPhongBan
                        };

            var nvList = query.ToList();
            switch (nvList.ToList().Count)
            {
                case > 1:
                    nhanVien.TenDangNhap = nhanVien.TenDangNhap + " này bị trùng lặp, yêu cầu kiểm tra lại";
                    break;
                case 1:
                    nhanVien = nvList.FirstOrDefault();

                    //Fix username
                    var user = db.SysNguoiDungs.Where(x => x.TenDangNhap == nhanVien.TenDangNhap).FirstOrDefault();
                    if (user != null)
                    {
                        //Fix User người dùng
                        var strList = user.TenDangNhap.Split("@");
                        switch (strList.Length)
                        {
                            case 0:
                                //Tạo user mới hoàn toàn ở đây
                                //CASE NÀY KHÔNG THỂ XẢY RA
                                break;
                            case 1:
                                //fix lại các trường hợp lỗi gmail.com
                                var abc = UsernameGenerator.LikeString(user.TenDangNhap, "gmail.com");

                                if (abc)
                                {
                                    //Update new email và chuyển email cá nhân sang
                                    var newNv1 = db.NhanViens.FirstOrDefault(x => x.NhanVienId == nhanVien.NhanVienId);
                                    if (newNv1 != null)
                                    {
                                        newNv1.Email = userName + "@haiphatland.com.vn";
                                        newNv1.EmailCaNhan = user.TenDangNhap.ToLower();

                                        nhanVien.Email = userName + "@haiphatland.com.vn";
                                        nhanVien.EmailCaNhan = user.TenDangNhap.ToLower();
                                    }

                                    //Fix user name
                                    user.TenDangNhap = userName;
                                    db.SaveChanges();

                                    nhanVien.TenDangNhap = userName;
                                }

                                break;
                            case 2:
                                //get username

                                //Update new email và chuyển email cá nhân sang
                                var newNv2 = db.NhanViens.FirstOrDefault(x => x.NhanVienId == nhanVien.NhanVienId);
                                if (newNv2 != null)
                                {
                                    newNv2.Email = userName + "@haiphatland.com.vn";
                                    newNv2.EmailCaNhan = user.TenDangNhap.ToLower();

                                    nhanVien.Email = userName + "@haiphatland.com.vn";
                                    nhanVien.EmailCaNhan = user.TenDangNhap.ToLower();
                                }

                                //Fix user name
                                user.TenDangNhap = userName;
                                db.SaveChanges();
                                db.SaveChanges();
                                db.Dispose();

                                nhanVien.TenDangNhap = userName;

                                break;
                            default:

                                break;
                        }
                    }
                    else//TẠO MỚI USER
                    {
                        //Tạo người dùng
                        user = new SysNguoiDung();
                        //int NguoiDungId // NguoiDungID (Primary key)
                        //int? NhanVienId // NhanVienId
                        user.NhanVienId = nhanVien.NhanVienId;
                        //string TenDangNhap // TenDangNhap (length: 50)
                        user.TenDangNhap = userName;
                        //string MatKhau // MatKhau (length: 50)
                        user.MatKhau = "[81DC9BDB52D04DC20036DBD8313ED055]";
                        //DateTime? LastLogin // LastLogin
                        //DateTime? LastLogout // LastLogout
                        //bool? Active // Active
                        user.Active = true;
                        //bool? IsPortalAccount // IsPortalAccount
                        user.IsPortalAccount = true;
                        //bool? IsAdAccount // IsADAccount
                        user.IsAdAccount = true;
                        //byte[] Settings // Settings (length: 2147483647)
                        user.Settings = new byte[] { 0, 0 };
                        //int? NhomNguoiDungId // NhomNguoiDungID
                        user.NhomNguoiDungId = 0;
                        //string ActiveModule // ActiveModule (length: 50)
                        user.ActiveModule = "";
                        //bool? IsDeleted // IsDeleted
                        user.IsDeleted = false;
                        //int? NhomQuyenId // NhomQuyenID
                        user.NhomQuyenId = 7;
                        //int? CapBacDanhGia // CapBacDanhGia
                        user.CapBacDanhGia = 0;
                        //bool? DanhGiaReadOnly // DanhGia_ReadOnly
                        user.DanhGiaReadOnly = false;
                        //int? CreatedById // CreatedByID
                        user.CreatedById = 1561;
                        //DateTime? CreatedDate // CreatedDate
                        user.CreatedDate = DateTime.Now;
                        //int? ModifyById // ModifyByID
                        //DateTime? ModifyDate // ModifyDate
                        //string Aid // AID (length: 50)
                        //DateTime? DueDate // DueDate
                        //int? HrisTuCapBac // HRIS_TuCapBac
                        user.HrisTuCapBac = 0;
                        //int? CbTuCapBac // CB_TuCapBac
                        user.CbTuCapBac = 0;

                        //string EmailAccount // EmailAccount (length: 250)
                        //=>KHÔNG TẠO CÁI NÀY, VÌ BỊ LỖI KHÔNG SỬA THÔNG TIN USER ĐƯỢC
                        //user.EmailAccount = userName + "@haiphatland.com.vn"; 
                        //string EmailPassword // EmailPassword (length: 250)
                        //user.EmailPassword = "[81DC9BDB52D04DC20036DBD8313ED055]";

                        //string NdHoVaTen // ND_HoVaTen (length: 50)
                        user.NdHoVaTen = nhanVien.Ho + " " + nhanVien.Ten;
                        //string NdMaNhanVien // ND_MaNhanVien (length: 50)
                        user.NdMaNhanVien = nhanVien.MaNhanVien;
                        //string DeviceId // DeviceId (length: 500)
                        //string Token // Token (length: 500)
                        user.Token = "";
                        //string RedirectUrl // RedirectURL (length: 250)

                        //Cập nhật lại email vào hồ sơ nhân sự
                        var newNv1 = db.NhanViens.FirstOrDefault(x => x.NhanVienId == nhanVien.NhanVienId);

                        if (newNv1 != null)
                        {
                            if (!string.IsNullOrEmpty(newNv1.Email))
                            {
                                if (string.IsNullOrEmpty(newNv1.EmailCaNhan))
                                {
                                    newNv1.EmailCaNhan = newNv1.Email;
                                }
                            }

                            newNv1.Email = user.EmailAccount;
                        }

                        //Cap nhat lai thong tin dang nhap
                        nhanVien.TenDangNhap = userName;

                        db.SysNguoiDungs.Add(user);
                        db.SaveChanges();
                        db.Dispose();

                        //CẬP NHẬT QUYỀN CƠ BẢN NGƯỜI DÙNG
                        TaoQuyenNguoiDung(user.NguoiDungId);
                    }

                    break;

                default:
                    nhanVien = null;
                    break;
            }

            return nhanVien;
        }

        public static NhanVienViewModel FixNhanVienTheoUsername(string userName)
        {
            var db = new HrmDbContext();
            var nhanVien = new NhanVienViewModel();
            //var qr1 = db.SysNguoiDungs.Where(x => x.TenDangNhap == username);

            var nvList = from nv in db.NhanViens
                         join nd in db.SysNguoiDungs on nv.NhanVienId equals nd.NhanVienId into table1
                         from nd in table1.DefaultIfEmpty()
                         join cv in db.NsDsChucVus on nv.ChucVuId equals cv.ChucVuId into table2
                         from cv in table2.DefaultIfEmpty()
                         join cd in db.NsDsChucDanhs on nv.ChucDanhId equals cd.ChucDanhId into table3
                         from cd in table3.DefaultIfEmpty()
                         join p in db.PhongBans on nv.PhongBanId equals p.PhongBanId into table4
                         from p in table4.DefaultIfEmpty()
                         where nd.TenDangNhap == userName
                         select new NhanVienViewModel
                         {
                             NhanVienId = nv.NhanVienId,
                             Ho = nv.Ho,
                             Ten = nv.HoTen,
                             GioiTinh = nv.GioiTinh,
                             MaNhanVien = nv.MaNhanVien,
                             TenDangNhap = nd.TenDangNhap,
                             Email = nv.Email,
                             EmailCaNhan = nv.EmailCaNhan,
                             DienThoai = nv.DienThoai,
                             CMTND = nv.Cmtnd,
                             TenChucVu = cv.TenChucVu,
                             TenChucDanh = cd.TenChucDanh,
                             PhongBanId = p.PhongBanId,
                             TenPhongBan = p.Ten,
                             MaPhongBan = p.MaPhongBan
                         };

            switch (nvList.ToList().Count)
            {
                case > 1:
                    nhanVien.TenDangNhap = "Username này bị trùng lặp, yêu cầu kiểm tra lại";
                    break;
                case 1:
                    nhanVien = nvList.FirstOrDefault();
                    var pbList = db.PhongBans.ToList();

                    //Lay co cau phong ban cua Nhan Su
                    var child = pbList.First(x => nhanVien != null && x.PhongBanId == nhanVien.PhongBanId);
                    var parents = FindAllParents(pbList, child).ToList();

                    int index = 0;
                    if (nhanVien != null)
                    {
                        foreach (var phongBan in parents)
                        {
                            index++;
                            switch (index)
                            {
                                case 1:
                                    nhanVien.PhongBanCha = phongBan.Ten;
                                    nhanVien.MaCha = phongBan.MaPhongBan;
                                    break;
                                case 2:
                                    nhanVien.PhongBanOng = phongBan.Ten;
                                    nhanVien.MaOng = phongBan.MaPhongBan;
                                    break;
                                case 3:
                                    nhanVien.PhongBanCo = phongBan.Ten;
                                    nhanVien.MaCo = phongBan.MaPhongBan;
                                    break;
                                case 4:
                                    nhanVien.PhongBanKy = phongBan.Ten;
                                    nhanVien.MaKy = phongBan.MaPhongBan;
                                    break;
                                case 5:
                                    nhanVien.PhongBan6 = phongBan.Ten;
                                    nhanVien.MaPb6 = phongBan.MaPhongBan;
                                    break;
                            }
                        }
                    }

                    //Fix username
                    var user = db.SysNguoiDungs.FirstOrDefault(x => x.TenDangNhap == userName);
                    if (user != null)
                    {
                        //Check User tren AD

                        //Fix Email
                        string email = userName + "@haiphatland.com.vn";
                        if (nhanVien.Email.Equals(email))
                        {

                        }

                    }

                    break;
            }

            return nhanVien;
        }

        public static NhanVienViewModel FixUsernameNhanVien(string maNhanVien)
        {
            var db = new HrmDbContext();
            var nhanVien = new NhanVienViewModel();
            //var qr1 = db.SysNguoiDungs.Where(x => x.TenDangNhap == username);

            var query = from nv in db.NhanViens
                        join nd in db.SysNguoiDungs on nv.NhanVienId equals nd.NhanVienId into table1
                        from nd in table1.DefaultIfEmpty()
                        join cv in db.NsDsChucVus on nv.ChucVuId equals cv.ChucVuId into table2
                        from cv in table2.DefaultIfEmpty()
                        join cd in db.NsDsChucDanhs on nv.ChucDanhId equals cd.ChucDanhId into table3
                        from cd in table3.DefaultIfEmpty()
                        join p in db.PhongBans on nv.PhongBanId equals p.PhongBanId into table4
                        from p in table4.DefaultIfEmpty()
                        where nv.MaNhanVien == maNhanVien
                        select new NhanVienViewModel
                        {
                            NhanVienId = nv.NhanVienId,
                            Ho = nv.Ho,
                            Ten = nv.HoTen,
                            GioiTinh = nv.GioiTinh,
                            MaNhanVien = nv.MaNhanVien,
                            TenDangNhap = nd.TenDangNhap,
                            Email = nv.Email,
                            EmailCaNhan = nv.EmailCaNhan,
                            DienThoai = nv.DienThoai,
                            CMTND = nv.Cmtnd,
                            TenChucVu = cv.TenChucVu,
                            TenChucDanh = cd.TenChucDanh,
                            PhongBanId = p.PhongBanId,
                            TenPhongBan = p.Ten,
                            MaPhongBan = p.MaPhongBan
                        };

            var nvList = query.ToList();
            switch (nvList.ToList().Count)
            {
                case > 1:
                    nhanVien.TenDangNhap = "Username này bị trùng lặp, yêu cầu kiểm tra lại";
                    break;
                case 1:
                    nhanVien = nvList.FirstOrDefault();
                    var pbList = db.PhongBans.ToList();

                    var child = pbList.First(x => nhanVien != null && x.PhongBanId == nhanVien.PhongBanId);
                    var parents = FindAllParents(pbList, child).ToList();

                    int index = 0;
                    if (nhanVien != null)
                    {
                        foreach (var phongBan in parents)
                        {
                            index++;
                            switch (index)
                            {
                                case 1:
                                    nhanVien.PhongBanCha = phongBan.Ten;
                                    nhanVien.MaCha = phongBan.MaPhongBan;
                                    break;
                                case 2:
                                    nhanVien.PhongBanOng = phongBan.Ten;
                                    nhanVien.MaOng = phongBan.MaPhongBan;
                                    break;
                                case 3:
                                    nhanVien.PhongBanCo = phongBan.Ten;
                                    nhanVien.MaCo = phongBan.MaPhongBan;
                                    break;
                                case 4:
                                    nhanVien.PhongBanKy = phongBan.Ten;
                                    nhanVien.MaKy = phongBan.MaPhongBan;
                                    break;
                                case 5:
                                    nhanVien.PhongBan6 = phongBan.Ten;
                                    nhanVien.MaPb6 = phongBan.MaPhongBan;
                                    break;
                            }
                        }
                    }
                    //Fix username
                    var user = db.SysNguoiDungs.Where(x => x.TenDangNhap == nhanVien.TenDangNhap).FirstOrDefault();
                    if (user != null)
                    {
                        //Fix User người dùng
                        var strList = user.TenDangNhap.Split("@");
                        switch (strList.Length)
                        {
                            case 0:
                                //Tạo user mới hoàn toàn ở đây
                                break;
                            case 1:
                                //fix lại các trường hợp lỗi gmail.com
                                var abc = UsernameGenerator.LikeString(user.TenDangNhap, "gmail.com");

                                if (abc)
                                {
                                    var username1 = UsernameGenerator.CreateUsernameFromName(nhanVien.Ho, nhanVien.Ten);
                                    var newUsername1 = UsernameGenerator.CreateNewUsername(username1);

                                    //Update new email và chuyển email cá nhân sang
                                    var newNv1 = db.NhanViens.FirstOrDefault(x => x.NhanVienId == nhanVien.NhanVienId);
                                    if (newNv1 != null)
                                    {
                                        newNv1.Email = newUsername1 + "@haiphatland.com.vn";
                                        newNv1.EmailCaNhan = user.TenDangNhap.ToLower();

                                        nhanVien.Email = newUsername1 + "@haiphatland.com.vn";
                                        nhanVien.EmailCaNhan = user.TenDangNhap.ToLower();
                                    }

                                    //Fix user name
                                    user.TenDangNhap = newUsername1;
                                    db.SaveChanges();

                                    nhanVien.TenDangNhap = newUsername1;
                                }

                                break;
                            case 2:
                                //get username
                                var username2 = UsernameGenerator.CreateUsernameFromName(nhanVien.Ho, nhanVien.Ten);
                                var newUsername2 = UsernameGenerator.CreateNewUsername(username2);

                                //Update new email và chuyển email cá nhân sang
                                var newNv2 = db.NhanViens.FirstOrDefault(x => x.NhanVienId == nhanVien.NhanVienId);
                                if (newNv2 != null)
                                {
                                    newNv2.Email = newUsername2 + "@haiphatland.com.vn";
                                    newNv2.EmailCaNhan = user.TenDangNhap.ToLower();

                                    nhanVien.Email = newUsername2 + "@haiphatland.com.vn";
                                    nhanVien.EmailCaNhan = user.TenDangNhap.ToLower();
                                }

                                //Fix user name
                                user.TenDangNhap = newUsername2;
                                db.SaveChanges();

                                nhanVien.TenDangNhap = newUsername2;

                                break;
                            default:

                                break;
                        }
                    }
                    else
                    {
                        //Tạo người dùng
                        user = new SysNguoiDung();
                        //int NguoiDungId // NguoiDungID (Primary key)
                        //int? NhanVienId // NhanVienId
                        user.NhanVienId = nhanVien.NhanVienId;
                        //string TenDangNhap // TenDangNhap (length: 50)
                        var username = UsernameGenerator.CreateUsernameFromName(nhanVien.Ho, nhanVien.Ten);
                        var newUsername = UsernameGenerator.CreateNewUsername(username);
                        user.TenDangNhap = newUsername;
                        //string MatKhau // MatKhau (length: 50)
                        user.MatKhau = "[81DC9BDB52D04DC20036DBD8313ED055]";
                        //DateTime? LastLogin // LastLogin
                        //DateTime? LastLogout // LastLogout
                        //bool? Active // Active
                        user.Active = true;
                        //bool? IsPortalAccount // IsPortalAccount
                        user.IsPortalAccount = true;
                        //bool? IsAdAccount // IsADAccount
                        user.IsAdAccount = true;
                        //byte[] Settings // Settings (length: 2147483647)
                        user.Settings = new byte[] { 0, 0 };
                        //int? NhomNguoiDungId // NhomNguoiDungID
                        user.NhomNguoiDungId = 0;
                        //string ActiveModule // ActiveModule (length: 50)
                        user.ActiveModule = "";
                        //bool? IsDeleted // IsDeleted
                        user.IsDeleted = false;
                        //int? NhomQuyenId // NhomQuyenID
                        user.NhomQuyenId = 7;
                        //int? CapBacDanhGia // CapBacDanhGia
                        user.CapBacDanhGia = 0;
                        //bool? DanhGiaReadOnly // DanhGia_ReadOnly
                        user.DanhGiaReadOnly = false;
                        //int? CreatedById // CreatedByID
                        user.CreatedById = 1561;
                        //DateTime? CreatedDate // CreatedDate
                        user.CreatedDate = DateTime.Now;
                        //int? ModifyById // ModifyByID
                        //DateTime? ModifyDate // ModifyDate
                        //string Aid // AID (length: 50)
                        //DateTime? DueDate // DueDate
                        //int? HrisTuCapBac // HRIS_TuCapBac
                        user.HrisTuCapBac = 0;
                        //int? CbTuCapBac // CB_TuCapBac
                        user.CbTuCapBac = 0;
                        //string EmailAccount // EmailAccount (length: 250)
                        user.EmailAccount = newUsername + "@haiphatland.com.vn";
                        //string EmailPassword // EmailPassword (length: 250)
                        user.EmailPassword = "[81DC9BDB52D04DC20036DBD8313ED055]";
                        //string NdHoVaTen // ND_HoVaTen (length: 50)
                        user.NdHoVaTen = nhanVien.Ho + " " + nhanVien.Ten;
                        //string NdMaNhanVien // ND_MaNhanVien (length: 50)
                        user.NdMaNhanVien = nhanVien.MaNhanVien;
                        //string DeviceId // DeviceId (length: 500)
                        //string Token // Token (length: 500)
                        user.Token = "";
                        //string RedirectUrl // RedirectURL (length: 250)

                        //Cập nhật Quyền ở đây
                        //TODO

                        //Cập nhật lại email vào hồ sơ nhân sự
                        var newNv1 = db.NhanViens.FirstOrDefault(x => x.NhanVienId == nhanVien.NhanVienId);

                        if (newNv1 != null)
                        {
                            if (!string.IsNullOrEmpty(newNv1.Email))
                            {
                                if (string.IsNullOrEmpty(newNv1.EmailCaNhan))
                                {
                                    newNv1.EmailCaNhan = newNv1.Email;
                                }
                            }

                            newNv1.Email = user.EmailAccount;
                        }

                        //Lấy thông tin người dùng để tạo email
                        nhanVien.TenDangNhap = newUsername;

                        db.SysNguoiDungs.Add(user);
                        db.SaveChanges();

                        //CẬP NHẬT QUYỀN CƠ BẢN NGƯỜI DÙNG
                        TaoQuyenNguoiDung(user.NguoiDungId);
                    }

                    break;

                default:
                    nhanVien = null;
                    break;
            }

            return nhanVien;
        }

        public static void TaoQuyenNguoiDung(int nguoiDungId)
        {
            var db = new HrmDbContext();
            //int NguoiDungQuyenId // NguoiDungQuyenID (Primary key)
            //int? NguoiDungId // NguoiDungID
            //string QuyenId // QuyenID (length: 50)
            //int? ThaoTac // ThaoTac
            //string Action // Action (length: 250)
            //string Controller // Controller (length: 250)
            //int? XetDuyet // XetDuyet
            //string TenQuyen // TenQuyen (length: 250)

            string[] dsQuyen = new string[]
            {
                "PORTAL_SELFSRV",
                "PORTAL_Attendance",
                "PORTAL_Payslip",
                "PORTAL_Evaluation",
                "PORTAL_Leave",
                "PORTAL_OT",
                "PORTAL_Mission",
                "PORTAL_ORG",
                "PORTAL_HRIS",
                "PORTAL_REPORT",
                "PORTAL_Experiences",
                "PORTAL_Experiences_NEW",
                "PORTAL_Trainning",
                "PORTAL_Trainning_NEW",
                "PORTAL_ChildReward",
                "PORTAL_ChildReward_NEW",
                "PORTAL_RelationShip",
                "PORTAL_Relationship_NEW",
                "HRPortal"
            };
            foreach (var s in dsQuyen)
            {
                var q1 = new SysNguoiDungQuyen();
                q1.NguoiDungId = nguoiDungId;
                q1.QuyenId = s;
                q1.ThaoTac = 1;
                q1.Action = "";
                q1.Controller = "";
                q1.XetDuyet = 1;
                q1.TenQuyen = "";
                db.SysNguoiDungQuyens.Add(q1);
            }

            db.SaveChanges();
        }

        public static SysNguoiDung GetSysNguoiDung(int nhanVienId)
        {
            var db = new HrmDbContext();
            return db.SysNguoiDungs.FirstOrDefault(x => x.NhanVienId == nhanVienId);
        }

        public static SysNguoiDung GetSysNguoiDung(string username)
        {
            var db = new HrmDbContext();
            return db.SysNguoiDungs.FirstOrDefault(x => x.TenDangNhap == username);
        }

        public static NhanVienViewModel GetNhanVienByUsername(string username)
        {
            var db = new HrmDbContext();
            var nhanVien = new NhanVienViewModel();
            //var qr1 = db.SysNguoiDungs.Where(x => x.TenDangNhap == username);

            var query = from u in db.SysNguoiDungs.ToList()
                        join n in db.NhanViens on u.NhanVienId equals n.NhanVienId into table1
                        from n in table1.ToList()
                        join cv in db.NsDsChucVus on n.ChucVuId equals cv.ChucVuId into table2
                        from cv in table2.DefaultIfEmpty()
                        join cd in db.NsDsChucDanhs on n.ChucDanhId equals cd.ChucDanhId into table3
                        from cd in table3.DefaultIfEmpty()
                        join p in db.PhongBans on n.PhongBanId equals p.PhongBanId into table4
                        from p in table4.DefaultIfEmpty()
                        where u.TenDangNhap == username
                        select new NhanVienViewModel
                        {
                            NhanVienId = n.NhanVienId,
                            Ho = n.Ho,
                            Ten = n.HoTen,
                            GioiTinh = n.GioiTinh,
                            MaNhanVien = n.MaNhanVien,
                            TenDangNhap = u.TenDangNhap,
                            Email = n.Email,
                            EmailCaNhan = n.EmailCaNhan,
                            DienThoai = n.DienThoai,
                            CMTND = n.Cmtnd,
                            TenChucVu = cv?.TenChucVu,
                            TenChucDanh = cd?.TenChucDanh,
                            PhongBanId = p.PhongBanId,
                            TenPhongBan = p.Ten,
                            MaPhongBan = p.MaPhongBan
                        };

            var nvList = query.ToList();
            switch (nvList.ToList().Count)
            {
                case > 1:
                    nhanVien.TenDangNhap = "Username này bị trùng lặp, yêu cầu kiểm tra lại";
                    break;
                case 1:
                    nhanVien = nvList.FirstOrDefault();
                    var pbList = db.PhongBans.ToList();

                    var child = pbList.First(x => nhanVien != null && x.PhongBanId == nhanVien.PhongBanId);
                    var parents = FindAllParents(pbList, child).ToList();

                    int index = 0;
                    if (nhanVien != null)
                    {
                        foreach (var phongBan in parents)
                        {
                            index++;
                            switch (index)
                            {
                                case 1:
                                    nhanVien.PhongBanCha = phongBan.Ten;
                                    nhanVien.MaCha = phongBan.MaPhongBan;
                                    break;
                                case 2:
                                    nhanVien.PhongBanOng = phongBan.Ten;
                                    nhanVien.MaOng = phongBan.MaPhongBan;
                                    break;
                                case 3:
                                    nhanVien.PhongBanCo = phongBan.Ten;
                                    nhanVien.MaCo = phongBan.MaPhongBan;
                                    break;
                                case 4:
                                    nhanVien.PhongBanKy = phongBan.Ten;
                                    nhanVien.MaKy = phongBan.MaPhongBan;
                                    break;
                                case 5:
                                    nhanVien.PhongBan6 = phongBan.Ten;
                                    nhanVien.MaPb6 = phongBan.MaPhongBan;
                                    break;
                            }
                        }
                    }

                    break;

                default:
                    nhanVien = null;
                    break;
            }

            return nhanVien;
        }

        public static NhanVienViewModel2 GetNhanVienByNhanVienId2(int nhanVienId)
        {
            var db = new HrmDbContext();
            var nv = new NhanVienViewModel2();

            var hs = db.NhanViens.FirstOrDefault(x => x.NhanVienId == nhanVienId);

            //int NhanVienId
            nv.NhanVienId = hs.NhanVienId;
            //string Ho
            nv.Ho = hs.Ho;
            //string Ten
            nv.Ten = hs.HoTen;
            //string GioiTinh
            nv.GioiTinh = hs.GioiTinh;
            //string MaNhanVien
            nv.MaNhanVien = hs.MaNhanVien;
            //string Email
            nv.Email = hs.Email;
            //string EmailCaNhan
            nv.EmailCaNhan = hs.EmailCaNhan;
            //string DienThoai
            nv.DienThoai = hs.DienThoai;
            //string CMTND
            nv.Cmnd = hs.Cmtnd;
            //THÔNG TIN USER
            //string TenDangNhap
            var nd = db.SysNguoiDungs.FirstOrDefault(x => x.NhanVienId == hs.NhanVienId);
            if (nd != null)
            {
                nv.TenDangNhap = nd.TenDangNhap;
            }
            //CHỨC VỤ
            //string TenChucVu
            var cv = db.NsDsChucVus.FirstOrDefault(x => x.ChucVuId == hs.ChucVuId);
            if (cv != null)
            {
                nv.TenChucVu = cv.TenChucVu;
            }
            //CHỨC DANH
            //string TenChucDanh
            var cd = db.NsDsChucDanhs.FirstOrDefault(x => x.ChucDanhId == hs.ChucDanhId);
            if (cd != null)
            {
                nv.TenChucDanh = cd.TenChucDanh;
            }
            //PHÒNG BAN
            //int PhongBanId
            if (hs.PhongBanId != null)
            {
                nv.PhongBanId = hs.PhongBanId;
                var pb = db.PhongBans.FirstOrDefault(x => x.PhongBanId == hs.PhongBanId);
                if (pb != null)
                {
                    //string TenPhongBan
                    nv.TenPhongBan = pb.Ten;
                    //string MaPhongBan
                    nv.MaPhongBan = pb.MaPhongBan;
                }

                //PHÒNG BAN CẤP 1
                //int PhongBanCap1Id
                var pbCap1 = GetPhongBanCap1CuaNhanVienTheoPbId(hs.PhongBanId.Value);
                if (pbCap1 != null)
                {
                    nv.PhongBanCap1Id = pbCap1.PhongBanId;
                    nv.TenPhongBanCap1 = pbCap1.Ten;
                    nv.MaPhongBanCap1 = pbCap1.MaPhongBan;
                }
            }

            return nv;
        }

        public static NhanVienViewModel GetNhanVienByNhanVienId(int nhanVienId)
        {
            var db = new HrmDbContext();
            var nhanVien = new NhanVienViewModel();
            //var qr1 = db.SysNguoiDungs.Where(x => x.TenDangNhap == username);

            var query = from n in db.NhanViens.ToList()
                        join u in db.SysNguoiDungs on n.NhanVienId equals u.NhanVienId into table1
                        from u in table1.DefaultIfEmpty()
                        join cv in db.NsDsChucVus on n.ChucVuId equals cv.ChucVuId into table2
                        from cv in table2.DefaultIfEmpty()
                        join cd in db.NsDsChucDanhs on n.ChucDanhId equals cd.ChucDanhId into table3
                        from cd in table3.DefaultIfEmpty()
                        join p in db.PhongBans on n.PhongBanId equals p.PhongBanId into table4
                        from p in table4.DefaultIfEmpty()
                        where n.NhanVienId == nhanVienId
                        select new NhanVienViewModel
                        {
                            NhanVienId = n.NhanVienId,
                            Ho = n.Ho,
                            Ten = n.HoTen,
                            GioiTinh = n.GioiTinh,
                            MaNhanVien = n.MaNhanVien,
                            TenDangNhap = u?.TenDangNhap,
                            Email = n.Email,
                            EmailCaNhan = n.EmailCaNhan,
                            DienThoai = n.DienThoai,
                            CMTND = n.Cmtnd,
                            TenChucVu = cv?.TenChucVu,
                            TenChucDanh = cd?.TenChucDanh,
                            PhongBanId = p.PhongBanId,
                            TenPhongBan = p.Ten,
                            MaPhongBan = p.MaPhongBan
                        };

            var nvList = query.ToList();
            switch (nvList.ToList().Count)
            {
                case > 1:
                    nhanVien = null;
                    //MailHelper.EmailSender("Không tìm thấy nhân sự trên HRM theo NhanVienId=" + model.NhanVienId,
                    //    "[ACM] Lỗi nhân sự tracking",
                    //    "baonxvn@gmail.com");
                    break;
                case 1:
                    nhanVien = nvList.FirstOrDefault();
                    break;
                default:
                    nhanVien = null;
                    break;
            }

            return nhanVien;
        }

        public static NhanVienViewModel GetNhanVienByMaNhanVien(string maNhanVien)
        {
            var db = new HrmDbContext();
            var nhanVien = new NhanVienViewModel();
            //var qr1 = db.SysNguoiDungs.Where(x => x.TenDangNhap == username);

            var query = from n in db.NhanViens
                        join u in db.SysNguoiDungs on n.NhanVienId equals u.NhanVienId into table1
                        from u in table1.DefaultIfEmpty()
                        join cv in db.NsDsChucVus on n.ChucVuId equals cv.ChucVuId into table2
                        from cv in table2.DefaultIfEmpty()
                        join cd in db.NsDsChucDanhs on n.ChucDanhId equals cd.ChucDanhId into table3
                        from cd in table3.DefaultIfEmpty()
                        join p in db.PhongBans on n.PhongBanId equals p.PhongBanId into table4
                        from p in table4.DefaultIfEmpty()
                        where n.MaNhanVien == maNhanVien
                        select new NhanVienViewModel
                        {
                            NhanVienId = n.NhanVienId,
                            Ho = n.Ho,
                            Ten = n.HoTen,
                            GioiTinh = n.GioiTinh,
                            MaNhanVien = n.MaNhanVien,
                            TenDangNhap = u.TenDangNhap,
                            Email = n.Email,
                            EmailCaNhan = n.EmailCaNhan,
                            DienThoai = n.DienThoai,
                            CMTND = n.Cmtnd,
                            TenChucVu = cv.TenChucVu,
                            TenChucDanh = cd.TenChucDanh,
                            PhongBanId = p.PhongBanId,
                            TenPhongBan = p.Ten,
                            MaPhongBan = p.MaPhongBan
                        };

            var nvList = query.ToList();
            switch (nvList.ToList().Count)
            {
                case > 1:
                    nhanVien.TenDangNhap = "Username này bị trùng lặp, yêu cầu kiểm tra lại";
                    break;
                case 1:
                    nhanVien = nvList.FirstOrDefault();
                    var pbList = db.PhongBans.ToList();

                    var child = pbList.First(x => nhanVien != null && x.PhongBanId == nhanVien.PhongBanId);
                    var parents = FindAllParents(pbList, child).ToList();

                    int index = 0;
                    if (nhanVien != null)
                    {
                        foreach (var phongBan in parents)
                        {
                            index++;
                            switch (index)
                            {
                                case 1:
                                    nhanVien.PhongBanCha = phongBan.Ten;
                                    nhanVien.MaCha = phongBan.MaPhongBan;
                                    break;
                                case 2:
                                    nhanVien.PhongBanOng = phongBan.Ten;
                                    nhanVien.MaOng = phongBan.MaPhongBan;
                                    break;
                                case 3:
                                    nhanVien.PhongBanCo = phongBan.Ten;
                                    nhanVien.MaCo = phongBan.MaPhongBan;
                                    break;
                                case 4:
                                    nhanVien.PhongBanKy = phongBan.Ten;
                                    nhanVien.MaKy = phongBan.MaPhongBan;
                                    break;
                                case 5:
                                    nhanVien.PhongBan6 = phongBan.Ten;
                                    nhanVien.MaPb6 = phongBan.MaPhongBan;
                                    break;
                            }
                        }
                    }

                    break;

                default:
                    nhanVien = null;
                    break;
            }

            return nhanVien;
        }

        public static PhongBan GetPhongBanCap1CuaNhanVienTheoPbId(int phongBanId)
        {
            var dbHrm = new HrmDbContext();
            var dbHpl = new AbpHplDbContext();

            var listIdHrm = GetAllChildrenAndParents(phongBanId).Select(x => x.PhongBanId);
            var listIdAbp = dbHpl.HplPhongBans.Select(x => x.PhongBanId);
            var listIds = listIdHrm.Intersect(listIdAbp).ToList();
            switch (listIds.Count)
            {
                case 1:
                    return dbHrm.PhongBans.FirstOrDefault(x => listIds.Contains(x.PhongBanId));
                case < 1:
                case > 1:
                    //TODO
                    break;
            }

            dbHrm.Dispose();
            dbHpl.Dispose();

            return null;
        }

        /// <summary>
        /// Lấy thông tin phòng ban Cấp 1 của Nhân Viên theo Ma Nhân Viên
        /// </summary>
        /// <param name="maNhanVien"></param>
        /// <returns></returns>
        public static PhongBan GetPhongBanCap1CuaNhanVien(string maNhanVien)
        {
            var dbHrm = new HrmDbContext();
            var dbHpl = new AbpHplDbContext();
            var listNvs = dbHrm.NhanViens.Where(x => x.MaNhanVien == maNhanVien);

            if (listNvs.Count() == 1)
            {
                var pb = listNvs.FirstOrDefault();
                if (pb?.PhongBanId != null)
                {
                    int phongBanId = pb.PhongBanId.Value;
                    var listIdHrm = GetAllChildrenAndParents(phongBanId).Select(x => x.PhongBanId);
                    var listIdAbp = dbHpl.HplPhongBans.Select(x => x.PhongBanId);
                    var listIds = listIdHrm.Intersect(listIdAbp).ToList();
                    switch (listIds.Count())
                    {
                        case 1:
                            return dbHrm.PhongBans.FirstOrDefault(x => listIds.Contains(x.PhongBanId));
                        case < 1:
                        case > 1:
                            //TODO
                            break;
                    }
                }
            }
            dbHrm.Dispose();
            dbHpl.Dispose();

            return null;
        }

        public static NhanVienViewModel2 GetNhanVienFullByNhanVien(NhanVien hs, List<SysNguoiDung> lstNds, List<NsDsChucVu> lstCvs,
            List<NsDsChucDanh> lstCds, List<PhongBan> lstPbs)
        {
            var nv = new NhanVienViewModel2();

            //int NhanVienId
            nv.NhanVienId = hs.NhanVienId;
            //string Ho
            nv.Ho = hs.Ho;
            //string Ten
            nv.Ten = hs.HoTen;
            //string GioiTinh
            nv.GioiTinh = hs.GioiTinh;
            //string MaNhanVien
            nv.MaNhanVien = hs.MaNhanVien;
            //string Email
            nv.Email = hs.Email;
            //string EmailCaNhan
            nv.EmailCaNhan = hs.EmailCaNhan;
            //string DienThoai
            nv.DienThoai = hs.DienThoai;
            //string CMTND
            nv.Cmnd = hs.Cmtnd;
            //THÔNG TIN USER
            //string TenDangNhap
            var nd = lstNds.FirstOrDefault(x => x.NhanVienId == hs.NhanVienId);
            if (nd != null)
            {
                nv.TenDangNhap = nd.TenDangNhap;
            }
            //CHỨC VỤ
            //string TenChucVu
            var cv = lstCvs.FirstOrDefault(x => x.ChucVuId == hs.ChucVuId);
            if (cv != null)
            {
                nv.TenChucVu = cv.TenChucVu;
            }
            //CHỨC DANH
            //string TenChucDanh
            var cd = lstCds.FirstOrDefault(x => x.ChucDanhId == hs.ChucDanhId);
            if (cd != null)
            {
                nv.TenChucDanh = cd.TenChucDanh;
            }
            //PHÒNG BAN
            //int PhongBanId
            if (hs.PhongBanId != null)
            {
                nv.PhongBanId = hs.PhongBanId;
                var pb = lstPbs.FirstOrDefault(x => x.PhongBanId == hs.PhongBanId);
                if (pb != null)
                {
                    //string TenPhongBan
                    nv.TenPhongBan = pb.Ten;
                    //string MaPhongBan
                    nv.MaPhongBan = pb.MaPhongBan;
                }

                //PHÒNG BAN CẤP 1
                //int PhongBanCap1Id
                var pbCap1 = GetPhongBanCap1CuaNhanVienTheoPbId(hs.PhongBanId.Value);
                if (pbCap1 != null)
                {
                    nv.PhongBanCap1Id = pbCap1.PhongBanId;
                    nv.TenPhongBanCap1 = pbCap1.Ten;
                    nv.MaPhongBanCap1 = pbCap1.MaPhongBan;
                }
                else
                {
                    //GỬI MAIL THÔNG BÁO KO XÁC ĐỊNH PB CẤP 1
                }
            }

            return nv;
        }

        public static NhanVienViewModel2? GetNhanVienByEmailAndMaNhanVien(string email, string maNhanVien)
        {
            var db = new HrmDbContext();
            var nv = new NhanVienViewModel2();
            var hs = db.NhanViens.FirstOrDefault(x => x.Email == email & x.MaNhanVien == maNhanVien);
            if (hs != null)
            {
                //int NhanVienId
                nv.NhanVienId = hs.NhanVienId;
                //string Ho
                nv.Ho = hs.Ho;
                //string Ten
                nv.Ten = hs.HoTen;
                //string GioiTinh
                nv.GioiTinh = hs.GioiTinh;
                //string MaNhanVien
                nv.MaNhanVien = hs.MaNhanVien;
                //string Email
                nv.Email = hs.Email;
                //string EmailCaNhan
                nv.EmailCaNhan = hs.EmailCaNhan;
                //string DienThoai
                nv.DienThoai = hs.DienThoai;
                //string CMTND
                nv.Cmnd = hs.Cmtnd;
                //THÔNG TIN USER
                //string TenDangNhap
                var nd = db.SysNguoiDungs.FirstOrDefault(x => x.NhanVienId == hs.NhanVienId);
                if (nd != null)
                {
                    nv.TenDangNhap = nd.TenDangNhap;
                }
                //CHỨC VỤ
                //string TenChucVu
                var cv = db.NsDsChucVus.FirstOrDefault(x => x.ChucVuId == hs.ChucVuId);
                if (cv != null)
                {
                    nv.TenChucVu = cv.TenChucVu;
                }
                //CHỨC DANH
                //string TenChucDanh
                var cd = db.NsDsChucDanhs.FirstOrDefault(x => x.ChucDanhId == hs.ChucDanhId);
                if (cd != null)
                {
                    nv.TenChucDanh = cd.TenChucDanh;
                }
                //PHÒNG BAN
                //int PhongBanId
                if (hs.PhongBanId != null)
                {
                    nv.PhongBanId = hs.PhongBanId;
                    var pb = db.PhongBans.FirstOrDefault(x => x.PhongBanId == hs.PhongBanId);
                    if (pb != null)
                    {
                        //string TenPhongBan
                        nv.TenPhongBan = pb.Ten;
                        //string MaPhongBan
                        nv.MaPhongBan = pb.MaPhongBan;
                    }

                    //PHÒNG BAN CẤP 1
                    //int PhongBanCap1Id
                    var pbCap1 = GetPhongBanCap1CuaNhanVienTheoPbId(hs.PhongBanId.Value);
                    if (pbCap1 != null)
                    {
                        nv.TenPhongBanCap1 = pbCap1.Ten;
                        nv.MaPhongBanCap1 = pbCap1.MaPhongBan;
                    }

                    //string MaPhongBanCap1
                    //string TenPhongBanCap1
                }

                return nv;
            }

            return null;
        }

        /// <summary>
        /// Trả về danh sách nhân viên đã chuẩn hóa
        /// </summary>
        /// <param name="phongBanId"></param>
        /// <returns>List NhanVienViewModel</returns>
        public static List<NhanVienViewModel> GetAllNhanVienCuaPhongBan(int phongBanId)
        {
            var db = new HrmDbContext();
            PhongBan phongBan = db.PhongBans.Single(x => x.PhongBanId == phongBanId);

            var listPbs = GetAllChildren1(db.PhongBans.ToList(), phongBanId);
            listPbs.Add(db.PhongBans.Single(x => x.PhongBanId == phongBanId));
            var listPbIds = listPbs.Select(x => x.PhongBanId).ToList();

            var query = db.NhanViens.Where(x => listPbIds.Contains((int)x.PhongBanId)).ToList();

            //var listNvs = from nv in db.NhanViens.ToList()
            var listNvs = from nv in query
                          join nd in db.SysNguoiDungs on nv.NhanVienId equals nd.NhanVienId into tb1
                          from nd in tb1
                          join cv in db.NsDsChucVus on nv.ChucVuId equals cv.ChucVuId into tb2
                          from cv in tb2.ToList()
                          join cd in db.NsDsChucDanhs on nv.ChucDanhId equals cd.ChucDanhId into tb3
                          from cd in tb3.ToList()
                              //join pb in db.PhongBans on nv.PhongBanId equals pb.PhongBanId into table4
                              //from pb in table4.ToList()
                          select new NhanVienViewModel
                          {
                              NhanVienId = nv.NhanVienId,
                              Ho = nv.Ho,
                              Ten = nv.HoTen,
                              GioiTinh = nv.GioiTinh,
                              MaNhanVien = nv.MaNhanVien,
                              TenDangNhap = nd.TenDangNhap,
                              Email = nv.Email,
                              EmailCaNhan = nv.EmailCaNhan,
                              DienThoai = nv.DienThoai,
                              CMTND = nv.Cmtnd,
                              TenChucVu = cv.TenChucVu,
                              TenChucDanh = cd.TenChucDanh,
                              PhongBanId = phongBan.PhongBanId,
                              TenPhongBan = phongBan.Ten,
                              MaPhongBan = phongBan.MaPhongBan
                          };

            return listNvs.OrderByDescending(x => x.NhanVienId).ToList();
        }

        public static List<NhanVienViewModel2> GetAllNhanVienTheoMa2(List<string> listMaNhanVien)
        {
            var db = new HrmDbContext();
            List<NhanVienViewModel2> lstOut = new List<NhanVienViewModel2>();

            foreach (var maNhanVien in listMaNhanVien)
            {
                var nv = new NhanVienViewModel2();
                var hs = db.NhanViens.FirstOrDefault(x => x.MaNhanVien == maNhanVien);
                if (hs != null)
                {
                    //int NhanVienId
                    nv.NhanVienId = hs.NhanVienId;
                    //string Ho
                    nv.Ho = hs.Ho;
                    //string Ten
                    nv.Ten = hs.HoTen;
                    //string GioiTinh
                    nv.GioiTinh = hs.GioiTinh;
                    //string MaNhanVien
                    nv.MaNhanVien = hs.MaNhanVien;
                    //string Email
                    nv.Email = hs.Email;
                    //string EmailCaNhan
                    nv.EmailCaNhan = hs.EmailCaNhan;
                    //string DienThoai
                    nv.DienThoai = hs.DienThoai;
                    //string CMTND
                    nv.Cmnd = hs.Cmtnd;
                    //THÔNG TIN USER
                    //string TenDangNhap
                    var nd = db.SysNguoiDungs.FirstOrDefault(x => x.NhanVienId == hs.NhanVienId);
                    if (nd != null)
                    {
                        nv.TenDangNhap = nd.TenDangNhap;
                    }
                    //CHỨC VỤ
                    //string TenChucVu
                    var cv = db.NsDsChucVus.FirstOrDefault(x => x.ChucVuId == hs.ChucVuId);
                    if (cv != null)
                    {
                        nv.TenChucVu = cv.TenChucVu;
                    }
                    //CHỨC DANH
                    //string TenChucDanh
                    var cd = db.NsDsChucDanhs.FirstOrDefault(x => x.ChucDanhId == hs.ChucDanhId);
                    if (cd != null)
                    {
                        nv.TenChucDanh = cd.TenChucDanh;
                    }
                    //PHÒNG BAN
                    //int PhongBanId
                    if (hs.PhongBanId != null)
                    {
                        nv.PhongBanId = hs.PhongBanId;
                        var pb = db.PhongBans.FirstOrDefault(x => x.PhongBanId == hs.PhongBanId);
                        if (pb != null)
                        {
                            //string TenPhongBan
                            nv.TenPhongBan = pb.Ten;
                            //string MaPhongBan
                            nv.MaPhongBan = pb.MaPhongBan;
                        }

                        //PHÒNG BAN CẤP 1
                        //int PhongBanCap1Id
                        var pbCap1 = GetPhongBanCap1CuaNhanVienTheoPbId(hs.PhongBanId.Value);
                        if (pbCap1 != null)
                        {
                            nv.PhongBanCap1Id = pbCap1.PhongBanId;
                            nv.TenPhongBanCap1 = pbCap1.Ten;
                            nv.MaPhongBanCap1 = pbCap1.MaPhongBan;
                        }

                        //string MaPhongBanCap1
                        //string TenPhongBanCap1
                    }

                    lstOut.Add(nv);
                }
            }


            return lstOut;
        }

        public static async Task<List<GetAllNhanVienTheoListMaNvReturnModel>> GetAllNhanVienTheoMa3(List<string> listMaNhanVien)
        {
            var db = new AbpHplDbContext();

            DataTable dt = new DataTable();
            dt.Clear();
            dt.Columns.Add("MaNhanVien");
            foreach (var str in listMaNhanVien)
            {
                DataRow dtRow = dt.NewRow();
                dtRow["MaNhanVien"] = str;
                dt.Rows.Add(dtRow);
            }

            return await db.GetAllNhanVienTheoListMaNvAsync(dt);
        }

        /// <summary>
        /// Trả về danh sách nhân viên trên DB HRM
        /// </summary>
        /// <param name="listMaNhanVien"></param>
        /// <returns>List NhanVienViewModel</returns>
        public static List<NhanVienViewModel> GetAllNhanVienTheoMa(List<string> listMaNhanVien)
        {
            var db = new HrmDbContext();
            try
            {
                var listNvs = from nv in db.NhanViens
                              join nd in db.SysNguoiDungs on nv.NhanVienId equals nd.NhanVienId into tb1
                              from nd in tb1.DefaultIfEmpty()
                              join cv in db.NsDsChucVus on nv.ChucVuId equals cv.ChucVuId into tb2
                              from cv in tb2.DefaultIfEmpty()
                              join cd in db.NsDsChucDanhs on nv.ChucDanhId equals cd.ChucDanhId into tb3
                              from cd in tb3.DefaultIfEmpty()
                              join pb in db.PhongBans on nv.PhongBanId equals pb.PhongBanId into table4
                              from pb in table4.DefaultIfEmpty()
                              join pb2 in db.PhongBans on pb.PhongBanChaId equals pb2.PhongBanId into table5
                              from pb2 in table5.DefaultIfEmpty()
                              join pb3 in db.PhongBans on pb2.PhongBanChaId equals pb3.PhongBanId into table6
                              from pb3 in table6.DefaultIfEmpty()
                              join pb4 in db.PhongBans on pb3.PhongBanChaId equals pb4.PhongBanId into table7
                              from pb4 in table7.DefaultIfEmpty()
                              join pb5 in db.PhongBans on pb4.PhongBanChaId equals pb5.PhongBanId into table8
                              from pb5 in table8.DefaultIfEmpty()
                              join pb6 in db.PhongBans on pb5.PhongBanChaId equals pb6.PhongBanId into table9
                              from pb6 in table9.DefaultIfEmpty()
                              where listMaNhanVien.Contains(nv.MaNhanVien) & !string.IsNullOrEmpty(nv.MaNhanVien)

                              select new NhanVienViewModel
                              {
                                  NhanVienId = nv.NhanVienId,
                                  Ho = nv.Ho,
                                  Ten = nv.HoTen,
                                  GioiTinh = nv.GioiTinh,
                                  MaNhanVien = nv.MaNhanVien,
                                  TenDangNhap = nd.TenDangNhap,
                                  Email = nv.Email,
                                  EmailCaNhan = nv.EmailCaNhan,
                                  DienThoai = nv.DienThoai,
                                  CMTND = nv.Cmtnd,
                                  TenChucVu = cv.TenChucVu,
                                  TenChucDanh = cd.TenChucDanh,
                                  PhongBanId = pb.PhongBanId,
                                  TenPhongBan = pb.Ten,
                                  MaPhongBan = pb.MaPhongBan,
                                  PhongBanCha = pb2.Ten,
                                  MaCha = pb.MaPhongBan,
                                  PhongBanOng = pb3.Ten,
                                  MaOng = pb3.MaPhongBan,
                                  PhongBanCo = pb4.Ten,
                                  MaCo = pb4.MaPhongBan,
                                  PhongBanKy = pb5.Ten,
                                  MaKy = pb5.MaPhongBan,
                                  PhongBan6 = pb6.Ten,
                                  MaPb6 = pb6.MaPhongBan
                              };

                return listNvs.OrderByDescending(x => x.NhanVienId).ToList();
            }
            catch (Exception e)
            {
                string abc = e.Message;
                return new List<NhanVienViewModel>();
            }
        }

        /// <summary>
        /// Trả về danh sách nhân viên trên DB HRM
        /// </summary>
        /// <param name="maNhanVien"></param>
        /// <returns>List NhanVienViewModel</returns>
        public static List<NhanVienViewModel> GetAllNhanVienTheoMa(string maNhanVien)
        {
            var db = new HrmDbContext();
            try
            {
                var listNvs = from nv in db.NhanViens
                              join nd in db.SysNguoiDungs on nv.NhanVienId equals nd.NhanVienId into tb1
                              from nd in tb1.DefaultIfEmpty()
                              join cv in db.NsDsChucVus on nv.ChucVuId equals cv.ChucVuId into tb2
                              from cv in tb2.DefaultIfEmpty()
                              join cd in db.NsDsChucDanhs on nv.ChucDanhId equals cd.ChucDanhId into tb3
                              from cd in tb3.DefaultIfEmpty()
                              join pb in db.PhongBans on nv.PhongBanId equals pb.PhongBanId into table4
                              from pb in table4.DefaultIfEmpty()
                              join pb2 in db.PhongBans on pb.PhongBanChaId equals pb2.PhongBanId into table5
                              from pb2 in table5.DefaultIfEmpty()
                              join pb3 in db.PhongBans on pb2.PhongBanChaId equals pb3.PhongBanId into table6
                              from pb3 in table6.DefaultIfEmpty()
                              join pb4 in db.PhongBans on pb3.PhongBanChaId equals pb4.PhongBanId into table7
                              from pb4 in table7.DefaultIfEmpty()
                              join pb5 in db.PhongBans on pb4.PhongBanChaId equals pb5.PhongBanId into table8
                              from pb5 in table8.DefaultIfEmpty()
                              join pb6 in db.PhongBans on pb5.PhongBanChaId equals pb6.PhongBanId into table9
                              from pb6 in table9.DefaultIfEmpty()
                              where nv.MaNhanVien.Equals(maNhanVien)
                              select new NhanVienViewModel
                              {
                                  NhanVienId = nv.NhanVienId,
                                  Ho = nv.Ho,
                                  Ten = nv.HoTen,
                                  GioiTinh = nv.GioiTinh,
                                  MaNhanVien = nv.MaNhanVien,
                                  TenDangNhap = nd.TenDangNhap,
                                  Email = nv.Email,
                                  EmailCaNhan = nv.EmailCaNhan,
                                  DienThoai = nv.DienThoai,
                                  CMTND = nv.Cmtnd,
                                  TenChucVu = cv.TenChucVu,
                                  TenChucDanh = cd.TenChucDanh,
                                  PhongBanId = pb.PhongBanId,
                                  TenPhongBan = pb.Ten,
                                  MaPhongBan = pb.MaPhongBan,
                                  PhongBanCha = pb2.Ten,
                                  MaCha = pb.MaPhongBan,
                                  PhongBanOng = pb3.Ten,
                                  MaOng = pb3.MaPhongBan,
                                  PhongBanCo = pb4.Ten,
                                  MaCo = pb4.MaPhongBan,
                                  PhongBanKy = pb5.Ten,
                                  MaKy = pb5.MaPhongBan,
                                  PhongBan6 = pb6.Ten,
                                  MaPb6 = pb6.MaPhongBan
                              };

                return listNvs.OrderByDescending(x => x.NhanVienId).ToList();
            }
            catch (Exception e)
            {
                string abc = e.Message;
                return new List<NhanVienViewModel>();
            }
        }

        public static List<NhanVienViewModel> GetNhanVienTheoUserName(string userName)
        {
            var db = new HrmDbContext();
            try
            {
                var listNvs = from nv in db.NhanViens
                              join nd in db.SysNguoiDungs on nv.NhanVienId equals nd.NhanVienId into tb1
                              from nd in tb1.DefaultIfEmpty()
                              join cv in db.NsDsChucVus on nv.ChucVuId equals cv.ChucVuId into tb2
                              from cv in tb2.DefaultIfEmpty()
                              join cd in db.NsDsChucDanhs on nv.ChucDanhId equals cd.ChucDanhId into tb3
                              from cd in tb3.DefaultIfEmpty()
                              join pb in db.PhongBans on nv.PhongBanId equals pb.PhongBanId into table4
                              from pb in table4.DefaultIfEmpty()
                              join pb2 in db.PhongBans on pb.PhongBanChaId equals pb2.PhongBanId into table5
                              from pb2 in table5.DefaultIfEmpty()
                              join pb3 in db.PhongBans on pb2.PhongBanChaId equals pb3.PhongBanId into table6
                              from pb3 in table6.DefaultIfEmpty()
                              join pb4 in db.PhongBans on pb3.PhongBanChaId equals pb4.PhongBanId into table7
                              from pb4 in table7.DefaultIfEmpty()
                              join pb5 in db.PhongBans on pb4.PhongBanChaId equals pb5.PhongBanId into table8
                              from pb5 in table8.DefaultIfEmpty()
                              join pb6 in db.PhongBans on pb5.PhongBanChaId equals pb6.PhongBanId into table9
                              from pb6 in table9.DefaultIfEmpty()
                              where nd.TenDangNhap.Equals(userName)
                              select new NhanVienViewModel
                              {
                                  NhanVienId = nv.NhanVienId,
                                  Ho = nv.Ho,
                                  Ten = nv.HoTen,
                                  GioiTinh = nv.GioiTinh,
                                  MaNhanVien = nv.MaNhanVien,
                                  TenDangNhap = nd.TenDangNhap,
                                  Email = nv.Email,
                                  EmailCaNhan = nv.EmailCaNhan,
                                  DienThoai = nv.DienThoai,
                                  CMTND = nv.Cmtnd,
                                  TenChucVu = cv.TenChucVu,
                                  TenChucDanh = cd.TenChucDanh,
                                  PhongBanId = pb.PhongBanId,
                                  TenPhongBan = pb.Ten,
                                  MaPhongBan = pb.MaPhongBan,
                                  PhongBanCha = pb2.Ten,
                                  MaCha = pb.MaPhongBan,
                                  PhongBanOng = pb3.Ten,
                                  MaOng = pb3.MaPhongBan,
                                  PhongBanCo = pb4.Ten,
                                  MaCo = pb4.MaPhongBan,
                                  PhongBanKy = pb5.Ten,
                                  MaKy = pb5.MaPhongBan,
                                  PhongBan6 = pb6.Ten,
                                  MaPb6 = pb6.MaPhongBan
                              };

                return listNvs.OrderByDescending(x => x.NhanVienId).ToList();
            }
            catch (Exception e)
            {
                string abc = e.Message;
                return new List<NhanVienViewModel>();
            }
        }

        public static List<GetAllNhanVienDangLamChuaCoUserReturnModel> GetAllNhanVienChuaCoUsername2()
        {
            //var db = new HrmDbContext();
            var dbApb = new AbpHplDbContext();

            return dbApb.GetAllNhanVienDangLamChuaCoUser();


            //List<NhanVienViewModel2> lst2 = new List<NhanVienViewModel2>();
            //foreach (var hs in listNvs)
            //{
            //    var nv = new NhanVienViewModel2();


            //    lst2.Add(nv);
            //}

            //return lst2.OrderBy(x => x.Ho).ToList();
        }


        /// <summary>
        /// Danh sách user đang làm việc chưa có username
        /// </summary>
        /// <returns>List NhanVienViewModel</returns>
        public static List<NhanVienViewModel2> GetAllNhanVienChuaCoUsername()
        {
            var db = new HrmDbContext();

            var listNvs = from nv in db.NhanViens
                          join nd in db.SysNguoiDungs on nv.NhanVienId equals nd.NhanVienId into tb1
                          from nd in tb1.DefaultIfEmpty()
                          where (nv.NghiViec == false || nv.NghiViec == null) &
                                (string.IsNullOrEmpty(nd.TenDangNhap) ||
                                 nd.TenDangNhap.Contains("@") ||
                                 nd.TenDangNhap.Contains("gmail.com"))
                          select nv;

            var lstNds = db.SysNguoiDungs.ToList();
            var lstCvs = db.NsDsChucVus.ToList();
            var lstCds = db.NsDsChucDanhs.ToList();
            var lstPbs = db.PhongBans.ToList();

            List<NhanVienViewModel2> lst2 = new List<NhanVienViewModel2>();
            foreach (var hs in listNvs)
            {
                var nv = GetNhanVienFullByNhanVien(hs, lstNds, lstCvs, lstCds, lstPbs);
                lst2.Add(nv);
            }

            return lst2.OrderBy(x => x.Ho).ToList();
        }

        public static List<NhanVienViewModel> GetAllUserName()
        {
            var db = new HrmDbContext();
            var nhanVien = new NhanVienViewModel();

            var query = from u in db.SysNguoiDungs.ToList()
                        join nv in db.NhanViens on u.NhanVienId equals nv.NhanVienId into table1
                        from nv in table1.ToList()
                        join cv in db.NsDsChucVus on nv.ChucVuId equals cv.ChucVuId into tb2
                        from cv in tb2.DefaultIfEmpty()
                        join cd in db.NsDsChucDanhs on nv.ChucDanhId equals cd.ChucDanhId into tb3
                        from cd in tb3.DefaultIfEmpty()
                        join p in db.PhongBans on nv.PhongBanId equals p.PhongBanId into table4
                        from p in table4.DefaultIfEmpty()
                        select new NhanVienViewModel
                        {
                            NhanVienId = nv.NhanVienId,
                            Ho = nv.Ho,
                            Ten = nv.HoTen,
                            GioiTinh = nv.GioiTinh,
                            MaNhanVien = nv.MaNhanVien,
                            TenDangNhap = u.TenDangNhap,
                            Email = nv.Email,
                            EmailCaNhan = nv.EmailCaNhan,
                            DienThoai = nv.DienThoai,
                            CMTND = nv.Cmtnd,
                            TenChucVu = cv?.TenChucVu,
                            TenChucDanh = cd?.TenChucDanh,
                            PhongBanId = p.PhongBanId,
                            TenPhongBan = p.Ten,
                            MaPhongBan = p.MaPhongBan
                        };

            return query.ToList();
        }

        /// <summary>
        /// Trả về danh sách nhân viên lỗi user trên DB HRM
        /// </summary>
        /// <returns>List NhanVienViewModel</returns>
        public static List<NhanVienViewModel> GetAllUserNameLamViec()
        {
            var db = new HrmDbContext();
            try
            {
                var listNvs = from nv in db.NhanViens
                              join nd in db.SysNguoiDungs on nv.NhanVienId equals nd.NhanVienId into tb1
                              from nd in tb1
                              join cv in db.NsDsChucVus on nv.ChucVuId equals cv.ChucVuId into tb2
                              from cv in tb2.DefaultIfEmpty()
                              join cd in db.NsDsChucDanhs on nv.ChucDanhId equals cd.ChucDanhId into tb3
                              from cd in tb3.DefaultIfEmpty()
                              join pb in db.PhongBans on nv.PhongBanId equals pb.PhongBanId into table4
                              from pb in table4.DefaultIfEmpty()
                              where nv.NghiViec == false | !nv.NghiViec.HasValue
                              //where nv.MaNhanVien.Equals(maNhanVien)
                              select new NhanVienViewModel
                              {
                                  NhanVienId = nv.NhanVienId,
                                  Ho = nv.Ho,
                                  Ten = nv.HoTen,
                                  GioiTinh = nv.GioiTinh,
                                  MaNhanVien = nv.MaNhanVien,
                                  TenDangNhap = nd.TenDangNhap,
                                  Email = nv.Email,
                                  EmailCaNhan = nv.EmailCaNhan,
                                  DienThoai = nv.DienThoai,
                                  CMTND = nv.Cmtnd,
                                  TenChucVu = cv.TenChucVu,
                                  TenChucDanh = cd.TenChucDanh,
                                  PhongBanId = pb.PhongBanId,
                                  TenPhongBan = pb.Ten,
                                  MaPhongBan = pb.MaPhongBan,
                              };

                return listNvs.OrderBy(x => x.NhanVienId).ToList();
            }
            catch (Exception e)
            {
                db.Dispose();
                return new List<NhanVienViewModel>();
            }
        }

        /// <summary>
        /// Trả về danh sách nhân viên lỗi user trên DB HRM
        /// </summary>
        /// <returns>List NhanVienViewModel</returns>
        public static List<NhanVienViewModel> GetAllNhanVienNghiViec()
        {
            var db = new HrmDbContext();
            try
            {
                var listNvs = from nv in db.NhanViens
                              join nd in db.SysNguoiDungs on nv.NhanVienId equals nd.NhanVienId into tb1
                              from nd in tb1.DefaultIfEmpty()
                              join cv in db.NsDsChucVus on nv.ChucVuId equals cv.ChucVuId into tb2
                              from cv in tb2.DefaultIfEmpty()
                              join cd in db.NsDsChucDanhs on nv.ChucDanhId equals cd.ChucDanhId into tb3
                              from cd in tb3.DefaultIfEmpty()
                              join pb in db.PhongBans on nv.PhongBanId equals pb.PhongBanId into table4
                              from pb in table4.DefaultIfEmpty()
                                  //where nv.NghiViec.Value & nv.NgayNghiViec.Value >= dt & !string.IsNullOrEmpty(nd.TenDangNhap)
                              where !(!nv.NghiViec.Value || nv.NgayNghiViec == null) & nd.Active.Value

                              select new NhanVienViewModel
                              {
                                  NhanVienId = nv.NhanVienId,
                                  Ho = nv.Ho,
                                  Ten = nv.HoTen,
                                  GioiTinh = nv.GioiTinh,
                                  MaNhanVien = nv.MaNhanVien,
                                  TenDangNhap = nd.TenDangNhap,
                                  Email = nv.Email,
                                  EmailCaNhan = nv.EmailCaNhan,
                                  DienThoai = nv.DienThoai,
                                  CMTND = nv.Cmtnd,
                                  TenChucVu = cv.TenChucVu,
                                  TenChucDanh = cd.TenChucDanh,
                                  PhongBanId = pb.PhongBanId,
                                  TenPhongBan = pb.Ten,
                                  MaPhongBan = pb.MaPhongBan
                              };

                return listNvs.OrderBy(x => x.NhanVienId).ToList();
            }
            catch (Exception e)
            {
                db.Dispose();

                return new List<NhanVienViewModel>();
            }
        }

        /// <summary>
        /// Chạy tự động disable hết các user nghỉ việc
        /// </summary>
        /// <returns></returns>
        public static List<NhanVienViewModel> GetAllNhanVienNghiViecCanDisable()
        {
            var dbAbp = new AbpHplDbContext();

            try
            {
                var listNvs = GetAllNhanVienNghiViec();
                var listLogs = dbAbp.HplDisableUserLogs.Select(x => x.UserName);
                var list = from nv in listNvs
                           where !listLogs.Contains(nv.TenDangNhap)
                           select nv;

                return list.ToList();
            }
            catch (Exception e)
            {
                dbAbp.Dispose();

                return new List<NhanVienViewModel>();
            }
        }

        /// <summary>
        /// Trả về danh sách nhân viên dữ liệu thô theo table của DB
        /// </summary>
        /// <param name="phongBanId"></param>
        /// <returns>List NhanVien</returns>
        public static List<NhanVien> GetAllNhanVienCuaPhongBanRaw(int phongBanId)
        {
            var db = new HrmDbContext();

            var listPbs = GetAllChildren1(db.PhongBans.ToList(), phongBanId);
            listPbs.Add(db.PhongBans.Single(x => x.PhongBanId == phongBanId));
            var listPbIds = listPbs.Select(x => x.PhongBanId).ToList();

            return db.NhanViens.Where(x => listPbIds.Contains((int)x.PhongBanId)).ToList();
        }

        public static List<NhanVien> GetAllNhanVienDangLamViec()
        {
            var db = new HrmDbContext();

            return db.NhanViens.Where(x => x.NghiViec == false || x.NghiViec == null).ToList();
        }

        /// <summary>
        /// Trả về danh sách nhân viên nhầm email (dự đoán)
        /// </summary>
        /// <returns>List NhanVien</returns>
        public static List<NhanVienViewModel> GetAllNhanVienNhamEmail()
        {
            var db = new HrmDbContext();

            var listNvs = from nv in db.NhanViens
                          join nd in db.SysNguoiDungs on nv.NhanVienId equals nd.NhanVienId into tb1
                          from nd in tb1.DefaultIfEmpty()
                          join cv in db.NsDsChucVus on nv.ChucVuId equals cv.ChucVuId into tb2
                          from cv in tb2.DefaultIfEmpty()
                          join cd in db.NsDsChucDanhs on nv.ChucDanhId equals cd.ChucDanhId into tb3
                          from cd in tb3.DefaultIfEmpty()
                          join pb in db.PhongBans on nv.PhongBanId equals pb.PhongBanId into table4
                          from pb in table4.DefaultIfEmpty()
                          where nv.NghiViec == false & nv.TrangThaiId.Value != 6 &
                                !string.IsNullOrEmpty(nv.Email)
                          select new NhanVienViewModel
                          {
                              NhanVienId = nv.NhanVienId,
                              Ho = nv.Ho,
                              Ten = nv.HoTen,
                              GioiTinh = nv.GioiTinh,
                              MaNhanVien = nv.MaNhanVien,
                              TenDangNhap = nd.TenDangNhap,
                              Email = nv.Email,
                              EmailCaNhan = nv.EmailCaNhan,
                              DienThoai = nv.DienThoai,
                              CMTND = nv.Cmtnd,
                              TenChucVu = cv.TenChucVu,
                              TenChucDanh = cd.TenChucDanh,
                              PhongBanId = pb.PhongBanId,
                              TenPhongBan = pb.Ten,
                              MaPhongBan = pb.MaPhongBan,
                          };
            var listOut = new List<NhanVienViewModel>();
            foreach (var nv in listNvs)
            {
                var userName = CommonHelper.GenerateUserNameFromFirstNameAndLastName(nv.Ten, nv.Ho);
                var strEmail = nv.Email.Split("@");
                switch (strEmail.Length)
                {
                    case < 1:
                        break;
                    case 1:
                        break;
                    case > 1:
                        if (userName.Length != strEmail[0].Length)
                        {
                            listOut.Add(nv);
                        }
                        else
                        {
                            if (!userName.Equals(strEmail[0]))
                            {
                                listOut.Add(nv);
                            }
                        }

                        break;
                }
            }


            db.Dispose();

            return listOut.OrderBy(x => x.Ten).ToList();
        }

        public static List<HplPhongBan> GetAllHplPhongBan()
        {
            var db = new AbpHplDbContext();
            var listPbs = db.HplPhongBans.OrderBy(x => x.TenPhongBan).ToList();

            return listPbs;

        }

        /// <summary>
        /// Lấy Danh sách Phòng Ban bao gồm danh sách con và cả danh sách cha (theo PhongBanId)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static List<PhongBan> GetAllChildrenAndParents(int id)
        {
            var db = new HrmDbContext();
            //Get all parents
            PhongBan pb = db.PhongBans.FirstOrDefault(x => x.PhongBanId == id);
            var listPbs = FindAllParents(db.PhongBans.ToList(), pb).ToList();
            //Add chính nó
            listPbs.Add(db.PhongBans.Single(x => x.PhongBanId == id));
            //Add danh sách con
            listPbs.AddRange(GetAllChildren1(db.PhongBans.ToList(), id));

            return listPbs;
        }

        /// <summary>
        /// Lấy Danh sách Phòng Ban bao gồm danh sách con và cả danh sách cha (theo MaNhanVien của nhân sự)
        /// </summary>
        /// <param name="maNhanVien">maNhanVien</param>
        /// <returns></returns>
        public static List<PhongBan> GetAllChildrenAndParents(string maNhanVien)
        {
            var db = new HrmDbContext();
            var listPbs = new List<PhongBan>();
            //Get all parents
            var lstNvs = db.NhanViens.Where(x => x.MaNhanVien == maNhanVien);
            switch (lstNvs.Count())
            {
                case 1:
                    var nhanVien = lstNvs.FirstOrDefault();
                    if (nhanVien is { PhongBanId: { } })
                    {
                        //Add danh sách con
                        listPbs = GetAllChildrenAndParents(nhanVien.PhongBanId.Value);
                    }

                    break;
                case < 1:
                case > 1:
                    //chưa xử lý gì
                    break;
            }

            return listPbs.OrderBy(x => x.PhongBanId).ToList();
        }

        /// <summary>
        /// Lấy bao gồm danh sách con và cả chính nó
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static List<PhongBan> GetAllChildrenAndSelf(int id)
        {
            var db = new HrmDbContext();
            var listPbs = GetAllChildren1(db.PhongBans.ToList(), id);
            listPbs.Add(db.PhongBans.Single(x => x.PhongBanId == id));

            return listPbs;
        }

        /// <summary>
        /// Lấy danh sách phòng ban Cha trở lên (không bao gồm chính nó)
        /// </summary>
        /// <param name="allData"></param>
        /// <param name="child"></param>
        /// <returns></returns>
        private static IEnumerable<PhongBan> FindAllParents(List<PhongBan> allData, PhongBan child)
        {
            var parent = allData.FirstOrDefault(x => x.PhongBanId == child.PhongBanChaId);

            if (parent == null)
                return Enumerable.Empty<PhongBan>();

            return new[] { parent }.Concat(FindAllParents(allData, parent));

        }

        /// <summary>
        /// Lấy danh sách Phòng ban con (không bao gồm chính nó)
        /// </summary>
        /// <param name="listPb"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static List<PhongBan> GetAllChildren1(List<PhongBan> listPb, int id)
        {
            var query = listPb
                .Where(x => x.PhongBanChaId == id)
                .Union(listPb.Where(x => x.PhongBanChaId == id)
                    .SelectMany(y => GetAllChildren1(listPb, y.PhongBanId))
                ).ToList();

            return query.ToList();
        }

        public static void UpdateEmailByUserName(string userName)
        {
            var db = new HrmDbContext();

            var query = from nv in db.NhanViens
                        join nd in db.SysNguoiDungs on nv.NhanVienId equals nd.NhanVienId
                        where nd.TenDangNhap.Equals(userName)
                        select nv;
            if (query.Count() == 1)
            {
                var nhanVien = query.FirstOrDefault();
                if (nhanVien != null)
                {
                    if (string.IsNullOrEmpty(nhanVien.Email))
                    {
                        nhanVien.Email = userName + "@haiphatland.com.vn";
                        db.SaveChanges();
                    }
                }
            }

            db.Dispose();
        }
    }
}