using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hpl.Acm.Web.Helpers;
using Hpl.Acm.Web.Services;
using Hpl.Common;
using Hpl.Common.Models;
using Hpl.HrmDatabase;
using Hpl.HrmDatabase.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Serilog;
using Unosquare.PassCore.Common;
using JsonSerializer = System.Text.Json.JsonSerializer;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Hpl.Acm.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IAbpHplDbContext _abpHplDb;
        private readonly IUriService _uriService;

        public EmployeeController(ILogger logger, IAbpHplDbContext abpHplDb, IUriService uriService)
        {
            _logger = logger;
            _abpHplDb = abpHplDb;
            _uriService = uriService;
        }


        [HttpGet]
        [Route("ListLuanChuyenCanBo")]
        public async Task<IActionResult> ListLuanChuyenCanBo([FromQuery] DateTime? dtFrom, [FromQuery] DateTime? dtTo)
        {
            List<ListLuanChuyenCanBoReturnModel> listNvs;

            if (dtFrom.HasValue & dtTo.HasValue)
            {
                listNvs = await _abpHplDb.ListLuanChuyenCanBoAsync(dtFrom, dtTo);
            }
            else
            {
                listNvs = await _abpHplDb.ListLuanChuyenCanBoAsync(null, null);
            }

            if (listNvs.Any())
            {
                return Ok(listNvs);
            }

            return Ok(new ApiErrorItem(ApiErrorCode.Generic, "Không có dữ liệu"));
        }

        [HttpGet]
        [Route("CreateUserTheoMaNhanVien")]
        public async Task<string> CreateUserTheoMaNhanVien(string listMaNvs)
        {
            var result = new ApiResult();
            listMaNvs = listMaNvs.Trim().Replace(" ", "").Trim();

            List<string> listMaNhanVien = listMaNvs.Split(",").ToList();

            listMaNhanVien = listMaNhanVien.Distinct().ToList();

            if (listMaNvs.Any())
            {
                string maNvs = "";
                foreach (var maNv in listMaNhanVien)
                {
                    maNvs += maNv + ",";
                }

                if (maNvs.Length > 1)
                {
                    maNvs = maNvs.Remove(maNvs.Length - 1);
                    AbpServices.AddCreateUserManual(maNvs);
                }

                result.Errors.Add(new ApiErrorItem(ApiErrorCode.Generic, "Danh sách Mã NV đã đưa vào Queue thành công. Kết quả Thêm user sẽ gửi về email."));
                result.Payload = maNvs;
            }
            else
            {
                result.Payload = "";
                result.Errors.Add(new ApiErrorItem(ApiErrorCode.UserNotFound, "Lỗi: Chuỗi nhập vào không đúng"));
            }

            return JsonConvert.SerializeObject(result);
        }

        [HttpGet]
        [Route("DisableTheoMaNhanVien")]
        public async Task<string> DisableTheoMaNhanVien(string listMaNvs)
        {
            var result = new ApiResult();
            listMaNvs = listMaNvs.Trim().Replace(" ", "").Trim();

            List<string> listMaNhanVien = listMaNvs.Split(",").ToList();

            listMaNhanVien = listMaNhanVien.Distinct().ToList();

            if (listMaNvs.Any())
            {
                string maNvs = "";
                foreach (var maNv in listMaNhanVien)
                {
                    maNvs += maNv + ",";
                }

                if (maNvs.Length > 1)
                {
                    maNvs = maNvs.Remove(maNvs.Length - 1);
                    AbpServices.DisableUserManual(maNvs);
                }

                result.Errors.Add(new ApiErrorItem(ApiErrorCode.Generic, "Danh sách Mã NV đã đưa vào Queue thành công. Kết quả DISABLE user sẽ gửi về email."));

                result.Payload = maNvs;
            }
            else
            {
                result.Payload = "";
                result.Errors.Add(new ApiErrorItem(ApiErrorCode.UserNotFound, "Lỗi: Chuỗi nhập vào không đúng"));
            }

            return JsonConvert.SerializeObject(result);
        }

        [HttpGet]
        [Route("ReactiveTheoMaNhanVien")]
        public async Task<string> ReactiveTheoMaNhanVien(string listMaNvs)
        {
            var result = new ApiResult();
            listMaNvs = listMaNvs.Trim().Replace(" ", "").Trim();

            List<string> listMaNhanVien = listMaNvs.Split(",").ToList();

            listMaNhanVien = listMaNhanVien.Distinct().ToList();

            if (listMaNvs.Any())
            {
                string maNvs = "";
                foreach (var maNv in listMaNhanVien)
                {
                    maNvs += maNv + ",";
                }

                if (maNvs.Length > 1)
                {
                    maNvs = maNvs.Remove(maNvs.Length - 1);
                    AbpServices.ReactiveUserManual(maNvs);
                }

                result.Errors.Add(new ApiErrorItem(ApiErrorCode.Generic, "Danh sách Mã NV đã đưa vào Queue thành công. Kết quả RE-ACTIVE user sẽ gửi về email."));

                result.Payload = maNvs;
            }
            else
            {
                result.Payload = "";
                result.Errors.Add(new ApiErrorItem(ApiErrorCode.UserNotFound, "Lỗi: Chuỗi nhập vào không đúng"));
            }

            return JsonConvert.SerializeObject(result);
        }


        [HttpGet]
        [Route("GetAllNhanVienNghiViec2")]
        public string GetAllNhanVienNghiViec2()
        {
            //var list = UserService.GetAllNhanVienNghiViec();
            var list = _abpHplDb.GetAllNhanVienNghiViecDaCoUser();
            return JsonSerializer.Serialize(list);
        }

        [HttpGet]
        [Route("GetAllNhanVienNghiViec")]
        public async Task<IActionResult> GetAllNhanVienNghiViec()
        {
            var listNvs = await _abpHplDb.GetAllNhanVienNghiViecDaCoUserAsync();

            return Ok(listNvs);
        }

        [HttpGet]
        [Route("GetAllNhanVienChuaCoUsername2")]
        public string GetAllNhanVienChuaCoUsername2()
        {
            var result = new ApiResult();

            //var listNvs = UserService.GetAllNhanVienChuaCoUsername2();
            //var listNvs = UserService.GetAllNhanVienChuaCoUsername();
            var listNvs = _abpHplDb.GetAllNhanVienDangLamChuaCoUser();

            result.Errors.Add(new ApiErrorItem(ApiErrorCode.Generic, "Tổng nhân sự: " + listNvs.Count));

            result.Payload = listNvs;
            return JsonSerializer.Serialize(result);
        }

        [HttpGet]
        [Route("GetAllNhanVienChuaCoUsername")]
        public async Task<IActionResult> GetAllNhanVienChuaCoUsername()
        {
            var listNvs = await _abpHplDb.GetAllNhanVienDangLamChuaCoUserAsync();

            return Ok(listNvs);
        }

        [HttpGet]
        [Route("GetAllLogNhanVienCreate2")]
        public string GetAllLogNhanVienCreate()
        {
            return JsonSerializer.Serialize(AbpServices.GetAllLogNhanVien());
        }

        [HttpGet]
        [Route("GetAllLogNhanVienCreate")]
        public async Task<IActionResult> GetAllLogNhanVienCreate([FromQuery] DateTime? dtFrom, [FromQuery] DateTime? dtTo)
        {
            List<GetAllLogNhanVienCreateReturnModel> listNvs;

            if (dtFrom.HasValue & dtTo.HasValue)
            {
                listNvs = await _abpHplDb.GetAllLogNhanVienCreateAsync(dtFrom, dtTo);
            }
            else
            {
                listNvs = await _abpHplDb.GetAllLogNhanVienCreateAsync(null, null);
            }

            if (listNvs.Any())
            {
                return Ok(listNvs);
            }

            return Ok(new ApiErrorItem(ApiErrorCode.Generic, "Không có dữ liệu"));
        }

        [HttpGet]
        [Route("GetAllLogNhanVienDisable2")]
        public string GetAllLogNhanVienDisable()
        {
            return JsonSerializer.Serialize(AbpServices.GetAllLogNhanVienDisable());
        }

        [HttpGet]
        [Route("GetAllLogNhanVienDisable")]
        public async Task<IActionResult> GetAllLogNhanVienDisable([FromQuery] DateTime? dtFrom, [FromQuery] DateTime? dtTo)
        {
            List<GetAllLogNhanVienDisableReturnModel> listNvs;

            if (dtFrom.HasValue & dtTo.HasValue)
            {
                listNvs = await _abpHplDb.GetAllLogNhanVienDisableAsync(dtFrom, dtTo);
            }
            else
            {
                listNvs = await _abpHplDb.GetAllLogNhanVienDisableAsync(null, null);
            }

            if (listNvs.Any())
            {
                return Ok(listNvs);
            }

            return Ok(new ApiErrorItem(ApiErrorCode.Generic, "Không có dữ liệu"));
        }

        [HttpGet]
        [Route("GetAllBlackListUser")]
        public async Task<IActionResult> GetAllBlackListUser()
        {
            var result = new ApiResult();
            var listUser = await _abpHplDb.BlackListUsers.ToListAsync();

            if (listUser.Any())
            {
                result.Payload = listUser;
                result.Errors.Add(new ApiErrorItem(ApiErrorCode.UserNotFound, "Tổng user " + listUser.Count));
            }
            else
            {
                result.Payload = "";
                result.Errors.Add(new ApiErrorItem(ApiErrorCode.UserNotFound, "Không có"));
            }

            return Ok(result);
        }

        [HttpGet]
        [Route("GetAllNhanVienTheoMa")]
        public string GetAllNhanVienTheoMa(string listMaNvs)
        {
            var result = new ApiResult();
            listMaNvs = listMaNvs.Trim().Replace(" ", "");
            List<string> listMaNhanVien = listMaNvs.Split(",").ToList();
            if (listMaNvs.Any())
            {
                var obj = UserService.GetAllNhanVienTheoMa(listMaNhanVien);

                if (obj != null)
                {
                    result.Payload = obj;
                }
                else
                {
                    result.Errors.Add(new ApiErrorItem(ApiErrorCode.UserNotFound, "Không tồn tại."));
                }
            }
            else
            {
                result.Payload = "";
                result.Errors.Add(new ApiErrorItem(ApiErrorCode.UserNotFound, "Lỗi: Chuỗi nhập vào không đúng"));
            }

            return JsonConvert.SerializeObject(result);
        }

        [HttpGet]
        [Route("DashboardTrungThongTinHrm")]
        public async Task<IActionResult> DashboardTrungThongTinHrm()
        {
            var result = await _abpHplDb.DashboardTrungThongTinHrmAsync();
            return Ok(result);
        }

        /// <summary>
        /// Cập nhật Branch của SaleOnline
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("UpdateBranch")]
        public IActionResult UpdateBranch()
        {
            //AbpServices.UpdateBranch();
            //return JsonSerializer.Serialize("OK!");

            var result = _abpHplDb.UpdateBranchSaleOnline();
            return Ok(result);
        }

        /// <summary>
        /// Làm phẳng Phòng ban cấp 1 và phòng ban con
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("FlattenAllHplPhongBan")]
        public IActionResult FlattenAllHplPhongBan()
        {
            var result = _abpHplDb.FlattenAllHplPhongBan();
            return Ok(result);
            //return JsonSerializer.Serialize(AbpServices.FlattenAllHplPhongBan());
        }

        [HttpGet]
        [Route("FlatenDeletePhongBan")]
        public IActionResult FlatenDeletePhongBan(string maPhongBan)
        {
            var result = _abpHplDb.FlatenDeletePhongBan(maPhongBan);
            return Ok(result);
        }

        /// <summary>
        /// CHẠY MỘT LẦN DO LỖI
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("UpdateLogDis")]
        public string UpdateLogDis()
        {
            return JsonSerializer.Serialize(AbpServices.UpdateLogDis());
        }

        // GET: api/<EmployeeController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<EmployeeController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<EmployeeController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<EmployeeController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<EmployeeController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
