using System.Linq;
using System.Threading.Tasks;
using Hpl.Acm.Web.Helpers;
using Hpl.Acm.Web.Services;
using Hpl.Acm.Web.Wrappers;
using Hpl.Common.Models;
using Hpl.HrmDatabase;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hpl.Acm.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly IAbpHplDbContext _context;
        private readonly IUriService _uriService;
        public CustomerController(IAbpHplDbContext context, IUriService uriService)
        {
            this._context = context;
            this._uriService = uriService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PaginationFilter filter)
        {
            var route = Request.Path.Value;
            var validFilter = new PaginationFilter(filter.PageNumber, filter.PageSize);
            var pagedData = await _context.Customers
               .Skip((validFilter.PageNumber - 1) * validFilter.PageSize)
               .Take(validFilter.PageSize)
               .ToListAsync();
            var totalRecords = await _context.Customers.CountAsync();
            var pagedReponse = PaginationHelper.CreatePagedReponse<Customer>(pagedData, validFilter, totalRecords, _uriService, route);
            return Ok(pagedReponse);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var customer = await _context.Customers.Where(a => a.Id == id).FirstOrDefaultAsync();
            return Ok(new Response<Customer>(customer));
        }
    }
}