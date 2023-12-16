using Hpl.SaleOnlineDatabase;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;

namespace Hpl.HrmDatabase.Services
{
    public class AbpServices
    {
        public static void UpdateAllAdUser(List<UserAdInfo> listNhanVien)
        {
            var db = new AbpHplDbContext();

            db.UserAdInfoes.AddRange(listNhanVien);
            db.SaveChanges();
            db.Dispose();
        }

        public static void AddLogNhanVien(List<HplCreateUserLog> listNhanVien)
        {
            var db = new AbpHplDbContext();

            db.HplCreateUserLogs.AddRange(listNhanVien);
            db.SaveChanges();
            db.Dispose();
        }

        public static void AddDeleteEmailDoTaoLoi(string email)
        {
            var db = new AbpHplDbContext();
            var item = new HplDeleteEmailDoTaoLoi()
            {
                Email = email
            };

            db.HplDeleteEmailDoTaoLois.Add(item);
            db.SaveChanges();
            db.Dispose();
        }

        public static void AddLogNhanVien(HplCreateUserLog nhanVienLog)
        {
            var db = new AbpHplDbContext();

            db.HplCreateUserLogs.Add(nhanVienLog);
            db.SaveChanges();
            db.Dispose();
        }

        public static void AddCreateUserManual(string listNvs)
        {
            var db = new AbpHplDbContext();
            var item = new CreateDisableUserManual();
            item.ListMaNhanVien = listNvs;
            item.ActionType = (int)ActionTypeUsers.CreateUser;
            item.DateCreated = DateTime.Now;

            db.CreateDisableUserManuals.Add(item);
            db.SaveChanges();
            db.Dispose();
        }

        public static void DisableUserManual(string listNvs)
        {
            var db = new AbpHplDbContext();
            var item = new CreateDisableUserManual();
            item.ListMaNhanVien = listNvs;
            item.ActionType = (int)ActionTypeUsers.DisableUser;
            item.DateCreated = DateTime.Now;

            db.CreateDisableUserManuals.Add(item);
            db.SaveChanges();
            db.Dispose();
        }

        public static void ReactiveUserManual(string listNvs)
        {
            var db = new AbpHplDbContext();
            var item = new CreateDisableUserManual();
            item.ListMaNhanVien = listNvs;
            item.ActionType = (int)ActionTypeUsers.ReActiveUser;
            item.DateCreated = DateTime.Now;

            db.CreateDisableUserManuals.Add(item);
            db.SaveChanges();
            db.Dispose();
        }

        public static List<string> GetAllEmailBlackList()
        {
            var db = new AbpHplDbContext();
            return db.BlackListUsers.Select(x => x.Email).ToList();
        }

        public static List<HplEmailCanXoa31082021> GetAllEmailCanXoa31082021()
        {
            var db = new AbpHplDbContext();
            return db.HplEmailCanXoa31082021.ToList();
        }

        public static List<HplCreateUserLog> GetHplCreateUserLogByDateCreate(DateTime fromDate, DateTime toDate)
        {
            var db = new AbpHplDbContext();
            return db.HplCreateUserLogs.Where(x => x.DateCreated >= fromDate & x.DateCreated <= toDate).ToList();
        }

        public static string GetMailListByMaPhongBan(string maPhongBan)
        {
            var db = new AbpHplDbContext();
            var query = db.HplPhongBans.FirstOrDefault(x => x.MaPhongBan == maPhongBan);
            if (query == null) return "";
            if (string.IsNullOrEmpty(query.MailingList)) return query.MailingList;

            db.Dispose();

            return "";
        }

        public static HplPhongBan GetAbpPhongBanByMaPhongBan(string maPhongBan)
        {
            var db = new AbpHplDbContext();
            var query = db.HplPhongBans.FirstOrDefault(x => x.MaPhongBan == maPhongBan);
            if (query == null) return null;

            return query;
        }

