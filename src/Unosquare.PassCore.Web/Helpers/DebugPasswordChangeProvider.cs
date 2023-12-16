namespace Unosquare.PassCore.Web.Helpers
{
    using System;
    using Common;
    using Serilog;
    using System.DirectoryServices;
    using System.DirectoryServices.AccountManagement;
    using Hpl.HrmDatabase.ViewModels;
    using System.Collections.Generic;
    using Hpl.HrmDatabase;

    internal class DebugPasswordChangeProvider : IPasswordChangeProvider
    {
        private readonly ILogger _logger;

        public DebugPasswordChangeProvider(ILogger logger)
        {
            _logger = logger;
        }

        public ApiResultAd? UpdateUserInfo(string username)
        {
            throw new NotImplementedException();
        }

        public string GetUserNewUserFormAd(string username)
        {
            throw new NotImplementedException();
        }

        public string DisableUser(string username)
        {
            throw new NotImplementedException();
        }

        public string ReactiveUser(string username)
        {
            throw new NotImplementedException();
        }

        public void CreateOrUpdateAdUser(NhanVienViewModel2 user, string pw)
        {
            throw new NotImplementedException();
        }

        public void CreateOrUpdateAdUser2(GetAllNhanVienTheoListMaNvReturnModel user, string pw)
        {
            throw new NotImplementedException();
        }

        public void UpdateAdUser(NhanVienViewModel2 model, string pw)
        {
            throw new NotImplementedException();
        }

        public void UpdateAdUser2(GetAllNhanVienTheoListMaNvReturnModel model, string pw)
        {
            throw new NotImplementedException();
        }

        public ApiResultAd CreateAdUser(UserInfoAd user, string pw)
        {
            throw new NotImplementedException();
        }

        public ApiResultAd UpdateDepartment(string userName, string tenPhongBan)
        {
            throw new NotImplementedException();
        }

        public List<ApiResultAd> UpdateUserInfoHrm(List<NhanVienViewModel> listNvs)
        {
            throw new NotImplementedException();
        }

        public List<ApiResultAd> UpdateUserInfoAd(List<DirectoryEntry> listNvs)
        {
            throw new NotImplementedException();
        }

        public void UpdateUserInfoAd(NhanVienViewModel model)
        {
            throw new NotImplementedException();
        }

        public void CreateOu(string ouName)
        {
            throw new NotImplementedException();
        }

        public void CreateAllOuHpl()
        {
            throw new NotImplementedException();
        }

        public ApiErrorItem? PerformPasswordChange(string username, string currentPassword, string newPassword)
        {
            _logger.Information("DebugPasswordChangeProvider.PerformPasswordChange: username=" + username +
                    ". currentPassword=" + currentPassword +
                    ". newPassword=" + newPassword);

            var currentUsername = username.IndexOf("@", StringComparison.Ordinal) > 0
                ? username.Substring(0, username.IndexOf("@", StringComparison.Ordinal))
                : username;
            _logger.Information("DebugPasswordChangeProvider.PerformPasswordChange: currentUsername=" + currentUsername);

            // Even in DEBUG, it is safe to make this call and check the password anyway
            if (PwnedPasswordsSearch.PwnedSearch.IsPwnedPassword(newPassword))
                return new ApiErrorItem(ApiErrorCode.PwnedPassword);

            return currentUsername switch
            {
                "error" => new ApiErrorItem(ApiErrorCode.Generic, "Error"),
                "changeNotPermitted" => new ApiErrorItem(ApiErrorCode.ChangeNotPermitted),
                "fieldMismatch" => new ApiErrorItem(ApiErrorCode.FieldMismatch),
                "fieldRequired" => new ApiErrorItem(ApiErrorCode.FieldRequired),
                "invalidCaptcha" => new ApiErrorItem(ApiErrorCode.InvalidCaptcha),
                "invalidCredentials" => new ApiErrorItem(ApiErrorCode.InvalidCredentials),
                "invalidDomain" => new ApiErrorItem(ApiErrorCode.InvalidDomain),
                "userNotFound" => new ApiErrorItem(ApiErrorCode.UserNotFound),
                "ldapProblem" => new ApiErrorItem(ApiErrorCode.LdapProblem),
                "pwnedPassword" => new ApiErrorItem(ApiErrorCode.PwnedPassword),
                _ => null
            };
        }

        public UserPrincipal GetUserPrincipal(string username, string pw)
        {
            throw new NotImplementedException();
        }

        public DirectoryEntry GetUserDirectoryEntry(string username, string pw)
        {
            throw new NotImplementedException();
        }

        public List<DirectoryEntry> GetAllUserHpl()
        {
            throw new NotImplementedException();
        }

        public List<UserInfoAd> GetAllUserHpl2()
        {
            throw new NotImplementedException();
        }

        public List<ApiResultAd> GetAllUsers()
        {
            throw new NotImplementedException();
        }

        public List<UserAdInfo> GetAllUsers2()
        {
            throw new NotImplementedException();
        }

        public ApiResultAd? GetUserInfo(string username, string pw)
        {
            throw new NotImplementedException();
        }
    }
}
