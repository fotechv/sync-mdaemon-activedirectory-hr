using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hpl.Common.Helper;
using Hpl.Common.MdaemonServices;
using Hpl.HrmDatabase;
using Hpl.HrmDatabase.Services;
using Hpl.HrmDatabase.ViewModels;
using Hpl.SaleOnlineDatabase;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Unosquare.PassCore.Common;
using Unosquare.PassCore.PasswordProvider;
using NhanVienSale = Hpl.SaleOnlineDatabase.NhanVien;
using PhongBan = Hpl.HrmDatabase.PhongBan;

namespace Hpl.Common
{
    public class HplServices
    {
        private static PasswordChangeOptions _options;
        private readonly ILogger _logger;
        private readonly IPasswordChangeProvider _passwordChangeProvider;
        private readonly IAbpHplDbContext _abpHplDb;

        public HplServices(IPasswordChangeProvider passwordChangeProvider, PasswordChangeOptions options, ILogger logger, IAbpHplDbContext abpHplDb)
        {
            _passwordChangeProvider = passwordChangeProvider;
            _logger = logger;
            _abpHplDb = abpHplDb;
            _options = options;
            //Log.Logger = new LoggerConfiguration()
            //    .MinimumLevel.Information()
            //    .WriteTo.Map("UtcDateTime", DateTime.UtcNow.ToString("yyyyMMdd"),
            //        (utcDateTime, wt) => wt.File($"logs/acm-log-{utcDateTime}.txt"))
            //    .CreateLogger();
            //Log.Information("----START HAI PHAT LAND ACM----");
        }

        public async Task<List<UserAdInfo>> UpdateAllAdUserAddEmailInfo(List<UserAdInfo> listNhanVien)
        {
            int k = 0;
            foreach (var t in listNhanVien)
            {
                k++;
                if (k % 100 == 0)
                {
                    _logger.Error("Đã update xong 100 users");
                }
                var res = await MdaemonXmlApi.GetUserInfo(t.Username);
                if (res.Payload == null) continue;

                try
                {
                    var json = JsonConvert.SerializeObject(res);
                    JObject o = JObject.Parse(json);
                    var message = o["Payload"]?["MDaemon"]?["API"]?["Response"]?["Status"]?["@message"];

                    if (message == null) continue;

                    if (!message.ToString().Equals("The operation completed successfully.")) continue;

                    var frozen = o["Payload"]?["MDaemon"]?["API"]?["Response"]?["Result"]?["User"]?["Details"]?["Frozen"];
                    if (frozen != null)
                    {
                        string strMes = frozen.ToString();
                        switch (strMes)
                        {
                            case "No":
                                t.MailFrozen = false;
                                break;

                            case "Yes":
                                t.MailFrozen = true;

                                break;
                        }
                    }

                    var disabled = o["Payload"]?["MDaemon"]?["API"]?["Response"]?["Result"]?["User"]?["Details"]?["Disabled"];
                    if (disabled != null)
                    {
                        string strMes = disabled.ToString();
                        switch (strMes)
                        {
                            case "No":
                                t.MailDisabled = false;
                                break;

                            case "Yes":
                                t.MailDisabled = true;

                                break;
                        }
                    }

                }
                catch (Exception e)
                {
                    _logger.Error("HplServices.UpdateAllAdUserAddEmailInfo Không nhận được Response khi call  MdaemonXmlApi.GetUserInfo" + ". Lỗi: " + e.Message);
                }
            }

            return listNhanVien;
        }

