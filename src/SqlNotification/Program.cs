using System;
using System.Data;
using Hpl.HrmDatabase;
using Microsoft.Data.SqlClient;
using TableDependency.SqlClient;
using TableDependency.SqlClient.Base;
using TableDependency.SqlClient.Base.Enums;
using TableDependency.SqlClient.Base.EventArgs;


namespace SqlNotification
{
    class Program
    {
        private static readonly string Con1 = "Server=54.251.3.45; Database=HPL_ACM; User ID=sa; Password=Zm*3_E}7gaR83+_G";
        private static readonly string Con2 = "Server=54.251.3.45; Database=HRM_db; User ID=hrm; Password=H@iphat2021";
        private static readonly string Con3 = "Server=54.251.3.45; Database=HRM_db; User ID=sa; Password=Zm*3_E}7gaR83+_G";
        //<add key="ConnStr" value="server=54.251.3.45,1433;database=HRM_db;persist security info=True; uid=hrm; pwd=H@iphat2021" />

        static void Main(string[] args)
        {
            //TEST HRM
            var mapper = new ModelToTableMapper<NsQtChuyenCanBo>();
            mapper.AddMapping(c => c.NhanVienId, "NhanVienId");
            mapper.AddMapping(c => c.PhongBanCuId, "PhongBanCuID");
            mapper.AddMapping(c => c.PhongBanMoiId, "PhongBanMoiID");

            using (var dep = new SqlTableDependency<NsQtChuyenCanBo>(Con2, "NS_QTChuyenCanBo", mapper: mapper))
            {
                dep.OnChanged += Changed;
                dep.Start();

                Console.WriteLine("Press a key to exit");
                Console.ReadKey();

                dep.Stop();
            }

            ////TEST ACM
            //var mapper = new ModelToTableMapper<HplTestTable>();
            //mapper.AddMapping(c => c.Name, "Name");
            //mapper.AddMapping(c => c.Description, "Description");

            //using (var dep = new SqlTableDependency<HplTestTable>(Con1, "HplTestTable", mapper: mapper))
            //{
            //    dep.OnChanged += Changed;
            //    dep.Start();

            //    Console.WriteLine("Press a key to exit");
            //    Console.ReadKey();

            //    dep.Stop();
            //}
        }

        public static void Changed(object sender, RecordChangedEventArgs<NsQtChuyenCanBo> e)
        {
            var changedEntity = e.Entity;

            //Console.WriteLine("DML operation: " + e.ChangeType);
            //Console.WriteLine("ID: " + changedEntity.NhanVienId);
            //Console.WriteLine("Name: " + changedEntity.HoTen);
            //Console.WriteLine("Surname: " + changedEntity.Ho);

            //Test ACM
            Console.WriteLine("DML operation: " + e.ChangeType);
            Console.WriteLine("NhanVienId: " + changedEntity.NhanVienId);
            Console.WriteLine("PhongBanCuID: " + changedEntity.PhongBanCuId);
            Console.WriteLine("PhongBanMoiID: " + changedEntity.PhongBanMoiId);

            if (e.ChangeType == ChangeType.Update)
            {
                //TODO

            }
        }
    }
}