        public static void UpdateBranch()
        {
            var db = new SaleOnlineDbContext();
            var dbHrm = new HrmDbContext();
            var dbApb = new AbpHplDbContext();
            var listPbs = dbApb.HplPhongBans;
            foreach (var pb in listPbs)
            {
                var branch = db.Branches.FirstOrDefault(x => x.BranchCode == pb.MaPhongBan);
                if (branch != null)
                {
                    pb.BranchId = branch.BranchId;
                    pb.BranchName = branch.BranchName;
                    pb.BranchCode = branch.BranchCode;
                }

                var pbHrm = dbHrm.PhongBans.FirstOrDefault(x => x.MaPhongBan == pb.MaPhongBan);
                if (pbHrm != null)
                {
                    pb.PhongBanId = pbHrm.PhongBanId;
                    pb.PhongBanParentId = pbHrm.PhongBanChaId;
                    pb.TenPhongBan = pbHrm.Ten;
                }

                pb.LastSyncToAd = DateTime.Now;
            }
            //SaleOnlineServices
            dbApb.SaveChanges();
            dbApb.Dispose();
            db.Dispose();
        }

        public static int FlattenAllHplPhongBan()
        {
            var db = new SaleOnlineDbContext();
            var dbHrm = new HrmDbContext();
            var dbApb = new AbpHplDbContext();

            return dbApb.FlattenAllHplPhongBan();
        }

        public static void AddDisableLogAbp(HplDisableUserLog model)
        {
            var db = new AbpHplDbContext();

            db.HplDisableUserLogs.Add(model);
            db.SaveChanges();
        }

        public static void AddDisableLogAbp(List<HplDisableUserLog> listLog)
        {
            var db = new AbpHplDbContext();

            db.HplDisableUserLogs.AddRange(listLog);
            db.SaveChanges();
            db.Dispose();
        }

        public static void AddSyncLogAbp(HplSyncLog model)
        {
            AbpHplDbContext db = new AbpHplDbContext();

            db.HplSyncLogs.Add(model);
            db.SaveChanges();
            db.Dispose();
        }

        public static void AddSyncLogAbp(List<HplSyncLog> listLogs)
        {
            AbpHplDbContext db = new AbpHplDbContext();

            db.HplSyncLogs.AddRange(listLogs);
            db.SaveChanges();
            db.Dispose();
        }

        public static List<HplCreateUserLog> GetAllLogNhanVien()
        {
            var db = new AbpHplDbContext();
            var dt = DateTime.Now.AddDays(-90);

            return db.HplCreateUserLogs.Where(x => x.DateCreated.Value >= dt)
                .OrderByDescending(x => x.DateCreated).ToList();
        }

        //public static List<HplDisableUserLog> GetAllNhanVienDisable()
        //{
        //    var db = new AbpHplDbContext();
        //    var dt = DateTime.Now.AddDays(-90);

        //    return db.HplDisableUserLogs.Where(x => x.DateCreated >= dt)
        //        .OrderByDescending(x => x.DateCreated).ToList();
        //}

        public static List<HplDisableUserLog> GetAllLogNhanVienDisable()
        {
            var db = new AbpHplDbContext();
            try
            {
                var listNvs = db.HplDisableUserLogs.Take(50);

                return listNvs.OrderByDescending(x => x.DateCreated.Value).ToList();
            }
            catch (Exception e)
            {
                db.Dispose();

                return new List<HplDisableUserLog>();
            }
        }

        /// <summary>
        /// CHẠY MỘT LẦN DO LỖI
        /// </summary>
        /// <returns></returns>
        public static List<HplDisableUserLog> UpdateLogDis()
        {
            var db = new AbpHplDbContext();
            var dbHrm = new HrmDbContext();

            var listNvs = db.HplDisableUserLogs;

            //foreach (var log in listNvs)
            //{
            //    var pb = dbHrm.PhongBans.FirstOrDefault(x => x.PhongBanId == log.PhongBanId);
            //    if (pb != null) log.MaPhongBan = pb.MaPhongBan;

            //    var pbCap1 = dbHrm.PhongBans.FirstOrDefault(x => x.PhongBanId == log.PhongBanCap1Id);
            //    if (pbCap1 != null) log.MaPhongBanCap1 = pbCap1.MaPhongBan;
            //}

            //db.SaveChanges();

            return listNvs.OrderByDescending(x => x.DateCreated.Value).ToList();
        }

        public static List<HplPhongBan> GetAllDepartment()
        {
            var db = new AbpHplDbContext();

            return db.HplPhongBans.OrderBy(x => x.TenPhongBan).ToList();
        }
    }
}