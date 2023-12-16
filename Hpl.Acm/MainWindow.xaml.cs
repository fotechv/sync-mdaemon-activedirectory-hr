using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Hpl.Common;
using Hpl.HrmDatabase;
using Hpl.HrmDatabase.Services;
using Hpl.HrmDatabase.ViewModels;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Serilog;
using TableDependency.SqlClient;
using TableDependency.SqlClient.Base;
using TableDependency.SqlClient.Base.Enums;
using TableDependency.SqlClient.Base.EventArgs;
using Unosquare.PassCore.Common;
using Unosquare.PassCore.PasswordProvider;

namespace Hpl.Acm
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static PasswordChangeOptions _options;
        private static ILogger _logger;
        private static IPasswordChangeProvider _passwordChangeProvider;
        private static IAbpHplDbContext _abpHplDb;

        //Declare a _timer of type System.Threading
        private static Timer _timer;
        private static Timer _timerDis;
        private static Timer _timerNotify;

        //Local Variable of type int
        //private static int _count = 1;
        private readonly SqlTableDependency<NsQtChuyenCanBo> _depChuyenPb;
        private readonly SqlTableDependency<CreateDisableUserManual> _depUpdateUser;
        private static readonly string ConAcm = "Server=54.251.3.45; Database=HPL_ACM; User ID=sa; Password=Zm*3_E}7gaR83+_G";
        private static readonly string ConHrm = "Server=54.251.3.45; Database=HRM_db; User ID=hrm; Password=H@iphat2021";

        public MainWindow(ILogger logger, IOptions<PasswordChangeOptions> options, IPasswordChangeProvider passwordChangeProvider, IAbpHplDbContext abpHplDb)
        {
            InitializeComponent();

            _logger = logger;
            _options = options.Value;
            _passwordChangeProvider = passwordChangeProvider;
            _abpHplDb = abpHplDb;

            _logger.Information("MainWindow");

            #region TOOL CHẠY UPDATE LẠI PHÒNG BAN CHO USER TRÊN AD
            //Test 2021-09-15 Test gửi thông báo
            //_ = CallTimerEmailThongBaoUserLoi();

            //CẬP NHẬT LẠI THÔNG TIN USER TRÊN AD(Tool sử dụng một lần)
            //try
            //{
            //    _passwordChangeProvider.CreateAllOuHpl();
            //    //Fullname & Department
            //    var listUser = _passwordChangeProvider.GetAllUserHpl();

            //    _logger.Information("TỔNG SỐ USER AD UPDATE THÔNG TIN: " + listUser.Count);
            //    var cb = _passwordChangeProvider.UpdateUserInfoAd(listUser);
            //}
            //catch (Exception e)
            //{
            //    _logger.Error("Lỗi: " + e);
            //}


            //var listNvs = GetAllNhanVienErrorUser();

            //_logger.Information("----TỔNG SỐ HỒ SƠ TẠO MỚI ĐÃ XỬ LÝ: " + listNvs.Count);
            //WriteToConsole("----TỔNG SỐ HỒ SƠ TẠO MỚI ĐÃ XỬ LÝ: " + listNvs.Count);
            //if (listNvs.Any())
            //{
            //    HplServices hplServices = new HplServices(_passwordChangeProvider, _options, _logger);
            //    hplServices.CreateUserAllSys2(listNvs);
            //}

            //Application.Current.Shutdown();
            #endregion

            //TẠO USER AD, MAIL, UPDATE SALE ONLINE, UPDATE HRM,
            //PHẦN NÀY KHÔNG CHẠY TỰ ĐỘNG NỮA, MÀ SẼ SỬ DỤNG MANUAL TẠI EVENT DepUpdateUserChanged
            #region TẠO USER AD, MAIL, UPDATE SALE ONLINE, UPDATE HRM,
            //_timer = new Timer(async x =>
            //{
            //    _logger.Information("START TASK CREATE USER " + DateTime.Now.ToString("G"));
            //    await CallTimerCreateUser();//TODO

            //    _logger.Information("END TASK CREATE USER " + DateTime.Now.ToString("G"));
            //}, null, Timeout.Infinite, Timeout.Infinite);

            ////DISABLE USER AD, XÓA EMAIL, LOCK SALE ONLINE
            //_timerDis = new Timer(async x =>
            //{
            //    _logger.Information("START TASK DISABLE USER " + DateTime.Now.ToString("G"));
            //    await CallTimerDisableUser();//TODO

            //    _logger.Information("END TASK DISABLE USER " + DateTime.Now.ToString("G"));
            //}, null, Timeout.Infinite, Timeout.Infinite);
            #endregion

            ////THÔNG BÁO NHÂN VIÊN LỖI
            //_timerNotify = new Timer(async x =>
            //{
            //    _logger.Information("START TASK DISABLE USER " + DateTime.Now.ToString("G"));
            //    await CallTimerEmailThongBaoUserLoi();

            //    _logger.Information("END TASK DISABLE USER " + DateTime.Now.ToString("G"));
            //}, null, Timeout.Infinite, Timeout.Infinite);

            //SetupTimer();

            //CẬP NHẬT THAY ĐỔI PHÒNG BAN CỦA NHÂN SỰ Table NsQtChuyenCanBo
            var mapper = new ModelToTableMapper<NsQtChuyenCanBo>();
            mapper.AddMapping(c => c.NhanVienId, "NhanVienId");
            mapper.AddMapping(c => c.PhongBanCuId, "PhongBanCuID");
            mapper.AddMapping(c => c.TenPhongBanCu, "TenPhongBanCu");
            mapper.AddMapping(c => c.PhongBanMoiId, "PhongBanMoiID");
            mapper.AddMapping(c => c.TenPhongBanMoi, "TenPhongBanMoi");
            mapper.AddMapping(c => c.IsDeleted, "IsDeleted");

            _depChuyenPb = new SqlTableDependency<NsQtChuyenCanBo>(ConHrm, "NS_QTChuyenCanBo", mapper: mapper);
            _depChuyenPb.OnChanged += DepChuyenPbChanged;
            _depChuyenPb.OnError += DepChuyenPbOnError;
            _depChuyenPb.Start();

            //TẠO/DISABLE USER TOÀN HỆ THỐNG CreateDisableUserManual
            var mapper2 = new ModelToTableMapper<CreateDisableUserManual>();
            mapper2.AddMapping(c => c.Id, "Id");
            mapper2.AddMapping(c => c.ListMaNhanVien, "ListMaNhanVien");
            mapper2.AddMapping(c => c.ActionType, "ActionType");
            mapper2.AddMapping(c => c.DateCreated, "DateCreated");

            _depUpdateUser = new SqlTableDependency<CreateDisableUserManual>(ConAcm, "CreateDisableUserManuals", mapper: mapper2);
            _depUpdateUser.OnChanged += DepUpdateUserChanged;
            _depUpdateUser.OnError += DepUpdateUserOnError;
            _depUpdateUser.Start();
        }

        private void DepChuyenPbOnError(object sender, ErrorEventArgs e)
        {
            _logger.Error("Lỗi tracking table NsQtChuyenCanBo: + " + e.Error);
        }

        private void DepUpdateUserOnError(object sender, ErrorEventArgs e)
        {
            _logger.Error("Lỗi tracking table CreateDisableUserManual: + " + e.Error);
        }

        public static void DepChuyenPbChanged(object sender, RecordChangedEventArgs<NsQtChuyenCanBo> e)
        {
            var canBo = e.Entity;

            switch (e.ChangeType)
            {
                case ChangeType.None:

                    _logger.Information("Tracking table: None" + ChangeType.None);
                    break;

                case ChangeType.Delete:

                    _logger.Information("Tracking table: Delete" + ChangeType.Delete);
                    break;

                case ChangeType.Insert:
                case ChangeType.Update:
                    try
                    {
                        HplServices hplServices = new HplServices(_passwordChangeProvider, _options, _logger, _abpHplDb);
                        _ = hplServices.UpdateUserAllSys(canBo);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("Lỗi call UpdateUserAllSys: " + ex);
                    }

                    break;
            }
        }

        public static void DepUpdateUserChanged(object sender, RecordChangedEventArgs<CreateDisableUserManual> e)
        {
            var nv = e.Entity;

            switch (e.ChangeType)
            {
                case ChangeType.None:
                    break;
                case ChangeType.Delete:
                    _logger.Information("Tracking CreateDisableUserManual: Delete" + ChangeType.Delete);
                    break;
                case ChangeType.Update:
                    break;
                case ChangeType.Insert:
                    var hplServices = new HplServices(_passwordChangeProvider, _options, _logger, _abpHplDb);
                    string fucError = "";
                    try
                    {
                        //LẤY TOÀN BỘ THÔNG TIN USER AD VÀ INSERT VÀO DB
                        if (nv.ActionType == (int)ActionTypeUsers.UpdateAllAdUser)
                        {
                            fucError = "ActionTypeUsers.UpdateAllAdUser";

                            _logger.Information("----START CẬP NHẬT ALL USERS----");
                            try
                            {
                                var listUser3 = _passwordChangeProvider.GetAllUsers2();
                                var listUser4 = hplServices.UpdateAllAdUserAddEmailInfo(listUser3).Result;
                                AbpServices.UpdateAllAdUser(listUser4);
                            }
                            catch (Exception ex)
                            {
                                _logger.Information("Lỗi: " + ex);
                            }

                            _logger.Information("----END CẬP NHẬT ALL USERS----");
                        }
                        else
                        {

                            var lst = nv.ListMaNhanVien.Split(",");
                            var listMaNvs = lst.ToList();
                            //var listNvs = UserService.GetAllNhanVienTheoMa2(listMaNvs);
                            var listNvs2 = UserService.GetAllNhanVienTheoMa3(listMaNvs).Result;

                            //TẠO USER TỪ DANH SÁCH MÃ NHÂN VIÊN
                            if (nv.ActionType == (int)ActionTypeUsers.CreateUser)
                            {
                                fucError = "ActionTypeUsers.CreateUser";

                                _logger.Information("----START CREATE USER----");
                                _logger.Information("----TỔNG SỐ HỒ SƠ TẠO MỚI ĐÃ XỬ LÝ: " + listNvs2.Count);

                                if (listNvs2.Any())
                                {
                                    hplServices.CreateUserTheoMaNhanVien2(listNvs2);
                                }
                                _logger.Information("----END CREATE USER----");
                            }

                            //DISABLE USER TỪ DANH SÁCH MÃ NHÂN VIÊN
                            if (nv.ActionType == (int)ActionTypeUsers.DisableUser)
                            {
                                fucError = "ActionTypeUsers.DisableUser";

                                _logger.Information("----START DISABLE USER----");
                                _logger.Information("----TỔNG SỐ HỒ SƠ NGỈ VIỆC ĐÃ XỬ LÝ: " + listNvs2.Count);

                                if (listNvs2.Any())
                                {
                                    _ = hplServices.DisableUser3(listNvs2);
                                }
                                _logger.Information("----END DISABLE USER----");
                            }

                            //KÍCH HOẠT LẠI USER
                            if (nv.ActionType == (int)ActionTypeUsers.ReActiveUser)
                            {
                                fucError = "ActionTypeUsers.ReActiveUser";
                                _logger.Information("----START ReActiveUser----");

                                if (listNvs2.Any())
                                {
                                    _ = hplServices.ReactiveUserTask2(listNvs2);
                                }
                                _logger.Information("----END ReActiveUser----");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("Lỗi call CreateDisableUserManual: " + fucError + "==>" + ex);
                    }

                    break;
            }
        }

        private static void SetupTimer()
        {
            DateTime nowTime = DateTime.Now;
            DateTime currentTime = DateTime.Now.AddDays(1);

            //THÔNG BÁO USER LỖI
            DateTime timerRunNotify = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 2, 0, 0);

            if (nowTime > timerRunNotify)
            {
                timerRunNotify = timerRunNotify.AddDays(1);
            }

            double tickTimeNoti = (timerRunNotify - nowTime).TotalSeconds;
            _timerNotify.Change(TimeSpan.FromSeconds(tickTimeNoti), TimeSpan.FromSeconds(tickTimeNoti));

            #region KHÔNG CHẠY TỰ ĐỘNG
            ////LỊCH CHẠY TẠO USER
            //DateTime timerRunCreate = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 0, 0, 0);
            ////DateTime timerRunCreate = DateTime.Now.AddMinutes(15);

            //double tickTime = (timerRunCreate - nowTime).TotalSeconds;
            //_timer.Change(TimeSpan.FromSeconds(tickTime), TimeSpan.FromSeconds(tickTime));

            ////LỊCH CHẠY DISABLE USER
            //DateTime timerRunDisable = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 1, 0, 0);
            ////DateTime timerRunDisable = DateTime.Now.AddMinutes(6);

            //double tickTimeDis = (timerRunDisable - nowTime).TotalSeconds;
            //_timerDis.Change(TimeSpan.FromSeconds(tickTimeDis), TimeSpan.FromSeconds(tickTimeDis));
            #endregion
        }

        private async Task CallTimerCreateUser()
        {
            _logger.Information("----START CREATE USER----");
            WriteToConsole("----START CREATE USER----");

            var listNvs = GetAllNhanVienErrorUser();

            _logger.Information("----TỔNG SỐ HỒ SƠ TẠO MỚI ĐÃ XỬ LÝ: " + listNvs.Count);
            WriteToConsole("----TỔNG SỐ HỒ SƠ TẠO MỚI ĐÃ XỬ LÝ: " + listNvs.Count);
            if (listNvs.Any())
            {
                HplServices hplServices = new HplServices(_passwordChangeProvider, _options, _logger, _abpHplDb);
                await hplServices.CreateUserAllSys2(listNvs);
            }
        }

        private async Task CallTimerDisableUser()
        {
            _logger.Information("----START DISABLE USER----");
            WriteToConsole("----START DISABLE USER----");

            var listNvs = UserService.GetAllNhanVienNghiViecCanDisable();

            _logger.Information("----TỔNG SỐ HỒ SƠ NGỈ VIỆC ĐÃ XỬ LÝ: " + listNvs.Count);
            WriteToConsole("----TỔNG SỐ HỒ SƠ NGỈ VIỆC ĐÃ XỬ LÝ: " + listNvs.Count);
            if (listNvs.Any())
            {
                HplServices hplServices = new HplServices(_passwordChangeProvider, _options, _logger, _abpHplDb);
                await hplServices.DisableUser(listNvs);
            }
            _logger.Information("END TASK DISABLE USER " + DateTime.Now.ToString("G"));
        }

        private async Task CallTimerEmailThongBaoUserLoi()
        {
            _logger.Information("----START THÔNG BÁO LỖI----");
            WriteToConsole("----START THÔNG BÁO LỖI----");

            HplServices hplServices = new HplServices(_passwordChangeProvider, _options, _logger, _abpHplDb);
            await hplServices.EmailThongBaoUserLoi();

            _logger.Information("END TASK THÔNG BÁO LỖI " + DateTime.Now.ToString("G"));
            WriteToConsole("----END TASK THÔNG BÁO LỖI----");
        }

        private async Task CallTimerMethodeTest()
        {
            _logger.Information("----START HAI PHAT LAND ACM----");
            WriteToConsole("----START HAI PHAT LAND ACM----");

            var listNvs = GetAllNhanVienErrorUser();
            _logger.Information("TỔNG SỐ HỒ SƠ XỬ LÝ: " + listNvs.Count);
            WriteToConsole("----TỔNG SỐ HỒ SƠ XỬ LÝ: " + listNvs.Count);
        }

        public static List<NhanVienViewModel2> GetAllNhanVienErrorUser()
        {
            try
            {
                var listNvs = UserService.GetAllNhanVienChuaCoUsername();

                return listNvs;
            }
            catch (Exception e)
            {
                _logger.Error("Lỗi call GetAllNhanVienChuaCoUsername: " + e.Message);
                return new List<NhanVienViewModel2>();
            }
        }

        private void WriteToConsole(string message)
        {
            Dispatcher?.Invoke(() => { RtbConsole.AppendText(DateTime.Now.ToString("G") + ": " + message + "\r"); });
        }

        private void BtnStartSync_Click(object sender, RoutedEventArgs e)
        {
            _logger.Information("Test DI " + _options.BackDateSchedule + " " +
                                _passwordChangeProvider.MeasureNewPasswordDistance("abc", "cbs"));
        }

        private void MainWindows_Loaded(object sender, RoutedEventArgs e)
        {
            _logger.Information("MainWindows_Loaded");
        }

        private void MainWindows_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _logger.Information("MainWindows_Closing");
            _depChuyenPb.Stop();
            _depUpdateUser.Stop();
        }
    }
}
