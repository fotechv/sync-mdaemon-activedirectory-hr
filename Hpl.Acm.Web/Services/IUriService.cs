using System;
using Hpl.Common.Models;

namespace Hpl.Acm.Web.Services
{
    public interface IUriService
    {
        public Uri GetPageUri(PaginationFilter filter, string route);
    }
}
