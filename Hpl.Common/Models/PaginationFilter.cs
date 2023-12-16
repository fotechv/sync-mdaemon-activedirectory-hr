namespace Hpl.Common.Models
{
    public class PaginationFilter
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public PaginationFilter()
        {
            this.PageNumber = 1;
            //this.PageSize = 10;
            this.PageSize = 100;
        }
        public PaginationFilter(int pageNumber,int pageSize)
        {
            this.PageNumber = pageNumber < 1 ? 1 : pageNumber;
            //this.PageSize = pageSize > 10 ? 10 : pageSize;
            this.PageSize = pageSize > 100 ? 100 : pageSize;
        }
    }

    public class PaginationFilter100
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public PaginationFilter100()
        {
            this.PageNumber = 1;
            this.PageSize = 100;
        }
        public PaginationFilter100(int pageNumber, int pageSize)
        {
            this.PageNumber = pageNumber < 1 ? 1 : pageNumber;
            this.PageSize = pageSize > 100 ? 100 : pageSize;
        }
    }

    public class PaginationFilter200
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public PaginationFilter200()
        {
            this.PageNumber = 1;
            this.PageSize = 200;
        }
        public PaginationFilter200(int pageNumber, int pageSize)
        {
            this.PageNumber = pageNumber < 1 ? 1 : pageNumber;
            this.PageSize = pageSize > 200 ? 200 : pageSize;
        }
    }
}
