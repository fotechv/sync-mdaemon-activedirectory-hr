using System;
using System.Collections.Generic;
using System.Linq;
using Hpl.SaleOnlineDatabase;
using Newtonsoft.Json;
using NhanVienSale = Hpl.SaleOnlineDatabase.NhanVien;
using PhongBan = Hpl.HrmDatabase.PhongBan;


namespace Hpl.HrmDatabase.Services
{
    public class DepartmentServices
    {

        public static List<NsQtChuyenCanBo> GetAllCanBo()
        {
            var db = new HrmDbContext();

            return db.NsQtChuyenCanBoes.ToList();
        }
    }
}