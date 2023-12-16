namespace Unosquare.PassCore.Web.Services
{
    using Hpl.HrmDatabase;
    using Hpl.HrmDatabase.Services;
    using Microsoft.Extensions.Options;
    using Serilog;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Unosquare.PassCore.Common;
    using Unosquare.PassCore.Web.Models;
    using Hpl.Common.Helper;
    using Hpl.Common.MdaemonServices;
    using Hpl.Common.Models;

    public class ServiceCommon : IServiceCommon
    {
        private readonly ILogger _logger;
        private readonly ClientSettings _options;
        private readonly IPasswordChangeProvider _passwordChangeProvider;

        public const string MailDomain = "haiphatland.com.vn";
        //public const string MailDomain = "company.test";
        public  string PwDefault = "Hpl@123";
        private static string ParamsNode = "/MDaemon/API/Request/Parameters";

        public ServiceCommon(IOptions<ClientSettings> options, ILogger logger, IPasswordChangeProvider passwordChangeProvider)
        {
            _options = options.Value;
            _logger = logger;
            _passwordChangeProvider = passwordChangeProvider;
        }

        public string TestApi()
        {
            return "this string";
        }

        /// <summary>
        /// Tạo User trên MDaemon
        /// </summary>
        /// <param name="maNhanVien"></param>
        /// <returns></returns>
        public async Task<ApiResult> CreateUserTheoMaNhanVien(string maNhanVien)
        {

            var dbAbp = new AbpHplDbContext();
            ApiResult result = new ApiResult();
            CreateUserInput inputMail = new CreateUserInput();
            inputMail.AdminNotes = "Add by Tools. Time: " + DateTime.Now.ToString("dd/MM/yyyy hh:mm tt");
            inputMail.Domain = MailDomain;
            //MailList { get; set; }
            //Group { get; set; }

            //Lấy thông tin hồ sơ nhân viên theo mã nhân viên
            var listNvs = UserService.GetAllNhanVienTheoMa(maNhanVien);
            switch (listNvs.Count)
            {
                case > 1:
                    string ids = "";
                    foreach (var model in listNvs)
                    {
                        ids += model.NhanVienId + ", ";
                    }
                    result.Errors.Add(new ApiErrorItem(ApiErrorCode.Generic,
                        "Lỗi: Mã nhân viên này được sử dụng cho nhiều hồ sơ. Có các ID: " + ids));
                    return result;
                case < 1:
                    result.Errors.Add(new ApiErrorItem(ApiErrorCode.Generic, "Không tìm thấy hồ sơ nhân sự."));
                    return result;
            }

            var nhanVien = listNvs.FirstOrDefault();
            if (nhanVien == null)
            {
                result.Errors.Add(new ApiErrorItem(ApiErrorCode.Generic, "Không tìm thấy hồ sơ nhân sự."));
                return result;
            }

            string ho = CommonHelper.ConvertToUnSign(nhanVien.Ho.Trim().ToLower());
            string ten = CommonHelper.ConvertToUnSign(nhanVien.Ten.Trim().ToLower());
            inputMail.FullName = CommonHelper.ConvertToUnSign(nhanVien.Ho + " " + nhanVien.Ten);
            inputMail.FirstName = ten;
            inputMail.LastName = ho;

            //Tạo username dựa trên Họ Và Tên nhân sự
            var hoList = CommonHelper.ConvertToUnSign(ho
                                    .Replace(" ", ",")
                                    .Replace(",,", ","))
                                    .Split(",");
            string newHo = "";
            foreach (var s in hoList)
            {
                newHo += s.Substring(0, 1);
            }

            string userNameGenerated = ten + newHo.ToLower();

            //TẠO LẠI MẬT KHẨU MẶC ĐỊNH
            string dienThoai = "";

            try
            {
                if (nhanVien.DienThoai != null && nhanVien.DienThoai.Trim().Length >= 9)
                {
                    dienThoai = nhanVien.DienThoai.Trim();
                    PwDefault = "Hpl@" + dienThoai.Substring(dienThoai.Length - 3, 3);
                }
            }
            catch (Exception e)
            {
                Log.Error("Số điện thoại lỗi " + e.Message);
            }

            //Get user trên MDaemon
            //inputMail.Username = MdaemonXmlApi.GetNewUserFromMdaemon(userNameGenerated);
            //Lấy AD user làm gốc
            inputMail.Username = _passwordChangeProvider.GetUserNewUserFormAd(userNameGenerated);
            inputMail.Password = PwDefault;

            //Xác định phòng, ban cấp 1 của Hồ sơ nhân sự dựa vào Mã Nhân viên
            PhongBan phongBan = UserService.GetPhongBanCap1CuaNhanVien(maNhanVien);
            if (phongBan == null)
            {
                result.Errors.Add(new ApiErrorItem(ApiErrorCode.Generic, "Mã nhân sự này không xác định được hồ sơ nhân sự."));
                return result;
            };
            //Xác định mail group cho nhân sự này
            HplPhongBan? hplPhong = dbAbp.HplPhongBans.FirstOrDefault(x => x.PhongBanId == nhanVien.PhongBanId);
            if (hplPhong == null)
            {
                result.Errors.Add(new ApiErrorItem(ApiErrorCode.Generic, "Trong cài đặt mapping phòng ban, không xác định được Phòng ban tương ứng. Mã nhân viên: " + maNhanVien));
                return result;
            }

            inputMail.Group = hplPhong.MaPhongBan;
            inputMail.MailList = hplPhong.MailingList;

            //Check server AD
            //TODO

            //Tạo email trên MDaemon
            try
            {
                result.Payload = await MdaemonXmlApi.CreateUser(inputMail);
                result.Errors.Add(new ApiErrorItem(ApiErrorCode.Generic, "Successful"));
            }
            catch (Exception e)
            {
                result.Errors.Add(new ApiErrorItem(ApiErrorCode.Generic, "Error: " + e.Message + ". Lưu ý: Kiểm tra lại Mail nhóm đã có trên MDaemon chưa?"));
            }

            return result;
        }

    }
}