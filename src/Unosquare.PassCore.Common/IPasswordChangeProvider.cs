using Hpl.HrmDatabase;

namespace Unosquare.PassCore.Common
{
    using System.DirectoryServices.AccountManagement;
    using System.DirectoryServices;
    using System;
    using System.Collections.Generic;
    using Hpl.HrmDatabase.ViewModels;

    /// <summary>
    /// Represents a interface for a password change provider.
    /// </summary>
    public interface IPasswordChangeProvider
    {
        /// <summary>
        /// Get Principal of User
        /// </summary>
        /// <param name="username">username</param>
        /// <param name="pw">password</param>
        /// <returns>UserPrincipal</returns>
        public UserPrincipal GetUserPrincipal(string username, string pw);

        /// <summary>
        /// Get DirectoryEntry of User
        /// </summary>
        /// <param name="username">username</param>
        /// <param name="pw">password</param>
        /// <returns>DirectoryEntry</returns>
        public DirectoryEntry GetUserDirectoryEntry(string username, string pw);

        public List<DirectoryEntry> GetAllUserHpl();

        public List<ApiResultAd> GetAllUsers();

        public List<UserAdInfo> GetAllUsers2();

        public ApiResultAd? GetUserInfo(string username, string pw);

        public string GetUserNewUserFormAd(string username);

        public string DisableUser(string username);

        public string ReactiveUser(string username);

        public void CreateOrUpdateAdUser(NhanVienViewModel2 user, string pw);
        public void CreateOrUpdateAdUser2(GetAllNhanVienTheoListMaNvReturnModel user, string pw);

        public void UpdateAdUser(NhanVienViewModel2 model, string pw);
        public void UpdateAdUser2(GetAllNhanVienTheoListMaNvReturnModel model, string pw);

        public ApiResultAd CreateAdUser(UserInfoAd user, string pw);

        public ApiResultAd UpdateDepartment(string userName, string tenPhongBan);

        public List<ApiResultAd> UpdateUserInfoHrm(List<NhanVienViewModel> listNvs);

        public List<ApiResultAd> UpdateUserInfoAd(List<DirectoryEntry> listNvs);

        public void UpdateUserInfoAd(NhanVienViewModel model);

        public void CreateOu(string ouName);

        public void CreateAllOuHpl();

        /// <summary>
        /// Performs the password change using the credentials provided.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="currentPassword">The current password.</param>
        /// <param name="newPassword">The new password.</param>
        /// <returns>The API error item or null if the change password operation was successful.</returns>
        ApiErrorItem? PerformPasswordChange(string username, string currentPassword, string newPassword);

        /// <summary>
        /// Compute the distance between two strings.
        /// Take it from https://www.csharpstar.com/csharp-string-distance-algorithm/.
        /// </summary>
        /// <param name="currentPassword">The current password.</param>
        /// <param name="newPassword">The new password.</param>
        /// <returns>
        /// The distance between strings.
        /// </returns>
        int MeasureNewPasswordDistance(string currentPassword, string newPassword)
        {
            var n = currentPassword.Length;
            var m = newPassword.Length;
            var d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
                return m;

            if (m == 0)
                return n;

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++) { }

            for (int j = 0; j <= m; d[0, j] = j++) { }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (newPassword[j - 1] == currentPassword[i - 1]) ? 0 : 1;
                    // Step 6
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }

            // Step 7
            return d[n, m];

        }
    }
}