        public async Task ReactiveUserTask(List<NhanVienViewModel2> listNvs)
        {
            string emailError = "<p style=\"font-weight: bold\">DANH SÁCH LỖI KHI THỰC HIỆN KÍCH HOẠT LẠI. NGÀY " + DateTime.Now.ToString("dd/MM/yy") + "</p>";
            string emailReactive = "<p style=\"font-weight: bold\">DANH SÁCH USER ĐÃ KÍCH HOẠT LẠI. NGÀY " + DateTime.Now.ToString("dd/MM/yy") + "</p>";

            List<string> listNoti = new List<string>();
            List<string> listActive = new List<string>();

            foreach (var model in listNvs)
            {
                //KIỂM TRA USER
                if (string.IsNullOrEmpty(model.TenDangNhap))
                {
                    string notiUser = model.Ho + " " + model.Ten;
                    notiUser += " (" + model.MaNhanVien + ") ";
                    notiUser += "không tồn tại user trên HRM";
                    listNoti.Add(notiUser);

                    continue;
                }

                //EMAIL TRỐNG ==> đưa ra thông báo
                var email = CommonHelper.IsValidEmail(model.Email);
                if (string.IsNullOrEmpty(email))
                {
                    string notiEmail = model.Ho + " " + model.Ten;
                    notiEmail += " (" + model.MaNhanVien + ") ";
                    notiEmail += "không tồn tại email";
                    listNoti.Add(notiEmail);

                    continue;
                }

                //EMAIL KHÔNG PHẢI LÀ @haiphatland.com.vn ==> đưa ra thông báo
                var str = email.Split("@");
                if (!str[1].ToLower().Equals("haiphatland.com.vn"))
                {
                    string notiEmail = model.Ho + " " + model.Ten;
                    notiEmail += " (" + model.MaNhanVien + "). ";
                    notiEmail += "username " + model.TenDangNhap + " ";
                    notiEmail += "có email không phải của HPL: " + email;
                    listNoti.Add(notiEmail);

                    continue;
                }

                //USERNAME VÀ EMAIL KHÔNG GIỐNG NHAU ==> Đưa ra thông báo
                if (!str[0].ToLower().Equals(model.TenDangNhap.ToLower()))
                {
                    string notiEmail = model.Ho + " " + model.Ten;
                    notiEmail += " (" + model.MaNhanVien + "). ";
                    notiEmail += " có username " + model.TenDangNhap + " ";
                    notiEmail += "và email không giống nhau " + email;
                    listNoti.Add(notiEmail);

                    continue;
                }

                //TẠO MẬT KHẨU MẶC ĐỊNH
                string pw = "Hpl@123";
                string dienThoai = model.DienThoai;

                try
                {
                    if (model.DienThoai != null && model.DienThoai.Trim().Length >= 9)
                    {
                        dienThoai = model.DienThoai.Trim();
                        model.DienThoai = "+84" + int.Parse(dienThoai);
                        pw = "Hpl@" + dienThoai.Substring(dienThoai.Length - 3, 3);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(model.MaPhongBan + " Số điện thoại lỗi " + e.Message);
                    model.DienThoai = "";
                }

                //RE-ACTIVE AD USER
                string userDis;
                try
                {
                    //TODO
                    userDis = _passwordChangeProvider.ReactiveUser(model.TenDangNhap);
                    _logger.Information("RE-ACTIVE user " + model.TenDangNhap);
                }
                catch (Exception e)
                {
                    userDis = model.TenDangNhap + " lỗi khi disable trên AD: " + e.Message;

                    string notiEmail = model.Ho + " " + model.Ten;
                    notiEmail += " (" + model.MaNhanVien + "). ";
                    notiEmail += "User:  " + model.TenDangNhap + " ";
                    notiEmail += "lỗi khi disable trên AD: " + e.Message;
                    listNoti.Add(notiEmail);

                    continue;
                }

                //Re-active HRM User
                try
                {
                    UserService.ReactiveUserHrm(model.TenDangNhap);
                }
                catch (Exception e)
                {
                    _logger.Error("Lỗi khi inactive user HRM: " + e.Message);
                }

                //UNLOCK SALE ONLINE
                var lockSale = SaleOnlineServices.UnlockUser(model.TenDangNhap);
                _logger.Information("UNLOCKED Sale Online " + model.TenDangNhap);

                //TẠO LẠI EMAIL CHO USER
                #region TẠO EMAIL
                var abpPhong = AbpServices.GetAbpPhongBanByMaPhongBan(model.MaPhongBanCap1);
                string userName = model.TenDangNhap;
                string mailList = "";
                if (abpPhong != null)
                {
                    if (!string.IsNullOrEmpty(abpPhong.MailingList))
                    {
                        mailList = abpPhong.MailingList;
                    }
                }

                string ten = UsernameGenerator.ConvertToUnSign(model.Ten);
                string ho = UsernameGenerator.ConvertToUnSign(model.Ho);
                CreateUserInput input = new CreateUserInput
                {
                    Domain = "haiphatland.com.vn",
                    Username = model.TenDangNhap,
                    FirstName = ten,
                    LastName = ho,
                    FullName = ho + " " + ten,
                    Password = pw,
                    AdminNotes = "Tạo từ tool, time: " + DateTime.Now.ToString("G"),
                    MailList = mailList,
                    Group = ""
                };
                var res = await MdaemonXmlApi.CreateUser(input);

                _logger.Information(userName + " CREATED on MDaemon at " + DateTime.Now.ToString("G"));
                #endregion

                listActive.Add(model.Ho + " " + model.Ten + " " + userName + ". Message: " + userDis);
            }

            //GỬI MAIL THÔNG BÁO USER LỖI
            if (listNoti.Any())
            {
                try
                {
                    string subject = "[ACM] DANH SÁCH LỖI KHI THỰC HIỆN ENABLE " + DateTime.Now.ToString("dd/MM/yy");
                    int k = 0;
                    foreach (var item in listNoti)
                    {
                        k++;
                        emailError += k + ". " + item + "<br />";
                    }
                    emailError += "Lưu ý:<br />";
                    emailError += "Các trường hợp trên cần xử lý thủ công<br />";

                    MailHelper.EmailSender(emailError, subject);
                }
                catch (Exception e)
                {
                    _logger.Error("Lỗi gửi mail: " + e);
                }
            }

            if (listActive.Any())
            {
                try
                {
                    string subject = "[ACM] DANH SÁCH USER ĐÃ KÍCH HOẠT LẠI NGÀY " + DateTime.Now.ToString("dd/MM/yy");
                    int l = 0;
                    foreach (var item in listActive)
                    {
                        l++;
                        emailReactive += l + ". " + item + "<br />";
                    }
                    emailReactive += "Lưu ý:<br />";
                    emailReactive += "1. Pass mặc định của mail và user là Hpl@xxx. Với xxx là 3 số cuối của điện thoại<br />";
                    emailReactive += "2. Nếu email đã có sẵn và không đăng nhập được,  yêu cầu HCNS reset<br />";

                    MailHelper.EmailSender(emailReactive, subject);
                }
                catch (Exception e)
                {
                    _logger.Error("Lỗi gửi mail: " + e);
                }

            }
        }

        public async Task ReactiveUserTask2(List<GetAllNhanVienTheoListMaNvReturnModel> listNvs)
        {
            string emailError = "<p style=\"font-weight: bold\">DANH SÁCH LỖI KHI THỰC HIỆN KÍCH HOẠT LẠI. NGÀY " + DateTime.Now.ToString("dd/MM/yy") + "</p>";
            string emailReactive = "<p style=\"font-weight: bold\">DANH SÁCH USER ĐÃ KÍCH HOẠT LẠI. NGÀY " + DateTime.Now.ToString("dd/MM/yy") + "</p>";

            List<string> listNoti = new List<string>();
            List<string> listActive = new List<string>();

            foreach (var model in listNvs)
            {
                //KIỂM TRA USER
                if (string.IsNullOrEmpty(model.TenDangNhap))
                {
                    string notiUser = model.HoVaTen;
                    notiUser += " (" + model.MaNhanVien + ") ";
                    notiUser += "không tồn tại user trên HRM";
                    listNoti.Add(notiUser);

                    continue;
                }

                //EMAIL TRỐNG ==> đưa ra thông báo
                var email = CommonHelper.IsValidEmail(model.Email);
                if (string.IsNullOrEmpty(email))
                {
                    string notiEmail = model.HoVaTen;
                    notiEmail += " (" + model.MaNhanVien + ") ";
                    notiEmail += "không tồn tại email";
                    listNoti.Add(notiEmail);

                    continue;
                }

                //EMAIL KHÔNG PHẢI LÀ @haiphatland.com.vn ==> đưa ra thông báo
                var str = email.Split("@");
                if (!str[1].ToLower().Equals("haiphatland.com.vn"))
                {
                    string notiEmail = model.HoVaTen;
                    notiEmail += " (" + model.MaNhanVien + "). ";
                    notiEmail += "username " + model.TenDangNhap + " ";
                    notiEmail += "có email không phải của HPL: " + email;
                    listNoti.Add(notiEmail);

                    continue;
                }

                //USERNAME VÀ EMAIL KHÔNG GIỐNG NHAU ==> Đưa ra thông báo
                if (!str[0].ToLower().Equals(model.TenDangNhap.ToLower()))
                {
                    string notiEmail = model.HoVaTen;
                    notiEmail += " (" + model.MaNhanVien + "). ";
                    notiEmail += " có username " + model.TenDangNhap + " ";
                    notiEmail += "và email không giống nhau " + email;
                    listNoti.Add(notiEmail);

                    continue;
                }

                //TẠO MẬT KHẨU MẶC ĐỊNH
                string pw = "Hpl@123";
                string dienThoai = model.DienThoai;

                try
                {
                    if (model.DienThoai != null && model.DienThoai.Trim().Length >= 9)
                    {
                        dienThoai = model.DienThoai.Trim();
                        model.DienThoai = "+84" + int.Parse(dienThoai);
                        pw = "Hpl@" + dienThoai.Substring(dienThoai.Length - 3, 3);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(model.MaPhongBan + " Số điện thoại lỗi " + e.Message);
                    model.DienThoai = "";
                }

                //RE-ACTIVE AD USER
                string userDis;
                try
                {
                    //TODO
                    userDis = _passwordChangeProvider.ReactiveUser(model.TenDangNhap);
                    _logger.Information("RE-ACTIVE user " + model.TenDangNhap);
                }
                catch (Exception e)
                {
                    userDis = model.TenDangNhap + " lỗi khi disable trên AD: " + e.Message;

                    string notiEmail = model.HoVaTen;
                    notiEmail += " (" + model.MaNhanVien + "). ";
                    notiEmail += "User:  " + model.TenDangNhap + " ";
                    notiEmail += "lỗi khi disable trên AD: " + e.Message;
                    listNoti.Add(notiEmail);

                    continue;
                }

                //Re-active HRM User
                try
                {
                    UserService.ReactiveUserHrm(model.TenDangNhap);
                }
                catch (Exception e)
                {
                    _logger.Error("Lỗi khi inactive user HRM: " + e.Message);
                }

                //UNLOCK SALE ONLINE
                var lockSale = SaleOnlineServices.UnlockUser(model.TenDangNhap);
                _logger.Information("UNLOCKED Sale Online " + model.TenDangNhap);

                //TẠO LẠI EMAIL CHO USER
                #region TẠO EMAIL
                var abpPhong = AbpServices.GetAbpPhongBanByMaPhongBan(model.MaPhongBanCap1);
                string userName = model.TenDangNhap;
                string mailList = "";
                if (abpPhong != null)
                {
                    if (!string.IsNullOrEmpty(abpPhong.MailingList))
                    {
                        mailList = abpPhong.MailingList;
                    }
                }

                string ten = UsernameGenerator.ConvertToUnSign(model.HoTen);
                string ho = UsernameGenerator.ConvertToUnSign(model.Ho);
                CreateUserInput input = new CreateUserInput
                {
                    Domain = "haiphatland.com.vn",
                    Username = model.TenDangNhap,
                    FirstName = ten,
                    LastName = ten,
                    FullName = ho + " " + ten,
                    Password = pw,
                    AdminNotes = "Tạo từ tool, time: " + DateTime.Now.ToString("G"),
                    MailList = mailList,
                    Group = ""
                };
                var res = await MdaemonXmlApi.CreateUser(input);

                _logger.Information(userName + " CREATED on MDaemon at " + DateTime.Now.ToString("G"));
                #endregion

                listActive.Add(model.HoVaTen + " " + userName + ". Message: " + userDis);
            }

            //GỬI MAIL THÔNG BÁO USER LỖI
            if (listNoti.Any())
            {
                try
                {
                    string subject = "[ACM] DANH SÁCH LỖI KHI THỰC HIỆN ENABLE " + DateTime.Now.ToString("dd/MM/yy");
                    int k = 0;
                    foreach (var item in listNoti)
                    {
                        k++;
                        emailError += k + ". " + item + "<br />";
                    }
                    emailError += "Lưu ý:<br />";
                    emailError += "Các trường hợp trên cần xử lý thủ công<br />";

                    MailHelper.EmailSender(emailError, subject);
                }
                catch (Exception e)
                {
                    _logger.Error("Lỗi gửi mail: " + e);
                }
            }

            if (listActive.Any())
            {
                try
                {
                    string subject = "[ACM] DANH SÁCH USER ĐÃ KÍCH HOẠT LẠI NGÀY " + DateTime.Now.ToString("dd/MM/yy");
                    int l = 0;
                    foreach (var item in listActive)
                    {
                        l++;
                        emailReactive += l + ". " + item + "<br />";
                    }
                    emailReactive += "Lưu ý:<br />";
                    emailReactive += "1. Pass mặc định của mail và user là Hpl@xxx. Với xxx là 3 số cuối của điện thoại<br />";
                    emailReactive += "2. Nếu email đã có sẵn và không đăng nhập được,  yêu cầu Admin reset<br />";

                    MailHelper.EmailSender(emailReactive, subject);
                }
                catch (Exception e)
                {
                    _logger.Error("Lỗi gửi mail: " + e);
                }

            }
        }

        public async Task DisableUser3(List<GetAllNhanVienTheoListMaNvReturnModel> listNvs)
        {
            string bodyMailError = "<p style=\"font-weight: bold\">DANH SÁCH LỖI KHI THỰC HIỆN DISABLE NGÀY " + DateTime.Now.ToString("dd/MM/yy") + "</p>";
            string bodyMailDis = "<p style=\"font-weight: bold\">DANH SÁCH USER ĐÃ DISABLE VÀ XÓA EMAIL NGÀY " + DateTime.Now.ToString("dd/MM/yy") + "</p>";

            int i = 0;

            List<string> listNotiError = new List<string>();
            List<string> listNotiAdmin = new List<string>();
            List<EmailNotifications> listNotifications = new List<EmailNotifications>();

            var listLog = new List<HplDisableUserLog>();
            foreach (var model in listNvs)
            {
                i++;

                //KIỂM TRA USER
                if (string.IsNullOrEmpty(model.TenDangNhap))
                {
                    string notiUser = model.HoVaTen;
                    notiUser += " (" + model.MaNhanVien + ") ";
                    notiUser += "không tồn tại user trên HRM";
                    listNotiError.Add(notiUser);

                    continue;
                }

                //EMAIL TRỐNG ==> đưa ra thông báo
                var email = CommonHelper.IsValidEmail(model.Email);
                if (string.IsNullOrEmpty(email))
                {
                    string notiEmail = model.HoVaTen;
                    notiEmail += " (" + model.MaNhanVien + ") ";
                    notiEmail += "không tồn tại email";
                    listNotiError.Add(notiEmail);

                    continue;
                }

                //EMAIL KHÔNG PHẢI LÀ @haiphatland.com.vn ==> đưa ra thông báo
                var str = email.Split("@");
                if (!str[1].ToLower().Equals("haiphatland.com.vn"))
                {
                    string notiEmail = model.HoVaTen;
                    notiEmail += " (" + model.MaNhanVien + "). ";
                    notiEmail += "username " + model.TenDangNhap + " ";
                    notiEmail += "có email không phải của HPL: " + email;
                    listNotiError.Add(notiEmail);

                    continue;
                }

                //USERNAME VÀ EMAIL KHÔNG GIỐNG NHAU ==> Đưa ra thông báo
                if (!str[0].ToLower().Equals(model.TenDangNhap.ToLower()))
                {
                    string notiEmail = model.HoVaTen;
                    notiEmail += " (" + model.MaNhanVien + "). ";
                    notiEmail += " có username " + model.TenDangNhap + " ";
                    notiEmail += "và email không giống nhau " + email;
                    listNotiError.Add(notiEmail);

                    continue;
                }

                //DISABLE AD USER
                string userDis = "";
                try
                {
                    //TODO
                    userDis = _passwordChangeProvider.DisableUser(model.TenDangNhap);
                    _logger.Information("DISABLED user " + model.TenDangNhap);
                }
                catch (Exception e)
                {
                    string notiEmail = model.HoVaTen;
                    notiEmail += " (" + model.MaNhanVien + "). ";
                    notiEmail += "User:  " + model.TenDangNhap + " ";
                    notiEmail += "lỗi khi disable trên AD: " + e.Message;
                    listNotiError.Add(notiEmail);

                    continue;
                }

                //Inactive USER TRÊN HRM
                try
                {
                    UserService.InactiveUserHrm(model.TenDangNhap);
                }
                catch (Exception e)
                {
                    _logger.Error("Lỗi khi inactive user HRM: " + e.Message);
                }

                //XÓA EMAIL
                var delRes = await MdaemonXmlApi.DeleteUserByUserName(model.TenDangNhap);
                string delMail = "N/A";
                try
                {
                    var json = JsonConvert.SerializeObject(delRes);
                    JObject o = JObject.Parse(json);
                    var message = o["Payload"]?["MDaemon"]?["API"]?["Response"]?["Status"]?["@message"];
                    if (message != null)
                    {
                        string strMes = message.ToString();
                        string strCheck1 = "User not found (haiphatland.com.vn\\" + model.TenDangNhap + ")";
                        if (strCheck1.Equals(strMes))
                        {
                            delMail = "Không tồn tại";
                        }
                        else
                        {
                            string strCheck2 = "The operation completed successfully.";
                            if (strCheck2.Equals(strMes))
                            {
                                delMail = "Đã xóa";
                            }
                        }
                        _logger.Information("DELETED email " + model.TenDangNhap +
                                            ". Message: " + strMes);
                    }
                }
                catch (Exception e)
                {
                    _logger.Error("DELETED email " + model.TenDangNhap +
                                        ". Lỗi: " + e.Message);
                }

                //LOCK SALE ONLINE
                var lockSale = SaleOnlineServices.LockUser(model.TenDangNhap);
                _logger.Information("LOCKED Sale Online " + model.TenDangNhap);

                //UPDATE LOG VÀO DB ACM USER ĐÃ DISABLE
                var userDisLog = new HplDisableUserLog
                {
                    NhanVienId = model.NhanVienId,
                    MaNhanVien = model.MaNhanVien,
                    UserName = model.TenDangNhap,
                    Ho = model.Ho,
                    Ten = model.HoTen,
                    Email = model.Email,
                    EmailCaNhan = model.EmailCaNhan,
                    DienThoai = model.DienThoai,
                    Cmnd = model.Cmnd,
                    PhongBanId = model.PhongBanId,
                    MaPhongBan = model.MaPhongBan,
                    TenPhongBan = model.TenPhongBan,
                    PhongBanCap1Id = model.PhongBanCap1Id,
                    MaPhongBanCap1 = model.MaPhongBanCap1,
                    TenPhongBanCap1 = model.TenPhongBanCap1
                };

                //public string DisableAd { get; set; } // DisableAd (length: 50)
                userDisLog.DisableAd = userDis;
                //public string DeleteEmail { get; set; } // DeleteEmail (length: 50)
                userDisLog.DeleteEmail = delMail;

                //public string LockSaleOnline { get; set; } // LockSaleOnline (length: 50)
                userDisLog.LockSaleOnline = lockSale;

                //public string LinkHrm { get; set; } // LinkHrm (length: 512)
                userDisLog.LinkHrm = "https://hrm.haiphatland.com.vn/HRIS/Profile/Index/" + model.NhanVienId;
                //public DateTime? DateCreated { get; set; } // DateCreated
                userDisLog.DateCreated = DateTime.Now;
                //public string JsonLog { get; set; } // JsonLog (length: 1073741823)
                userDisLog.JsonLog = JsonConvert.SerializeObject(model);

                listLog.Add(userDisLog);

                //ADD LIST GỬI THEO TỪNG BỘ PHẬN
                var emailNoti = listNotifications.FirstOrDefault(x => x.MaPhongBanCap1 == model.MaPhongBanCap1);

                if (emailNoti == null)
                {
                    var abpPhong = AbpServices.GetAbpPhongBanByMaPhongBan(model.MaPhongBanCap1);
                    if (!string.IsNullOrEmpty(model.MaPhongBanCap1))
                    {
                        emailNoti = new EmailNotifications();
                        emailNoti.MaPhongBanCap1 = model.MaPhongBanCap1;
                        emailNoti.TenPhongBanCap1 = model.TenPhongBanCap1;
                        if (!string.IsNullOrEmpty(abpPhong.EmailNotification))
                        {
                            emailNoti.EmailNotifyReceiver = abpPhong.EmailNotification;
                        }
                        else
                        {
                            listNotiError.Add(model.TenPhongBanCap1 + " chưa cập nhật email trợ lý vào ACM");
                        }
                    }
                    else
                    {
                        listNotiError.Add(model.TenPhongBanCap1 + " ==> KHÔNG XÁC ĐỊNH ĐƯỢC PHÒNG BAN CẤP 1 CỦA USER " + model.TenDangNhap);
                    }

                    emailNoti.ListUsers = new List<string>();

                    listNotifications.Add(emailNoti);
                }

                emailNoti.ListUsers.Add("<a href=\"https://hrm.haiphatland.com.vn/HRIS/Profile/Index/" +
                                        model.NhanVienId + "\">" + model.HoVaTen +
                                        "</a>. Mã NV: " +
                                        model.MaNhanVien + ". User: " + model.TenDangNhap +
                                        " (" + model.MaPhongBanCap1 + ")<br />");

                //Log SEND email to ADMIN
                string notiAdmin = "<a href=\"https://hrm.haiphatland.com.vn/HRIS/Profile/Index/" + model.NhanVienId + "\">";
                notiAdmin += model.HoVaTen + "</a>. Mã NV: ";
                notiAdmin += model.MaNhanVien + ". User: " + model.TenDangNhap;
                notiAdmin += " (" + model.MaPhongBanCap1 + ")";
                notiAdmin += " ==> [AD: " + userDis + ". Email: " + delMail + ". Sale: " + lockSale + "]";
                listNotiAdmin.Add(notiAdmin);
            }

            //ADD LOG DANH SÁCH USER ĐÃ DISABLE
            if (listLog.Any())
            {
                AbpServices.AddDisableLogAbp(listLog);
            }

            //GỬI EMAIL THÔNG BÁO LỖI KHI XỬ LÝ
            if (listNotiError.Any())
            {
                try
                {
                    string subject = "[ACM] DANH SÁCH LỖI KHI THỰC HIỆN DISABLE " + DateTime.Now.ToString("dd/MM/yy");
                    int k = 0;
                    foreach (var item in listNotiError)
                    {
                        k++;
                        bodyMailDis += k + ". " + item + "<br />";
                    }
                    bodyMailDis += "Lưu ý:<br />";
                    bodyMailDis += "Các trường hợp trên cần xử lý thủ công<br />";
                    MailHelper.EmailSender(bodyMailDis, subject);
                }
                catch (Exception e)
                {
                    _logger.Error("Lỗi gửi mail: " + e);
                }
            }

            //GỬI THÔNG BÁO DANH SÁCH USER ĐÃ ĐƯỢC DISABLE CHO ADMIN
            if (listNotiAdmin.Any())
            {
                try
                {
                    string subject = "[ACM] DANH SÁCH USER ĐÃ DISABLE VÀ XÓA EMAIL NGÀY " + DateTime.Now.ToString("dd/MM/yy");
                    int k = 0;
                    foreach (var item in listNotiAdmin)
                    {
                        k++;
                        bodyMailError += k + ". " + item + "<br />";
                    }

                    MailHelper.EmailSender(bodyMailError, subject);
                }
                catch (Exception e)
                {
                    _logger.Error("Lỗi gửi mail: " + e);
                }

            }

            if (listNvs.Any())
            {
                //GỬI THÔNG BÁO EMAIL CHO CÁC TRỢ LÝ
                if (listNotifications.Any())
                {
                    foreach (var notification in listNotifications)
                    {
                        List<string> listReceivers = new List<string>();

                        int l = 0;
                        if (!string.IsNullOrEmpty(notification.EmailNotifyReceiver))
                        {
                            var listEmails = notification.EmailNotifyReceiver.Split(",");
                            foreach (var email in listEmails)
                            {
                                if (MailHelper.IsValidEmail(email))
                                {
                                    listReceivers.Add(email);
                                }
                            }
                            //Danh sách thông tin tài khoản đã DISABLE
                            string bodyEmail = "<p style=\"font-weight: bold\">DANH SÁCH TÀI KHOẢN DISABLE và XÓA email</p>";
                            bodyEmail += "<p style=\"font-weight: bold\">" + notification.TenPhongBanCap1 + "</p>";
                            foreach (var user in notification.ListUsers)
                            {
                                l++;
                                bodyEmail += l + ". " + user;
                            }

                            bodyEmail += "Lưu ý:<br />";
                            bodyEmail += "Trợ lý nhân sự nếu phát hiện sai sót, yêu cầu liên hệ gấp với ban HCNS<br />";

                            string subject = "[HPL] HCNS THÔNG BÁO TÀI KHOẢN ĐÃ XÓA NGÀY " +
                                             DateTime.Now.ToString("dd/MM/yyyy");

                            MailHelper.EmailSender(bodyEmail, subject, listReceivers);
                        }
                    }
                }
            }
        }

        public async Task DisableUser2(List<NhanVienViewModel2> listNvs)
        {
            string bodyMailError = "<p style=\"font-weight: bold\">DANH SÁCH LỖI KHI THỰC HIỆN DISABLE NGÀY " + DateTime.Now.ToString("dd/MM/yy") + "</p>";
            string bodyMailDis = "<p style=\"font-weight: bold\">DANH SÁCH USER ĐÃ DISABLE VÀ XÓA EMAIL NGÀY " + DateTime.Now.ToString("dd/MM/yy") + "</p>";

            int i = 0;

            List<string> listNotiError = new List<string>();
            List<string> listNotiAdmin = new List<string>();
            List<EmailNotifications> listNotifications = new List<EmailNotifications>();

            var listLog = new List<HplDisableUserLog>();
            foreach (var model in listNvs)
            {
                i++;

                //KIỂM TRA USER
                if (string.IsNullOrEmpty(model.TenDangNhap))
                {
                    string notiUser = model.Ho + " " + model.Ten;
                    notiUser += " (" + model.MaNhanVien + ") ";
                    notiUser += "không tồn tại user trên HRM";
                    listNotiError.Add(notiUser);

                    continue;
                }

                //EMAIL TRỐNG ==> đưa ra thông báo
                var email = CommonHelper.IsValidEmail(model.Email);
                if (string.IsNullOrEmpty(email))
                {
                    string notiEmail = model.Ho + " " + model.Ten;
                    notiEmail += " (" + model.MaNhanVien + ") ";
                    notiEmail += "không tồn tại email";
                    listNotiError.Add(notiEmail);

                    continue;
                }

                //EMAIL KHÔNG PHẢI LÀ @haiphatland.com.vn ==> đưa ra thông báo
                var str = email.Split("@");
                if (!str[1].ToLower().Equals("haiphatland.com.vn"))
                {
                    string notiEmail = model.Ho + " " + model.Ten;
                    notiEmail += " (" + model.MaNhanVien + "). ";
                    notiEmail += "username " + model.TenDangNhap + " ";
                    notiEmail += "có email không phải của HPL: " + email;
                    listNotiError.Add(notiEmail);

                    continue;
                }

                //USERNAME VÀ EMAIL KHÔNG GIỐNG NHAU ==> Đưa ra thông báo
                if (!str[0].ToLower().Equals(model.TenDangNhap.ToLower()))
                {
                    string notiEmail = model.Ho + " " + model.Ten;
                    notiEmail += " (" + model.MaNhanVien + "). ";
                    notiEmail += " có username " + model.TenDangNhap + " ";
                    notiEmail += "và email không giống nhau " + email;
                    listNotiError.Add(notiEmail);

                    continue;
                }

                //DISABLE AD USER
                string userDis = "";
                try
                {
                    //TODO
                    userDis = _passwordChangeProvider.DisableUser(model.TenDangNhap);
                    _logger.Information("DISABLED user " + model.TenDangNhap);
                }
                catch (Exception e)
                {
                    string notiEmail = model.Ho + " " + model.Ten;
                    notiEmail += " (" + model.MaNhanVien + "). ";
                    notiEmail += "User:  " + model.TenDangNhap + " ";
                    notiEmail += "lỗi khi disable trên AD: " + e.Message;
                    listNotiError.Add(notiEmail);

                    continue;
                }

                //Inactive USER TRÊN HRM
                try
                {
                    UserService.InactiveUserHrm(model.TenDangNhap);
                }
                catch (Exception e)
                {
                    _logger.Error("Lỗi khi inactive user HRM: " + e.Message);
                }

                //XÓA EMAIL
                var delRes = await MdaemonXmlApi.DeleteUserByUserName(model.TenDangNhap);
                string delMail = "N/A";
                try
                {
                    var json = JsonConvert.SerializeObject(delRes);
                    JObject o = JObject.Parse(json);
                    var message = o["Payload"]?["MDaemon"]?["API"]?["Response"]?["Status"]?["@message"];
                    if (message != null)
                    {
                        string strMes = message.ToString();
                        string strCheck1 = "User not found (haiphatland.com.vn\\" + model.TenDangNhap + ")";
                        if (strCheck1.Equals(strMes))
                        {
                            delMail = "Không tồn tại";
                        }
                        else
                        {
                            string strCheck2 = "The operation completed successfully.";
                            if (strCheck2.Equals(strMes))
                            {
                                delMail = "Đã xóa";
                            }
                        }
                        _logger.Information("DELETED email " + model.TenDangNhap +
                                            ". Message: " + strMes);
                    }

                }
                catch (Exception e)
                {
                    _logger.Error("DELETED email " + model.TenDangNhap +
                                        ". Lỗi: " + e.Message);
                }

                //LOCK SALE ONLINE
                var lockSale = SaleOnlineServices.LockUser(model.TenDangNhap);
                _logger.Information("LOCKED Sale Online " + model.TenDangNhap);

                //UPDATE LOG VÀO DB ACM USER ĐÃ DISABLE
                var userDisLog = new HplDisableUserLog
                {
                    NhanVienId = model.NhanVienId,
                    MaNhanVien = model.MaNhanVien,
                    UserName = model.TenDangNhap,
                    Ho = model.Ho,
                    Ten = model.Ten,
                    Email = model.Email,
                    EmailCaNhan = model.EmailCaNhan,
                    DienThoai = model.DienThoai,
                    Cmnd = model.Cmnd,
                    PhongBanId = model.PhongBanId,
                    MaPhongBan = model.MaPhongBan,
                    TenPhongBan = model.TenPhongBan,
                    PhongBanCap1Id = model.PhongBanCap1Id,
                    MaPhongBanCap1 = model.MaPhongBanCap1,
                    TenPhongBanCap1 = model.TenPhongBanCap1
                };

                //public string DisableAd { get; set; } // DisableAd (length: 50)
                userDisLog.DisableAd = userDis;
                //public string DeleteEmail { get; set; } // DeleteEmail (length: 50)
                userDisLog.DeleteEmail = delMail;

                //public string LockSaleOnline { get; set; } // LockSaleOnline (length: 50)
                userDisLog.LockSaleOnline = lockSale;

                //public string LinkHrm { get; set; } // LinkHrm (length: 512)
                userDisLog.LinkHrm = "https://hrm.haiphatland.com.vn/HRIS/Profile/Index/" + model.NhanVienId;
                //public DateTime? DateCreated { get; set; } // DateCreated
                userDisLog.DateCreated = DateTime.Now;
                //public string JsonLog { get; set; } // JsonLog (length: 1073741823)
                userDisLog.JsonLog = JsonConvert.SerializeObject(model);

                listLog.Add(userDisLog);

                //ADD LIST GỬI THEO TỪNG BỘ PHẬN
                var emailNoti = listNotifications.FirstOrDefault(x => x.MaPhongBanCap1 == model.MaPhongBanCap1);

                if (emailNoti == null)
                {
                    var abpPhong = AbpServices.GetAbpPhongBanByMaPhongBan(model.MaPhongBanCap1);
                    if (!string.IsNullOrEmpty(model.MaPhongBanCap1))
                    {
                        emailNoti = new EmailNotifications();
                        emailNoti.MaPhongBanCap1 = model.MaPhongBanCap1;
                        emailNoti.TenPhongBanCap1 = model.TenPhongBanCap1;
                        if (!string.IsNullOrEmpty(abpPhong.EmailNotification))
                        {
                            emailNoti.EmailNotifyReceiver = abpPhong.EmailNotification;
                        }
                        else
                        {
                            listNotiError.Add(model.TenPhongBanCap1 + " chưa cập nhật email trợ lý vào ACM");
                        }
                    }
                    else
                    {
                        listNotiError.Add(model.TenPhongBanCap1 + " ==> KHÔNG XÁC ĐỊNH ĐƯỢC PHÒNG BAN CẤP 1 CỦA USER " + model.TenDangNhap);
                    }

                    emailNoti.ListUsers = new List<string>();

                    listNotifications.Add(emailNoti);
                }

                emailNoti.ListUsers.Add("<a href=\"https://hrm.haiphatland.com.vn/HRIS/Profile/Index/" +
                                        model.NhanVienId + "\">" + model.Ho + " " + model.Ten +
                                        "</a>. Mã NV: " +
                                        model.MaNhanVien + ". User: " + model.TenDangNhap +
                                        " (" + model.MaPhongBanCap1 + ")<br />");

                //Log SEND email to ADMIN
                string notiAdmin = i + ". <a href=\"https://hrm.haiphatland.com.vn/HRIS/Profile/Index/" + model.NhanVienId + "\">";
                notiAdmin += model.Ho + " " + model.Ten + "</a>. Mã NV: ";
                notiAdmin += model.MaNhanVien + ". User: " + model.TenDangNhap;
                notiAdmin += " (" + model.MaPhongBanCap1 + ")";
                notiAdmin += " [AD: " + userDis + ". Email: " + delMail + ". Sale: " + lockSale + "]<br />";
                listNotiAdmin.Add(notiAdmin);
            }

            //ADD LOG DANH SÁCH USER ĐÃ DISABLE
            if (listLog.Any())
            {
                AbpServices.AddDisableLogAbp(listLog);
            }

            //GỬI EMAIL THÔNG BÁO LỖI KHI XỬ LÝ
            if (listNotiError.Any())
            {
                try
                {
                    string subject = "[ACM] DANH SÁCH LỖI KHI THỰC HIỆN DISABLE " + DateTime.Now.ToString("dd/MM/yy");
                    int k = 0;
                    foreach (var item in listNotiError)
                    {
                        k++;
                        bodyMailDis += k + ". " + item + "<br />";
                    }
                    bodyMailDis += "Lưu ý:<br />";
                    bodyMailDis += "Các trường hợp trên cần xử lý thủ công<br />";
                    MailHelper.EmailSender(bodyMailDis, subject);
                }
                catch (Exception e)
                {
                    _logger.Error("Lỗi gửi mail: " + e);
                }
            }

            //GỬI THÔNG BÁO DANH SÁCH USER ĐÃ ĐƯỢC DISABLE CHO ADMIN
            if (listNotiAdmin.Any())
            {
                try
                {
                    string subject = "[ACM] DANH SÁCH USER ĐÃ DISABLE VÀ XÓA EMAIL NGÀY " + DateTime.Now.ToString("dd/MM/yy");
                    int k = 0;
                    foreach (var item in listNotiAdmin)
                    {
                        k++;
                        bodyMailError += k + ". " + item + "<br />";
                    }
                    bodyMailError += "Lưu ý:<br />";
                    bodyMailError += "Các trường hợp trên cần xử lý thủ công<br />";


                    MailHelper.EmailSender(bodyMailError, subject);
                }
                catch (Exception e)
                {
                    _logger.Error("Lỗi gửi mail: " + e);
                }

            }

            if (listNvs.Any())
            {
                //GỬI THÔNG BÁO EMAIL CHO CÁC TRỢ LÝ
                if (listNotifications.Any())
                {
                    foreach (var notification in listNotifications)
                    {
                        List<string> listReceivers = new List<string>();

                        int l = 0;
                        if (!string.IsNullOrEmpty(notification.EmailNotifyReceiver))
                        {
                            var listEmails = notification.EmailNotifyReceiver.Split(",");
                            foreach (var email in listEmails)
                            {
                                if (MailHelper.IsValidEmail(email))
                                {
                                    listReceivers.Add(email);
                                }
                            }
                            //Danh sách thông tin tài khoản đã tạo
                            string bodyEmail = "<p style=\"font-weight: bold\">DANH SÁCH TÀI KHOẢN ĐÃ TẠO</p>";
                            bodyEmail += "<p style=\"font-weight: bold\">" + notification.TenPhongBanCap1 + "</p>";
                            foreach (var user in notification.ListUsers)
                            {
                                l++;
                                bodyEmail += l + ". " + user;
                            }

                            bodyEmail += "Lưu ý:<br />";
                            bodyEmail += "Pass mặc định của mail và user là Hpl@xxx. Với xxx là 3 số cuối của điện thoại<br />";

                            string subject = "[HPL] HCNS THÔNG BÁO TÀI KHOẢN ĐÃ TẠO NGÀY " +
                                             DateTime.Now.ToString("dd/MM/yyyy");

                            MailHelper.EmailSender(bodyEmail, subject, listReceivers);
                        }
                    }
                }
            }
        }

        public async Task DisableUser(List<NhanVienViewModel> listNvs)
        {
            string bodyEmail = "<p style=\"font-weight: bold\">DANH SÁCH USER DISABLE</p>";
            int i = 0;

            var listLog = new List<HplDisableUserLog>();
            foreach (var model in listNvs)
            {
                i++;
                //DISABLE AD USER
                var userDis = _passwordChangeProvider.DisableUser(model.TenDangNhap);
                _logger.Information("DISABLED user " + model.TenDangNhap);

                //XÓA EMAIL
                var delRes = await MdaemonXmlApi.DeleteUserByUserName(model.TenDangNhap);
                string delMail = "N/A";
                try
                {
                    var json = JsonConvert.SerializeObject(delRes);
                    JObject o = JObject.Parse(json);
                    var message = o["Payload"]?["MDaemon"]?["API"]?["Response"]?["Status"]?["@message"];
                    if (message != null)
                    {
                        string strMes = message.ToString();
                        string strCheck1 = "User not found (haiphatland.com.vn\\" + model.TenDangNhap + ")";
                        if (strCheck1.Equals(strMes))
                        {
                            delMail = "Không tồn tại";
                        }
                        else
                        {
                            string strCheck2 = "The operation completed successfully.";
                            if (strCheck2.Equals(strMes))
                            {
                                delMail = "Đã xóa";
                            }
                        }
                        _logger.Information("DELETED email " + model.TenDangNhap +
                                            ". Message: " + strMes);
                    }

                }
                catch (Exception e)
                {
                    _logger.Error("DELETED email " + model.TenDangNhap +
                                        ". Lỗi: " + e.Message);
                }

                //LOCK SALE ONLINE
                var lockSale = SaleOnlineServices.LockUser(model.TenDangNhap);
                _logger.Information("LOCKED Sale Online " + model.TenDangNhap);

                //UPDATE VÀO DB ACM USER ĐÃ DISABLE
                var userDisLog = new HplDisableUserLog
                {
                    NhanVienId = model.NhanVienId,
                    MaNhanVien = model.MaNhanVien,
                    UserName = model.TenDangNhap,
                    Ho = model.Ho,
                    Ten = model.Ten,
                    Email = model.Email,
                    EmailCaNhan = model.EmailCaNhan,
                    DienThoai = model.DienThoai,
                    Cmnd = model.CMTND,
                    PhongBanId = model.PhongBanId,
                    TenPhongBan = model.TenPhongBan
                };

                var pbCap1 = UserService.GetPhongBanCap1CuaNhanVien(model.MaNhanVien);
                if (pbCap1 == null)
                {
                    pbCap1 = new PhongBan();
                    pbCap1.PhongBanId = 0;
                    pbCap1.Ten = "KHÔNG XÁC ĐỊNH";
                    pbCap1.MaPhongBan = "KHÔNG XÁC ĐỊNH";
                    _logger.Error("KHÔNG XÁC ĐỊNH PHÒNG BAN CẤP 1 CỦA " + model.TenDangNhap + " (" + model.MaNhanVien + ")");
                }
                //public int? PhongBanCap1Id { get; set; } // PhongBanCap1Id
                userDisLog.PhongBanCap1Id = pbCap1.PhongBanId;
                //public string TenPhongBanCap1 { get; set; } // TenPhongBanCap1 (length: 256)
                userDisLog.TenPhongBanCap1 = pbCap1.Ten;
                //public string DisableAd { get; set; } // DisableAd (length: 50)
                userDisLog.DisableAd = userDis;
                //public string DeleteEmail { get; set; } // DeleteEmail (length: 50)
                userDisLog.DeleteEmail = delMail;

                //public string LockSaleOnline { get; set; } // LockSaleOnline (length: 50)
                userDisLog.LockSaleOnline = lockSale;

                //public string LinkHrm { get; set; } // LinkHrm (length: 512)
                userDisLog.LinkHrm = "https://hrm.haiphatland.com.vn/HRIS/Profile/Index/" + model.NhanVienId;
                //public DateTime? DateCreated { get; set; } // DateCreated
                userDisLog.DateCreated = DateTime.Now;
                //public string JsonLog { get; set; } // JsonLog (length: 1073741823)
                userDisLog.JsonLog = JsonConvert.SerializeObject(model);

                listLog.Add(userDisLog);

                //Log SEND email to ADMIN
                bodyEmail += i + ". <a href=\"https://hrm.haiphatland.com.vn/HRIS/Profile/Index/" + model.NhanVienId + "\">";
                bodyEmail += model.Ho + " " + model.Ten + "</a>. Mã NV: ";
                bodyEmail += model.MaNhanVien + ". User: " + model.TenDangNhap;
                bodyEmail += " (" + pbCap1.MaPhongBan + ")";
                bodyEmail += " [AD: " + userDis + ". Email: " + delMail + ". Sale: " + lockSale + "]<br />";
            }

            if (listLog.Any())
            {
                AbpServices.AddDisableLogAbp(listLog);
            }

            if (listNvs.Any())
            {
                try
                {
                    string subject = "[ACM] DANH SÁCH USER ĐÃ DISABLE VÀ XÓA NGÀY " + DateTime.Now.ToString("dd/MM/yy");
                    MailHelper.EmailSender(bodyEmail, subject);
                }
                catch (Exception e)
                {
                    _logger.Error("Lỗi gửi mail: " + e);
                }

            }
        }

        public async Task EmailThongBaoUserLoi()
        {
            bool isSend = false;
            string bodyEmail = "<p style=\"font-weight: bold\">DANH SÁCH USER LỖI TRÊN HRM " + DateTime.Now.ToString("dd/MM/yyyy") + "</p>";

            MdaemonService mdService = new MdaemonService();
            int b = await mdService.CountEmailKhongDung();
            if (b > 0)
            {
                bodyEmail += "<p style=\"font-weight: bold\">DANH SÁCH EMAIL KHÔNG ĐƯỢC SỬ DỤNG TRÊN HRM</p>";
                bodyEmail += "Tổng email: " + b + "<br />";
                bodyEmail += "<a href=\"https://acm.haiphatland.com.vn/api/mdaemon/DanhSachEmailKhongDung/\">XEM DANH SÁCH</a>";
            }

            int i = 0;

            //NhanVienErrorUsername
            #region NhanVienErrorUsername
            var errorUsers = await _abpHplDb.NhanVienErrorUsernameAsync();
            if (errorUsers.Any())
            {
                isSend = true;
                bodyEmail += "<p style=\"font-weight: bold\">DANH SÁCH USERNAME VÀ EMAIL KHÔNG GIỐNG NHAU</p>";
                foreach (var model in errorUsers)
                {
                    i++;
                    bodyEmail += i + ". <a href=\"https://hrm.haiphatland.com.vn/HRIS/Profile/Index/" + model.NhanVienID + "\"> ";
                    bodyEmail += model.HoVaTen + "</a>. ";
                    bodyEmail += model.TrangThai;
                    bodyEmail += ". Mã NV: " + model.MaNhanVien;
                    bodyEmail += ". User: " + model.TenDangNhap;
                    bodyEmail += ". Email: " + model.Email;
                    bodyEmail += " (" + model.MaPhongBanCap1 + ")<br />";
                }
            }
            #endregion

            //NhanVienEmailKoDung
            #region NhanVienEmailKoDung
            i = 0;
            var emailNotHpls = await _abpHplDb.NhanVienEmailKoDungAsync();
            if (emailNotHpls.Any())
            {
                isSend = true;
                bodyEmail += "<p style=\"font-weight: bold\">DANH SÁCH NHÂN SỰ CÓ EMAIL KHÔNG PHẢI CỦA HPL</p>";
                foreach (var model in emailNotHpls)
                {
                    i++;
                    bodyEmail += i + ". <a href=\"https://hrm.haiphatland.com.vn/HRIS/Profile/Index/" + model.NhanVienID + "\"> ";
                    bodyEmail += model.HoVaTen + "</a>. ";
                    bodyEmail += model.TrangThai;
                    bodyEmail += ". Mã NV: " + model.MaNhanVien;
                    bodyEmail += ". Email: " + model.Email;
                    bodyEmail += " (" + model.MaPhongBanCap1 + ")<br />";
                }
            }
            #endregion

            //NhanVienTrungMaNhanVien
            #region NhanVienTrungMaNhanVien

            i = 0;
            var maNvs = await _abpHplDb.NhanVienTrungMaNhanVienAsync();
            if (maNvs.Any())
            {
                isSend = true;
                bodyEmail += "<p style=\"font-weight: bold\">DANH SÁCH TRÙNG MÃ NHÂN VIÊN</p>";
                foreach (var model in maNvs)
                {
                    i++;
                    bodyEmail += i + ". <a href=\"https://hrm.haiphatland.com.vn/HRIS/Profile/Index/" + model.NhanVienID + "\"> ";
                    bodyEmail += model.HoVaTen + "</a>. ";
                    bodyEmail += model.TrangThai;
                    bodyEmail += ". Mã NV: " + model.MaNhanVien;
                    bodyEmail += ". User: " + model.TenDangNhap;
                    bodyEmail += " (" + model.MaPhongBanCap1 + ")<br />";
                }
            }

            #endregion

            //NhanVienTrungUser
            #region NhanVienTrungUser

            i = 0;
            var users = await _abpHplDb.NhanVienTrungUserAsync();
            if (users.Any())
            {
                isSend = true;
                bodyEmail += "<p style=\"font-weight: bold\">DANH SÁCH TRÙNG USERNAME</p>";
                foreach (var model in users)
                {
                    i++;
                    bodyEmail += i + ". <a href=\"https://hrm.haiphatland.com.vn/HRIS/Profile/Index/" + model.NhanVienID + "\"> ";
                    bodyEmail += model.HoVaTen + "</a>. ";
                    bodyEmail += model.TrangThai;
                    bodyEmail += ". Mã NV: " + model.MaNhanVien;
                    bodyEmail += ". User: " + model.TenDangNhap;
                    bodyEmail += " (" + model.MaPhongBanCap1 + ")<br />";
                }
            }
            #endregion

            //NhanVienTrungEmail
            #region NhanVienTrungEmail
            i = 0;
            var emails = await _abpHplDb.NhanVienTrungEmailAsync();
            if (emails.Any())
            {
                isSend = true;
                bodyEmail += "<p style=\"font-weight: bold\">DANH SÁCH TRÙNG EMAIL</p>";
                foreach (var model in emails)
                {
                    i++;
                    bodyEmail += i + ". <a href=\"https://hrm.haiphatland.com.vn/HRIS/Profile/Index/" + model.NhanVienID + "\"> ";
                    bodyEmail += model.HoVaTen + "</a>. ";
                    bodyEmail += model.TrangThai;
                    bodyEmail += ". Mã NV: " + model.MaNhanVien;
                    bodyEmail += ". User: " + model.TenDangNhap;
                    bodyEmail += " (" + model.MaPhongBanCap1 + ") ";
                    bodyEmail += model.Email + "<br />";
                }
            }

            #endregion

            //NhanVienCoNhieuUser
            #region NhanVienCoNhieuUser
            i = 0;
            var nhieuUsers = await _abpHplDb.NhanVienCoNhieuUserAsync();
            if (nhieuUsers.Any())
            {
                isSend = true;
                bodyEmail += "<p style=\"font-weight: bold\">DANH SÁCH NHÂN SỰ CÓ NHIỀU USERS</p>";
                foreach (var model in nhieuUsers)
                {
                    i++;
                    bodyEmail += i + ". <a href=\"https://hrm.haiphatland.com.vn/HRIS/Profile/Index/" + model.NhanVienID + "\"> ";
                    bodyEmail += model.HoVaTen + "</a>. ";
                    bodyEmail += model.TrangThai;
                    bodyEmail += ". Mã NV: " + model.MaNhanVien;
                    bodyEmail += ". User: " + model.TenDangNhap;
                    bodyEmail += " (" + model.MaPhongBanCap1 + ")<br />";
                }
            }

            #endregion

            //NhanVienTrungCmnd
            #region NhanVienTrungCmnd
            i = 0;
            var cmnds = await _abpHplDb.NhanVienTrungCmndAsync();
            if (cmnds.Any())
            {
                //isSend = true;
                bodyEmail += "<p style=\"font-weight: bold\">DANH SÁCH NHÂN SỰ TRÙNG CMND</p>";
                foreach (var model in cmnds)
                {
                    i++;
                    bodyEmail += i + ". <a href=\"https://hrm.haiphatland.com.vn/HRIS/Profile/Index/" + model.NhanVienID + "\"> ";
                    bodyEmail += model.HoVaTen + "</a>. ";
                    bodyEmail += model.TrangThai;
                    bodyEmail += ". Mã NV: " + model.MaNhanVien;
                    bodyEmail += ". User: " + model.TenDangNhap;
                    bodyEmail += " (" + model.MaPhongBanCap1 + ") ";
                    bodyEmail += model.CMTND + "<br />";
                }
            }

            #endregion

            //NhanVienTrungDienThoai
            #region NhanVienTrungDienThoai
            i = 0;
            var phones = await _abpHplDb.NhanVienTrungDienThoaiAsync();
            if (phones.Any())
            {
                //isSend = true;
                bodyEmail += "<p style=\"font-weight: bold\">DANH SÁCH NHÂN SỰ TRÙNG SỐ ĐIỆN THOẠI</p>";
                foreach (var model in phones)
                {
                    i++;
                    bodyEmail += i + ". <a href=\"https://hrm.haiphatland.com.vn/HRIS/Profile/Index/" + model.NhanVienID + "\"> ";
                    bodyEmail += model.HoVaTen + "</a>. ";
                    bodyEmail += model.TrangThai;
                    bodyEmail += ". Mã NV: " + model.MaNhanVien;
                    bodyEmail += ". User: " + model.TenDangNhap;
                    bodyEmail += " (" + model.MaPhongBanCap1 + ") ";
                    bodyEmail += model.DienThoai + "<br />";
                }
            }
            #endregion

            if (isSend)
            {
                string subject = "[ACM] DÁNH SÁCH HỒ SƠ LỖI TRÊN HRM " + DateTime.Now.ToString("dd/MM/yyy");
                MailHelper.EmailSender(bodyEmail, subject);
            }
        }

        public void CreateUserTheoMaNhanVien2(List<GetAllNhanVienTheoListMaNvReturnModel> listNvs)
        {
            string listUserBodyEmail = "<p style=\"font-weight: bold\">DANH SÁCH HỒ SƠ KHÔNG TẠO ĐƯỢC USER</p>";

            List<string> listNoti = new List<string>();

            int i = 0;

            var listCreateAll = new List<GetAllNhanVienTheoListMaNvReturnModel>();
            //var listUpdateAd = new List<NhanVienViewModel>();

            foreach (var model in listNvs)
            {
                i++;

                //A. KIỂM TRA THÔNG TIN ĐỐI VỚI USER NÀY: Username hoặc Email
                //A.1 KIỂM TRA USER
                if (!string.IsNullOrEmpty(model.TenDangNhap))
                {
                    //ĐƯA RA THÔNG BÁO USER KHÔNG ĐƯỢC TẠO
                    string notiUser = model.HoVaTen;
                    notiUser += " (" + model.MaNhanVien + ") ";
                    notiUser += "đã tồn tại username: " + model.TenDangNhap;
                    listNoti.Add(notiUser);

                    continue;
                }

                //A.2. KIỂM TRA EMAIL
                var email = CommonHelper.IsValidEmail(model.Email);
                if (string.IsNullOrEmpty(email)) //EMAIL TRỐNG
                {
                    //ĐƯA VÀO DANH SÁCH TẠO MỚI
                    listCreateAll.Add(model);
                }
                else //ĐÃ CÓ EMAIL
                {
                    //KIỂM TRA EMAIL CÓ PHẢI LÀ @haiphatland.com.vn
                    var str = email.Split("@");

                    if (str[1].ToLower().Equals("haiphatland.com.vn"))
                    {
                        //THÔNG BÁO EMAIL ĐÃ CÓ, CẦN KIỂM TRA LẠI
                        string notiEmail = model.HoVaTen;
                        notiEmail += " (" + model.MaNhanVien + ") ";
                        notiEmail += "đã tồn tại Email: " + email;
                        listNoti.Add(notiEmail);

                        continue;
                    }

                    listCreateAll.Add(model);
                }
            }

            if (listNoti.Any())
            {
                string subject = "[ACM] DANH SÁCH HỒ SƠ KHÔNG TẠO ĐƯỢC USER NGÀY " + DateTime.Now.ToString("dd/MM/yy");
                int k = 0;
                foreach (var str in listNoti)
                {
                    k++;
                    listUserBodyEmail += k + ". " + str + "<br />";
                }

                listUserBodyEmail += "Lưu ý:<br />";
                listUserBodyEmail += "Các trường hợp trên cần xử lý thủ công<br />";

                MailHelper.EmailSender(listUserBodyEmail, subject);
            }

            if (listCreateAll.Any())
            {
                _ = CreateUserAllSys3(listCreateAll);
            }
        }

        /// <summary>
        /// Danh sách nhân sự cần fix thông tin (lọc theo Mã nhập vào)
        /// </summary>
        /// <param name="listNvs"></param>
        /// <returns></returns>
        public void CreateUserTheoMaNhanVien(List<NhanVienViewModel2> listNvs)
        {
            string listUserBodyEmail = "<p style=\"font-weight: bold\">DANH SÁCH HỒ SƠ KHÔNG TẠO ĐƯỢC USER</p>";

            List<string> listNoti = new List<string>();

            int i = 0;

            var listCreateAll = new List<NhanVienViewModel2>();
            //var listUpdateAd = new List<NhanVienViewModel>();

            foreach (var model in listNvs)
            {
                i++;

                //A. KIỂM TRA THÔNG TIN ĐỐI VỚI USER NÀY: Username hoặc Email
                //A.1 KIỂM TRA USER
                if (!string.IsNullOrEmpty(model.TenDangNhap))
                {
                    //ĐƯA RA THÔNG BÁO USER KHÔNG ĐƯỢC TẠO
                    string notiUser = model.Ho + " " + model.Ten;
                    notiUser += " (" + model.MaNhanVien + ") ";
                    notiUser += "đã tồn tại username: " + model.TenDangNhap;
                    listNoti.Add(notiUser);

                    continue;
                }

                //A.2. KIỂM TRA EMAIL
                var email = CommonHelper.IsValidEmail(model.Email);
                if (string.IsNullOrEmpty(email)) //EMAIL TRỐNG
                {
                    //ĐƯA VÀO DANH SÁCH TẠO MỚI
                    listCreateAll.Add(model);
                }
                else //ĐÃ CÓ EMAIL
                {
                    //KIỂM TRA EMAIL CÓ PHẢI LÀ @haiphatland.com.vn
                    var str = email.Split("@");

                    if (str[1].ToLower().Equals("haiphatland.com.vn"))
                    {
                        //THÔNG BÁO EMAIL ĐÃ CÓ, CẦN KIỂM TRA LẠI
                        string notiEmail = model.Ho + " " + model.Ten;
                        notiEmail += " (" + model.MaNhanVien + ") ";
                        notiEmail += "đã tồn tại Email: " + email;
                        listNoti.Add(notiEmail);

                        continue;
                    }

                    listCreateAll.Add(model);
                }
            }

            if (listNoti.Any())
            {
                string subject = "[ACM] DANH SÁCH HỒ SƠ KHÔNG TẠO ĐƯỢC USER NGÀY " + DateTime.Now.ToString("dd/MM/yy");
                int k = 0;
                foreach (var str in listNoti)
                {
                    k++;
                    listUserBodyEmail += k + ". " + str + "<br />";
                }

                listUserBodyEmail += "Lưu ý:<br />";
                listUserBodyEmail += "Các trường hợp trên cần xử lý thủ công<br />";

                MailHelper.EmailSender(listUserBodyEmail, subject);
            }

            if (listCreateAll.Any())
            {
                //_ = CreateUserAllSys(listCreateAll);
                _ = CreateUserAllSys2(listCreateAll);
            }
        }

        public async Task CreateUserAllSys3(List<GetAllNhanVienTheoListMaNvReturnModel> listNvs)
        {
            string listUserBodyEmail = "";
            listUserBodyEmail += "<p style=\"font-weight: bold\">DANH SÁCH USER ĐÃ TẠO MỚI HOẶC SỬA THÔNG TIN</p>";

            string saleUserBodyMail = "<p style=\"font-weight: bold\">DANH SÁCH USER ĐÃ TẠO SALE ONLINE</p>";
            int i = 0;
            int j = 0;
            List<HplSyncLog> listLog = new List<HplSyncLog>();
            List<HplCreateUserLog> listNvLogs = new List<HplCreateUserLog>();
            List<EmailNotifications> listNotifications = new List<EmailNotifications>();
            List<string> listEmailLoi = new List<string>();
            string loiKhongCoPhongBanAcm = "";

            //foreach (var model in listNvs)
            //{
            //    string userName = UsernameGenerator.CreateUsernameFromName(model.Ho, model.Ten);
            //}

            foreach (var model in listNvs)
            {
                try
                {
                    if (string.IsNullOrEmpty(model.TenPhongBanCap1))
                    {
                        loiKhongCoPhongBanAcm += "Mã NV: " + model.MaNhanVien + " Không xác định được Phòng Ban cấp 1 <br />";
                        _logger.Error("KHÔNG XÁC ĐỊNH PHÒNG BAN CẤP 1 của " + model.TenDangNhap + " (" + model.MaNhanVien + ")");
                        continue;
                    }

                    var abpPhong = AbpServices.GetAbpPhongBanByMaPhongBan(model.MaPhongBanCap1);
                    if (abpPhong == null)
                    {
                        loiKhongCoPhongBanAcm += "Mã NV: " + model.MaNhanVien + " Không xác định mã PB ACM <br />";
                        _logger.Error("KHÔNG XÁC ĐỊNH PHÒNG BAN CẤU HÌNH ACM của " + model.TenDangNhap + " (" + model.MaNhanVien + ")");
                        continue;
                    }

                    i++;
                    string userName = UsernameGenerator.CreateUsernameFromName(model.Ho, model.HoTen);
                    //CẬP NHẬT LẠI TÊN ĐĂNG NHẬP
                    model.TenDangNhap = userName;

                    string ten = UsernameGenerator.ConvertToUnSign(model.HoTen);
                    string ho = UsernameGenerator.ConvertToUnSign(model.Ho);
                    string pw = "Hpl@123";
                    string hoVaTen = ho + " " + ten;
                    string dienThoai = "";

                    try
                    {
                        if (model.DienThoai.Trim().Length >= 9)
                        {
                            dienThoai = model.DienThoai.Trim();
                            model.DienThoai = "+84" + int.Parse(dienThoai);
                            pw = "Hpl@" + dienThoai.Substring(dienThoai.Length - 3, 3);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error(model.MaPhongBan + " Số điện thoại lỗi " + e.Message);
                        model.DienThoai = "";
                    }

                    //TẠO USER AD
                    //var userInfoAd = _passwordChangeProvider.CreateAdUser(userAd, pw);
                    _passwordChangeProvider.CreateOrUpdateAdUser2(model, pw);

                    //TẠO USER HRM
                    var nhanVien = UserService.CreateUserHrm4(model, userName);
                    _logger.Information(userName + " CREATED on HRM at " + DateTime.Now.ToString("G"));

                    //TẠO EMAIL
                    #region TẠO EMAIL
                    string mailList = "";
                    if (abpPhong != null)
                    {
                        if (!string.IsNullOrEmpty(abpPhong.MailingList))
                        {
                            mailList = abpPhong.MailingList;
                        }
                    }

                    CreateUserInput input = new CreateUserInput
                    {
                        Domain = "haiphatland.com.vn",
                        Username = userName,
                        FirstName = ten,
                        LastName = ho,
                        FullName = hoVaTen,
                        Password = pw,
                        AdminNotes = "Tạo từ tool, time: " + DateTime.Now.ToString("G"),
                        MailList = mailList,
                        Group = ""
                    };
                    var res = await MdaemonXmlApi.CreateUser(input);

                    _logger.Information(userName + " CREATED on MDaemon at " + DateTime.Now.ToString("G"));

                    //Update log vào DB
                    HplSyncLog syncLog = new HplSyncLog
                    {
                        UserName = userName,
                        MaNhanVien = model.MaNhanVien,
                        Payload = JsonConvert.SerializeObject(nhanVien),
                        LogForSys = "HRM"
                    };
                    listLog.Add(syncLog);
                    #endregion

                    //TẠO USER TRÊN SALEONLINE
                    #region TẠO USER TRÊN SALEONLINE
                    string isSaleOnline = "Đã tồn tại";
                    var nvSale = SaleOnlineServices.GetNhanVienByUserName(userName);
                    if (nvSale != null)
                    {
                        //CẬP NHẬT LẠI TRẠNG THÁI USER
                        //Check theo mã Nhân Viên
                        if (nvSale.KeyCode == model.MaNhanVien)
                        {
                            if (nvSale.Lock.Value)
                            {
                                nvSale.Lock = false;
                                isSaleOnline = "Đã unlock user";
                            }

                            //Xác định BranchId và Phòng Ban trên SaleOnline
                            var salePb = SaleOnlineServices.GetPhongBan(model.MaPhongBan);

                            if (salePb != null)
                            {
                                j++;
                                if (nvSale.BranchId != salePb.BranchId)
                                {
                                    nvSale.BranchId = salePb.BranchId;
                                    isSaleOnline = "Đã cập nhật BranchID";
                                }

                                if (nvSale.MaPb != salePb.MaPb)
                                {
                                    nvSale.MaPb = salePb.MaPb;
                                }
                            }

                            var syncLogSale = new HplSyncLog
                            {
                                UserName = userName,
                                MaNhanVien = model.MaNhanVien,
                                Payload = JsonConvert.SerializeObject(nvSale),
                                LogForSys = "SaleOnline"
                            };
                            //Cập nhật user
                            SaleOnlineServices.UpdateUserSale(nvSale);//TODO

                            listLog.Add(syncLogSale);

                            saleUserBodyMail += j + ". " + model.HoVaTen + " - " +
                                        model.MaNhanVien + " - " + userName + "<br />";

                            _logger.Information(userName + " UPDATED on SaleOnline at " + DateTime.Now.ToString("G"));
                            Console.WriteLine(userName + " UPDATED on SaleOnline at " + DateTime.Now.ToString("G"));
                        }
                        else
                        {
                            //Add số 1 vào user này và cập nhật
                            nvSale.MaSo = "1" + nvSale.MaSo;
                            SaleOnlineServices.UpdateUserSale(nvSale);//TODO
                            //Và tạo mới user khác.
                            //TẠO MỚI
                            nvSale = new NhanVienSale
                            {
                                MaSo = userName,
                                //nvSale.MatKhau = model.
                                HoTen = model.HoVaTen,
                                Ho = model.Ho,
                                Ten = model.HoTen,
                                DienThoai = dienThoai,
                                Email = nhanVien.Email,
                                //nvSale.NgaySinh = model.
                                UserType = 1,
                                Lock = false,
                                SoCmnd = model.Cmnd,
                                KeyCode = model.MaNhanVien,
                                MaNvcn = 8,
                                NgayCn = DateTime.Now,
                                IsDeleted = false
                            };

                            //Xác định BranchId trên SaleOnline
                            var salePb = SaleOnlineServices.GetPhongBan(model.MaPhongBan);
                            if (salePb != null)
                            {
                                j++;
                                nvSale.BranchId = salePb.BranchId;
                                nvSale.MaPb = salePb.MaPb;
                                SaleOnlineServices.CreateUserSale(nvSale);//TODO

                                var syncLogSale = new HplSyncLog
                                {
                                    UserName = userName,
                                    MaNhanVien = model.MaNhanVien,
                                    Payload = JsonConvert.SerializeObject(nvSale),
                                    LogForSys = "SaleOnline"
                                };

                                isSaleOnline = "Đã tạo lại";
                                listLog.Add(syncLogSale);

                                saleUserBodyMail += j + ". " + model.HoVaTen + " - " +
                                            model.MaNhanVien + " - " + userName +
                                            " (" + model.MaPhongBanCap1 + ")<br />";

                                _logger.Information(userName + " UPDATED & CREATED on SaleOnline at " + DateTime.Now.ToString("G"));
                                Console.WriteLine(userName + " UPDATED & CREATED on SaleOnline at " + DateTime.Now.ToString("G"));
                            }
                        }
                    }
                    else
                    {
                        //TẠO MỚI
                        nvSale = new NhanVienSale();
                        nvSale.MaSo = userName;
                        //nvSale.MatKhau = model.
                        nvSale.HoTen = model.HoVaTen;
                        nvSale.Ho = model.Ho;
                        nvSale.Ten = model.HoTen;
                        nvSale.DienThoai = dienThoai;
                        nvSale.Email = nhanVien.Email;
                        //nvSale.NgaySinh = model.
                        nvSale.UserType = 1;
                        nvSale.Lock = false;
                        nvSale.SoCmnd = model.Cmnd;
                        nvSale.KeyCode = model.MaNhanVien;
                        nvSale.MaNvcn = 8;
                        nvSale.NgayCn = DateTime.Now;
                        nvSale.IsDeleted = false;

                        //Xác định BranchId trên SaleOnline
                        var salePb = SaleOnlineServices.GetPhongBan(model.MaPhongBan);
                        if (salePb != null)
                        {
                            nvSale.BranchId = salePb.BranchId;
                            nvSale.MaPb = salePb.MaPb;
                        }

                        isSaleOnline = "Đã tạo";
                        SaleOnlineServices.CreateUserSale(nvSale);//TODO

                        j++;
                        saleUserBodyMail += j + ". " + model.HoVaTen + " - ";
                        saleUserBodyMail += model.MaNhanVien + " - " + userName + "<br />";
                        _logger.Information(userName + " CREATED on SaleOnline at " + DateTime.Now.ToString("G"));

                        var syncLogSale = new HplSyncLog
                        {
                            UserName = userName,
                            MaNhanVien = model.MaNhanVien,
                            Payload = JsonConvert.SerializeObject(nvSale),
                            LogForSys = "SaleOnline"
                        };
                        listLog.Add(syncLogSale);
                    }

                    listUserBodyEmail += i + ". <a href=\"https://hrm.haiphatland.com.vn/HRIS/Profile/Index/" + model.NhanVienId + "\">";
                    listUserBodyEmail += model.HoVaTen + "</a>. Mã NV: ";
                    listUserBodyEmail += model.MaNhanVien + ". User: " + userName;
                    listUserBodyEmail += " (" + model.MaPhongBanCap1 + ")<br />";
                    #endregion

                    //ADD LIST GỬI THEO TỪNG BỘ PHẬN
                    var emailNoti = listNotifications.FirstOrDefault(x => x.MaPhongBanCap1 == model.MaPhongBanCap1);

                    if (emailNoti == null)
                    {
                        emailNoti = new EmailNotifications();
                        emailNoti.MaPhongBanCap1 = model.MaPhongBanCap1;
                        emailNoti.TenPhongBanCap1 = model.TenPhongBanCap1;
                        if (!string.IsNullOrEmpty(abpPhong.EmailNotification))
                        {
                            emailNoti.EmailNotifyReceiver = abpPhong.EmailNotification;
                        }
                        else
                        {
                            listEmailLoi.Add(model.TenPhongBanCap1 + " chưa cập nhật email trợ lý vào ACM");
                        }

                        emailNoti.ListUsers = new List<string>();

                        listNotifications.Add(emailNoti);
                    }

                    //TODO for testing
                    //emailNoti.ListUsers.Add("<a href=\"https://hrm.haiphatland.com.vn/HRIS/Profile/Index/" +
                    //                        model.NhanVienId + "\">" + model.Ho + " " + model.Ten +
                    //                        "</a>. Mã NV: " +
                    //                        model.MaNhanVien + ". User: <br />");

                    emailNoti.ListUsers.Add("<a href=\"https://hrm.haiphatland.com.vn/HRIS/Profile/Index/" +
                                            model.NhanVienId + "\">" + model.HoVaTen +
                                            "</a>. Mã NV: " +
                                            model.MaNhanVien + ". User: " + userName +
                                            " (" + model.MaPhongBanCap1 + ")<br />");

                    //Add new LogNhanVien
                    var nvLog = new HplCreateUserLog
                    {
                        NhanVienId = model.NhanVienId,
                        FirstName = model.HoTen,
                        LastName = model.Ho,
                        GioiTinh = model.GioiTinh,
                        MaNhanVien = model.MaNhanVien,
                        TenDangNhap = userName,
                        Email = nhanVien.Email,
                        EmailCaNhan = nhanVien.EmailCaNhan,
                        DienThoai = dienThoai,
                        Cmtnd = model.Cmnd,
                        TenChucVu = model.TenChucVu,
                        TenChucDanh = model.TenChucDanh,
                        PhongBanId = model.PhongBanId,
                        TenPhongBan = model.TenPhongBan,
                        MaPhongBan = model.MaPhongBan,
                        PhongBanCap1Id = model.PhongBanCap1Id,
                        TenPhongBanCap1 = model.TenPhongBanCap1,
                        MaPhongBanCap1 = model.MaPhongBanCap1,
                        IsAd = "OK",
                        IsHrm = "OK",
                        IsSaleOnline = isSaleOnline,
                        IsEmail = "OK",
                        LinkHrm = "https://hrm.haiphatland.com.vn/HRIS/Profile/Index/" + nhanVien.NhanVienId + "/",
                        LinkEmail = "https://acm.haiphatland.com.vn/api/mdaemon/GetUserInfo?username=" + userName,
                        LinkSaleOnline = "https://acm.haiphatland.com.vn/api/SaleOnline/GetNhanVien?username=" + userName,
                        DateCreated = DateTime.Now
                    };

                    listNvLogs.Add(nvLog);
                }
                catch (Exception e)
                {
                    _logger.Error("Error create user for MaNhanVien: " + model.MaNhanVien +
                                  ". Errors: " + e);
                    Console.WriteLine("Error create user for MaNhanVien: " + model.MaNhanVien +
                                      ". Errors: " + e);
                }
            }

            if (listLog.Any())
            {
                AbpServices.AddSyncLogAbp(listLog);
            }

            if (listNvLogs.Any())
            {
                AbpServices.AddLogNhanVien(listNvLogs);
            }

            if (listNvs.Any())
            {
                //GỬI MAIL CHO ADMIN
                if (j > 0) listUserBodyEmail += saleUserBodyMail;

                listUserBodyEmail += "Lưu ý:<br />";
                listUserBodyEmail += "1. Pass mặc định của mail và user là Hpl@xxx. Với xxx là 3 số cuối của điện thoại<br />";
                listUserBodyEmail += "2. Nếu không có số điện thoại, pass là Hpl@123<br />";

                MailHelper.EmailSender(listUserBodyEmail);

                if (listNotifications.Any())
                {
                    //GỬI MAIL CHO CÁC TRỢ LÝ
                    foreach (var notification in listNotifications)
                    {
                        List<string> listReceivers = new List<string>();

                        int l = 0;
                        if (!string.IsNullOrEmpty(notification.EmailNotifyReceiver))
                        {
                            var listEmails = notification.EmailNotifyReceiver.Split(",");
                            foreach (var email in listEmails)
                            {
                                if (MailHelper.IsValidEmail(email))
                                {
                                    listReceivers.Add(email);
                                }
                            }
                            //Danh sách thông tin tài khoản đã tạo
                            string bodyEmail = "<p style=\"font-weight: bold\">DANH SÁCH TÀI KHOẢN ĐÃ TẠO</p>";
                            bodyEmail += "<p style=\"font-weight: bold\">" + notification.TenPhongBanCap1 + "</p>";
                            foreach (var user in notification.ListUsers)
                            {
                                l++;
                                bodyEmail += l + ". " + user;
                            }

                            bodyEmail += "Lưu ý:<br />";
                            bodyEmail += "1. Pass mặc định của mail và user là Hpl@xxx. Với xxx là 3 số cuối của điện thoại<br />";
                            bodyEmail += "2. Nếu không có số điện thoại, pass là Hpl@123<br />";

                            string subject = "[HPL] HCNS THÔNG BÁO TÀI KHOẢN ĐÃ TẠO NGÀY " +
                                             DateTime.Now.ToString("dd/MM/yyyy");

                            MailHelper.EmailSender(bodyEmail, subject, listReceivers);
                        }
                    }

                    //GỬI MAIL THÔNG BÁO ADMIN CÁC EMAIL TRỢ LÝ KHÔNG ĐÚNG.
                    if (listEmailLoi.Any())
                    {
                        string bodyEmail = "<p style=\"font-weight: bold\">DANH SÁCH EMAIL TRỢ LÝ KHÔNG ĐÚNG</p>";
                        foreach (var str in listEmailLoi)
                        {
                            bodyEmail += str + "<br />";
                        }

                        string subject = "[HPL] LỖI KHÔNG GỬI ĐƯỢC EMAIL";
                        MailHelper.EmailSender(bodyEmail, subject);
                    }
                }
            }

            //GỬI MAIL THÔNG BÁO LỖI KHÔNG CÓ PHÒNG BAN CẤP 1 TRONG ACM
            if (!string.IsNullOrEmpty(loiKhongCoPhongBanAcm))
            {
                loiKhongCoPhongBanAcm += "Lưu ý: Có thể TRÙNG MÃ HỒ SƠ<br />";
                string subject = "[ACM] LỖI THIẾU CẤU HÌNH PHÒNG BAN " + DateTime.Now.ToString("dd/MM/yy");
                MailHelper.EmailSender(loiKhongCoPhongBanAcm, subject);
            }
        }

        public async Task CreateUserAllSys2(List<NhanVienViewModel2> listNvs)
        {
            string listUserBodyEmail = "";
            listUserBodyEmail += "<p style=\"font-weight: bold\">DANH SÁCH USER ĐÃ TẠO MỚI HOẶC SỬA THÔNG TIN</p>";

            string saleUserBodyMail = "<p style=\"font-weight: bold\">DANH SÁCH USER ĐÃ TẠO SALE ONLINE</p>";
            int i = 0;
            int j = 0;
            List<HplSyncLog> listLog = new List<HplSyncLog>();
            List<HplCreateUserLog> listNvLogs = new List<HplCreateUserLog>();
            List<EmailNotifications> listNotifications = new List<EmailNotifications>();
            List<string> listEmailLoi = new List<string>();
            string loiKhongCoPhongBanAcm = "";

            //foreach (var model in listNvs)
            //{
            //    string userName = UsernameGenerator.CreateUsernameFromName(model.Ho, model.Ten);
            //}

            foreach (var model in listNvs)
            {
                try
                {
                    if (string.IsNullOrEmpty(model.TenPhongBanCap1))
                    {
                        loiKhongCoPhongBanAcm += "Mã NV: " + model.MaNhanVien + " Không xác định được Phòng Ban cấp 1 <br />";
                        _logger.Error("KHÔNG XÁC ĐỊNH PHÒNG BAN CẤP 1 của " + model.TenDangNhap + " (" + model.MaNhanVien + ")");
                        continue;
                    }

                    var abpPhong = AbpServices.GetAbpPhongBanByMaPhongBan(model.MaPhongBanCap1);
                    if (abpPhong == null)
                    {
                        loiKhongCoPhongBanAcm += "Mã NV: " + model.MaNhanVien + " Không xác định mã PB ACM <br />";
                        _logger.Error("KHÔNG XÁC ĐỊNH PHÒNG BAN CẤU HÌNH ACM của " + model.TenDangNhap + " (" + model.MaNhanVien + ")");
                        continue;
                    }

                    i++;
                    string userName = UsernameGenerator.CreateUsernameFromName(model.Ho, model.Ten);
                    //CẬP NHẬT LẠI TÊN ĐĂNG NHẬP
                    model.TenDangNhap = userName;

                    string ten = UsernameGenerator.ConvertToUnSign(model.Ten);
                    string ho = UsernameGenerator.ConvertToUnSign(model.Ho);
                    string pw = "Hpl@123";
                    string hoVaTen = ho + " " + ten;
                    string dienThoai = "";

                    try
                    {
                        if (model.DienThoai.Trim().Length >= 9)
                        {
                            dienThoai = model.DienThoai.Trim();
                            model.DienThoai = "+84" + int.Parse(dienThoai);
                            pw = "Hpl@" + dienThoai.Substring(dienThoai.Length - 3, 3);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error(model.MaPhongBan + " Số điện thoại lỗi " + e.Message);
                        model.DienThoai = "";
                    }

                    //TẠO USER AD
                    //var userInfoAd = _passwordChangeProvider.CreateAdUser(userAd, pw);
                    _passwordChangeProvider.CreateOrUpdateAdUser(model, pw);

                    //TẠO USER HRM
                    var nhanVien = UserService.CreateUserHrm3(model, userName);
                    _logger.Information(userName + " CREATED on HRM at " + DateTime.Now.ToString("G"));

                    //TẠO EMAIL
                    #region TẠO EMAIL
                    string mailList = "";
                    if (abpPhong != null)
                    {
                        if (!string.IsNullOrEmpty(abpPhong.MailingList))
                        {
                            mailList = abpPhong.MailingList;
                        }
                    }

                    CreateUserInput input = new CreateUserInput
                    {
                        Domain = "haiphatland.com.vn",
                        Username = userName,
                        FirstName = ten,
                        LastName = ho,
                        FullName = hoVaTen,
                        Password = pw,
                        AdminNotes = "Tạo từ tool, time: " + DateTime.Now.ToString("G"),
                        MailList = mailList,
                        Group = ""
                    };
                    var res = await MdaemonXmlApi.CreateUser(input);

                    _logger.Information(userName + " CREATED on MDaemon at " + DateTime.Now.ToString("G"));

                    //Update log vào DB
                    HplSyncLog syncLog = new HplSyncLog
                    {
                        UserName = userName,
                        MaNhanVien = model.MaNhanVien,
                        Payload = JsonConvert.SerializeObject(nhanVien),
                        LogForSys = "HRM"
                    };
                    listLog.Add(syncLog);
                    #endregion

                    //TẠO USER TRÊN SALEONLINE
                    #region TẠO USER TRÊN SALEONLINE
                    string isSaleOnline = "Đã tồn tại";
                    var nvSale = SaleOnlineServices.GetNhanVienByUserName(userName);
                    if (nvSale != null)
                    {
                        //CẬP NHẬT LẠI TRẠNG THÁI USER
                        //Check theo mã Nhân Viên
                        if (nvSale.KeyCode == model.MaNhanVien)
                        {
                            if (nvSale.Lock.Value)
                            {
                                nvSale.Lock = false;
                                isSaleOnline = "Đã unlock user";
                            }

                            //Xác định BranchId và Phòng Ban trên SaleOnline
                            var salePb = SaleOnlineServices.GetPhongBan(model.MaPhongBan);

                            if (salePb != null)
                            {
                                j++;
                                if (nvSale.BranchId != salePb.BranchId)
                                {
                                    nvSale.BranchId = salePb.BranchId;
                                    isSaleOnline = "Đã cập nhật BranchID";
                                }

                                if (nvSale.MaPb != salePb.MaPb)
                                {
                                    nvSale.MaPb = salePb.MaPb;
                                }
                            }

                            var syncLogSale = new HplSyncLog
                            {
                                UserName = userName,
                                MaNhanVien = model.MaNhanVien,
                                Payload = JsonConvert.SerializeObject(nvSale),
                                LogForSys = "SaleOnline"
                            };
                            //Cập nhật user
                            SaleOnlineServices.UpdateUserSale(nvSale);//TODO

                            listLog.Add(syncLogSale);

                            saleUserBodyMail += j + ". " + model.Ho + " " + model.Ten + " - " +
                                        model.MaNhanVien + " - " + userName + "<br />";

                            _logger.Information(userName + " UPDATED on SaleOnline at " + DateTime.Now.ToString("G"));
                            Console.WriteLine(userName + " UPDATED on SaleOnline at " + DateTime.Now.ToString("G"));
                        }
                        else
                        {
                            //Add số 1 vào user này và cập nhật
                            nvSale.MaSo = "1" + nvSale.MaSo;
                            SaleOnlineServices.UpdateUserSale(nvSale);//TODO
                            //Và tạo mới user khác.
                            //TẠO MỚI
                            nvSale = new NhanVienSale
                            {
                                MaSo = userName,
                                //nvSale.MatKhau = model.
                                HoTen = model.Ho + " " + model.Ten,
                                Ho = model.Ho,
                                Ten = model.Ten,
                                DienThoai = dienThoai,
                                Email = nhanVien.Email,
                                //nvSale.NgaySinh = model.
                                UserType = 1,
                                Lock = false,
                                SoCmnd = model.Cmnd,
                                KeyCode = model.MaNhanVien,
                                MaNvcn = 8,
                                NgayCn = DateTime.Now,
                                IsDeleted = false
                            };

                            //Xác định BranchId trên SaleOnline
                            var salePb = SaleOnlineServices.GetPhongBan(model.MaPhongBan);
                            if (salePb != null)
                            {
                                j++;
                                nvSale.BranchId = salePb.BranchId;
                                nvSale.MaPb = salePb.MaPb;
                                SaleOnlineServices.CreateUserSale(nvSale);//TODO

                                var syncLogSale = new HplSyncLog
                                {
                                    UserName = userName,
                                    MaNhanVien = model.MaNhanVien,
                                    Payload = JsonConvert.SerializeObject(nvSale),
                                    LogForSys = "SaleOnline"
                                };

                                isSaleOnline = "Đã tạo lại";
                                listLog.Add(syncLogSale);

                                saleUserBodyMail += j + ". " + model.Ho + " " + model.Ten + " - " +
                                            model.MaNhanVien + " - " + userName +
                                            " (" + model.MaPhongBanCap1 + ")<br />";

                                _logger.Information(userName + " UPDATED & CREATED on SaleOnline at " + DateTime.Now.ToString("G"));
                                Console.WriteLine(userName + " UPDATED & CREATED on SaleOnline at " + DateTime.Now.ToString("G"));
                            }
                        }
                    }
                    else
                    {
                        //TẠO MỚI
                        nvSale = new NhanVienSale();
                        nvSale.MaSo = userName;
                        //nvSale.MatKhau = model.
                        nvSale.HoTen = model.Ho + " " + model.Ten;
                        nvSale.Ho = model.Ho;
                        nvSale.Ten = model.Ten;
                        nvSale.DienThoai = dienThoai;
                        nvSale.Email = nhanVien.Email;
                        //nvSale.NgaySinh = model.
                        nvSale.UserType = 1;
                        nvSale.Lock = false;
                        nvSale.SoCmnd = model.Cmnd;
                        nvSale.KeyCode = model.MaNhanVien;
                        nvSale.MaNvcn = 8;
                        nvSale.NgayCn = DateTime.Now;
                        nvSale.IsDeleted = false;

                        //Xác định BranchId trên SaleOnline
                        var salePb = SaleOnlineServices.GetPhongBan(model.MaPhongBan);
                        if (salePb != null)
                        {
                            nvSale.BranchId = salePb.BranchId;
                            nvSale.MaPb = salePb.MaPb;
                        }

                        isSaleOnline = "Đã tạo";
                        SaleOnlineServices.CreateUserSale(nvSale);//TODO

                        j++;
                        saleUserBodyMail += j + ". " + model.Ho + " " + model.Ten + " - ";
                        saleUserBodyMail += model.MaNhanVien + " - " + userName + "<br />";
                        _logger.Information(userName + " CREATED on SaleOnline at " + DateTime.Now.ToString("G"));

                        var syncLogSale = new HplSyncLog
                        {
                            UserName = userName,
                            MaNhanVien = model.MaNhanVien,
                            Payload = JsonConvert.SerializeObject(nvSale),
                            LogForSys = "SaleOnline"
                        };
                        listLog.Add(syncLogSale);
                    }

                    listUserBodyEmail += i + ". <a href=\"https://hrm.haiphatland.com.vn/HRIS/Profile/Index/" + model.NhanVienId + "\">";
                    listUserBodyEmail += model.Ho + " " + model.Ten + "</a>. Mã NV: ";
                    listUserBodyEmail += model.MaNhanVien + ". User: " + userName;
                    listUserBodyEmail += " (" + model.MaPhongBanCap1 + ")<br />";
                    #endregion

                    //ADD LIST GỬI THEO TỪNG BỘ PHẬN
                    var emailNoti = listNotifications.FirstOrDefault(x => x.MaPhongBanCap1 == model.MaPhongBanCap1);

                    if (emailNoti == null)
                    {
                        emailNoti = new EmailNotifications();
                        emailNoti.MaPhongBanCap1 = model.MaPhongBanCap1;
                        emailNoti.TenPhongBanCap1 = model.TenPhongBanCap1;
                        if (!string.IsNullOrEmpty(abpPhong.EmailNotification))
                        {
                            emailNoti.EmailNotifyReceiver = abpPhong.EmailNotification;
                        }
                        else
                        {
                            listEmailLoi.Add(model.TenPhongBanCap1 + " chưa cập nhật email trợ lý vào ACM");
                        }

                        emailNoti.ListUsers = new List<string>();

                        listNotifications.Add(emailNoti);
                    }

                    //TODO for testing
                    //emailNoti.ListUsers.Add("<a href=\"https://hrm.haiphatland.com.vn/HRIS/Profile/Index/" +
                    //                        model.NhanVienId + "\">" + model.Ho + " " + model.Ten +
                    //                        "</a>. Mã NV: " +
                    //                        model.MaNhanVien + ". User: <br />");

                    emailNoti.ListUsers.Add("<a href=\"https://hrm.haiphatland.com.vn/HRIS/Profile/Index/" +
                                            model.NhanVienId + "\">" + model.Ho + " " + model.Ten +
                                            "</a>. Mã NV: " +
                                            model.MaNhanVien + ". User: " + userName +
                                            " (" + model.MaPhongBanCap1 + ")<br />");

                    //Add new LogNhanVien
                    var nvLog = new HplCreateUserLog
                    {
                        NhanVienId = model.NhanVienId,
                        FirstName = model.Ten,
                        LastName = model.Ho,
                        GioiTinh = model.GioiTinh,
                        MaNhanVien = model.MaNhanVien,
                        TenDangNhap = userName,
                        Email = nhanVien.Email,
                        EmailCaNhan = nhanVien.EmailCaNhan,
                        DienThoai = dienThoai,
                        Cmtnd = model.Cmnd,
                        TenChucVu = model.TenChucVu,
                        TenChucDanh = model.TenChucDanh,
                        PhongBanId = model.PhongBanId,
                        TenPhongBan = model.TenPhongBan,
                        MaPhongBan = model.MaPhongBan,
                        PhongBanCap1Id = model.PhongBanCap1Id,
                        TenPhongBanCap1 = model.TenPhongBanCap1,
                        MaPhongBanCap1 = model.MaPhongBanCap1,
                        IsAd = "OK",
                        IsHrm = "OK",
                        IsSaleOnline = isSaleOnline,
                        IsEmail = "OK",
                        LinkHrm = "https://hrm.haiphatland.com.vn/HRIS/Profile/Index/" + nhanVien.NhanVienId + "/",
                        LinkEmail = "https://acm.haiphatland.com.vn/api/mdaemon/GetUserInfo?username=" + userName,
                        LinkSaleOnline = "https://acm.haiphatland.com.vn/api/SaleOnline/GetNhanVien?username=" + userName,
                        DateCreated = DateTime.Now
                    };

                    listNvLogs.Add(nvLog);
                }
                catch (Exception e)
                {
                    _logger.Error("Error create user for MaNhanVien: " + model.MaNhanVien +
                                  ". Errors: " + e);
                    Console.WriteLine("Error create user for MaNhanVien: " + model.MaNhanVien +
                                      ". Errors: " + e);
                }
            }

            if (listLog.Any())
            {
                AbpServices.AddSyncLogAbp(listLog);
            }

            if (listNvLogs.Any())
            {
                AbpServices.AddLogNhanVien(listNvLogs);
            }

            if (listNvs.Any())
            {
                //GỬI MAIL CHO ADMIN
                if (j > 0) listUserBodyEmail += saleUserBodyMail;

                listUserBodyEmail += "Lưu ý:<br />";
                listUserBodyEmail += "1. Pass mặc định của mail và user là Hpl@xxx. Với xxx là 3 số cuối của điện thoại<br />";
                listUserBodyEmail += "2. Nếu không có số điện thoại, pass là Hpl@123<br />";

                MailHelper.EmailSender(listUserBodyEmail);

                if (listNotifications.Any())
                {
                    //GỬI MAIL CHO CÁC TRỢ LÝ
                    foreach (var notification in listNotifications)
                    {
                        List<string> listReceivers = new List<string>();

                        int l = 0;
                        if (!string.IsNullOrEmpty(notification.EmailNotifyReceiver))
                        {
                            var listEmails = notification.EmailNotifyReceiver.Split(",");
                            foreach (var email in listEmails)
                            {
                                if (MailHelper.IsValidEmail(email))
                                {
                                    listReceivers.Add(email);
                                }
                            }
                            //Danh sách thông tin tài khoản đã tạo
                            string bodyEmail = "<p style=\"font-weight: bold\">DANH SÁCH TÀI KHOẢN ĐÃ TẠO</p>";
                            bodyEmail += "<p style=\"font-weight: bold\">" + notification.TenPhongBanCap1 + "</p>";
                            foreach (var user in notification.ListUsers)
                            {
                                l++;
                                bodyEmail += l + ". " + user;
                            }

                            bodyEmail += "Lưu ý:<br />";
                            bodyEmail += "1. Pass mặc định của mail và user là Hpl@xxx. Với xxx là 3 số cuối của điện thoại<br />";
                            bodyEmail += "2. Nếu không có số điện thoại, pass là Hpl@123<br />";

                            string subject = "[HPL] HCNS THÔNG BÁO TÀI KHOẢN ĐÃ TẠO NGÀY " +
                                             DateTime.Now.ToString("dd/MM/yyyy");
                            MailHelper.EmailSender(bodyEmail, subject, listReceivers);
                        }
                    }

                    //GỬI MAIL THÔNG BÁO ADMIN CÁC EMAIL TRỢ LÝ KHÔNG ĐÚNG.
                    if (listEmailLoi.Any())
                    {
                        string bodyEmail = "<p style=\"font-weight: bold\">DANH SÁCH EMAIL TRỢ LÝ KHÔNG ĐÚNG</p>";
                        foreach (var str in listEmailLoi)
                        {
                            bodyEmail += str + "<br />";
                        }

                        string subject = "[HPL] LỖI KHÔNG GỬI ĐƯỢC EMAIL";
                        MailHelper.EmailSender(bodyEmail, subject);
                    }
                }
            }

            //GỬI MAIL THÔNG BÁO LỖI KHÔNG CÓ PHÒNG BAN CẤP 1 TRONG ACM
            if (!string.IsNullOrEmpty(loiKhongCoPhongBanAcm))
            {
                loiKhongCoPhongBanAcm += "Lưu ý: Có thể TRÙNG MÃ HỒ SƠ<br />";
                string subject = "[ACM] LỖI THIẾU CẤU HÌNH PHÒNG BAN " + DateTime.Now.ToString("dd/MM/yy");
                MailHelper.EmailSender(loiKhongCoPhongBanAcm, subject);
            }
        }

        public async Task CreateUserAllSys(List<NhanVienViewModel> listNvs)
        {
            string listUserBodyEmail = "";
            listUserBodyEmail += "<p style=\"font-weight: bold\">DANH SÁCH USER ĐÃ TẠO MỚI HOẶC SỬA THÔNG TIN</p>";

            string saleUserBodyMail = "<p style=\"font-weight: bold\">DANH SÁCH USER ĐÃ TẠO SALE ONLINE</p>";
            int i = 0;
            int j = 0;
            List<HplSyncLog> listLog = new List<HplSyncLog>();
            List<HplCreateUserLog> listNvLogs = new List<HplCreateUserLog>();
            List<EmailNotifications> listNotifications = new List<EmailNotifications>();
            List<string> listEmailLoi = new List<string>();
            string loiKhongCoPhongBanAcm = "";

            foreach (var model in listNvs)
            {
                //if (!model.MaNhanVien.Equals("KD8-332")) continue;

                try
                {
                    PhongBan phongBanCap1 = UserService.GetPhongBanCap1CuaNhanVien(model.MaNhanVien);
                    if (phongBanCap1 == null)
                    {
                        loiKhongCoPhongBanAcm += "Mã NV: " + model.MaNhanVien + " Không xác định được Phòng Ban cấp 1 <br />";
                        _logger.Error("KHÔNG XÁC ĐỊNH PHÒNG BAN CẤP 1 của " + model.TenDangNhap + " (" + model.MaNhanVien + ")");
                        continue;
                    }

                    var abpPhong = AbpServices.GetAbpPhongBanByMaPhongBan(phongBanCap1.MaPhongBan);
                    if (abpPhong == null)
                    {
                        loiKhongCoPhongBanAcm += "Mã NV: " + model.MaNhanVien + " Không xác định mã PB ACM <br />";
                        _logger.Error("KHÔNG XÁC ĐỊNH PHÒNG BAN CẤU HÌNH ACM của " + model.TenDangNhap + " (" + model.MaNhanVien + ")");
                        continue;
                    }
                    i++;

                    string tenPhongBan = "HAI PHAT LAND COMPANY";
                    if (phongBanCap1 != null)
                    {
                        tenPhongBan = phongBanCap1.Ten;
                    }
                    else
                    {
                        listUserBodyEmail = "<p style=\"font-weight: bold\">LỖI: NHÂN VIÊN ";
                        listUserBodyEmail += model.Ho + " " + model.Ten;
                        listUserBodyEmail += " (" + model.MaNhanVien + ") KHÔNG XÁC ĐỊNH ĐƯỢC PHÒNG BAN CẤP 1</p>";
                        MailHelper.EmailSender(listUserBodyEmail);
                        return;
                    }

                    string userName = UsernameGenerator.CreateUsernameFromName(model.Ho, model.Ten);
                    string ten = UsernameGenerator.ConvertToUnSign(model.Ten);
                    string tenVn = model.Ten.Trim();
                    string ho = UsernameGenerator.ConvertToUnSign(model.Ho);
                    string hoVn = model.Ho.Trim();
                    string telephoneNumber = "";
                    string pw = "Hpl@123";
                    string hoVaTen = ho + " " + ten;
                    string hoVaTenVn = hoVn + " " + tenVn;
                    try
                    {
                        telephoneNumber = "+84" + int.Parse(model.DienThoai.Trim());
                        pw = "Hpl@" + model.DienThoai.Trim().Substring(model.DienThoai.Trim().Length - 3, 3);
                    }
                    catch (Exception e)
                    {
                        Log.Error(model.MaPhongBan + " Số điện thoại lỗi " + e.Message);
                    }

                    //TẠO USER AD
                    UserInfoAd userAd = new UserInfoAd
                    {
                        userPrincipalName = "",
                        sAMAccountName = userName,
                        name = "",
                        sn = tenVn,
                        givenName = hoVn,
                        displayName = hoVaTenVn,
                        mail = "",
                        telephoneNumber = telephoneNumber,
                        department = tenPhongBan,
                        title = model.TenChucDanh,
                        employeeID = model.MaNhanVien,
                        description = "Created by tool. Time: " + DateTime.Now.ToString("G")
                    };

                    var userInfoAd = _passwordChangeProvider.CreateAdUser(userAd, pw);
                    var userInfo = userInfoAd.UserInfo;

                    userName = userInfo.sAMAccountName;

                    //TẠO USER HRM
                    var nhanVien = UserService.CreateUserHrm2(model, userName);
                    _logger.Information(userName + " CREATED on HRM at " + DateTime.Now.ToString("G"));
                    Console.WriteLine(userName + " CREATED on HRM at " + DateTime.Now.ToString("G"));

                    //TẠO EMAIL
                    #region TẠO EMAIL
                    string mailList = "";
                    if (abpPhong != null)
                    {
                        if (string.IsNullOrEmpty(abpPhong.MailingList))
                        {
                            mailList = abpPhong.MailingList;
                        }
                    }

                    CreateUserInput input = new CreateUserInput
                    {
                        Domain = "haiphatland.com.vn",
                        Username = userName,
                        FirstName = ten,
                        LastName = ho,
                        FullName = hoVaTen,
                        Password = pw,
                        AdminNotes = "Tạo từ tool, time: " + DateTime.Now.ToString("G"),
                        MailList = mailList,
                        Group = ""
                    };
                    var res = await MdaemonXmlApi.CreateUser(input);

                    _logger.Information(userName + " CREATED on MDaemon at " + DateTime.Now.ToString("G"));
                    Console.WriteLine(userName + " CREATED on MDaemon at " + DateTime.Now.ToString("G"));

                    //Update log vào DB
                    HplSyncLog syncLog = new HplSyncLog
                    {
                        UserName = userName,
                        MaNhanVien = model.MaNhanVien,
                        Payload = JsonConvert.SerializeObject(nhanVien),
                        LogForSys = "HRM"
                    };
                    listLog.Add(syncLog);
                    #endregion

                    //TẠO USER TRÊN SALEONLINE
                    #region TẠO USER TRÊN SALEONLINE
                    string isSaleOnline = "Đã tồn tại";
                    var nvSale = SaleOnlineServices.GetNhanVienByUserName(userName);
                    if (nvSale != null)
                    {
                        //CẬP NHẬT LẠI TRẠNG THÁI USER
                        //Check theo mã Nhân Viên
                        if (nvSale.KeyCode == model.MaNhanVien)
                        {
                            if (nvSale.Lock.Value)
                            {
                                nvSale.Lock = false;
                                isSaleOnline = "Đã unlock user";
                            }

                            //Xác định BranchId trên SaleOnline
                            //var branch = SaleOnlineServices.GetBranchId(phongBanCap1.MaPhongBan);
                            //var branchId = GetBranchIdOnSaleOnline(model.MaPhongBan, phongBanCap1.MaPhongBan);
                            var salePb = SaleOnlineServices.GetPhongBan(model.MaPhongBan);

                            if (salePb != null)
                            {
                                j++;
                                if (nvSale.BranchId != salePb.BranchId)
                                {
                                    nvSale.BranchId = salePb.BranchId;
                                    isSaleOnline = "Đã cập nhật BranchID";
                                }

                                if (nvSale.MaPb != salePb.MaPb)
                                {
                                    nvSale.MaPb = salePb.MaPb;
                                }
                            }

                            var syncLogSale = new HplSyncLog
                            {
                                UserName = userName,
                                MaNhanVien = model.MaNhanVien,
                                Payload = JsonConvert.SerializeObject(nvSale),
                                LogForSys = "SaleOnline"
                            };
                            //Cập nhật user
                            SaleOnlineServices.UpdateUserSale(nvSale);

                            listLog.Add(syncLogSale);

                            saleUserBodyMail += j + ". " + model.Ho + " " + model.Ten + " - " +
                                        model.MaNhanVien + " - " + userName + "<br />";

                            _logger.Information(userName + " UPDATED on SaleOnline at " + DateTime.Now.ToString("G"));
                            Console.WriteLine(userName + " UPDATED on SaleOnline at " + DateTime.Now.ToString("G"));
                        }
                        else
                        {
                            //Add số 1 vào user này và cập nhật
                            nvSale.MaSo = "1" + nvSale.MaSo;
                            SaleOnlineServices.UpdateUserSale(nvSale);
                            //Và tạo mới user khác.
                            //TẠO MỚI
                            nvSale = new NhanVienSale
                            {
                                MaSo = userName,
                                //nvSale.MatKhau = model.
                                HoTen = model.Ho + " " + model.Ten,
                                Ho = model.Ho,
                                Ten = model.Ten,
                                DienThoai = model.DienThoai,
                                Email = nhanVien.Email,
                                //nvSale.NgaySinh = model.
                                UserType = 1,
                                Lock = false,
                                SoCmnd = model.CMTND,
                                KeyCode = model.MaNhanVien,
                                MaNvcn = 8,
                                NgayCn = DateTime.Now,
                                IsDeleted = false
                            };

                            //Xác định BranchId trên SaleOnline
                            //var branch = SaleOnlineServices.GetBranchId(phongBanCap1.MaPhongBan);
                            //var branchId = GetBranchIdOnSaleOnline(model.MaPhongBan, phongBanCap1.MaPhongBan);
                            var salePb = SaleOnlineServices.GetPhongBan(model.MaPhongBan);
                            if (salePb != null)
                            {
                                j++;
                                nvSale.BranchId = salePb.BranchId;
                                nvSale.MaPb = salePb.MaPb;
                                SaleOnlineServices.CreateUserSale(nvSale);

                                var syncLogSale = new HplSyncLog
                                {
                                    UserName = userName,
                                    MaNhanVien = model.MaNhanVien,
                                    Payload = JsonConvert.SerializeObject(nvSale),
                                    LogForSys = "SaleOnline"
                                };

                                isSaleOnline = "Đã tạo lại";
                                listLog.Add(syncLogSale);

                                saleUserBodyMail += j + ". " + model.Ho + " " + model.Ten + " - " +
                                            model.MaNhanVien + " - " + userName +
                                            " (" + phongBanCap1.MaPhongBan + ")<br />";

                                _logger.Information(userName + " UPDATED & CREATED on SaleOnline at " + DateTime.Now.ToString("G"));
                                Console.WriteLine(userName + " UPDATED & CREATED on SaleOnline at " + DateTime.Now.ToString("G"));
                            }
                        }
                    }
                    else
                    {
                        //TẠO MỚI
                        nvSale = new NhanVienSale();
                        nvSale.MaSo = userName;
                        //nvSale.MatKhau = model.
                        nvSale.HoTen = model.Ho + " " + model.Ten;
                        nvSale.Ho = model.Ho;
                        nvSale.Ten = model.Ten;
                        nvSale.DienThoai = model.DienThoai;
                        nvSale.Email = nhanVien.Email;
                        //nvSale.NgaySinh = model.
                        nvSale.UserType = 1;
                        nvSale.Lock = false;
                        nvSale.SoCmnd = model.CMTND;
                        nvSale.KeyCode = model.MaNhanVien;
                        nvSale.MaNvcn = 8;
                        nvSale.NgayCn = DateTime.Now;
                        nvSale.IsDeleted = false;

                        //Xác định BranchId trên SaleOnline
                        //var branch = SaleOnlineServices.GetBranchId(phongBanCap1.MaPhongBan);
                        //var branchId = GetBranchIdOnSaleOnline(model.MaPhongBan, phongBanCap1.MaPhongBan);
                        var salePb = SaleOnlineServices.GetPhongBan(model.MaPhongBan);
                        if (salePb != null)
                        {
                            j++;
                            nvSale.BranchId = salePb.BranchId;
                            nvSale.MaPb = salePb.MaPb;
                            SaleOnlineServices.CreateUserSale(nvSale);

                            var syncLogSale = new HplSyncLog
                            {
                                UserName = userName,
                                MaNhanVien = model.MaNhanVien,
                                Payload = JsonConvert.SerializeObject(nvSale),
                                LogForSys = "SaleOnline"
                            };

                            isSaleOnline = "Đã tạo";
                            listLog.Add(syncLogSale);

                            saleUserBodyMail += j + ". " + model.Ho + " " + model.Ten + " - ";
                            saleUserBodyMail += model.MaNhanVien + " - " + userName + "<br />";

                            _logger.Information(userName + " CREATED on SaleOnline at " + DateTime.Now.ToString("G"));
                            Console.WriteLine(userName + " CREATED on SaleOnline at " + DateTime.Now.ToString("G"));
                        }
                    }

                    listUserBodyEmail += i + ". <a href=\"https://hrm.haiphatland.com.vn/HRIS/Profile/Index/" + model.NhanVienId + "\">";
                    listUserBodyEmail += model.Ho + " " + model.Ten + "</a>. Mã NV: ";
                    listUserBodyEmail += model.MaNhanVien + ". User: " + userInfo.sAMAccountName;
                    listUserBodyEmail += " (" + phongBanCap1.MaPhongBan + ")<br />";
                    #endregion

                    //ADD LIST GỬI THEO TỪNG BỘ PHẬN
                    var emailNoti = listNotifications.FirstOrDefault(x => x.MaPhongBanCap1 == phongBanCap1.MaPhongBan);

                    if (emailNoti == null)
                    {
                        emailNoti = new EmailNotifications();
                        emailNoti.MaPhongBanCap1 = phongBanCap1.MaPhongBan;
                        emailNoti.TenPhongBanCap1 = phongBanCap1.Ten;
                        if (!string.IsNullOrEmpty(abpPhong.EmailNotification))
                        {
                            emailNoti.EmailNotifyReceiver = abpPhong.EmailNotification;
                        }
                        else
                        {
                            listEmailLoi.Add(phongBanCap1.Ten + " chưa cập nhật email trợ lý vào ACM");
                        }

                        emailNoti.ListUsers = new List<string>();

                        listNotifications.Add(emailNoti);
                    }

                    //TODO for testing
                    //emailNoti.ListUsers.Add("<a href=\"https://hrm.haiphatland.com.vn/HRIS/Profile/Index/" +
                    //                        model.NhanVienId + "\">" + model.Ho + " " + model.Ten +
                    //                        "</a>. Mã NV: " +
                    //                        model.MaNhanVien + ". User: <br />");

                    emailNoti.ListUsers.Add("<a href=\"https://hrm.haiphatland.com.vn/HRIS/Profile/Index/" +
                                            model.NhanVienId + "\">" + model.Ho + " " + model.Ten +
                                            "</a>. Mã NV: " +
                                            model.MaNhanVien + ". User: " + userInfo.sAMAccountName +
                                            " (" + phongBanCap1.MaPhongBan + ")<br />");

                    //Add new LogNhanVien
                    var nvLog = new HplCreateUserLog
                    {
                        NhanVienId = model.NhanVienId,
                        FirstName = model.Ten,
                        LastName = model.Ho,
                        GioiTinh = model.GioiTinh,
                        MaNhanVien = model.MaNhanVien,
                        TenDangNhap = userName,
                        Email = nhanVien.Email,
                        EmailCaNhan = nhanVien.EmailCaNhan,
                        DienThoai = nhanVien.DienThoai,
                        Cmtnd = nhanVien.CMTND,
                        TenChucVu = nhanVien.TenChucVu,
                        TenChucDanh = nhanVien.TenChucDanh,
                        PhongBanId = nhanVien.PhongBanId,
                        TenPhongBan = nhanVien.TenPhongBan,
                        MaPhongBan = nhanVien.MaPhongBan,
                        PhongBanCap1Id = phongBanCap1.PhongBanId,
                        TenPhongBanCap1 = phongBanCap1.Ten,
                        MaPhongBanCap1 = phongBanCap1.MaPhongBan,
                        IsAd = "OK",
                        IsHrm = "OK",
                        IsSaleOnline = isSaleOnline,
                        IsEmail = "OK",
                        LinkHrm = "https://hrm.haiphatland.com.vn/HRIS/Profile/Index/" + nhanVien.NhanVienId + "/",
                        LinkEmail = "https://id.haiphatland.com.vn/api/mdaemon/GetUserInfo?username=" + userName,
                        DateCreated = DateTime.Now
                    };

                    listNvLogs.Add(nvLog);
                }
                catch (Exception e)
                {
                    _logger.Error("Error create user for MaNhanVien: " + model.MaNhanVien +
                                  ". Errors: " + e);
                    Console.WriteLine("Error create user for MaNhanVien: " + model.MaNhanVien +
                                      ". Errors: " + e);
                }
            }

            if (listLog.Any())
            {
                AbpServices.AddSyncLogAbp(listLog);
            }

            if (listNvLogs.Any())
            {
                AbpServices.AddLogNhanVien(listNvLogs);
            }

            if (listNvs.Any())
            {
                //GỬI MAIL CHO ADMIN
                if (j > 0) listUserBodyEmail += saleUserBodyMail;

                listUserBodyEmail += "Lưu ý:<br />";
                listUserBodyEmail += "1. Pass mặc định của mail và user là Hpl@xxx. Với xxx là 3 số cuối của điện thoại<br />";
                listUserBodyEmail += "2. Nếu không có số điện thoại, pass là Hpl@123<br />";
                MailHelper.EmailSender(listUserBodyEmail);

                if (listNotifications.Any())
                {
                    //GỬI MAIL CHO CÁC TRỢ LÝ
                    foreach (var notification in listNotifications)
                    {
                        List<string> listReceivers = new List<string>();

                        int l = 0;
                        if (!string.IsNullOrEmpty(notification.EmailNotifyReceiver))
                        {
                            var listEmails = notification.EmailNotifyReceiver.Split(",");
                            foreach (var email in listEmails)
                            {
                                if (MailHelper.IsValidEmail(email))
                                {
                                    listReceivers.Add(email);
                                }
                            }
                            //Danh sách thông tin tài khoản đã tạo
                            string bodyEmail = "<p style=\"font-weight: bold\">DANH SÁCH TÀI KHOẢN ĐÃ TẠO</p>";
                            bodyEmail += "<p style=\"font-weight: bold\">" + notification.TenPhongBanCap1 + "</p>";
                            foreach (var user in notification.ListUsers)
                            {
                                l++;
                                bodyEmail += l + ". " + user;
                            }

                            bodyEmail += "Lưu ý:<br />";
                            bodyEmail += "1. Pass mặc định của mail và user là Hpl@xxx. Với xxx là 3 số cuối của điện thoại<br />";
                            bodyEmail += "2. Nếu không có số điện thoại, pass là Hpl@123<br />";

                            string subject = "[HPL] HCNS THÔNG BÁO TÀI KHOẢN ĐÃ TẠO NGÀY " +
                                             DateTime.Now.ToString("dd/MM/yyyy");

                            MailHelper.EmailSender(bodyEmail, subject, listReceivers);
                        }
                    }

                    //GỬI MAIL THÔNG BÁO ADMIN CÁC EMAIL TRỢ LÝ KHÔNG ĐÚNG.
                    if (listEmailLoi.Any())
                    {
                        string bodyEmail = "<p style=\"font-weight: bold\">DANH SÁCH EMAIL TRỢ LÝ KHÔNG ĐÚNG</p>";
                        foreach (var str in listEmailLoi)
                        {
                            bodyEmail += str + "<br />";
                        }

                        string subject = "[HPL] LỖI KHÔNG GỬI ĐƯỢC EMAIL";
                        MailHelper.EmailSender(bodyEmail, subject);
                    }
                }
            }

            //GỬI MAIL THÔNG BÁO LỖI KHÔNG CÓ PHÒNG BAN CẤP 1 TRONG ACM
            if (!string.IsNullOrEmpty(loiKhongCoPhongBanAcm))
            {
                loiKhongCoPhongBanAcm += "Lưu ý: Có thể TRÙNG MÃ HỒ SƠ<br />";
                string subject = "[ACM] LỖI THIẾU CẤU HÌNH PHÒNG BAN " + DateTime.Now.ToString("dd/MM/yy");
                MailHelper.EmailSender(loiKhongCoPhongBanAcm, subject);
            }
        }

        /// <summary>
        /// Cập nhật Phòng/Ban và các thông tin khác của User
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task UpdateUserAllSys(NsQtChuyenCanBo model)
        {
            if (model == null) return;
            string baoEmail = "";
            string reciever = "baonxvn@gmail.com";
            string subject = "[ACM] Lỗi nhân sự tracking";

            if (!model.PhongBanCuId.Equals(model.PhongBanMoiId))
            {
                if (model.NhanVienId == null) return;

                //var nhanVien = UserService.GetNhanVienByNhanVienId(model.NhanVienId.Value);
                var nhanVien = UserService.GetNhanVienByNhanVienId2(model.NhanVienId.Value);
                if (nhanVien == null)
                {
                    _logger.Error("Không tìm thấy nhân sự trên HRM theo NhanVienId=" + model.NhanVienId);
                    string bodyMail1 = "Không tìm thấy nhân sự trên HRM theo NhanVienId=" + model.NhanVienId;
                    MailHelper.EmailSender(bodyMail1, subject, reciever);
                    return;
                }

                //PhongBan pbCap1Current = UserService.GetPhongBanCap1CuaNhanVien(nhanVien.MaNhanVien);
                if (string.IsNullOrEmpty(nhanVien.TenPhongBanCap1))
                {
                    _logger.Error("Không xác định được phòng ban cấp 1 của nhân sự trên ACM theo NhanVienId=" + model.NhanVienId);
                    string bodyMail2 = "Không xác định được phòng ban cấp 1 của nhân sự trên ACM theo NhanVienId=" +
                                       model.NhanVienId;
                    string reciever2 = "baonxvn@gmail.com";
                    MailHelper.EmailSender(bodyMail2, subject, reciever);
                    return;
                }

                //KIỂM TRA PHÒNG BAN CẤP 1 CỦA NHÂN VIÊN NÀY
                PhongBan pbCap1Moi = UserService.GetPhongBanCap1CuaNhanVienTheoPbId(model.PhongBanMoiId.Value);
                //Trên thực tế pbCap1Current và pbCap1Moi là 1
                if (!pbCap1Moi.PhongBanId.Equals(nhanVien.PhongBanCap1Id))
                {
                    _logger.Error("Phòng ban Mới và Phòng Ban hiện tại không giống nhau NhanVienId=" + model.NhanVienId);
                    string bodyMail3 = "Phòng ban Mới và Phòng Ban hiện tại không giống nhau NhanVienId=" +
                                       model.NhanVienId;
                    MailHelper.EmailSender(bodyMail3,subject,reciever);
                    return;
                }

                PhongBan pbCap1Cu = UserService.GetPhongBanCap1CuaNhanVienTheoPbId(model.PhongBanCuId.Value);
                if (pbCap1Cu.PhongBanId.Equals(pbCap1Moi.PhongBanId))
                {
                    _logger.Error("Phòng ban MỚI và Phòng Ban CŨ giống nhau, không cần update SaleOnline NhanVienId=" + model.NhanVienId);
                    return;
                }

                string userName = nhanVien.TenDangNhap;
                if (!pbCap1Moi.MaPhongBan.Equals(pbCap1Cu.MaPhongBan))
                {
                    //UPDATE LẠI DEPARTMENT CỦA AD
                    //var abc = _passwordChangeProvider.UpdateDepartment(userName, pbCap1Moi.Ten);
                    _passwordChangeProvider.UpdateAdUser(nhanVien, userName);

                    //UPDATE LẠI EMAIL GROUP
                    //Xóa khỏi mail list cũ
                    var abpPbCu = AbpServices.GetAbpPhongBanByMaPhongBan(pbCap1Cu.MaPhongBan);
                    var a = await MdaemonXmlApi.RemoveFromMailList(abpPbCu.MailingList, userName);
                    //add vào mail list mới
                    var abpPbMoi = AbpServices.GetAbpPhongBanByMaPhongBan(pbCap1Moi.MaPhongBan);
                    string hoVaTen = UsernameGenerator.ConvertToUnSign(nhanVien.Ho + " " + nhanVien.Ten);
                    CreateUserInput input = new CreateUserInput
                    {
                        Domain = "haiphatland.com.vn",
                        Username = userName,
                        FullName = hoVaTen,
                        AdminNotes = "Tạo từ tool, time: " + DateTime.Now.ToString("G"),
                    };
                    var b = await MdaemonXmlApi.AddToMailList(abpPbMoi.MailingList, input);
                }

                string saleBodyMail = "<p style=\"font-weight: bold\">HỒ SƠ NHÂN SỰ ĐÃ THAY ĐỔI PHÒNG BAN</p>";

                //TẠO USER TRÊN SALE ONLINE
                string isSaleOnline = "Đã tồn tại_tracking";
                var nvSale = SaleOnlineServices.GetNhanVienByUserName(nhanVien.TenDangNhap);
                if (nvSale != null)
                {
                    //CẬP NHẬT LẠI TRẠNG THÁI USER
                    //Check theo mã Nhân Viên
                    if (nvSale.KeyCode == nhanVien.MaNhanVien)
                    {
                        if (nvSale.Lock != null && nvSale.Lock.Value)
                        {
                            nvSale.Lock = false;
                            isSaleOnline = "Đã unlock user";
                        }

                        //Xác định BranchId trên SaleOnline
                        var salePb = SaleOnlineServices.GetPhongBan(nhanVien.MaPhongBan);
                        if (salePb != null)
                        {
                            if (nvSale.BranchId != salePb.BranchId)
                            {
                                nvSale.BranchId = salePb.BranchId;
                                isSaleOnline = "Đã cập nhật BranchID";
                            }

                            if (nvSale.MaPb != salePb.MaPb)
                            {
                                nvSale.MaPb = salePb.MaPb;
                            }
                        }

                        var syncLogSale = new HplSyncLog
                        {
                            UserName = userName,
                            MaNhanVien = nhanVien.MaNhanVien,
                            Payload = JsonConvert.SerializeObject(nvSale),
                            LogForSys = "SaleOnline_tracking"
                        };
                        //Cập nhật user
                        //TODO
                        SaleOnlineServices.UpdateUserSale(nvSale);
                        //TODO
                        AbpServices.AddSyncLogAbp(syncLogSale);

                        _logger.Information(userName + " UPDATED on SaleOnline_tracking at " + DateTime.Now.ToString("G"));
                        _logger.Information("CẬP NHẬT LẠI TRẠNG THÁI USER: " + userName +
                                             ". Mã NV: " + nhanVien.MaNhanVien);
                    }
                    else
                    {
                        //KHÔNG TRÙNG MÃ NHÂN VIÊN THÌ TẠO MỚI USER SALE ONLINE
                        //Add số 1 vào user này và cập nhật
                        nvSale.MaSo = "1" + nvSale.MaSo;
                        SaleOnlineServices.UpdateUserSale(nvSale);
                        //Và tạo mới user khác.
                        //TẠO MỚI
                        nvSale = new NhanVienSale
                        {
                            MaSo = userName,
                            //nvSale.MatKhau = nhanVien.
                            HoTen = nhanVien.Ho + " " + nhanVien.Ten,
                            Ho = nhanVien.Ho,
                            Ten = nhanVien.Ten,
                            DienThoai = nhanVien.DienThoai,
                            Email = nhanVien.Email,
                            //nvSale.NgaySinh = nhanVien.
                            UserType = 1,
                            Lock = false,
                            SoCmnd = nhanVien.Cmnd,
                            KeyCode = nhanVien.MaNhanVien,
                            MaNvcn = 8,
                            NgayCn = DateTime.Now,
                            IsDeleted = false
                        };

                        //Xác định BranchId trên SaleOnline
                        var salePb = SaleOnlineServices.GetPhongBan(nhanVien.MaPhongBan);
                        if (salePb != null)
                        {
                            nvSale.BranchId = salePb.BranchId;
                            nvSale.MaPb = salePb.MaPb;
                            //TODO
                            SaleOnlineServices.CreateUserSale(nvSale);

                            var syncLogSale = new HplSyncLog
                            {
                                UserName = userName,
                                MaNhanVien = nhanVien.MaNhanVien,
                                Payload = JsonConvert.SerializeObject(nvSale),
                                LogForSys = "SaleOnline_tracking"
                            };

                            isSaleOnline = "Đã tạo lại_tracking";
                            //TODO
                            AbpServices.AddSyncLogAbp(syncLogSale);

                            _logger.Information(userName + " UPDATED & CREATED on SaleOnline_tracking at " + DateTime.Now.ToString("G"));
                            _logger.Information("KHÔNG TRÙNG MÃ NHÂN VIÊN THÌ TẠO MỚI USER SALE ONLINE: " + userName +
                                                ". Mã NV: " + nhanVien.MaNhanVien);
                        }
                    }
                }
                else
                {
                    //TẠO MỚI USER SALE ONLINE
                    nvSale = new NhanVienSale();
                    nvSale.MaSo = userName;
                    //nvSale.MatKhau = nhanVien.
                    nvSale.HoTen = nhanVien.Ho + " " + nhanVien.Ten;
                    nvSale.Ho = nhanVien.Ho;
                    nvSale.Ten = nhanVien.Ten;
                    nvSale.DienThoai = nhanVien.DienThoai;
                    nvSale.Email = nhanVien.Email;
                    //nvSale.NgaySinh = nhanVien.
                    nvSale.UserType = 1;
                    nvSale.Lock = false;
                    nvSale.SoCmnd = nhanVien.Cmnd;
                    nvSale.KeyCode = nhanVien.MaNhanVien;
                    nvSale.MaNvcn = 8;
                    nvSale.NgayCn = DateTime.Now;
                    nvSale.IsDeleted = false;

                    //Xác định BranchId trên SaleOnline
                    var salePb = SaleOnlineServices.GetPhongBan(nhanVien.MaPhongBan);
                    if (salePb != null)
                    {
                        nvSale.BranchId = salePb.BranchId;
                        nvSale.MaPb = salePb.MaPb;
                        //TODO
                        SaleOnlineServices.CreateUserSale(nvSale);

                        var syncLogSale = new HplSyncLog
                        {
                            UserName = userName,
                            MaNhanVien = nhanVien.MaNhanVien,
                            Payload = JsonConvert.SerializeObject(nvSale),
                            LogForSys = "SaleOnline_tracking"
                        };

                        isSaleOnline = "Đã tạo_tracking";
                        //TODO
                        AbpServices.AddSyncLogAbp(syncLogSale);

                        _logger.Information(userName + " CREATED on SaleOnline_tracking at " + DateTime.Now.ToString("G"));
                        _logger.Information("TẠO MỚI USER SALE ONLINE: " + userName +
                                            ". Mã NV: " + nhanVien.MaNhanVien);
                    }
                }

                saleBodyMail += nhanVien.Ho + " ";
                saleBodyMail += nhanVien.Ten + " - ";
                saleBodyMail += nhanVien.MaNhanVien + " - " + userName;
                saleBodyMail += " (" + nhanVien.MaPhongBanCap1 + ")<br />";
                saleBodyMail += "Đơn vị cũ: " + pbCap1Cu.Ten + " (" + pbCap1Cu.MaPhongBan + ")<br />";
                saleBodyMail += "Đơn vị mới: " + pbCap1Moi.Ten + " (" + pbCap1Moi.MaPhongBan + ")";

                //Add new LogNhanVien
                var nvLog = new HplCreateUserLog
                {
                    NhanVienId = model.NhanVienId.Value,
                    FirstName = nhanVien.Ten,
                    LastName = nhanVien.Ho,
                    GioiTinh = nhanVien.GioiTinh,
                    MaNhanVien = nhanVien.MaNhanVien,
                    TenDangNhap = userName,
                    Email = nhanVien.Email,
                    EmailCaNhan = nhanVien.EmailCaNhan,
                    DienThoai = nhanVien.DienThoai,
                    Cmtnd = nhanVien.Cmnd,
                    TenChucVu = nhanVien.TenChucVu,
                    TenChucDanh = nhanVien.TenChucDanh,
                    MaPhongBan = nhanVien.MaPhongBan,
                    PhongBanId = nhanVien.PhongBanId,
                    TenPhongBan = nhanVien.TenPhongBan,
                    PhongBanCap1Id = nhanVien.PhongBanCap1Id,
                    TenPhongBanCap1 = nhanVien.TenPhongBanCap1,
                    MaPhongBanCap1 = nhanVien.MaPhongBanCap1,
                    IsAd = "OK",
                    IsHrm = "OK",
                    IsSaleOnline = isSaleOnline,
                    IsEmail = "OK",
                    LinkHrm = "https://hrm.haiphatland.com.vn/HRIS/Profile/Index/" + nhanVien.NhanVienId + "/",
                    LinkEmail = "https://id.haiphatland.com.vn/api/mdaemon/GetUserInfo?username=" + userName,
                    DateCreated = DateTime.Now
                };

                AbpServices.AddLogNhanVien(nvLog);

                //GỬI MAIL THÔNG BÁO CHO ADMIN
                string subject2 = "[ACM] CẬP NHẬT THÔNG TIN ĐIỀU CHUYỂN TRÊN SALE ONLINE";
                MailHelper.EmailSender(saleBodyMail, subject2);
            }
        }

        public void UpdateUserInfo()
        {
            List<NhanVienViewModel> listNvs = UserService.GetAllUserNameLamViec();

            _passwordChangeProvider.UpdateUserInfoHrm(listNvs);
        }

        /// <summary>
        /// Trả về ID của PhongBanId (nếu có). Ngược lại trả về BranchId
        /// </summary>
        /// <param name="maPhongBan">Mã phòng/ban con của Nhân Sự</param>
        /// <param name="maPbCap1">Mã Phong/Ban cấp 1 của Nhân sự</param>
        /// <returns></returns>
        private int GetBranchIdOnSaleOnline1(string maPhongBan, string maPbCap1)
        {
            //Lấy mã phòng ban trên SaleOnline
            var phongBan = SaleOnlineServices.GetPhongBan(maPhongBan);

            if (phongBan != null)
            {
                return phongBan.MaPb;
            }

            //Nếu không xác định được thì lấy trên Branch treenn SaleOnline
            var branch = SaleOnlineServices.GetBranch(maPbCap1);
            if (branch != null)
            {
                return branch.BranchId;
            }

            return 0;
        }
    }
}