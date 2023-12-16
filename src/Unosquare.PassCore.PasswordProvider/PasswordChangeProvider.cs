using Hpl.Common.Helper;
using Hpl.HrmDatabase;
using Hpl.HrmDatabase.Services;
namespace Unosquare.PassCore.PasswordProvider
{
    using Common;
    using Microsoft.Extensions.Options;
    using Serilog;
    using Serilog.Sinks.EventLog;
    using System;
    using System.Collections.Generic;
    using System.DirectoryServices;
    using System.DirectoryServices.AccountManagement;
    using System.DirectoryServices.ActiveDirectory;
    using System.Linq;
    using Newtonsoft.Json;
    using Hpl.HrmDatabase.ViewModels;

    /// <inheritdoc />
    /// <summary>
    /// Default Change Password Provider using 'System.DirectoryServices' from Microsoft.
    /// </summary>
    /// <seealso cref="IPasswordChangeProvider" />
    public partial class PasswordChangeProvider : IPasswordChangeProvider
    {
        private readonly PasswordChangeOptions _options;
        private readonly ILogger _logger;
        private IdentityType _idType = IdentityType.UserPrincipalName;

        //private const string PathOu = "LDAP://OU=Company Structure,DC=haiphatland,DC=local";
        //private const string RootOuHpl = "OU=Company Structure,DC=haiphatland,DC=local";

        private const string PathOu = "LDAP://OU=Company Structure,DC=haiphat,DC=local";
        private const string RootOuHpl = "OU=Company Structure,DC=haiphat,DC=local";

        //private const string PathOu = "LDAP://OU=Company Structure,DC=baonx,DC=com";
        //private const string RootOuHpl = "OU=Company Structure,DC=baonx,DC=com";

        /// <summary>
        /// Initializes a new instance of the <see cref="PasswordChangeProvider"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="options">The options.</param>
        public PasswordChangeProvider(ILogger logger, IOptions<PasswordChangeOptions> options)
        {
            _logger = logger;
            _options = options.Value;
            SetIdType();
        }

        public int BackDateSchedule()
        {
            try
            {
                return int.Parse(_options.BackDateSchedule);
            }
            catch (Exception e)
            {
                _logger.Error("Loi: " + e);
                return 1;
            }
        }

        public string Test()
        {
            return "cc";
        }

        public List<DirectoryEntry> GetAllUserHpl()
        {
            List<DirectoryEntry> listUsers = new List<DirectoryEntry>();

            var dirEntry = new DirectoryEntry("LDAP://localhost:389/" + RootOuHpl);
            var searcher = new DirectorySearcher(dirEntry)
            {
                Filter = "(&(&(objectClass=user)(objectClass=person)))"
            };

            searcher.PageSize = 100000;
            var resultCollection = searcher.FindAll();
            foreach (SearchResult searchResult in resultCollection)
            {
                //var abc = searchResult.Path;
                listUsers.Add(searchResult.GetDirectoryEntry());
            }

            return listUsers;
        }

        /// <summary>
        /// Get all user của AD (Default 1000)
        /// </summary>
        /// <returns></returns>
        public List<ApiResultAd> GetAllUsers()
        {
            var listUser = new List<ApiResultAd>();
            var result = new ApiResultAd();
            int i = 0;

            //using (var searcher = new PrincipalSearcher(new UserPrincipal(new PrincipalContext(ContextType.Domain, Environment.UserDomainName))))

            var principalContext = AcquirePrincipalContext();
            var userPrincipal = new UserPrincipal(principalContext);
            using var searcher = new PrincipalSearcher(userPrincipal);

            List<UserPrincipal> users = searcher.FindAll().Select(u => (UserPrincipal)u).ToList();

            if (users.Any())
            {
                foreach (var u in users)
                {
                    DirectoryEntry dirEntry = (DirectoryEntry)u.GetUnderlyingObject();
                    result.UserInfo = ConvertUserProfiles(dirEntry);
                    listUser.Add(result);
                    u.Dispose();
                }
            }
            else
            {
                result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "Không có user nào.");
            }

            principalContext.Dispose();
            userPrincipal.Dispose();
            searcher.Dispose();

            return listUser;
        }

        public List<UserAdInfo> GetAllUsers2()
        {
            var listUser = new List<UserAdInfo>();
            int i = 0;

            //using (var searcher = new PrincipalSearcher(new UserPrincipal(new PrincipalContext(ContextType.Domain, Environment.UserDomainName))))

            var principalContext = AcquirePrincipalContext();
            var userPrincipal = new UserPrincipal(principalContext);
            using var searcher = new PrincipalSearcher(userPrincipal);

            List<UserPrincipal> users = searcher.FindAll().Select(u => (UserPrincipal)u).ToList();

            if (users.Any())
            {
                foreach (var u in users)
                {
                    var result = new UserAdInfo();
                    DirectoryEntry dirEntry = u.GetUnderlyingObject() as DirectoryEntry;
                    result = ConvertUserProfiles2(dirEntry);
                    result.IsLocked = u.IsAccountLockedOut();

                    listUser.Add(result);
                    u.Dispose();
                }
            }

            principalContext.Dispose();
            userPrincipal.Dispose();
            searcher.Dispose();

            return listUser;
        }

        public UserPrincipal GetUserPrincipal(string username, string pw)
        {
            var fixedUsername = FixUsernameWithDomain(username);
            //using var principalContext = AcquirePrincipalContext();
            using var principalContext = AcquirePrincipalContext(username, pw);
            return UserPrincipal.FindByIdentity(principalContext, _idType, fixedUsername);
        }

        public DirectoryEntry GetUserDirectoryEntry(string username, string pw)
        {
            var fixedUsername = FixUsernameWithDomain(username);
            using var principalContext = AcquirePrincipalContext();
            //using var principalContext = AcquirePrincipalContext(username, pw);
            var userPrincipal = UserPrincipal.FindByIdentity(principalContext, _idType, fixedUsername);
            if (userPrincipal != null)
            {
                var directoryEntry = userPrincipal.GetUnderlyingObject() as DirectoryEntry;

                return directoryEntry;
            }

            return null;
        }

        public ApiResultAd? GetUserInfo(string username, string pw)
        {
            _logger.Information("PasswordChangeProvider.GetUserInfo");
            var result = new ApiResultAd();
            result.UserInfo = null;

            var fixedUsername = FixUsernameWithDomain(username);
            using var principalContext = AcquirePrincipalContext();
            //using var principalContext = AcquirePrincipalContext(username, pw);//Không sử dụng user trong setting
            var userPrincipal = UserPrincipal.FindByIdentity(principalContext, _idType, fixedUsername);

            // Check if the user principal exists
            if (userPrincipal == null)
            {
                _logger.Warning($"The User principal ({fixedUsername}) doesn't exist");
                result.Errors = new ApiErrorItem(ApiErrorCode.UserNotFound, "User khong ton tai!");

                return result;
            }

            //Không cần check nhóm
            //var item = ValidateGroups(userPrincipal);
            //if (item != null)
            //{
            //    result.Errors = item;
            //    return result;
            //}

            // Use always UPN for password check.
            if (!ValidateUserCredentials(userPrincipal.UserPrincipalName, pw, principalContext))
            {
                _logger.Warning("The User principal password is not valid");

                result.Errors = new ApiErrorItem(ApiErrorCode.InvalidCredentials, "Mật khẩu không đúng!");
                return result;
            }

            var userInfo = new UserInfoAd
            {
                isLocked = userPrincipal.IsAccountLockedOut(),
                displayName = userPrincipal.DisplayName,
                userPrincipalName = userPrincipal.UserPrincipalName,
                sAMAccountName = userPrincipal.SamAccountName,
                name = userPrincipal.Name,
                givenName = userPrincipal.GivenName,//Họ LastName
                sn = userPrincipal.Surname,//Tên FirstName
                description = userPrincipal.Description,
                mail = userPrincipal.EmailAddress,
                telephoneNumber = userPrincipal.VoiceTelephoneNumber,
                //otherTelephone = "",
                //physicalDeliveryOfficeName = "",
                //initials = "",
                //wWWHomePage = "",
                //url = "",
                //CN = "",
                //homePhone = "",
                //mobile = ""
            };

            if (userPrincipal.GetUnderlyingObject() is DirectoryEntry directoryEntry)
            {
                userInfo = ConvertUserProfiles(directoryEntry);
                directoryEntry.Dispose();
            }

            result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "Successful");
            result.UserInfo = userInfo;

            principalContext.Dispose();
            userPrincipal.Dispose();

