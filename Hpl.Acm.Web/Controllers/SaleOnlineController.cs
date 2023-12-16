using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hpl.Acm.Web.Services;
using Hpl.HrmDatabase;
using Hpl.SaleOnlineDatabase;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Unosquare.PassCore.Common;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Hpl.Acm.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SaleOnlineController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IUriService _uriService;
        private readonly IAbpHplDbContext _abpHplDb;
        private readonly IHrmDbContext _iHrmDbContext;
        private readonly ISaleOnlineDbContext _saleDb;

        public SaleOnlineController(ILogger logger, IUriService uriService, IAbpHplDbContext abpHplDb, IHrmDbContext iHrmDbContext, ISaleOnlineDbContext saleDb)
        {
            _logger = logger;
            _uriService = uriService;
            _abpHplDb = abpHplDb;
            _iHrmDbContext = iHrmDbContext;
            _saleDb = saleDb;
        }

        // GET: api/<SaleOnlineController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<SaleOnlineController>/5
        [HttpGet]
        [Route("GetNhanVien")]
        public async Task<IActionResult> GetNhanVien([FromQuery] string username)
        {
            var item = await _saleDb.NhanViens.FirstOrDefaultAsync(x => x.MaSo == username);
            if (item != null)
            {
                return Ok(item);
            }

            return Ok(new ApiErrorItem(ApiErrorCode.Generic, "Không tồn tại user")
            {
                FieldName = "MaSo"
            });
        }

        // POST api/<SaleOnlineController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<SaleOnlineController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<SaleOnlineController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
