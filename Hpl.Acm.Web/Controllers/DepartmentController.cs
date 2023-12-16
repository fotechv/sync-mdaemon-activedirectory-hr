using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Hpl.Acm.Web.Services;
using Hpl.Common.Models;
using Hpl.HrmDatabase;
using Hpl.HrmDatabase.Services;
using Hpl.SaleOnlineDatabase;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Serilog;
using Unosquare.PassCore.Common;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860
namespace Hpl.Acm.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DepartmentController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IUriService _uriService;
        private readonly IAbpHplDbContext _abpHplDb;
        private readonly IHrmDbContext _iHrmDbContext;
        private readonly ISaleOnlineDbContext _saleDb;

        public DepartmentController(ILogger logger, IAbpHplDbContext abpHplDb, IUriService uriService, IHrmDbContext iHrmDbContext, ISaleOnlineDbContext saleDb)
        {
            _logger = logger;
            _abpHplDb = abpHplDb;
            _uriService = uriService;
            _iHrmDbContext = iHrmDbContext;
            _saleDb = saleDb;
        }

        // GET: api/<DepartmentController>
        [HttpGet]
        [Route("GetAllDepartment")]
        public async Task<IActionResult> GetAllDepartment()
        {
            var item = await _abpHplDb.HplPhongBans.OrderBy(x => x.TenPhongBan).ToListAsync();
            //return JsonSerializer.Serialize(AbpServices.GetAllDepartment());
            return Ok(item);
        }

        // GET: api/<DepartmentController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<DepartmentController>/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var item = await _abpHplDb.HplPhongBans.FindAsync(id);
            if (item != null)
            {
                return Ok(item);
            }

            return Ok(new ApiErrorItem(ApiErrorCode.Success, "Success")
            {
                FieldName = "Id"
            });
        }

        // POST api/<DepartmentController>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] HplPhongBan pb)
        {
            var item = await _abpHplDb.HplPhongBans.FirstOrDefaultAsync(x => x.MaPhongBan == pb.MaPhongBan);
            if (item != null)
            {
                return Ok(new ApiErrorItem(ApiErrorCode.Generic, "Đã tồn tại mã Đơn vị, không thể thêm mới")
                {
                    FieldName = "MaPhongBan"
                });
            }

            var item2 = await _abpHplDb.HplPhongBanFlattens.FirstOrDefaultAsync(x => x.MaPhongBanChild == pb.MaPhongBan);
            if (item2 != null)
            {
                return Ok(new ApiErrorItem(ApiErrorCode.Generic, "Mã đơn vị này đang là con của một Đơn vị khác, không thể thêm mới")
                {
                    FieldName = "MaPhongBan"
                });
            }

            var hrmPb = await _iHrmDbContext.PhongBans.FirstOrDefaultAsync(x => x.MaPhongBan == pb.MaPhongBan);
            if (hrmPb == null)
            {
                return Ok(new ApiErrorItem(ApiErrorCode.Generic, "Không tồn tại Mã đơn vị này trên HRM")
                {
                    FieldName = "MaPhongBan"
                });
            }

            //Bổ sung data
            //public int PhongBanId { get; set; } // PhongBanId
            pb.PhongBanId = hrmPb.PhongBanId;
            //public string MaPhongBan { get; set; } // MaPhongBan (length: 50)
            //public int? PhongBanParentId { get; set; } // PhongBanParentId
            pb.PhongBanParentId = hrmPb.PhongBanChaId;
            //public string TenPhongBan { get; set; } // TenPhongBan (length: 512)
            pb.TenPhongBan = hrmPb.Ten;
            //public DateTime? CreationTime { get; set; } // CreationTime
            pb.CreationTime = DateTime.Now;
            //public string MailingList { get; set; } // MailingList (length: 512)
            //public DateTime? LastSyncToAd { get; set; } // LastSyncToAd
            pb.LastSyncToAd = DateTime.Now;

            //LẤY THÔNG TIN TRÊN SALE ONLINE
            var branch = await _saleDb.Branches.FirstOrDefaultAsync(x => x.BranchCode == pb.MaPhongBan);
            if (branch != null)
            {
                //public int? BranchId { get; set; } // BranchID
                pb.BranchId = branch.BranchId;
                //public string BranchCode { get; set; } // BranchCode (length: 512)
                pb.BranchCode = branch.BranchCode;
                //public string BranchName { get; set; } // BranchName (length: 512)
                pb.BranchName = branch.BranchName;
            }
            //public string EmailNotification { get; set; } // EmailNotification (length: 1024)

            try
            {
                await _abpHplDb.HplPhongBans.AddAsync(pb);
                await _abpHplDb.SaveChangesAsync();

                //FlattenAllHplPhongBan
                var result = _abpHplDb.FlattenAllHplPhongBan();
                //UpdateBranch
                var result2 = _abpHplDb.UpdateBranchSaleOnline();

                return Ok(new ApiErrorItem(ApiErrorCode.Success, "Success")
                {
                    FieldName = "MaPhongBan"
                });

            }
            catch (Exception e)
            {
                return Ok(new ApiErrorItem(ApiErrorCode.Generic, "Lỗi: " + e.Message)
                {
                    FieldName = "MaPhongBan"
                });
            }
        }

        // PUT api/<DepartmentController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] HplPhongBan pb)
        {
            var item = await _abpHplDb.HplPhongBans.FindAsync(id);
            if (item == null)
            {
                return Ok(new ApiErrorItem(ApiErrorCode.Generic, "ID không tồn tại")
                {
                    FieldName = "MaPhongBan"
                });
            }

            try
            {
                item.MailingList = pb.MailingList;
                item.EmailNotification = pb.EmailNotification;

                await _abpHplDb.SaveChangesAsync();

                //FlattenAllHplPhongBan
                var result = _abpHplDb.FlattenAllHplPhongBan();
                //UpdateBranch
                var result2 = _abpHplDb.UpdateBranchSaleOnline();

                return Ok(new ApiErrorItem(ApiErrorCode.Success, "Success")
                {
                    FieldName = "MaPhongBan"
                });
            }
            catch (Exception e)
            {
                return Ok(new ApiErrorItem(ApiErrorCode.Generic, "Lỗi: " + e.Message)
                {
                    FieldName = "MaPhongBan"
                });
            }
        }

        // DELETE api/<DepartmentController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _abpHplDb.HplPhongBans.FindAsync(id);
            if (item == null)
            {
                return Ok(new ApiErrorItem(ApiErrorCode.Generic, "ID không tồn tại")
                {
                    FieldName = "MaPhongBan"
                });
            }

            try
            {
                _abpHplDb.HplPhongBans.Remove(item);
                await _abpHplDb.SaveChangesAsync();

                //FlattenAllHplPhongBan
                var result = _abpHplDb.FlattenAllHplPhongBan();
                //UpdateBranch
                var result2 = _abpHplDb.UpdateBranchSaleOnline();

                return Ok(new ApiErrorItem(ApiErrorCode.Success, "Success")
                {
                    FieldName = "MaPhongBan"
                });
            }
            catch (Exception e)
            {
                return Ok(new ApiErrorItem(ApiErrorCode.Generic, "Lỗi: " + e.Message)
                {
                    FieldName = "MaPhongBan"
                });
            }
        }
    }
}