            return result;
        }

        public string GetUserNewUserFormAd(string username)
        {
            _logger.Information("PasswordChangeProvider.GetUserNewUserFormAd");

            string newUser = username;
            var principalContext = AcquirePrincipalContext();

            //Kiểm tra user đã tồn tại chưa
            bool check = true;
            int i = 0;
            while (check)
            {
                string fixedUsername = FixUsernameWithDomain(newUser);
                var up = UserPrincipal.FindByIdentity(principalContext, _idType, fixedUsername);
                if (up != null)
                {
                    i++;
                    newUser = username + i;
                    up.Dispose();
                }
                else
                {
                    check = false;
                }
            }

            return newUser;
        }

        public ApiResultAd UpdateDepartment(string userName, string tenPhongBan)
        {
            var result = new ApiResultAd();

            using var principalContext = AcquirePrincipalContext();
            var fixedUsername = FixUsernameWithDomain(userName);
            var userPrincipal = UserPrincipal.FindByIdentity(principalContext, _idType, fixedUsername);

            // Check if the user principal exists
            if (userPrincipal == null)
            {
                _logger.Warning($"The User principal ({fixedUsername}) doesn't exist");
                result.Errors = new ApiErrorItem(ApiErrorCode.UserNotFound, "User " + userName + " không tồn tại trên AD.");

                return result;
            }

            if (userPrincipal.GetUnderlyingObject() is DirectoryEntry directoryEntry)
            {
                //department: Chi nhánh/Phòng ban
                if (directoryEntry.Properties.Contains(UserPropertiesAd.Department))
                {
                    directoryEntry.Properties[UserPropertiesAd.Department].Value = tenPhongBan;
                }

                try
                {
                    directoryEntry.CommitChanges();
                    result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "Successful");
                }
                catch (Exception e)
                {
                    _logger.Error("Lỗi call UpdateDepartment: " + e);
                    result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "Lỗi call UpdateDepartment: " + e.Message);
                }
            }

            return result;
        }

        /// <summary>
        /// Cập nhật một số thông tin user trên AD dua tren danh sach user cuar HRM
        /// </summary>
        /// <param name="listNvs"></param>
        /// <returns></returns>
        public List<ApiResultAd> UpdateUserInfoHrm(List<NhanVienViewModel> listNvs)
        {
            _logger.Information("START PasswordChangeProvider.UpdateUserInfoHrm");
            List<ApiResultAd> listResult = new List<ApiResultAd>();
            var result = new ApiResultAd();

            using var principalContext = AcquirePrincipalContext();
            if (principalContext == null)
            {
                result.Errors = new ApiErrorItem(ApiErrorCode.InvalidCredentials, "Không kết nối được đến server AD.");
                listResult.Add(result);
                return listResult;
            }

            //using var principalContext = AcquirePrincipalContext(username, pw);//Không sử dụng user trong setting
            foreach (var model in listNvs)
            {
                var fixedUsername = FixUsernameWithDomain(model.TenDangNhap);
                var userPrincipal = UserPrincipal.FindByIdentity(principalContext, _idType, fixedUsername);

                // Check if the user principal exists
                if (userPrincipal == null)
                {
                    _logger.Warning($"The User principal ({fixedUsername}) doesn't exist");
                    result.Errors = new ApiErrorItem(ApiErrorCode.UserNotFound, "User " + model.TenDangNhap + " không tồn tại trên AD.");

                    listResult.Add(result);
                    continue;
                }

                //Không cần check Groups
                //var item = ValidateGroups(userPrincipal);
                //if (item != null)
                //{
                //    result.Errors = item;
                //    listResult.Add(result);
                //    break;
                //}

                //Lấy thông tin của User
                var userInfo = new UserInfoAd
                {
                    isLocked = userPrincipal.IsAccountLockedOut(),
                    displayName = userPrincipal.DisplayName,
                    userPrincipalName = userPrincipal.UserPrincipalName,
                    sAMAccountName = userPrincipal.SamAccountName,
                    givenName = userPrincipal.GivenName,
                    name = userPrincipal.Name,
                    sn = userPrincipal.Surname,
                    description = userPrincipal.Description,
                    mail = userPrincipal.EmailAddress,
                    telephoneNumber = userPrincipal.VoiceTelephoneNumber,
                    //otherTelephone = "",
                    //physicalDeliveryOfficeName = "",
                    //initials = "",
                    //wWWHomePage = "",
                    //url = "",
                    //CN = "",
                    //homePhone = "",
                    //mobile = ""
                };

                //var ten = CommonHelper.ConvertToUnSign(model.Ten);
                //var ho = CommonHelper.ConvertToUnSign(model.Ho);
                var ten = model.Ten.Trim();
                var ho = model.Ho.Trim();

                bool checkForUpdate = false;
                if (userPrincipal.GetUnderlyingObject() is DirectoryEntry adEntry)
                {
                    //var xx = CommonHelper.ConvertToUnSign(model.xx);
                    //userInfo.sn = xx;
                    //if (directoryEntry.Properties.Contains(UserPropertiesAd.xx))
                    //{
                    //    if (!model.xx.Equals(directoryEntry.Properties[UserPropertiesAd.xx].Value.ToString()))
                    //    {
                    //        directoryEntry.Properties[UserPropertiesAd.xx].Value = model.xx;
                    //        checkForUpdate = true;
                    //    }
                    //}
                    //else
                    //{
                    //    directoryEntry.Properties[UserPropertiesAd.xx].Value = model.xx;
                    //    checkForUpdate = true;
                    //}
                }

                checkForUpdate = false;
                if (userPrincipal.GetUnderlyingObject() is DirectoryEntry directoryEntry)
                {
                    //Update lại tên của Nhân sự
                    //Fix CN=Ho va Ten
                    try
                    {
                        if (directoryEntry.Properties.Contains(UserPropertiesAd.ContainerName))
                        {
                            //if (!model.TenDangNhap.Equals(directoryEntry.Properties[UserPropertiesAd.LoginName].Value.ToString()))
                            //{
                            //    directoryEntry.Rename("CN=" + ho + " " + ten);
                            //}
                            directoryEntry.Rename("CN=" + model.TenDangNhap);
                        }
                        else
                        {
                            //directoryEntry.Rename("CN=" + ho + " " + ten);
                        }

                        result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "Successful");
                    }
                    catch (Exception e)
                    {
                        result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "Lỗi: Update dữ liệu " + model.TenDangNhap + " vào AD không thành công. Error: " + e.Message);
                        listResult.Add(result);
                        continue;
                    }

                    //DirectoryEntry childEntry = ouEntry.Children.Add("CN=" + username, "user");
                    //childEntry.Properties[UserPropertiesAd.UserPrincipalName].Value = fixedUsername;
                    //childEntry.Properties[UserPropertiesAd.LoginName].Value = username; //SamAccountName
                    //childEntry.Properties[UserPropertiesAd.Name].Value = username;
                    //childEntry.Properties[UserPropertiesAd.LastName].Value = user.givenName;//sn, Surname, LastName = Ho va ten dem
                    //childEntry.Properties[UserPropertiesAd.FirstName].Value = user.sn;//GivenName, FirstName = Ten
                    //childEntry.Properties[UserPropertiesAd.DisplayName].Value = user.displayName;//GivenName, FirstName
                    //childEntry.Properties[UserPropertiesAd.EmailAddress].Value = username + "@haiphatland.com.vn";

                    //Fix HỌ: sn=LastName=Ho; 
                    userInfo.sn = ho;
                    if (directoryEntry.Properties.Contains(UserPropertiesAd.LastName))
                    {
                        if (!ho.Equals(directoryEntry.Properties[UserPropertiesAd.LastName].Value.ToString()))
                        {
                            directoryEntry.Properties[UserPropertiesAd.LastName].Value = ho;
                            checkForUpdate = true;
                        }
                    }
                    else
                    {
                        directoryEntry.Properties[UserPropertiesAd.LastName].Value = ho;
                        checkForUpdate = true;
                    }

                    //Tên: givenName = First name = Ten
                    userInfo.givenName = ten;
                    if (directoryEntry.Properties.Contains(UserPropertiesAd.FirstName))
                    {
                        if (!ten.Equals(directoryEntry.Properties[UserPropertiesAd.FirstName].Value.ToString()))
                        {
                            directoryEntry.Properties[UserPropertiesAd.FirstName].Value = ten;
                            checkForUpdate = true;
                        }
                    }
                    else
                    {
                        directoryEntry.Properties[UserPropertiesAd.FirstName].Value = ten;
                        checkForUpdate = true;
                    }

                    //displayName=(Ho va ten)
                    var displayName = ho + " " + ten;
                    userInfo.displayName = displayName;
                    if (directoryEntry.Properties.Contains(UserPropertiesAd.DisplayName))
                    {
                        if (!displayName.Equals(directoryEntry.Properties[UserPropertiesAd.DisplayName].Value.ToString()))
                        {
                            directoryEntry.Properties[UserPropertiesAd.DisplayName].Value = displayName;
                            checkForUpdate = true;
                        }
                    }
                    else
                    {
                        directoryEntry.Properties[UserPropertiesAd.DisplayName].Value = displayName;
                        checkForUpdate = true;
                    }

                    //department: Chi nhánh/Phòng ban
                    userInfo.department = model.TenPhongBan;
                    if (directoryEntry.Properties.Contains(UserPropertiesAd.Department))
                    {
                        if (!model.TenPhongBan.Equals(directoryEntry.Properties[UserPropertiesAd.Department].Value.ToString()))
                        {
                            directoryEntry.Properties[UserPropertiesAd.Department].Value = model.TenPhongBan;
                            checkForUpdate = true;
                        }
                    }
                    else
                    {
                        directoryEntry.Properties[UserPropertiesAd.Department].Value = model.TenPhongBan;
                        checkForUpdate = true;
                    }

                    //title = Chức danh
                    if (!string.IsNullOrEmpty(model.TenChucDanh))
                    {
                        userInfo.title = model.TenChucDanh;
                        if (directoryEntry.Properties.Contains(UserPropertiesAd.Title))
                        {
                            if (!model.TenChucDanh.Equals(directoryEntry.Properties[UserPropertiesAd.Title].Value.ToString()))
                            {
                                directoryEntry.Properties[UserPropertiesAd.Title].Value = model.TenChucDanh;
                                checkForUpdate = true;
                            }
                        }
                        else
                        {
                            directoryEntry.Properties[UserPropertiesAd.Title].Value = model.TenChucDanh;
                            checkForUpdate = true;
                        }
                    }

                    //employeeID mã nhân viên
                    userInfo.employeeID = model.MaNhanVien;
                    if (directoryEntry.Properties.Contains(UserPropertiesAd.EmployeeId))
                    {
                        if (!model.MaNhanVien.Equals(directoryEntry.Properties[UserPropertiesAd.EmployeeId].Value.ToString()))
                        {
                            directoryEntry.Properties[UserPropertiesAd.EmployeeId].Value = model.MaNhanVien;
                            checkForUpdate = true;
                        }
                    }
                    else
                    {
                        directoryEntry.Properties[UserPropertiesAd.EmployeeId].Value = model.MaNhanVien;
                        checkForUpdate = true;
                    }

                    //telephoneNumber=Điện thoại
                    try
                    {
                        string dt = "+84" + int.Parse(model.DienThoai);
                        userInfo.telephoneNumber = dt;
                        if (directoryEntry.Properties.Contains(UserPropertiesAd.TelePhoneNumber))
                        {
                            if (!dt.Equals(directoryEntry.Properties[UserPropertiesAd.TelePhoneNumber].Value.ToString()))
                            {
                                directoryEntry.Properties[UserPropertiesAd.TelePhoneNumber].Value = dt;
                                checkForUpdate = true;
                            }
                        }
                        else
                        {
                            directoryEntry.Properties[UserPropertiesAd.TelePhoneNumber].Value = dt;
                            checkForUpdate = true;
                        }
                    }
                    catch (Exception)
                    {
                        result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "Số điện thoại của " + model.TenDangNhap + " không đúng.");
                    }

                    //Cập nhật Email
                    if (!string.IsNullOrEmpty(model.Email))
                    {
                        var email = CommonHelper.IsValidEmail(model.Email);
                        userInfo.mail = email;
                        if (directoryEntry.Properties.Contains(UserPropertiesAd.EmailAddress))
                        {
                            if (!email.Equals(directoryEntry.Properties[UserPropertiesAd.EmailAddress].Value.ToString()))
                            {
                                directoryEntry.Properties[UserPropertiesAd.EmailAddress].Value = email;
                                checkForUpdate = true;
                            }
                        }
                        else
                        {
                            directoryEntry.Properties[UserPropertiesAd.EmailAddress].Value = email;

                        }
                        checkForUpdate = true;
                    }

                    //Lấy một số thông tin mà HRM không có
                    //mobile
                    if (directoryEntry.Properties.Contains(UserPropertiesAd.Mobile))
                    {
                        userInfo.mobile = directoryEntry.Properties[UserPropertiesAd.Mobile].Value.ToString();
                    }
                    //homePhone
                    if (directoryEntry.Properties.Contains(UserPropertiesAd.Homephone))
                    {
                        userInfo.homePhone = directoryEntry.Properties[UserPropertiesAd.Homephone].Value.ToString();
                    }
                    //cn
                    if (directoryEntry.Properties.Contains(UserPropertiesAd.ContainerName))
                    {
                        userInfo.CN = directoryEntry.Properties[UserPropertiesAd.ContainerName].Value.ToString();
                    }

                    //prop.Value = -1;
                    //directoryEntry.Properties[UserPropertiesAd.Department].Value = "Day la phong ban moi";
                    //directoryEntry.CommitChanges();

                    if (checkForUpdate)
                    {
                        try
                        {
                            directoryEntry.CommitChanges();
                            result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "Successful");
                        }
                        catch (Exception e)
                        {
                            result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "Lỗi: Update dữ liệu " + model.TenDangNhap + " vào AD không thành công. Error: " + e.Message);
                        }
                    }
                    else
                    {
                        result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "User " + model.TenDangNhap + " không có thông tin cần update.");
                    }

                    result.UserInfo = userInfo;
                }
                else
                {
                    result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "Lỗi: Không xác định được các Attributes của User " + model.TenDangNhap);
                }

                listResult.Add(result);
            }

            return listResult;
        }

        /// <summary>
        /// Cập nhật một số thông tin user trên AD dua tren danh sach user cua AD
        /// </summary>
        /// <param name="listNvs"></param>
        /// <returns></returns>
        public List<ApiResultAd> UpdateUserInfoAd(List<DirectoryEntry> listNvs)
        {
            _logger.Information("START PasswordChangeProvider.UpdateUserInfoAd");
            List<ApiResultAd> listResult = new List<ApiResultAd>();
            var result = new ApiResultAd();

            using var principalContext = AcquirePrincipalContext();
            if (principalContext == null)
            {
                result.Errors = new ApiErrorItem(ApiErrorCode.InvalidCredentials, "Không kết nối được đến server AD.");
                listResult.Add(result);
                return listResult;
            }

            //Lay danh sach userName
            var listAlUserName = UserService.GetAllUserName();

            //using var principalContext = AcquirePrincipalContext(username, pw);//Không sử dụng user trong setting
            foreach (var entry in listNvs)
            {
                string userName = entry.Properties[UserPropertiesAd.LoginName].Value.ToString();
                _logger.Information("UPDATE INFO AD USER: " + userName);
                var model = listAlUserName.FirstOrDefault(x => x.TenDangNhap == userName);
                if (model == null)
                {
                    string ouName = "NOT_IN_HRM";
                    if (DirectoryEntry.Exists("LDAP://localhost:389/CN=" + userName + ",OU=" + ouName + "," + RootOuHpl))
                    {
                        //Neu user da ton tai trong OU thi khong lam gi ca
                    }
                    else
                    {
                        _logger.Information("Change OU=" + ouName + " user: " + userName);
                        DirectoryEntry nLocation = new DirectoryEntry("LDAP://localhost:389/OU=" + ouName + "," + RootOuHpl);
                        entry.MoveTo(nLocation);
                        nLocation.Close();
                    }

                    _logger.Information("User not in HRM: " + userName);
                    continue;
                }

                var fixedUsername = entry.Properties[UserPropertiesAd.UserPrincipalName].Value.ToString();
                var userPrincipal = UserPrincipal.FindByIdentity(principalContext, _idType, fixedUsername);

                // Check if the user principal exists
                if (userPrincipal == null)
                {
                    _logger.Warning($"The User principal ({fixedUsername}) doesn't exist");
                    result.Errors = new ApiErrorItem(ApiErrorCode.UserNotFound, "User " + model.TenDangNhap + " không tồn tại trên AD.");

                    listResult.Add(result);
                    continue;
                }

                //Lấy thông tin của User
                var userInfo = new UserInfoAd
                {
                    isLocked = userPrincipal.IsAccountLockedOut(),
                    displayName = userPrincipal.DisplayName,
                    userPrincipalName = userPrincipal.UserPrincipalName,
                    sAMAccountName = userPrincipal.SamAccountName,
                    givenName = userPrincipal.GivenName,
                    name = userPrincipal.Name,
                    sn = userPrincipal.Surname,
                    description = userPrincipal.Description,
                    mail = userPrincipal.EmailAddress,
                    telephoneNumber = userPrincipal.VoiceTelephoneNumber,
                    //otherTelephone = "",
                    //physicalDeliveryOfficeName = "",
                    //initials = "",
                    //wWWHomePage = "",
                    //url = "",
                    //CN = "",
                    //homePhone = "",
                    //mobile = ""
                };

                //var ten = CommonHelper.ConvertToUnSign(model.Ten);
                //var ho = CommonHelper.ConvertToUnSign(model.Ho);
                var ten = model.Ten.Trim();
                var ho = model.Ho.Trim();

                bool checkForUpdate = false;

                //Update lại tên của Nhân sự
                //Fix CN=Ho va Ten
                try
                {
                    if (entry.Properties.Contains(UserPropertiesAd.ContainerName))
                    {
                        //if (!model.TenDangNhap.Equals(directoryEntry.Properties[UserPropertiesAd.LoginName].Value.ToString()))
                        //{
                        //    directoryEntry.Rename("CN=" + ho + " " + ten);
                        //}
                        entry.Rename("CN=" + model.TenDangNhap);
                    }
                    else
                    {
                        //directoryEntry.Rename("CN=" + ho + " " + ten);
                    }

                    result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "Successful");
                }
                catch (Exception e)
                {
                    result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "Lỗi: Update dữ liệu " + model.TenDangNhap + " vào AD không thành công. Error: " + e.Message);
                    listResult.Add(result);
                    continue;
                }

                //Fix HỌ: sn=LastName=Ho; 
                userInfo.sn = ho;
                if (entry.Properties.Contains(UserPropertiesAd.LastName))
                {
                    if (!ho.Equals(entry.Properties[UserPropertiesAd.LastName].Value.ToString()))
                    {
                        entry.Properties[UserPropertiesAd.LastName].Value = ho;
                        checkForUpdate = true;
                    }
                }
                else
                {
                    entry.Properties[UserPropertiesAd.LastName].Value = ho;
                    checkForUpdate = true;
                }

                //Tên: givenName = First name = Ten
                userInfo.givenName = ten;
                if (entry.Properties.Contains(UserPropertiesAd.FirstName))
                {
                    if (!ten.Equals(entry.Properties[UserPropertiesAd.FirstName].Value.ToString()))
                    {
                        entry.Properties[UserPropertiesAd.FirstName].Value = ten;
                        checkForUpdate = true;
                    }
                }
                else
                {
                    entry.Properties[UserPropertiesAd.FirstName].Value = ten;
                    checkForUpdate = true;
                }

                //displayName=(Ho va ten)
                var displayName = ho + " " + ten;
                userInfo.displayName = displayName;
                if (entry.Properties.Contains(UserPropertiesAd.DisplayName))
                {
                    if (!displayName.Equals(entry.Properties[UserPropertiesAd.DisplayName].Value.ToString()))
                    {
                        entry.Properties[UserPropertiesAd.DisplayName].Value = displayName;
                        checkForUpdate = true;
                    }
                }
                else
                {
                    entry.Properties[UserPropertiesAd.DisplayName].Value = displayName;
                    checkForUpdate = true;
                }

                //department: Chi nhánh/Phòng ban
                bool isPbExist = false;
                var pbCap1 = UserService.GetPhongBanCap1CuaNhanVien(model.MaNhanVien);
                if (pbCap1 != null)
                {
                    isPbExist = true;
                    userInfo.department = pbCap1.Ten;
                    if (entry.Properties.Contains(UserPropertiesAd.Department))
                    {
                        if (!pbCap1.Ten.Equals(entry.Properties[UserPropertiesAd.Department].Value.ToString()))
                        {
                            entry.Properties[UserPropertiesAd.Department].Value = pbCap1.Ten;
                            checkForUpdate = true;
                        }
                    }
                    else
                    {
                        entry.Properties[UserPropertiesAd.Department].Value = pbCap1.Ten;
                        checkForUpdate = true;
                    }
                }

                //title = Chức danh
                if (!string.IsNullOrEmpty(model.TenChucDanh))
                {
                    userInfo.title = model.TenChucDanh;
                    if (entry.Properties.Contains(UserPropertiesAd.Title))
                    {
                        if (!model.TenChucDanh.Equals(entry.Properties[UserPropertiesAd.Title].Value.ToString()))
                        {
                            entry.Properties[UserPropertiesAd.Title].Value = model.TenChucDanh;
                            checkForUpdate = true;
                        }
                    }
                    else
                    {
                        entry.Properties[UserPropertiesAd.Title].Value = model.TenChucDanh;
                        checkForUpdate = true;
                    }
                }

                //employeeID mã nhân viên
                userInfo.employeeID = model.MaNhanVien;
                if (entry.Properties.Contains(UserPropertiesAd.EmployeeId))
                {
                    if (!model.MaNhanVien.Equals(entry.Properties[UserPropertiesAd.EmployeeId].Value.ToString()))
                    {
                        entry.Properties[UserPropertiesAd.EmployeeId].Value = model.MaNhanVien;
                        checkForUpdate = true;
                    }
                }
                else
                {
                    entry.Properties[UserPropertiesAd.EmployeeId].Value = model.MaNhanVien;
                    checkForUpdate = true;
                }

                //telephoneNumber=Điện thoại
                try
                {
                    string dt = "+84" + int.Parse(model.DienThoai);
                    userInfo.telephoneNumber = dt;
                    if (entry.Properties.Contains(UserPropertiesAd.TelePhoneNumber))
                    {
                        if (!dt.Equals(entry.Properties[UserPropertiesAd.TelePhoneNumber].Value.ToString()))
                        {
                            entry.Properties[UserPropertiesAd.TelePhoneNumber].Value = dt;
                            checkForUpdate = true;
                        }
                    }
                    else
                    {
                        entry.Properties[UserPropertiesAd.TelePhoneNumber].Value = dt;
                        checkForUpdate = true;
                    }
                }
                catch (Exception)
                {
                    result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "Số điện thoại của " + model.TenDangNhap + " không đúng.");
                    _logger.Error("Số điện thoại của " + model.TenDangNhap + " không đúng.");
                }

                //Email nhân sự
                if (!string.IsNullOrEmpty(model.Email))
                {
                    try
                    {
                        var email = CommonHelper.IsValidEmail(model.Email);
                        userInfo.mail = email;
                        if (entry.Properties.Contains(UserPropertiesAd.EmailAddress))
                        {
                            if (!email.Equals(entry.Properties[UserPropertiesAd.EmailAddress].Value.ToString()))
                            {
                                entry.Properties[UserPropertiesAd.EmailAddress].Value = email;
                                checkForUpdate = true;
                            }
                        }
                        else
                        {
                            entry.Properties[UserPropertiesAd.EmailAddress].Value = email;
                            checkForUpdate = true;
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.Error("Lỗi update email cho user: " + userName + " (" + model.MaNhanVien + "): " + e.Message);
                    }
                }


                //Lấy một số thông tin mà HRM không có
                //mobile
                if (entry.Properties.Contains(UserPropertiesAd.Mobile))
                {
                    userInfo.mobile = entry.Properties[UserPropertiesAd.Mobile].Value.ToString();
                }
                //homePhone
                if (entry.Properties.Contains(UserPropertiesAd.Homephone))
                {
                    userInfo.homePhone = entry.Properties[UserPropertiesAd.Homephone].Value.ToString();
                }
                //cn
                if (entry.Properties.Contains(UserPropertiesAd.ContainerName))
                {
                    userInfo.CN = entry.Properties[UserPropertiesAd.ContainerName].Value.ToString();
                }

                //prop.Value = -1;
                //directoryEntry.Properties[UserPropertiesAd.Department].Value = "Day la phong ban moi";
                //directoryEntry.CommitChanges();

                if (checkForUpdate)
                {
                    try
                    {
                        entry.CommitChanges();
                        result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "Successful");
                    }
                    catch (Exception e)
                    {
                        result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "Lỗi: Update dữ liệu " + model.TenDangNhap + " vào AD không thành công. Error: " + e.Message);
                    }
                }
                else
                {
                    result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "User " + model.TenDangNhap + " không có thông tin cần update.");
                }

                result.UserInfo = userInfo;

                listResult.Add(result);

                //CHANGE OU CUA USER
                if (isPbExist)
                {
                    string ouName = CommonHelper.ConvertToUnSign(pbCap1.Ten);
                    try
                    {
                        if (DirectoryEntry.Exists("LDAP://localhost:389/CN=" + userName + ",OU=" + ouName + "," + RootOuHpl))
                        {

                        }
                        else
                        {
                            DirectoryEntry nLocation = new DirectoryEntry("LDAP://localhost:389/OU=" + ouName + "," + RootOuHpl);
                            entry.MoveTo(nLocation);
                            nLocation.Close();
                            _logger.Information("Change OU=" + ouName + " user: " + userName);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.Error("Lỗi change OU=" + ouName + " user: " + userName + " ==> " + e);
                    }
                }
                else
                {
                    try
                    {
                        if (!DirectoryEntry.Exists("LDAP://localhost:389/CN=" + userName + ",OU=HAI PHAT LAND," + RootOuHpl))
                        {
                            DirectoryEntry nLocation = new DirectoryEntry("LDAP://localhost:389/OU=HAI PHAT LAND," + RootOuHpl);
                            entry.MoveTo(nLocation);
                            nLocation.Close();
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.Error("Lỗi change OU=HAI PHAT LAND user: " + userName + " ==> " + e);
                    }
                }

                entry.Close();
            }

            return listResult;
        }

        public void UpdateUserInfoAd(NhanVienViewModel model)
        {
            _logger.Information("START PasswordChangeProvider.UpdateUserInfoAd(model)");

            using var principalContext = AcquirePrincipalContext();
            if (principalContext == null)
            {
                _logger.Error("Không kết nối được đến server AD.");
                return;
            }

            // Check if the user principal exists
            var fixedUsername = FixUsernameWithDomain(model.TenDangNhap);
            var userPrincipal = UserPrincipal.FindByIdentity(principalContext, _idType, fixedUsername);
            if (userPrincipal == null)
            {
                _logger.Error($"The User principal ({fixedUsername}) doesn't exist");
                _logger.Error("User " + model.TenDangNhap + " không tồn tại trên AD.");
                //TẠO MỚI USER
                //Lấy thông tin của User
                string hoTen = model.Ho + " " + model.Ten;
                var userCreate = new UserInfoAd
                {
                    isLocked = false,
                    displayName = hoTen,
                    userPrincipalName = fixedUsername,
                    sAMAccountName = model.TenDangNhap,
                    givenName = model.Ten,
                    name = model.TenDangNhap,
                    sn = model.Ho,
                    description = "Create by tool " + DateTime.Now.ToString("G"),
                    mail = model.TenDangNhap + "@haiphatland.com.vn",
                    telephoneNumber = model.DienThoai,
                    title = model.TenChucDanh
                };
                var newUser = CreateAdUser(userCreate, "Hpl@123");
                return;
            }

            try
            {
                if (userPrincipal.Enabled is null or false)
                {
                    userPrincipal.Enabled = true;
                    userPrincipal.Save();
                }
            }
            catch (Exception e)
            {
                _logger.Error("Lỗi Enable user: " + e);
            }

            var entry = userPrincipal.GetUnderlyingObject() as DirectoryEntry;
            if (entry == null)
            {
                _logger.Error("User " + model.TenDangNhap + " không tồn tại trên AD.");
                return;
            }

            string userName = entry.Properties[UserPropertiesAd.LoginName].Value.ToString();
            _logger.Information("UPDATE INFO AD USER: " + userName);

            //Lấy thông tin của User
            var userInfo = new UserInfoAd
            {
                isLocked = userPrincipal.IsAccountLockedOut(),
                displayName = userPrincipal.DisplayName,
                userPrincipalName = userPrincipal.UserPrincipalName,
                sAMAccountName = userPrincipal.SamAccountName,
                givenName = userPrincipal.GivenName,
                name = userPrincipal.Name,
                sn = userPrincipal.Surname,
                description = userPrincipal.Description,
                mail = userPrincipal.EmailAddress,
                telephoneNumber = userPrincipal.VoiceTelephoneNumber,
            };

            var ten = model.Ten.Trim();
            var ho = model.Ho.Trim();
            bool checkForUpdate = false;

            //Update lại tên của Nhân sự
            //Fix CN=Ho va Ten
            try
            {
                if (entry.Properties.Contains(UserPropertiesAd.ContainerName))
                {
                    entry.Rename("CN=" + model.TenDangNhap);
                }
            }
            catch (Exception e)
            {
                _logger.Error(e.Message);
            }

            //Fix HỌ: sn=LastName=Ho; 
            userInfo.sn = ho;
            if (entry.Properties.Contains(UserPropertiesAd.LastName))
            {
                if (!ho.Equals(entry.Properties[UserPropertiesAd.LastName].Value.ToString()))
                {
                    entry.Properties[UserPropertiesAd.LastName].Value = ho;
                    checkForUpdate = true;
                }
            }
            else
            {
                entry.Properties[UserPropertiesAd.LastName].Value = ho;
                checkForUpdate = true;
            }

            //Tên: givenName = First name = Ten
            userInfo.givenName = ten;
            if (entry.Properties.Contains(UserPropertiesAd.FirstName))
            {
                if (!ten.Equals(entry.Properties[UserPropertiesAd.FirstName].Value.ToString()))
                {
                    entry.Properties[UserPropertiesAd.FirstName].Value = ten;
                    checkForUpdate = true;
                }
            }
            else
            {
                entry.Properties[UserPropertiesAd.FirstName].Value = ten;
                checkForUpdate = true;
            }

            //displayName=(Ho va ten)
            var displayName = ho + " " + ten;
            userInfo.displayName = displayName;
            if (entry.Properties.Contains(UserPropertiesAd.DisplayName))
            {
                if (!displayName.Equals(entry.Properties[UserPropertiesAd.DisplayName].Value.ToString()))
                {
                    entry.Properties[UserPropertiesAd.DisplayName].Value = displayName;
                    checkForUpdate = true;
                }
            }
            else
            {
                entry.Properties[UserPropertiesAd.DisplayName].Value = displayName;
                checkForUpdate = true;
            }

            //department: Chi nhánh/Phòng ban
            bool isPbExist = false;
            var pbCap1 = UserService.GetPhongBanCap1CuaNhanVien(model.MaNhanVien);
            if (pbCap1 != null)
            {
                isPbExist = true;
                userInfo.department = pbCap1.Ten;
                if (entry.Properties.Contains(UserPropertiesAd.Department))
                {
                    if (!pbCap1.Ten.Equals(entry.Properties[UserPropertiesAd.Department].Value.ToString()))
                    {
                        entry.Properties[UserPropertiesAd.Department].Value = pbCap1.Ten;
                        checkForUpdate = true;
                    }
                }
                else
                {
                    entry.Properties[UserPropertiesAd.Department].Value = pbCap1.Ten;
                    checkForUpdate = true;
                }
            }

            //title = Chức danh
            if (!string.IsNullOrEmpty(model.TenChucDanh))
            {
                userInfo.title = model.TenChucDanh;
                if (entry.Properties.Contains(UserPropertiesAd.Title))
                {
                    if (!model.TenChucDanh.Equals(entry.Properties[UserPropertiesAd.Title].Value.ToString()))
                    {
                        entry.Properties[UserPropertiesAd.Title].Value = model.TenChucDanh;
                        checkForUpdate = true;
                    }
                }
                else
                {
                    entry.Properties[UserPropertiesAd.Title].Value = model.TenChucDanh;
                    checkForUpdate = true;
                }
            }

            //employeeID mã nhân viên
            userInfo.employeeID = model.MaNhanVien;
            if (entry.Properties.Contains(UserPropertiesAd.EmployeeId))
            {
                if (!model.MaNhanVien.Equals(entry.Properties[UserPropertiesAd.EmployeeId].Value.ToString()))
                {
                    entry.Properties[UserPropertiesAd.EmployeeId].Value = model.MaNhanVien;
                    checkForUpdate = true;
                }
            }
            else
            {
                entry.Properties[UserPropertiesAd.EmployeeId].Value = model.MaNhanVien;
                checkForUpdate = true;
            }

            //telephoneNumber=Điện thoại
            try
            {
                string dt = "+84" + int.Parse(model.DienThoai);
                userInfo.telephoneNumber = dt;
                if (entry.Properties.Contains(UserPropertiesAd.TelePhoneNumber))
                {
                    if (!dt.Equals(entry.Properties[UserPropertiesAd.TelePhoneNumber].Value.ToString()))
                    {
                        entry.Properties[UserPropertiesAd.TelePhoneNumber].Value = dt;
                        checkForUpdate = true;
                    }
                }
                else
                {
                    entry.Properties[UserPropertiesAd.TelePhoneNumber].Value = dt;
                    checkForUpdate = true;
                }
            }
            catch (Exception)
            {
                _logger.Error("Số điện thoại của " + model.TenDangNhap + " không đúng.");
            }

            //Email nhân sự
            if (!string.IsNullOrEmpty(model.Email))
            {
                try
                {
                    var email = CommonHelper.IsValidEmail(model.Email);
                    userInfo.mail = email;
                    if (entry.Properties.Contains(UserPropertiesAd.EmailAddress))
                    {
                        if (!email.Equals(entry.Properties[UserPropertiesAd.EmailAddress].Value.ToString()))
                        {
                            entry.Properties[UserPropertiesAd.EmailAddress].Value = email;
                            checkForUpdate = true;
                        }
                    }
                    else
                    {
                        entry.Properties[UserPropertiesAd.EmailAddress].Value = email;
                        checkForUpdate = true;
                    }
                }
                catch (Exception e)
                {
                    _logger.Error("Lỗi update email cho user: " + userName + " (" + model.MaNhanVien + "): " + e.Message);
                }
            }


            //Lấy một số thông tin mà HRM không có
            //mobile
            if (entry.Properties.Contains(UserPropertiesAd.Mobile))
            {
                userInfo.mobile = entry.Properties[UserPropertiesAd.Mobile].Value.ToString();
            }
            //homePhone
            if (entry.Properties.Contains(UserPropertiesAd.Homephone))
            {
                userInfo.homePhone = entry.Properties[UserPropertiesAd.Homephone].Value.ToString();
            }
            //cn
            if (entry.Properties.Contains(UserPropertiesAd.ContainerName))
            {
                userInfo.CN = entry.Properties[UserPropertiesAd.ContainerName].Value.ToString();
            }

            //prop.Value = -1;
            //directoryEntry.Properties[UserPropertiesAd.Department].Value = "Day la phong ban moi";
            //directoryEntry.CommitChanges();

            if (checkForUpdate)
            {
                try
                {
                    entry.CommitChanges();
                }
                catch (Exception e)
                {
                    _logger.Error("Lỗi: Update dữ liệu " + model.TenDangNhap + " vào AD không thành công. Error: " + e.Message);
                }
            }
            else
            {
                _logger.Error("User " + model.TenDangNhap + " không có thông tin cần update.");
            }

            //CHANGE OU CUA USER
            if (isPbExist)
            {
                string ouName = CommonHelper.ConvertToUnSign(pbCap1.Ten);
                try
                {
                    if (!DirectoryEntry.Exists("LDAP://localhost:389/CN=" + userName + ",OU=" + ouName + "," + RootOuHpl))
                    {
                        DirectoryEntry nLocation = new DirectoryEntry("LDAP://localhost:389/OU=" + ouName + "," + RootOuHpl);
                        entry.MoveTo(nLocation);
                        nLocation.Close();
                    }
                }
                catch (Exception e)
                {
                    _logger.Error("Lỗi change OU=" + ouName + " user: " + userName + " ==> " + e);
                }
            }
            else
            {
                try
                {
                    if (!DirectoryEntry.Exists("LDAP://localhost:389/CN=" + userName + ",OU=HAI PHAT LAND," + RootOuHpl))
                    {
                        DirectoryEntry nLocation = new DirectoryEntry("LDAP://localhost:389/OU=HAI PHAT LAND," + RootOuHpl);
                        entry.MoveTo(nLocation);
                        nLocation.Close();
                    }
                }
                catch (Exception e)
                {
                    _logger.Error("Lỗi change OU=HAI PHAT LAND user: " + userName + " ==> " + e);
                }
            }

            entry.Close();
        }

        public void CreateOu(string ouName)
        {
            DirectoryEntry objADAM;  // Binding object.
            DirectoryEntry objOU;    // Organizational unit.
            string strDescription;   // Description of OU.
            string strOU;            // Organiztional unit.
            string strPath;          // Binding path.

            //Construct the binding string. OU=Company Structure,DC=baonx,DC=com
            strPath = "LDAP://localhost:389/" + RootOuHpl;

            Console.WriteLine("Bind to: {0}", strPath);

            // Get AD LDS object.
            try
            {
                objADAM = new DirectoryEntry(strPath);
                objADAM.RefreshCache();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error:   Bind failed.");
                Console.WriteLine("         {0}", e.Message);
                return;
            }

            // Specify Organizational Unit.
            strOU = "OU=TestOU";
            strDescription = "AD LDS Test Organizational Unit";
            Console.WriteLine("Create:  {0}", strOU);

            // Create Organizational Unit.
            try
            {
                if (DirectoryEntry.Exists("LDAP://localhost:389/OU=" + ouName + "," + RootOuHpl))
                {
                    // ......
                    var baonx = 1;
                }

                objOU = objADAM.Children.Add(strOU, "OrganizationalUnit");
                objOU.Properties["description"].Add(strDescription);
                objOU.CommitChanges();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error:   Create failed.");
                Console.WriteLine("         {0}", e.Message);
                return;
            }

            // Output Organizational Unit attributes.
            Console.WriteLine("Success: Create succeeded.");
            Console.WriteLine("Name:    {0}", objOU.Name);
            Console.WriteLine("         {0}", objOU.Properties["description"].Value);
            return;
        }

        public void CreateAllOuHpl()
        {
            var listPbs = UserService.GetAllHplPhongBan();
            DirectoryEntry objADAM;  // Binding object.
            DirectoryEntry objOU;    // Organizational unit.
            string strDescription;   // Description of OU.
            string ouName;            // Organiztional unit.
            string strPath;          // Binding path.

            //Construct the binding string. OU=Company Structure,DC=baonx,DC=com
            strPath = "LDAP://localhost:389/" + RootOuHpl;

            // Get AD LDS object.
            try
            {
                objADAM = new DirectoryEntry(strPath);
                objADAM.RefreshCache();
            }
            catch (Exception e)
            {
                _logger.Error("Loi tao OU: " + e.Message);
                return;
            }

            //Tao mac dinh OU HAI PHAT LAND
            if (!DirectoryEntry.Exists("LDAP://localhost:389/OU=HAI PHAT LAND," + RootOuHpl))
            {
                objOU = objADAM.Children.Add("OU=HAI PHAT LAND", "OrganizationalUnit");
                objOU.Properties["description"].Add("OU mac dinh");
                objOU.CommitChanges();
                _logger.Error("Tao thanh cong OU: HAI PHAT LAND");
            }

            //Tao mac dinh OU NOT_IN_HRM
            if (!DirectoryEntry.Exists("LDAP://localhost:389/OU=NOT_IN_HRM," + RootOuHpl))
            {
                objOU = objADAM.Children.Add("OU=NOT_IN_HRM", "OrganizationalUnit");
                objOU.Properties["description"].Add("OU mac dinh");
                objOU.CommitChanges();
                _logger.Error("Tao thanh cong OU: NOT_IN_HRM");
            }

            foreach (var pb in listPbs)
            {
                ouName = CommonHelper.ConvertToUnSign(pb.TenPhongBan);

                // Specify Organizational Unit.
                strDescription = pb.TenPhongBan + " (" + pb.MaPhongBan + ")";

                // Create Organizational Unit.
                try
                {
                    if (!DirectoryEntry.Exists("LDAP://localhost:389/OU=" + ouName + "," + RootOuHpl))
                    {
                        objOU = objADAM.Children.Add("OU=" + ouName, "OrganizationalUnit");
                        objOU.Properties["description"].Add(strDescription);
                        objOU.CommitChanges();
                        _logger.Error("Tao thanh cong OU: " + ouName);
                    }
                }
                catch (Exception e)
                {
                    _logger.Error("Error create OU: " + ouName + ": " + e.Message);
                }
            }
        }

        public void ChangeOuForUser(string oldOu, string newOu)
        {
            //Remove user from OU
            if (DirectoryEntry.Exists("LDAP://localhost:389/OU=" + oldOu + "," + RootOuHpl))
            {
                // ......
                var baonx = 1;
                DirectoryEntry eLocation = new DirectoryEntry("LDAP://");
                DirectoryEntry nLocation = new DirectoryEntry("LDAP://");
                eLocation.MoveTo(nLocation);
                nLocation.Close();
                eLocation.Close();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userDn">the distinguishedName of the user: CN=user, OU=USERS, DC=contoso, DC=com</param>
        /// <param name="groupDn"> the distinguishedName of the group: CN=group,OU=GROUPS,DC=contoso,DC=com</param>
        public void AddUserToOu(string userDn, string strOu)
        {
            try
            {
                DirectoryEntry dirEntry = new DirectoryEntry("LDAP://localhost:389/" + strOu + ",");
                dirEntry.Properties["member"].Add(userDn);
                dirEntry.CommitChanges();
                dirEntry.Close();
            }
            catch (System.DirectoryServices.DirectoryServicesCOMException E)
            {
                //doSomething with E.Message.ToString();
            }
        }

        public string DisableUser(string userName)
        {
            string strOut = "N/A";
            try
            {
                using var principalContext = AcquirePrincipalContext();
                var fixedUsername = FixUsernameWithDomain(userName);
                var userPrincipal = UserPrincipal.FindByIdentity(principalContext, _idType, fixedUsername);

                //DirectoryEntry dirEntry = new DirectoryEntry(user.distinguishedName);
                if (userPrincipal != null)
                {
                    DirectoryEntry dirEntry = (DirectoryEntry)userPrincipal.GetUnderlyingObject();
                    int val = (int)dirEntry.Properties["userAccountControl"].Value;
                    dirEntry.Properties["userAccountControl"].Value = val | 0x2;
                    //dirEntry.Properties["IsAccountLocked"].Value = false;

                    //ADS_UF_ACCOUNTDISABLE;
                    dirEntry.CommitChanges();
                    dirEntry.Close();

                    strOut = "Đã disable";
                }
                else
                {
                    _logger.Information("User " + userName + " không tồn tại.");
                    strOut = "Không tồn tại";
                }
            }
            catch (DirectoryServicesCOMException e)
            {
                //DoSomethingWith --> E.Message.ToString();
                _logger.Error("Lỗi DisableUser: " + e);
                strOut = "Lỗi: " + e.Message;
            }

            return strOut;
        }

        public string ReactiveUser(string username)
        {
            string strOut = "N/A";
            try
            {
                using var principalContext = AcquirePrincipalContext();
                var fixedUsername = FixUsernameWithDomain(username);
                var userPrincipal = UserPrincipal.FindByIdentity(principalContext, _idType, fixedUsername);

                //DirectoryEntry dirEntry = new DirectoryEntry(user.distinguishedName);
                if (userPrincipal != null)
                {
                    DirectoryEntry dirEntry = (DirectoryEntry)userPrincipal.GetUnderlyingObject();
                    int val = (int)dirEntry.Properties["userAccountControl"].Value;
                    dirEntry.Properties["userAccountControl"].Value = 512;//ENABLE user
                    //dirEntry.Properties["userAccountControl"].Value = 514;//DISABLE user

                    //ADS_UF_ACCOUNTDISABLE;
                    dirEntry.CommitChanges();
                    dirEntry.Close();

                    dirEntry.Invoke("SetPassword", "Hpl@123");
                    dirEntry.CommitChanges();

                    strOut = "Đã ENABLE";
                }
                else
                {
                    _logger.Information("User " + username + " không tồn tại.");
                    strOut = "Không tồn tại";
                }
            }
            catch (DirectoryServicesCOMException e)
            {
                //DoSomethingWith --> E.Message.ToString();
                _logger.Error("Lỗi DisableUser: " + e);
                strOut = "Lỗi: " + e.Message;
            }

            return strOut;
        }

        public void CreateOrUpdateAdUser(NhanVienViewModel2 user, string pw)
        {
            _logger.Information("PasswordChangeProvider.CreateOrUpdateAdUser");

            string username = user.TenDangNhap;
            var principalContext = AcquirePrincipalContext();

            string fixedUsername = FixUsernameWithDomain(username);
            var up = UserPrincipal.FindByIdentity(principalContext, _idType, fixedUsername);
            if (up != null)
            {
                //CẬP NHẬT LẠI USER NÀY
                UpdateAdUser(user, pw);
            }
            else
            {
                //TẠO MỚI USER NÀY
                CreateAdUser2(user, pw);
            }
        }

        public void CreateOrUpdateAdUser2(GetAllNhanVienTheoListMaNvReturnModel user, string pw)
        {
            _logger.Information("PasswordChangeProvider.CreateOrUpdateAdUser2");

            string username = user.TenDangNhap;
            var principalContext = AcquirePrincipalContext();

            string fixedUsername = FixUsernameWithDomain(username);
            var up = UserPrincipal.FindByIdentity(principalContext, _idType, fixedUsername);
            if (up != null)
            {
                //CẬP NHẬT LẠI USER NÀY
                UpdateAdUser2(user, pw);
            }
            else
            {
                //TẠO MỚI USER NÀY
                CreateAdUser3(user, pw);
            }
        }

        public void UpdateAdUser(NhanVienViewModel2 model, string pw)
        {
            var result = new ApiResultAd();

            string username = model.TenDangNhap;
            var principalContext = AcquirePrincipalContext();

            string fixedUsername = FixUsernameWithDomain(username);

            var userPrincipal = UserPrincipal.FindByIdentity(principalContext, _idType, fixedUsername);

            // Check if the user principal exists
            if (userPrincipal == null)
            {
                _logger.Warning($"The User principal ({fixedUsername}) doesn't exist");
                result.Errors = new ApiErrorItem(ApiErrorCode.UserNotFound, "User " + username + " không tồn tại trên AD.");
            }
            var entry = (DirectoryEntry)userPrincipal.GetUnderlyingObject();

            var ten = model.Ten.Trim();
            var ho = model.Ho.Trim();

            bool checkForUpdate = false;

            //Update lại tên của Nhân sự
            //Fix CN=Ho va Ten
            try
            {
                if (entry.Properties.Contains(UserPropertiesAd.ContainerName))
                {
                    entry.Rename("CN=" + model.TenDangNhap);
                }
                else
                {
                    //directoryEntry.Rename("CN=" + ho + " " + ten);
                }

                result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "Successful");
            }
            catch (Exception e)
            {
                result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "Lỗi: Update dữ liệu " + model.TenDangNhap + " vào AD không thành công. Error: " + e.Message);
            }

            //Fix HỌ: sn=LastName=Ho; 
            //userInfo.sn = ho;
            if (entry.Properties.Contains(UserPropertiesAd.LastName))
            {
                if (!ho.Equals(entry.Properties[UserPropertiesAd.LastName].Value.ToString()))
                {
                    entry.Properties[UserPropertiesAd.LastName].Value = ho;
                    checkForUpdate = true;
                }
            }
            else
            {
                entry.Properties[UserPropertiesAd.LastName].Value = ho;
                checkForUpdate = true;
            }

            //Tên: givenName = First name = Ten
            //userInfo.givenName = ten;
            if (entry.Properties.Contains(UserPropertiesAd.FirstName))
            {
                if (!ten.Equals(entry.Properties[UserPropertiesAd.FirstName].Value.ToString()))
                {
                    entry.Properties[UserPropertiesAd.FirstName].Value = ten;
                    checkForUpdate = true;
                }
            }
            else
            {
                entry.Properties[UserPropertiesAd.FirstName].Value = ten;
                checkForUpdate = true;
            }

            //displayName=(Ho va ten)
            var displayName = ho + " " + ten;
            //userInfo.displayName = displayName;
            if (entry.Properties.Contains(UserPropertiesAd.DisplayName))
            {
                if (!displayName.Equals(entry.Properties[UserPropertiesAd.DisplayName].Value.ToString()))
                {
                    entry.Properties[UserPropertiesAd.DisplayName].Value = displayName;
                    checkForUpdate = true;
                }
            }
            else
            {
                entry.Properties[UserPropertiesAd.DisplayName].Value = displayName;
                checkForUpdate = true;
            }

            //department: Chi nhánh/Phòng ban
            if (entry.Properties.Contains(UserPropertiesAd.Department))
            {
                if (model.TenPhongBanCap1 != null)
                {
                    if (!model.TenPhongBanCap1.Equals(entry.Properties[UserPropertiesAd.Department].Value.ToString()))
                    {
                        entry.Properties[UserPropertiesAd.Department].Value = model.TenPhongBanCap1;
                        checkForUpdate = true;
                    }
                }
            }
            else
            {
                entry.Properties[UserPropertiesAd.Department].Value = model.TenPhongBanCap1;
                checkForUpdate = true;
            }

            //title = Chức danh
            if (!string.IsNullOrEmpty(model.TenChucDanh))
            {
                if (entry.Properties.Contains(UserPropertiesAd.Title))
                {
                    if (!model.TenChucDanh.Equals(entry.Properties[UserPropertiesAd.Title].Value.ToString()))
                    {
                        entry.Properties[UserPropertiesAd.Title].Value = model.TenChucDanh;
                        checkForUpdate = true;
                    }
                }
                else
                {
                    entry.Properties[UserPropertiesAd.Title].Value = model.TenChucDanh;
                    checkForUpdate = true;
                }
            }

            //employeeID mã nhân viên
            if (entry.Properties.Contains(UserPropertiesAd.EmployeeId))
            {
                if (!string.IsNullOrEmpty(model.MaNhanVien))
                {
                    if (!model.MaNhanVien.Equals(entry.Properties[UserPropertiesAd.EmployeeId].Value.ToString()))
                    {
                        entry.Properties[UserPropertiesAd.EmployeeId].Value = model.MaNhanVien;
                        checkForUpdate = true;
                    }
                }
            }
            else
            {
                entry.Properties[UserPropertiesAd.EmployeeId].Value = model.MaNhanVien;
                checkForUpdate = true;
            }

            //telephoneNumber=Điện thoại
            try
            {
                string dt = model.DienThoai;
                if (entry.Properties.Contains(UserPropertiesAd.TelePhoneNumber))
                {
                    if (!dt.Equals(entry.Properties[UserPropertiesAd.TelePhoneNumber].Value.ToString()))
                    {
                        entry.Properties[UserPropertiesAd.TelePhoneNumber].Value = dt;
                        checkForUpdate = true;
                    }
                }
                else
                {
                    entry.Properties[UserPropertiesAd.TelePhoneNumber].Value = dt;
                    checkForUpdate = true;
                }
            }
            catch (Exception)
            {
                result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "Số điện thoại của " + model.TenDangNhap + " không đúng.");
                _logger.Error("Số điện thoại của " + model.TenDangNhap + " không đúng.");
            }

            //Email nhân sự
            if (!string.IsNullOrEmpty(model.Email))
            {
                try
                {
                    var email = CommonHelper.IsValidEmail(model.Email);
                    if (entry.Properties.Contains(UserPropertiesAd.EmailAddress))
                    {
                        if (!email.Equals(entry.Properties[UserPropertiesAd.EmailAddress].Value.ToString()))
                        {
                            entry.Properties[UserPropertiesAd.EmailAddress].Value = email;
                            checkForUpdate = true;
                        }
                    }
                    else
                    {
                        entry.Properties[UserPropertiesAd.EmailAddress].Value = email;
                        checkForUpdate = true;
                    }
                }
                catch (Exception e)
                {
                    _logger.Error("Lỗi update email cho user: " + username + " (" + model.MaNhanVien + "): " + e.Message);
                }
            }

            if (checkForUpdate)
            {
                try
                {
                    entry.CommitChanges();
                    result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "Successful");
                }
                catch (Exception e)
                {
                    result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "Lỗi: Update dữ liệu " + model.TenDangNhap + " vào AD không thành công. Error: " + e.Message);
                }
            }
            else
            {
                result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "User " + model.TenDangNhap + " không có thông tin cần update.");
            }

            //CHANGE OU CUA USER
            string ouName = CommonHelper.ConvertToUnSign(model.TenPhongBanCap1);
            try
            {
                if (DirectoryEntry.Exists("LDAP://localhost:389/CN=" + username + ",OU=" + ouName + "," + RootOuHpl))
                {

                }
                else
                {
                    DirectoryEntry nLocation = new DirectoryEntry("LDAP://localhost:389/OU=" + ouName + "," + RootOuHpl);
                    entry.MoveTo(nLocation);
                    nLocation.Close();
                    _logger.Information("Change OU=" + ouName + " user: " + username);
                }
            }
            catch (Exception e)
            {
                _logger.Error("Lỗi change OU=" + ouName + " user: " + username + " ==> " + e);
            }

            entry.Close();
        }

        public void UpdateAdUser2(GetAllNhanVienTheoListMaNvReturnModel model, string pw)
        {
            var result = new ApiResultAd();

            string username = model.TenDangNhap;
            var principalContext = AcquirePrincipalContext();

            string fixedUsername = FixUsernameWithDomain(username);

            var userPrincipal = UserPrincipal.FindByIdentity(principalContext, _idType, fixedUsername);

            // Check if the user principal exists
            if (userPrincipal == null)
            {
                _logger.Warning($"The User principal ({fixedUsername}) doesn't exist");
                result.Errors = new ApiErrorItem(ApiErrorCode.UserNotFound, "User " + username + " không tồn tại trên AD.");
            }
            var entry = (DirectoryEntry)userPrincipal.GetUnderlyingObject();

            var ten = model.HoTen.Trim();
            var ho = model.Ho.Trim();

            bool checkForUpdate = false;

            //Update lại tên của Nhân sự
            //Fix CN=Ho va Ten
            try
            {
                if (entry.Properties.Contains(UserPropertiesAd.ContainerName))
                {
                    entry.Rename("CN=" + model.TenDangNhap);
                }
                else
                {
                    //directoryEntry.Rename("CN=" + ho + " " + ten);
                }

                result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "Successful");
            }
            catch (Exception e)
            {
                result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "Lỗi: Update dữ liệu " + model.TenDangNhap + " vào AD không thành công. Error: " + e.Message);
            }

            //Fix HỌ: sn=LastName=Ho; 
            //userInfo.sn = ho;
            if (entry.Properties.Contains(UserPropertiesAd.LastName))
            {
                if (!ho.Equals(entry.Properties[UserPropertiesAd.LastName].Value.ToString()))
                {
                    entry.Properties[UserPropertiesAd.LastName].Value = ho;
                    checkForUpdate = true;
                }
            }
            else
            {
                entry.Properties[UserPropertiesAd.LastName].Value = ho;
                checkForUpdate = true;
            }

            //Tên: givenName = First name = Ten
            //userInfo.givenName = ten;
            if (entry.Properties.Contains(UserPropertiesAd.FirstName))
            {
                if (!ten.Equals(entry.Properties[UserPropertiesAd.FirstName].Value.ToString()))
                {
                    entry.Properties[UserPropertiesAd.FirstName].Value = ten;
                    checkForUpdate = true;
                }
            }
            else
            {
                entry.Properties[UserPropertiesAd.FirstName].Value = ten;
                checkForUpdate = true;
            }

            //displayName=(Ho va ten)
            var displayName = ho + " " + ten;
            //userInfo.displayName = displayName;
            if (entry.Properties.Contains(UserPropertiesAd.DisplayName))
            {
                if (!displayName.Equals(entry.Properties[UserPropertiesAd.DisplayName].Value.ToString()))
                {
                    entry.Properties[UserPropertiesAd.DisplayName].Value = displayName;
                    checkForUpdate = true;
                }
            }
            else
            {
                entry.Properties[UserPropertiesAd.DisplayName].Value = displayName;
                checkForUpdate = true;
            }

            //department: Chi nhánh/Phòng ban
            if (entry.Properties.Contains(UserPropertiesAd.Department))
            {
                if (model.TenPhongBanCap1 != null)
                {
                    if (!model.TenPhongBanCap1.Equals(entry.Properties[UserPropertiesAd.Department].Value.ToString()))
                    {
                        entry.Properties[UserPropertiesAd.Department].Value = model.TenPhongBanCap1;
                        checkForUpdate = true;
                    }
                }
            }
            else
            {
                entry.Properties[UserPropertiesAd.Department].Value = model.TenPhongBanCap1;
                checkForUpdate = true;
            }

            //title = Chức danh
            if (!string.IsNullOrEmpty(model.TenChucDanh))
            {
                if (entry.Properties.Contains(UserPropertiesAd.Title))
                {
                    if (!model.TenChucDanh.Equals(entry.Properties[UserPropertiesAd.Title].Value.ToString()))
                    {
                        entry.Properties[UserPropertiesAd.Title].Value = model.TenChucDanh;
                        checkForUpdate = true;
                    }
                }
                else
                {
                    entry.Properties[UserPropertiesAd.Title].Value = model.TenChucDanh;
                    checkForUpdate = true;
                }
            }

            //employeeID mã nhân viên
            if (entry.Properties.Contains(UserPropertiesAd.EmployeeId))
            {
                if (!string.IsNullOrEmpty(model.MaNhanVien))
                {
                    if (!model.MaNhanVien.Equals(entry.Properties[UserPropertiesAd.EmployeeId].Value.ToString()))
                    {
                        entry.Properties[UserPropertiesAd.EmployeeId].Value = model.MaNhanVien;
                        checkForUpdate = true;
                    }
                }
            }
            else
            {
                entry.Properties[UserPropertiesAd.EmployeeId].Value = model.MaNhanVien;
                checkForUpdate = true;
            }

            //telephoneNumber=Điện thoại
            try
            {
                string dt = model.DienThoai;
                if (entry.Properties.Contains(UserPropertiesAd.TelePhoneNumber))
                {
                    if (!dt.Equals(entry.Properties[UserPropertiesAd.TelePhoneNumber].Value.ToString()))
                    {
                        entry.Properties[UserPropertiesAd.TelePhoneNumber].Value = dt;
                        checkForUpdate = true;
                    }
                }
                else
                {
                    entry.Properties[UserPropertiesAd.TelePhoneNumber].Value = dt;
                    checkForUpdate = true;
                }
            }
            catch (Exception)
            {
                result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "Số điện thoại của " + model.TenDangNhap + " không đúng.");
                _logger.Error("Số điện thoại của " + model.TenDangNhap + " không đúng.");
            }

            //Email nhân sự
            if (!string.IsNullOrEmpty(model.Email))
            {
                try
                {
                    var email = CommonHelper.IsValidEmail(model.Email);
                    if (entry.Properties.Contains(UserPropertiesAd.EmailAddress))
                    {
                        if (!email.Equals(entry.Properties[UserPropertiesAd.EmailAddress].Value.ToString()))
                        {
                            entry.Properties[UserPropertiesAd.EmailAddress].Value = email;
                            checkForUpdate = true;
                        }
                    }
                    else
                    {
                        entry.Properties[UserPropertiesAd.EmailAddress].Value = email;
                        checkForUpdate = true;
                    }
                }
                catch (Exception e)
                {
                    _logger.Error("Lỗi update email cho user: " + username + " (" + model.MaNhanVien + "): " + e.Message);
                }
            }

            if (checkForUpdate)
            {
                try
                {
                    entry.CommitChanges();
                    result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "Successful");
                }
                catch (Exception e)
                {
                    result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "Lỗi: Update dữ liệu " + model.TenDangNhap + " vào AD không thành công. Error: " + e.Message);
                }
            }
            else
            {
                result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "User " + model.TenDangNhap + " không có thông tin cần update.");
            }

            //CHANGE OU CUA USER
            string ouName = CommonHelper.ConvertToUnSign(model.TenPhongBanCap1);
            try
            {
                if (DirectoryEntry.Exists("LDAP://localhost:389/CN=" + username + ",OU=" + ouName + "," + RootOuHpl))
                {

                }
                else
                {
                    DirectoryEntry nLocation = new DirectoryEntry("LDAP://localhost:389/OU=" + ouName + "," + RootOuHpl);
                    entry.MoveTo(nLocation);
                    nLocation.Close();
                    _logger.Information("Change OU=" + ouName + " user: " + username);
                }
            }
            catch (Exception e)
            {
                _logger.Error("Lỗi change OU=" + ouName + " user: " + username + " ==> " + e);
            }

            entry.Close();
        }

        public void CreateAdUser2(NhanVienViewModel2 user, string pw)
        {
            _logger.Information("START CALL PasswordChangeProvider.CreateAdUser2");

            var result = new ApiResultAd();

            string username = user.TenDangNhap;
            _logger.Information(username + " START create USER on AD at " + DateTime.Now.ToString("G"));

            var principalContext = AcquirePrincipalContext();
            string fixedUsername = FixUsernameWithDomain(username);

            //Cach 1 Create User
            //OU: OU=Company Structure,DC=baonx,DC=com
            //"LDAP://OU=Company Structure,DC=baonx,DC=com";
            string pbCap1 = CommonHelper.ConvertToUnSign(user.TenPhongBanCap1);
            //string ouName = "LDAP://OU=" + pbCap1 + ",OU=Company Structure,DC=baonx,DC=com";
            //string ouName = "LDAP://OU=" + pbCap1 + "," + RootOuHpl;
            string ouName = "LDAP://OU=" + pbCap1 + "," + RootOuHpl;
            DirectoryEntry ouEntry = new DirectoryEntry(ouName);

            //==Fix lỗi: Server is unwilling to process the request
            //"LDAP://hpladds01/CN=" + userDn + ",OU=optional,DC=your-domain,DC=com" 
            //"LDAP://hpladds01/CN=userName,OU=optional,DC=your-domain,DC=com" 
            //string userStr = "LDAP://hpladds01/CN=" + username + "," + PathOu;
            //_logger.Information("PasswordChangeProvider.CreateUser ==> Call ouEntry.Children.Add(" + userStr);
            //DirectoryEntry childEntry = ouEntry.Children.Add(userStr, "user");

            DirectoryEntry childEntry = ouEntry.Children.Add("CN=" + username, "user");
            childEntry.Properties[UserPropertiesAd.UserPrincipalName].Value = fixedUsername;
            childEntry.Properties[UserPropertiesAd.LoginName].Value = username; //SamAccountName
            childEntry.Properties[UserPropertiesAd.Name].Value = username;
            childEntry.Properties[UserPropertiesAd.LastName].Value = user.Ho;//sn, Surname, LastName = Ho va ten dem
            childEntry.Properties[UserPropertiesAd.FirstName].Value = user.Ten;//GivenName, FirstName = Ten
            childEntry.Properties[UserPropertiesAd.DisplayName].Value = user.Ho + " " + user.Ten;//GivenName, FirstName
            childEntry.Properties[UserPropertiesAd.EmailAddress].Value = username + "@haiphatland.com.vn";

            if (!string.IsNullOrEmpty(user.DienThoai))
            {
                childEntry.Properties[UserPropertiesAd.TelePhoneNumber].Value = user.DienThoai;
            }
            childEntry.Properties[UserPropertiesAd.Description].Value = "Tạo bởi tool " + DateTime.Now.ToString("G");
            //Thong tin phong ban
            if (!string.IsNullOrEmpty(user.MaNhanVien))
            {
                childEntry.Properties[UserPropertiesAd.EmployeeId].Value = user.MaNhanVien;
            }
            if (!string.IsNullOrEmpty(user.TenPhongBanCap1))
            {
                childEntry.Properties[UserPropertiesAd.Department].Value = user.TenPhongBanCap1;
            }

            if (!string.IsNullOrEmpty(user.TenChucDanh))
            {
                childEntry.Properties[UserPropertiesAd.Title].Value = user.TenChucDanh;
            }

            //Enable user
            childEntry.Properties[UserPropertiesAd.PwdLastSet].Value = -1;
            childEntry.Properties[UserPropertiesAd.AccountExpires].Value = "9223372036854775807";

            //_logger.Information("PasswordChangeProvider.CreateUser ==> Call method: childEntry.CommitChanges()");
            childEntry.CommitChanges();

            childEntry.Invoke("SetPassword", pw);
            childEntry.Properties[UserPropertiesAd.UserAccountControl].Value = 66048;
            childEntry.CommitChanges();

            ouEntry.CommitChanges();

            //Add user vao Group
            string groupName = "Employees";
            GroupPrincipal group = GroupPrincipal.FindByIdentity(principalContext, groupName);
            group.Members.Add(principalContext, IdentityType.UserPrincipalName, fixedUsername);
            group.Save();

            //var userPrincipal = UserPrincipal.FindByIdentity(principalContext, _idType, fixedUsername);
            ////Update lại mot so thong tin cua nhan su
            //if (userPrincipal.GetUnderlyingObject() is DirectoryEntry directoryEntry)
            //{
            //    result.UserInfo = ConvertUserProfiles(directoryEntry);
            //    directoryEntry.Dispose();
            //}
            //else
            //{
            //    result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "Lỗi: Không xác định được các Attributes của User");
            //}
            //userPrincipal.Dispose();

            principalContext.Dispose();
            group.Dispose();

            _logger.Information(username + " created on AD at " + DateTime.Now.ToString("G"));

            result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "Successful");

            _logger.Information("END CALL PasswordChangeProvider.CreateAdUser2");
        }

        public void CreateAdUser3(GetAllNhanVienTheoListMaNvReturnModel user, string pw)
        {
            _logger.Information("START CALL PasswordChangeProvider.CreateAdUser2");

            var result = new ApiResultAd();

            string username = user.TenDangNhap;
            _logger.Information(username + " START create USER on AD at " + DateTime.Now.ToString("G"));

            var principalContext = AcquirePrincipalContext();
            string fixedUsername = FixUsernameWithDomain(username);

            //Cach 1 Create User
            //OU: OU=Company Structure,DC=baonx,DC=com
            //"LDAP://OU=Company Structure,DC=baonx,DC=com";
            string pbCap1 = CommonHelper.ConvertToUnSign(user.TenPhongBanCap1);
            //string ouName = "LDAP://OU=" + pbCap1 + ",OU=Company Structure,DC=baonx,DC=com";
            //string ouName = "LDAP://OU=" + pbCap1 + "," + RootOuHpl;
            string ouName = "LDAP://OU=" + pbCap1 + "," + RootOuHpl;
            DirectoryEntry ouEntry = new DirectoryEntry(ouName);

            //==Fix lỗi: Server is unwilling to process the request
            //"LDAP://hpladds01/CN=" + userDn + ",OU=optional,DC=your-domain,DC=com" 
            //"LDAP://hpladds01/CN=userName,OU=optional,DC=your-domain,DC=com" 
            //string userStr = "LDAP://hpladds01/CN=" + username + "," + PathOu;
            //_logger.Information("PasswordChangeProvider.CreateUser ==> Call ouEntry.Children.Add(" + userStr);
            //DirectoryEntry childEntry = ouEntry.Children.Add(userStr, "user");

            DirectoryEntry childEntry = ouEntry.Children.Add("CN=" + username, "user");
            childEntry.Properties[UserPropertiesAd.UserPrincipalName].Value = fixedUsername;
            childEntry.Properties[UserPropertiesAd.LoginName].Value = username; //SamAccountName
            childEntry.Properties[UserPropertiesAd.Name].Value = username;
            childEntry.Properties[UserPropertiesAd.LastName].Value = user.Ho;//sn, Surname, LastName = Ho va ten dem
            childEntry.Properties[UserPropertiesAd.FirstName].Value = user.HoTen;//GivenName, FirstName = Ten
            childEntry.Properties[UserPropertiesAd.DisplayName].Value = user.Ho + " " + user.HoTen;//GivenName, FirstName
            childEntry.Properties[UserPropertiesAd.EmailAddress].Value = username + "@haiphatland.com.vn";

            if (!string.IsNullOrEmpty(user.DienThoai))
            {
                childEntry.Properties[UserPropertiesAd.TelePhoneNumber].Value = user.DienThoai;
            }
            childEntry.Properties[UserPropertiesAd.Description].Value = "Tạo bởi tool " + DateTime.Now.ToString("G");
            //Thong tin phong ban
            if (!string.IsNullOrEmpty(user.MaNhanVien))
            {
                childEntry.Properties[UserPropertiesAd.EmployeeId].Value = user.MaNhanVien;
            }
            if (!string.IsNullOrEmpty(user.TenPhongBanCap1))
            {
                childEntry.Properties[UserPropertiesAd.Department].Value = user.TenPhongBanCap1;
            }

            if (!string.IsNullOrEmpty(user.TenChucDanh))
            {
                childEntry.Properties[UserPropertiesAd.Title].Value = user.TenChucDanh;
            }

            //Enable user
            childEntry.Properties[UserPropertiesAd.PwdLastSet].Value = -1;
            childEntry.Properties[UserPropertiesAd.AccountExpires].Value = "9223372036854775807";

            //_logger.Information("PasswordChangeProvider.CreateUser ==> Call method: childEntry.CommitChanges()");
            childEntry.CommitChanges();

            childEntry.Invoke("SetPassword", pw);
            childEntry.Properties[UserPropertiesAd.UserAccountControl].Value = 66048;
            childEntry.CommitChanges();

            ouEntry.CommitChanges();

            //Add user vao Group
            string groupName = "Employees";
            GroupPrincipal group = GroupPrincipal.FindByIdentity(principalContext, groupName);
            group.Members.Add(principalContext, IdentityType.UserPrincipalName, fixedUsername);
            group.Save();

            //var userPrincipal = UserPrincipal.FindByIdentity(principalContext, _idType, fixedUsername);
            ////Update lại mot so thong tin cua nhan su
            //if (userPrincipal.GetUnderlyingObject() is DirectoryEntry directoryEntry)
            //{
            //    result.UserInfo = ConvertUserProfiles(directoryEntry);
            //    directoryEntry.Dispose();
            //}
            //else
            //{
            //    result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "Lỗi: Không xác định được các Attributes của User");
            //}
            //userPrincipal.Dispose();

            principalContext.Dispose();
            group.Dispose();

            _logger.Information(username + " created on AD at " + DateTime.Now.ToString("G"));

            result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "Successful");

            _logger.Information("END CALL PasswordChangeProvider.CreateAdUser2");
        }

        public ApiResultAd CreateAdUser(UserInfoAd user, string pw)
        {
            _logger.Information("PasswordChangeProvider.CreateUser");

            var result = new ApiResultAd();

            string fixedUsername = "";
            string username = user.sAMAccountName;
            var principalContext = AcquirePrincipalContext();

            //Kiểm tra user đã tồn tại chưa
            bool check = true;
            int i = 0;
            while (check)
            {
                fixedUsername = FixUsernameWithDomain(username);
                var up = UserPrincipal.FindByIdentity(principalContext, _idType, fixedUsername);
                if (up != null)
                {
                    i++;
                    username = user.sAMAccountName + i;
                    up.Dispose();
                }
                else
                {
                    check = false;
                }
            }
            _logger.Information("PasswordChangeProvider.CreateUser fixedUsername=" + fixedUsername + ". username=" + username);

            //Cach 1 Create User
            //OU: OU=Company Structure,DC=baonx,DC=com
            DirectoryEntry ouEntry = new DirectoryEntry(PathOu);

            //==Fix lỗi: Server is unwilling to process the request
            //"LDAP://hpladds01/CN=" + userDn + ",OU=optional,DC=your-domain,DC=com" 
            //"LDAP://hpladds01/CN=userName,OU=optional,DC=your-domain,DC=com" 
            //string userStr = "LDAP://hpladds01/CN=" + username + "," + PathOu;
            //_logger.Information("PasswordChangeProvider.CreateUser ==> Call ouEntry.Children.Add(" + userStr);
            //DirectoryEntry childEntry = ouEntry.Children.Add(userStr, "user");

            DirectoryEntry childEntry = ouEntry.Children.Add("CN=" + username, "user");
            childEntry.Properties[UserPropertiesAd.UserPrincipalName].Value = fixedUsername;
            childEntry.Properties[UserPropertiesAd.LoginName].Value = username; //SamAccountName
            childEntry.Properties[UserPropertiesAd.Name].Value = username;
            childEntry.Properties[UserPropertiesAd.LastName].Value = user.givenName;//sn, Surname, LastName = Ho va ten dem
            childEntry.Properties[UserPropertiesAd.FirstName].Value = user.sn;//GivenName, FirstName = Ten
            childEntry.Properties[UserPropertiesAd.DisplayName].Value = user.displayName;//GivenName, FirstName
            childEntry.Properties[UserPropertiesAd.EmailAddress].Value = username + "@haiphatland.com.vn";

            if (!string.IsNullOrEmpty(user.telephoneNumber))
            {
                childEntry.Properties[UserPropertiesAd.TelePhoneNumber].Value = user.telephoneNumber;
            }
            childEntry.Properties[UserPropertiesAd.Description].Value = user.description;
            //Thong tin phong ban
            if (!string.IsNullOrEmpty(user.employeeID))
            {
                childEntry.Properties[UserPropertiesAd.EmployeeId].Value = user.employeeID;
            }
            if (!string.IsNullOrEmpty(user.department))
            {
                childEntry.Properties[UserPropertiesAd.Department].Value = user.department;
            }

            if (!string.IsNullOrEmpty(user.title))
            {
                childEntry.Properties[UserPropertiesAd.Title].Value = user.title;
            }

            //Enable user
            childEntry.Properties[UserPropertiesAd.PwdLastSet].Value = -1;
            childEntry.Properties[UserPropertiesAd.AccountExpires].Value = "9223372036854775807";

            //_logger.Information("PasswordChangeProvider.CreateUser ==> Call method: childEntry.CommitChanges()");
            childEntry.CommitChanges();

            childEntry.Invoke("SetPassword", pw);
            childEntry.Properties[UserPropertiesAd.UserAccountControl].Value = 66048;
            childEntry.CommitChanges();

            ouEntry.CommitChanges();
            //_logger.Information("PasswordChangeProvider.CreateUser ouEntry.CommitChanges()");

            //Add user vao Group
            string groupName = "Employees";
            GroupPrincipal group = GroupPrincipal.FindByIdentity(principalContext, groupName);
            group.Members.Add(principalContext, IdentityType.UserPrincipalName, fixedUsername);
            group.Save();

            var userPrincipal = UserPrincipal.FindByIdentity(principalContext, _idType, fixedUsername);
            //Update lại mot so thong tin cua nhan su
            if (userPrincipal.GetUnderlyingObject() is DirectoryEntry directoryEntry)
            {
                result.UserInfo = ConvertUserProfiles(directoryEntry);
                directoryEntry.Dispose();
            }
            else
            {
                result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "Lỗi: Không xác định được các Attributes của User");
            }

            principalContext.Dispose();
            userPrincipal.Dispose();
            group.Dispose();

            _logger.Information(username + " created on AD at " + DateTime.Now.ToString("G"));
            Console.WriteLine(username + " created on AD at " + DateTime.Now.ToString("G"));

            result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "Successful");

            return result;
        }

        public ApiResultAd CreateUser2(UserInfoAd user, string pw)
        {
            _logger.Information("PasswordChangeProvider.CreateUser");

            var result = new ApiResultAd { UserInfo = null };

            string fixedUsername = "";
            string username = user.sAMAccountName;
            var principalContext = AcquirePrincipalContext();

            //Kiểm tra user đã tồn tại chưa
            bool check = true;
            int i = 0;
            while (check)
            {
                fixedUsername = FixUsernameWithDomain(username);
                var up = UserPrincipal.FindByIdentity(principalContext, _idType, fixedUsername);
                if (up != null)
                {
                    i++;
                    username = user.sAMAccountName + i;
                    up.Dispose();
                }
                else
                {
                    check = false;
                }
            }

            //Cach 1 Create User
            //OU: OU=Company Structure,DC=baonx,DC=com
            //DirectoryEntry ouEntry = new DirectoryEntry("LDAP://OU=Company Structure,DC=baonx,DC=com");
            //for (int j = 0; j < 2; j++)
            //{
            //    try
            //    {
            //        DirectoryEntry childEntry = ouEntry.Children.Add("CN=TestUser" + i, "user");
            //        childEntry.CommitChanges();
            //        ouEntry.CommitChanges();
            //        childEntry.Invoke("SetPassword", new object[] { "password" });
            //        childEntry.CommitChanges();
            //    }
            //    catch (Exception ex)
            //    {

            //    }
            //}

            //Cach 2 Create User
            var userPrincipal = new UserPrincipal(principalContext)
            {
                UserPrincipalName = fixedUsername,
                SamAccountName = username,
                Name = username,
                Surname = user.sn,
                GivenName = user.givenName,
                DisplayName = user.displayName,
                EmailAddress = username + "@haiphatland.com.vn",
                VoiceTelephoneNumber = user.telephoneNumber,
                Description = user.description
            };
            userPrincipal.SetPassword(pw);
            userPrincipal.Enabled = true;
            userPrincipal.PasswordNeverExpires = true;
            userPrincipal.Save();

            //Add user vao Group
            string groupName = "Employees";
            GroupPrincipal group = GroupPrincipal.FindByIdentity(principalContext, groupName);
            group.Members.Add(principalContext, IdentityType.UserPrincipalName, fixedUsername);
            group.Save();

            var userInfo = new UserInfoAd
            {
                isLocked = userPrincipal.IsAccountLockedOut(),
                userPrincipalName = userPrincipal.UserPrincipalName,
                sAMAccountName = userPrincipal.SamAccountName,
                name = userPrincipal.Name,
                sn = userPrincipal.Surname,
                givenName = userPrincipal.GivenName,
                displayName = userPrincipal.DisplayName,
                mail = userPrincipal.EmailAddress,
                telephoneNumber = userPrincipal.VoiceTelephoneNumber,
                description = userPrincipal.Description
            };

            //Update lại mot so thong tin cua nhan su
            bool checkForUpdate = false;
            if (userPrincipal.GetUnderlyingObject() is DirectoryEntry directoryEntry)
            {
                //try
                //{
                //    DirectoryEntry dirEntry = new DirectoryEntry("LDAP://" + groupDn);
                //    dirEntry.Properties["member"].Add(userDn);
                //    dirEntry.CommitChanges();
                //    dirEntry.Close();
                //}
                //catch (System.DirectoryServices.DirectoryServicesCOMException E)
                //{
                //    //doSomething with E.Message.ToString();
                //}

                //department: Chi nhánh/Phòng ban
                directoryEntry.Properties[UserPropertiesAd.Department].Value = "Ten phong ban";
                userInfo.department = "Ten phong ban";
                //title = Chức danh
                directoryEntry.Properties[UserPropertiesAd.Title].Value = "Chuc danh";
                userInfo.title = "Chuc vu";
                //employeeID mã nhân viên
                directoryEntry.Properties[UserPropertiesAd.EmployeeId].Value = "Ma Nhan Vien";
                userInfo.employeeID = "ma nha vien";

                //CN
                if (directoryEntry.Properties.Contains(UserPropertiesAd.ContainerName))
                {
                    userInfo.CN = directoryEntry.Properties[UserPropertiesAd.ContainerName].Value.ToString();
                }

                //distinguishedName (CN=baonx,OU=Company Structure,DC=baonx,DC=com)
                if (directoryEntry.Properties.Contains(UserPropertiesAd.DistinguishedName))
                {
                    //directoryEntry.Properties[UserPropertiesAd.DistinguishedName].Value = "OU=Company Structure,DC=baonx,DC=com";
                    userInfo.distinguishedName = directoryEntry.Properties[UserPropertiesAd.DistinguishedName].Value.ToString();
                }

                //memberOf: (CN=Employees,CN=Users,DC=baonx,DC=com)
                if (directoryEntry.Properties.Contains(UserPropertiesAd.MemberOf))
                {
                    userInfo.memberOf = directoryEntry.Properties[UserPropertiesAd.MemberOf].Value.ToString();
                }

                //objectCategory: (CN=Person,CN=Schema,CN=Configuration,DC=baonx,DC=com)
                if (directoryEntry.Properties.Contains(UserPropertiesAd.ObjectCategory))
                {
                    userInfo.objectCategory = directoryEntry.Properties[UserPropertiesAd.ObjectCategory].Value.ToString();
                }

                try
                {
                    directoryEntry.CommitChanges();
                    result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "Successful");
                }
                catch (Exception e)
                {
                    result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "Lỗi: Update dữ liệu: " + e.Message);
                }

                result.UserInfo = userInfo;

                directoryEntry.Close();
                directoryEntry.Dispose();
            }
            else
            {
                result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "Lỗi: Không xác định được các Attributes của User");
            }

            principalContext.Dispose();
            userPrincipal.Dispose();

            result.Errors = new ApiErrorItem(ApiErrorCode.Generic, "Successful");
            result.UserInfo = userInfo;

            return result;
        }

        /// <inheritdoc />
        public ApiErrorItem? PerformPasswordChange(string username, string currentPassword, string newPassword)
        {
            try
            {
                var fixedUsername = FixUsernameWithDomain(username);
                _logger.Information("PasswordChangeProvider.PerformPasswordChange: fixedUsername=" + fixedUsername);

                using var principalContext = AcquirePrincipalContext();
                var userPrincipal = UserPrincipal.FindByIdentity(principalContext, _idType, fixedUsername);

                // Check if the user principal exists
                if (userPrincipal == null)
                {
                    _logger.Warning($"The User principal ({fixedUsername}) doesn't exist");
                    return new ApiErrorItem(ApiErrorCode.UserNotFound, "Khong ton tai user");
                }

                //BAONX
                //var minPwdLength = AcquireDomainPasswordLength();

                //if (newPassword.Length < minPwdLength)
                //{
                //    _logger.Error("Failed due to password complex policies: New password length is shorter than AD minimum password length");

                //    return new ApiErrorItem(ApiErrorCode.ComplexPassword);
                //}

                //// Check if the newPassword is Pwned
                //if (PwnedPasswordsSearch.PwnedSearch.IsPwnedPassword(newPassword))
                //{
                //    _logger.Error("Failed due to pwned password: New password is publicly known and can be used in dictionary attacks");

                //    return new ApiErrorItem(ApiErrorCode.PwnedPassword);
                //}

                _logger.Information($"PerformPasswordChange for user {fixedUsername}");

                //Check User thuộc group nào, có một số Groups đặc biệt, không cho phép user đổi pass bằng tool này
                var item = ValidateGroups(userPrincipal);
                if (item != null) return item;

                // Check if password change is allowed
                if (userPrincipal.UserCannotChangePassword)
                {
                    _logger.Warning("The User principal cannot change the password");

                    return new ApiErrorItem(ApiErrorCode.ChangeNotPermitted);
                }

                // Check if password expired or must be changed
                if (_options.UpdateLastPassword && userPrincipal.LastPasswordSet == null)
                {
                    SetLastPassword(userPrincipal);
                }

                // Use always UPN for password check.
                if (!ValidateUserCredentials(userPrincipal.UserPrincipalName, currentPassword, principalContext))
                {
                    _logger.Warning("The User principal password is not valid");

                    return new ApiErrorItem(ApiErrorCode.InvalidCredentials);
                }

                // Change the password via 2 different methods. Try SetPassword if ChangePassword fails.
                ChangePassword(currentPassword, newPassword, userPrincipal);

                userPrincipal.Save();
                _logger.Debug("The User principal password updated with setPassword");
            }
            //BAONX
            //catch (PasswordException passwordEx)
            //{
            //    var item = new ApiErrorItem(ApiErrorCode.ComplexPassword, passwordEx.Message);

            //    _logger.Warning(item.Message, passwordEx);

            //    return item;
            //}
            catch (Exception ex)
            {
                var item = ex is ApiErrorException apiError
                    ? apiError.ToApiErrorItem()
                    : new ApiErrorItem(ApiErrorCode.Generic, ex.InnerException?.Message ?? ex.Message);

                _logger.Warning(item.Message, ex);

                return item;
            }

            return null;
        }

        private bool ValidateUserCredentials(string upn, string currentPassword, PrincipalContext principalContext)
        {
            if (principalContext.ValidateCredentials(upn, currentPassword))
                return true;

            if (LogonUser(upn, string.Empty, currentPassword, LogonTypes.Network, LogonProviders.Default, out _))
                return true;

            var errorCode = System.Runtime.InteropServices.Marshal.GetLastWin32Error();

            _logger.Debug($"ValidateUserCredentials GetLastWin32Error {errorCode}");

            // Both of these means that the password CAN change and that we got the correct password
            return errorCode == ErrorPasswordMustChange || errorCode == ErrorPasswordExpired;
        }

        private string FixUsernameWithDomain(string username)
        {
            if (_idType != IdentityType.UserPrincipalName) return username;

            // Check for default domain: if none given, ensure EFLD can be used as an override.
            var parts = username.Split(new[] { '@' }, StringSplitOptions.RemoveEmptyEntries);
            var domain = parts.Length > 1 ? parts[1] : _options.DefaultDomain;

            return string.IsNullOrWhiteSpace(domain) || parts.Length > 1 ? username : $"{username}@{domain}";
        }

        private ApiErrorItem? ValidateGroups(UserPrincipal userPrincipal)
        {
            try
            {
                PrincipalSearchResult<Principal> groups;

                try
                {
                    groups = userPrincipal.GetGroups();
                }
                catch (Exception exception)
                {
                    //_logger.Error(new EventId(887), exception, nameof(ValidateGroups));
                    string eventId = new Microsoft.Extensions.Logging.EventId(887).ToString();
                    _logger.Error(eventId, exception, nameof(ValidateGroups));

                    groups = userPrincipal.GetAuthorizationGroups();
                }

                if (_options.RestrictedADGroups != null)
                    if (groups.Any(x => _options.RestrictedADGroups.Contains(x.Name)))
                    {
                        return new ApiErrorItem(ApiErrorCode.ChangeNotPermitted,
                            "The User " + userPrincipal.SamAccountName + " principal is listed as restricted.");
                    }

                var valueReturn = groups?.Any(x => _options.AllowedADGroups?.Contains(x.Name) == true) == true
                    ? null
                    : new ApiErrorItem(ApiErrorCode.ChangeNotPermitted, "The User " + userPrincipal.SamAccountName + " principal is not listed as allowed");

                return valueReturn;
            }
            catch (Exception exception)
            {
                //_logger.Error(new EventId(888), exception, nameof(ValidateGroups));
                string eventId = new Microsoft.Extensions.Logging.EventId(888).ToString();
                _logger.Error(eventId, exception, nameof(ValidateGroups));
            }

            return null;
        }

        private void SetLastPassword(Principal userPrincipal)
        {
            var directoryEntry = (DirectoryEntry)userPrincipal.GetUnderlyingObject();
            var prop = directoryEntry.Properties["pwdLastSet"];

            if (prop == null)
            {
                _logger.Warning("The User principal password have no last password, but the property is missing");
                return;
            }

            try
            {
                prop.Value = -1;
                directoryEntry.CommitChanges();
                _logger.Warning("The User principal last password was updated");
            }
            catch (Exception ex)
            {
                throw new ApiErrorException($"Failed to update password: {ex.Message}",
                    ApiErrorCode.ChangeNotPermitted);
            }
        }

        private void ChangePassword(string currentPassword, string newPassword, AuthenticablePrincipal userPrincipal)
        {
            try
            {
                // Try by regular ChangePassword method
                _logger.Warning("Gọi method userPrincipal.ChangePassword()");
                userPrincipal.ChangePassword(currentPassword, newPassword);
            }
            catch (Exception e)
            {
                _logger.Debug("Lỗi khi call userPrincipal.ChangePassword: " + e.Message);
                if (_options.UseAutomaticContext)
                {
                    _logger.Warning("The User principal password cannot be changed and setPassword won't be called");

                    throw;
                }

                // If the previous attempt failed, use the SetPassword method.
                _logger.Debug("Goi method userPrincipal.SetPassword()");
                userPrincipal.SetPassword(newPassword);

                _logger.Debug("The User principal password updated with setPassword");
            }
        }

        /// <summary>
        /// Use the values from appsettings.IdTypeForUser as fault-tolerant as possible.
        /// </summary>
        private void SetIdType()
        {
            _idType = _options.IdTypeForUser?.Trim().ToLower() switch
            {
                "distinguishedname" => IdentityType.DistinguishedName,
                "distinguished name" => IdentityType.DistinguishedName,
                "dn" => IdentityType.DistinguishedName,
                "globally unique identifier" => IdentityType.Guid,
                "globallyuniqueidentifier" => IdentityType.Guid,
                "guid" => IdentityType.Guid,
                "name" => IdentityType.Name,
                "nm" => IdentityType.Name,
                "samaccountname" => IdentityType.SamAccountName,
                "accountname" => IdentityType.SamAccountName,
                "sam account" => IdentityType.SamAccountName,
                "sam account name" => IdentityType.SamAccountName,
                "sam" => IdentityType.SamAccountName,
                "securityidentifier" => IdentityType.Sid,
                "securityid" => IdentityType.Sid,
                "secid" => IdentityType.Sid,
                "security identifier" => IdentityType.Sid,
                "sid" => IdentityType.Sid,
                _ => IdentityType.UserPrincipalName
            };
        }

        /// <summary>
        /// Gọi hàm này một trong 2 trường hợp sau
        /// UseAutomaticContext=true:
        /// + Code phải đặt trên server AD và không cần điền thông tin Admin Quản trị AD
        /// UseAutomaticContext=false:
        /// + Code đặt trên AD hoặc ngoài AD đều được và bắt buộc phải điền thông tin Admin quản trị domain
        /// </summary>
        /// <returns></returns>
        private PrincipalContext AcquirePrincipalContext()
        {
            //_logger.Warning(_options.ToJson());
            if (_options.UseAutomaticContext)
            {
                _logger.Warning("Using AutomaticContext");
                return new PrincipalContext(ContextType.Domain);
            }

            var domain = $"{_options.LdapHostnames.First()}:{_options.LdapPort}";
            _logger.Warning($"Not using AutomaticContext  {domain}");
            try
            {
                return new PrincipalContext(ContextType.Domain, domain, _options.LdapUsername, _options.LdapPassword);
            }
            catch (Exception e)
            {
                _logger.Warning("Lỗi call AcquirePrincipalContext: " + e);
                Console.WriteLine("Lỗi call AcquirePrincipalContext: " + e);
                return null;
            }
        }

        /// <summary>
        /// Gọi hàm này trong trường hợp (AutomaticContext=TRUE or FALSE không quan trọng)
        /// Khi source code không đặt trên server AD và không muốn điền thông tin(username&pass) của Admin quản trị domain
        /// Reset pass dựa vào Authorization Username & Password của User truyền vào.
        /// Trong file appsettings.json cần setting 2 tham số LdapHostnames:LdapPort.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="pw"></param>
        /// <returns></returns>
        private PrincipalContext AcquirePrincipalContext(string username, string pw)
        {
            _logger.Warning("PasswordChangeProvider.AcquirePrincipalContext: " + JsonConvert.SerializeObject(_options));

            var domain = $"{_options.LdapHostnames.First()}:{_options.LdapPort}";
            //_logger.Warning($"Not using AutomaticContext  {domain}");

            return new PrincipalContext(
                ContextType.Domain,
                domain,
                username,
                pw);
        }

        private UserInfoAd ConvertUserProfiles(DirectoryEntry dirEntry)
        {
            UserInfoAd user = new UserInfoAd();

            //CN: Hiện thị ContainerName
            if (dirEntry.Properties.Contains(UserPropertiesAd.ContainerName))
            {
                user.CN = dirEntry.Properties[UserPropertiesAd.ContainerName].Value.ToString();
            }
            //user có dạng admin@baonx.com
            if (dirEntry.Properties.Contains(UserPropertiesAd.UserPrincipalName))
            {
                user.userPrincipalName = dirEntry.Properties[UserPropertiesAd.UserPrincipalName].Value.ToString();// = fixedUsername;
            }

            if (dirEntry.Properties.Contains(UserPropertiesAd.LoginName))
            {
                user.sAMAccountName = dirEntry.Properties[UserPropertiesAd.LoginName].Value.ToString();//username; //SamAccountName
            }

            if (dirEntry.Properties.Contains(UserPropertiesAd.Name))
            {
                user.name = dirEntry.Properties[UserPropertiesAd.Name].Value.ToString();//trùng với username;
            }
            //user.givenName;//sn, Surname, LastName = Ho va ten dem
            if (dirEntry.Properties.Contains(UserPropertiesAd.LastName))
            {
                user.givenName = dirEntry.Properties[UserPropertiesAd.LastName].Value.ToString();
            }
            //user.sn;//GivenName, FirstName = Ten
            if (dirEntry.Properties.Contains(UserPropertiesAd.FirstName))
            {
                user.sn = dirEntry.Properties[UserPropertiesAd.FirstName].Value.ToString();
            }
            //user.displayName;//GivenName, FirstName
            if (dirEntry.Properties.Contains(UserPropertiesAd.DisplayName))
            {
                user.displayName = dirEntry.Properties[UserPropertiesAd.DisplayName].Value.ToString();
            }
            //username + "@haiphatland.com.vn";
            if (dirEntry.Properties.Contains(UserPropertiesAd.EmailAddress))
            {
                user.mail = dirEntry.Properties[UserPropertiesAd.EmailAddress].Value.ToString();
            }

            if (dirEntry.Properties.Contains(UserPropertiesAd.TelePhoneNumber))
            {
                user.telephoneNumber = dirEntry.Properties[UserPropertiesAd.TelePhoneNumber].Value.ToString();//user.telephoneNumber;
            }

            if (dirEntry.Properties.Contains(UserPropertiesAd.Description))
            {
                user.description = dirEntry.Properties[UserPropertiesAd.Description].Value.ToString();//user.description;
            }

            //Thong tin phong ban
            //employeeID mã nhân viên
            if (dirEntry.Properties.Contains(UserPropertiesAd.EmployeeId))
            {
                user.employeeID = dirEntry.Properties[UserPropertiesAd.EmployeeId].Value.ToString();//"MaNhanVien";
            }
            //department: Chi nhánh/Phòng ban
            if (dirEntry.Properties.Contains(UserPropertiesAd.Department))
            {
                user.department = dirEntry.Properties[UserPropertiesAd.Department].Value.ToString();//"Phong Ban";
            }
            //title = Chức danh
            if (dirEntry.Properties.Contains(UserPropertiesAd.Title))
            {
                user.title = dirEntry.Properties[UserPropertiesAd.Title].Value.ToString();//"Chuc Danh";
            }

            //OU
            if (dirEntry.Properties.Contains(UserPropertiesAd.DistinguishedName))
            {
                user.distinguishedName = dirEntry.Properties[UserPropertiesAd.DistinguishedName].Value.ToString();
            }

            //MemberOf
            if (dirEntry.Properties.Contains(UserPropertiesAd.MemberOf))
            {
                user.memberOf = dirEntry.Properties[UserPropertiesAd.MemberOf].Value.ToString();
            }

            //Object Category
            if (dirEntry.Properties.Contains(UserPropertiesAd.ObjectCategory))
            {
                user.objectCategory = dirEntry.Properties[UserPropertiesAd.ObjectCategory].Value.ToString();
            }


            return user;
        }

        private UserAdInfo ConvertUserProfiles2(DirectoryEntry dirEntry)
        {
            UserAdInfo user = new UserAdInfo();

            //Thông tin disable của User
            if (dirEntry.NativeGuid == null)
            {
                user.IsEnabled = false;
            }
            else
            {
                int flags = (int)dirEntry.Properties["userAccountControl"].Value;

                user.IsEnabled = !Convert.ToBoolean(flags & 0x0002);
            }

            //CN: Hiện thị ContainerName
            if (dirEntry.Properties.Contains(UserPropertiesAd.ContainerName))
            {
                user.ContainerName = dirEntry.Properties[UserPropertiesAd.ContainerName].Value.ToString();
            }
            //user có dạng admin@baonx.com
            //if (dirEntry.Properties.Contains(UserPropertiesAd.UserPrincipalName))
            //{
            //    user.userPrincipalName = dirEntry.Properties[UserPropertiesAd.UserPrincipalName].Value.ToString();// = fixedUsername;
            //}

            if (dirEntry.Properties.Contains(UserPropertiesAd.LoginName))
            {
                user.Username = dirEntry.Properties[UserPropertiesAd.LoginName].Value.ToString();//username; //SamAccountName
            }

            if (dirEntry.Properties.Contains(UserPropertiesAd.Name))
            {
                user.Name = dirEntry.Properties[UserPropertiesAd.Name].Value.ToString();//trùng với username;
            }
            //user.givenName;//sn, Surname, LastName = Ho va ten dem
            if (dirEntry.Properties.Contains(UserPropertiesAd.LastName))
            {
                user.LastName = dirEntry.Properties[UserPropertiesAd.LastName].Value.ToString();
            }
            //user.sn;//GivenName, FirstName = Ten
            if (dirEntry.Properties.Contains(UserPropertiesAd.FirstName))
            {
                user.FirstName = dirEntry.Properties[UserPropertiesAd.FirstName].Value.ToString();
            }
            //user.displayName;//GivenName, FirstName
            if (dirEntry.Properties.Contains(UserPropertiesAd.DisplayName))
            {
                user.DisplayName = dirEntry.Properties[UserPropertiesAd.DisplayName].Value.ToString();
            }
            //username + "@haiphatland.com.vn";
            if (dirEntry.Properties.Contains(UserPropertiesAd.EmailAddress))
            {
                user.Email = dirEntry.Properties[UserPropertiesAd.EmailAddress].Value.ToString();
            }

            if (dirEntry.Properties.Contains(UserPropertiesAd.TelePhoneNumber))
            {
                user.TelePhoneNumber = dirEntry.Properties[UserPropertiesAd.TelePhoneNumber].Value.ToString();//user.telephoneNumber;
            }

            if (dirEntry.Properties.Contains(UserPropertiesAd.Description))
            {
                user.Description = dirEntry.Properties[UserPropertiesAd.Description].Value.ToString();//user.description;
            }

            //Thong tin phong ban
            //employeeID mã nhân viên
            if (dirEntry.Properties.Contains(UserPropertiesAd.EmployeeId))
            {
                user.EmployeeId = dirEntry.Properties[UserPropertiesAd.EmployeeId].Value.ToString();//"MaNhanVien";
            }
            //department: Chi nhánh/Phòng ban
            if (dirEntry.Properties.Contains(UserPropertiesAd.Department))
            {
                user.Department = dirEntry.Properties[UserPropertiesAd.Department].Value.ToString();//"Phong Ban";
            }
            //title = Chức danh
            if (dirEntry.Properties.Contains(UserPropertiesAd.Title))
            {
                user.Title = dirEntry.Properties[UserPropertiesAd.Title].Value.ToString();//"Chuc Danh";
            }

            //OU
            if (dirEntry.Properties.Contains(UserPropertiesAd.DistinguishedName))
            {
                user.OuName = dirEntry.Properties[UserPropertiesAd.DistinguishedName].Value.ToString();
            }

            //MemberOf
            if (dirEntry.Properties.Contains(UserPropertiesAd.MemberOf))
            {
                user.MemberOf = dirEntry.Properties[UserPropertiesAd.MemberOf].Value.ToString();
            }

            //Object Category
            if (dirEntry.Properties.Contains(UserPropertiesAd.ObjectCategory))
            {
                user.ObjectCategory = dirEntry.Properties[UserPropertiesAd.ObjectCategory].Value.ToString();
            }


            return user;
        }

        private int AcquireDomainPasswordLength()
        {
            DirectoryEntry entry;
            if (_options.UseAutomaticContext)
            {
                entry = Domain.GetCurrentDomain().GetDirectoryEntry();
            }
            else
            {
                entry = new DirectoryEntry(
                    $"{_options.LdapHostnames.First()}:{_options.LdapPort}",
                    _options.LdapUsername,
                    _options.LdapPassword
                    );
            }
            return (int)entry.Properties["minPwdLength"].Value;
        }
    }
}
