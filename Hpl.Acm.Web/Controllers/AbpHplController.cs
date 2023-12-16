using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hpl.Acm.Web.Models;
using Hpl.Acm.Web.Services;
using Hpl.Common;
using Hpl.HrmDatabase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Unosquare.PassCore.Common;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Hpl.Acm.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AbpHplController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IAbpHplDbContext _abpHplDb;
        private readonly IUriService _uriService;
        
        private readonly ClientSettings _options;
        private readonly IPasswordChangeProvider _passwordChangeProvider;

        public AbpHplController(ILogger logger, IAbpHplDbContext abpHplDb, IUriService uriService, IOptions<ClientSettings> optionsAccessor, IPasswordChangeProvider passwordChangeProvider)
        {
            _logger = logger;
            _abpHplDb = abpHplDb;
            _uriService = uriService;
            _options = optionsAccessor.Value;
            _passwordChangeProvider = passwordChangeProvider;
        }

        [HttpGet]
        [Route("gfyvhxnueb")]
        public IActionResult GetUserInfo(string username, string pw)
        {
            _logger.Information("START PasswordController.GetUserInfo: ");
            _logger.Information("PasswordController.GetUserInfo: " + username);

            try
            {
                var obj = _passwordChangeProvider.GetUserInfo(username, pw);
                return Ok(obj);
            }
            catch (Exception e)
            {
                return Ok(e);
            }

        }

        [HttpGet]
        [Route("GetAllUserSystems")]
        public async Task<IActionResult> GetAllUserSystems()
        {
            var result = await _abpHplDb.GetAllUserSystemsAsync();
            return Ok(result);
        }

        [HttpGet]
        [Route("UpdateAllUserAdInfo")]
        public IActionResult UpdateAllUserAdInfo()
        {
            var result = _abpHplDb.DeleteAllUserAdInfo();

            return Ok(new ApiErrorItem(ApiErrorCode.Success, "Đã gửi lệnh update thành công. Chờ 1 phút nhấn F5 để tải lại trang này."));
        }

        [HttpGet]
        [Route("Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var users = await _abpHplDb.ThongKeTheoThangUserAsync();

            var infos = await _abpHplDb.DashboardTrungThongTinHrmAsync();

            //var checkProcess = ServerStatus.ListProcesses();
            var checkProcess = ServerStatus.GetProcessAcm();

            var result = new[]
            {
                new {
                    ThongKeTheoThangUser = users,
                    TrungThongTinHrm = infos,
                    ServerStatus = checkProcess
                }
            };

            return Ok(result);
        }

        [HttpGet]
        [Route("ThongKeTheoThangUser")]
        public async Task<IActionResult> ThongKeTheoThangUser()
        {
            var result = await _abpHplDb.ThongKeTheoThangUserAsync();
            return Ok(result);
        }

        [HttpGet]
        [Route("GetAllUserAdInfo")]
        public async Task<IActionResult> GetAllUserAdInfo()
        {
            var result = await _abpHplDb.UserAdInfoes.ToListAsync();
            return Ok(result);
        }

        // GET: api/<AbpHplController>
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(_options);
        }

        // GET api/<AbpHplController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<AbpHplController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
            var abc = value;
            var item = new Customer();
            item = JsonConvert.DeserializeObject<Customer>(abc);
            if (item != null)
            {
                var callApi = _abpHplDb.Customers.AddAsync(item);
                _abpHplDb.SaveChangesAsync();
            }
        }

        // PUT api/<AbpHplController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<AbpHplController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
