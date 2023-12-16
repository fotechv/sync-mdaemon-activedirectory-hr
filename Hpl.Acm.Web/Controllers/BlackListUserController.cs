using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hpl.Acm.Web.Services;
using Hpl.HrmDatabase;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Unosquare.PassCore.Common;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Hpl.Acm.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlackListUserController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IUriService _uriService;
        private readonly IAbpHplDbContext _abpHplDb;

        public BlackListUserController(ILogger logger, IUriService uriService, IAbpHplDbContext abpHplDb)
        {
            _logger = logger;
            _uriService = uriService;
            _abpHplDb = abpHplDb;
        }

        // GET: api/<BlackListUserController>
        [HttpGet]
        [Route("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _abpHplDb.BlackListUsers.OrderBy(x => x.Email).ToListAsync();
            return Ok(result);
        }

        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<BlackListUserController>/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var item = await _abpHplDb.BlackListUsers.FindAsync(id);
            if (item != null)
            {
                return Ok(item);
            }

            return Ok(new ApiErrorItem(ApiErrorCode.Success, "Success")
            {
                FieldName = "Id"
            });
        }

        // POST api/<BlackListUserController>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] BlackListUser item)
        {
            //public int Id { get; set; } // Id (Primary key)
            //public string MaNhanVien { get; set; } // MaNhanVien (length: 50)
            //public string Username { get; set; } // Username (length: 50)
            //public string Email { get; set; } // Email (length: 256)
            //public string MoTa { get; set; } // MoTa (length: 500)
            var check1 = await _abpHplDb.BlackListUsers.FirstOrDefaultAsync(x => x.MaNhanVien == item.MaNhanVien);
            if (check1 != null)
            {
                return Ok(new ApiErrorItem(ApiErrorCode.Generic, "Mã này đã tồn tại, không thể thêm mới")
                {
                    FieldName = "MaNhanVien"
                });
            }

            var check2 = await _abpHplDb.BlackListUsers.FirstOrDefaultAsync(x => x.Username == item.Username);
            if (check2 != null)
            {
                return Ok(new ApiErrorItem(ApiErrorCode.Generic, "Username này đã tồn tại, không thể thêm mới")
                {
                    FieldName = "Username"
                });
            }

            var check3 = await _abpHplDb.BlackListUsers.FirstOrDefaultAsync(x => x.Email == item.Email);
            if (check3 != null)
            {
                return Ok(new ApiErrorItem(ApiErrorCode.Generic, "Email này đã tồn tại, không thể thêm mới")
                {
                    FieldName = "Email"
                });
            }

            try
            {
                await _abpHplDb.BlackListUsers.AddAsync(item);
                await _abpHplDb.SaveChangesAsync();

                return Ok(new ApiErrorItem(ApiErrorCode.Success, "Success"));
            }
            catch (Exception e)
            {
                return Ok(new ApiErrorItem(ApiErrorCode.Generic, "Lỗi: " + e.Message));
            }
        }

        // PUT api/<BlackListUserController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] BlackListUser item)
        {
            var item2 = await _abpHplDb.BlackListUsers.FindAsync(id);
            if (item2 == null)
            {
                return Ok(new ApiErrorItem(ApiErrorCode.Generic, "ID không tồn tại"));
            }

            var item3 = await _abpHplDb.BlackListUsers.FirstOrDefaultAsync(x => x.Id != id & x.MaNhanVien == item.MaNhanVien);
            if (item3 != null)
            {
                return Ok(new ApiErrorItem(ApiErrorCode.Generic, "MaNhanVien này đã tồn tại, không thể thêm mới")
                {
                    FieldName = "MaNhanVien"
                });
            }

            var item4 = await _abpHplDb.BlackListUsers.FirstOrDefaultAsync(x => x.Id != id & x.Username == item.Username);
            if (item4 != null)
            {
                return Ok(new ApiErrorItem(ApiErrorCode.Generic, "Username này đã tồn tại, không thể thêm mới")
                {
                    FieldName = "Username"
                });
            }

            var item5 = await _abpHplDb.BlackListUsers.FirstOrDefaultAsync(x => x.Id != id & x.Email == item.Email);
            if (item5 != null)
            {
                return Ok(new ApiErrorItem(ApiErrorCode.Generic, "Email này đã tồn tại, không thể thêm mới")
                {
                    FieldName = "Email"
                });
            }

            try
            {
                item2.MaNhanVien = item.MaNhanVien;
                item2.Username = item.Username;
                item2.Email = item.Email;
                item2.MoTa = item.MoTa;

                await _abpHplDb.SaveChangesAsync();
            }
            catch (Exception e)
            {
                return Ok(new ApiErrorItem(ApiErrorCode.Generic, "Lỗi: " + e.Message));
            }

            return Ok(new ApiErrorItem(ApiErrorCode.Success, "Success"));
        }

        // DELETE api/<BlackListUserController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _abpHplDb.BlackListUsers.FindAsync(id);
            if (item == null)
            {
                return Ok(new ApiErrorItem(ApiErrorCode.Generic, "ID không tồn tại")
                {
                    FieldName = "MaPhongBan"
                });
            }

            try
            {
                _abpHplDb.BlackListUsers.Remove(item);
                await _abpHplDb.SaveChangesAsync();

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
