using System.Linq;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Hpl.HrmDatabase.Services
{
    public static class MyStringExtensions
    {
        public static bool Like(this string toSearch, string toFind)
        {
            return new Regex(@"\A" + new Regex(@"\.|\$|\^|\{|\[|\(|\||\)|\*|\+|\?|\\").Replace(toFind, ch => @"\" + ch).Replace('_', '.').Replace("%", ".*") + @"\z", RegexOptions.Singleline).IsMatch(toSearch);
        }
    }

    public class UsernameGenerator
    {
        public static bool LikeString(string toSearch, string toFind)
        {
            return toSearch.Like("%" + toFind);
        }

        public static string ConvertToUnSign(string s)
        {
            Regex regex = new Regex("\\p{IsCombiningDiacriticalMarks}+");
            string temp = s.Normalize(NormalizationForm.FormD);
            return regex.Replace(temp, String.Empty).Replace('\u0111', 'd').Replace('\u0110', 'D');
        }

        public static string CreateUsernameFromName(string lastName, string firstName)
        {
            firstName = ConvertToUnSign(firstName).Trim().ToLower();
            lastName = ConvertToUnSign(lastName).Trim().ToLower();


            //Tạo username dựa trên Họ Và Tên nhân sự
            var ten = firstName;
            string userNameGenerated;
            //if (userNameGenerated.Length >= 3)
            //{
            //    if (UserNotExist(userNameGenerated))
            //    {
            //        return userNameGenerated;
            //    }
            //}

            //string ho = lastName.Replace("thi", "").Trim();
            string ho = lastName.Trim();
            var strHo = ho.Replace(" ", ",").Trim()
                .Replace(",,", ",").Trim()
                .Split(",");

            //int count = strHo.Length;
            //switch (count)
            //{
            //    case 1:
            //        //Ten + Ho
            //        userNameGenerated = ten + strHo[0];
            //        if (UserNotExist(userNameGenerated))
            //        {
            //            return userNameGenerated;
            //        }
            //        //Ho + Ten
            //        userNameGenerated = strHo[0] + ten;
            //        if (UserNotExist(userNameGenerated))
            //        {
            //            return userNameGenerated;
            //        }

            //        break;

            //    case 2:
            //        //Ten dem + Ten
            //        userNameGenerated = strHo[1] + ten;
            //        if (UserNotExist(userNameGenerated))
            //        {
            //            return userNameGenerated;
            //        }
            //        //Ten + Ho
            //        userNameGenerated = ten + strHo[0];
            //        if (UserNotExist(userNameGenerated))
            //        {
            //            return userNameGenerated;
            //        }
            //        //Ho + Ten
            //        userNameGenerated = strHo[0] + ten;
            //        if (UserNotExist(userNameGenerated))
            //        {
            //            return userNameGenerated;
            //        }

            //        break;

            //    case 3:
            //        //Ten dem + Ten
            //        userNameGenerated = strHo[2] + ten;
            //        if (UserNotExist(userNameGenerated))
            //        {
            //            return userNameGenerated;
            //        }
            //        //Ten + Ho
            //        userNameGenerated = ten + strHo[0];
            //        if (UserNotExist(userNameGenerated))
            //        {
            //            return userNameGenerated;
            //        }
            //        //Ho + Ten
            //        userNameGenerated = strHo[0] + ten;
            //        if (UserNotExist(userNameGenerated))
            //        {
            //            return userNameGenerated;
            //        }

            //        break;

            //    case 4:
            //        //Ten dem + Ten
            //        userNameGenerated = strHo[3] + ten;
            //        if (UserNotExist(userNameGenerated))
            //        {
            //            return userNameGenerated;
            //        }
            //        //Ten + Ho
            //        userNameGenerated = ten + strHo[0];
            //        if (UserNotExist(userNameGenerated))
            //        {
            //            return userNameGenerated;
            //        }
            //        //Ho + Ten
            //        userNameGenerated = strHo[0] + ten;
            //        if (UserNotExist(userNameGenerated))
            //        {
            //            return userNameGenerated;
            //        }

            //        break;
            //}

            string newHo = "";
            foreach (var s in strHo)
            {
                newHo += s.Substring(0, 1);
            }

            userNameGenerated = ten + newHo.ToLower();
            //SO SÁNH USER TRONG HRM VÀ TẠO USER MỚI
            //FORMAT: ten + ho + tenDem + [số]. vd: baonx1
            userNameGenerated = CreateNewUsername(userNameGenerated);

            return userNameGenerated;
        }

        public static bool UserNotExist(string userName)
        {
            var db = new HrmDbContext();
            var user = db.SysNguoiDungs.FirstOrDefault(x => x.TenDangNhap == userName);
            if (user != null)
            {
                return false;
            }

            return true;
        }

        public static string CreateNewUsername(string userName)
        {
            var db = new HrmDbContext();
            string newUsername = userName;
            bool check = true;
            int i = 0;
            while (check)
            {
                var user = db.SysNguoiDungs.FirstOrDefault(x => x.TenDangNhap == newUsername);
                if (user != null)
                {
                    i++;
                    newUsername = userName + i;
                }
                else
                {
                    check = false;
                }

            }

            return newUsername;
        }

        //public static string CreateUsername(string hoVaTen)
        //{
        //    //Tạo username dựa trên Họ Và Tên nhân sự
        //    var str1 = CommonHelper.ConvertToUnSign(hoVaTen.Trim()
        //            .Replace(" ", ",")
        //            .Replace(",,", ","))
        //        .Split(",");

        //    var ten = str1.LastOrDefault();

        //    string newHo = "";
        //    foreach (var s in str1)
        //    {
        //        newHo += s.Substring(0, 1);
        //    }
        //    var ten = CommonHelper.ConvertToUnSign(firstName.Trim().ToLower());
        //    string userNameGenerated = ten + newHo.ToLower();

        //    return userNameGenerated;
        //}
    }
}