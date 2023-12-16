﻿using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;

namespace Hpl.Common
{
    public class MailHelper
    {
        //string emailSender = "baofsoft@gmail.com";
        //string emailSenderPassword = "tadsgszyizelkorl";
        static string emailSender = "hplandtech@gmail.com";
        static string displayName = "HCNS HPL";
        static string emailSenderPassword = "hnuslkzkgvswnzko";
        static string emailSenderHost = "smtp.gmail.com";
        static int emailSenderPort = 587;
        static bool emailIsSSL = true;

        public static void EmailSender(string body)
        {
            //Fetching Settings from WEB.CONFIG file.

            //Fetching Email Body Text from EmailTemplate File.
            //string FilePath = "D:\\MBK\\SendEmailByEmailTemplate\\EmailTemplates\\SignUp.html";
            //StreamReader str = new StreamReader(FilePath);
            //string mailText = str.ReadToEnd();
            //str.Close();

            string bodyContent = "Đây là nội dung email";

            //Repalce [newusername] = signup user name 
            //mailText = mailText.Replace("[body_content]", bodyContent);


            string subject = "Thông báo tạo và đồng bộ Tài khoản AD, HRM, MDaemon, SaleOnline";

            //Base class for sending email
            MailMessage _mailmsg = new MailMessage();

            //Make TRUE because our body text is html
            _mailmsg.IsBodyHtml = true;

            //Set From Email ID
            _mailmsg.From = new MailAddress(emailSender);

            //Set To Email ID
            _mailmsg.To.Add("baonx@haiphatland.com.vn");
            _mailmsg.To.Add("hoangnq@haiphatland.com.vn");
            _mailmsg.To.Add("daolq@haiphatland.com.vn");
            _mailmsg.To.Add("trang.nh@haiphatland.com.vn");

            //Set Subject
            _mailmsg.Subject = subject;

            //Set Body Text of Email 
            //_mailmsg.Body = mailText;
            _mailmsg.Body = body;
            //_mailmsg.Body = bodyContent;

            //Now set your SMTP 
            SmtpClient _smtp = new SmtpClient();

            //Set HOST server SMTP detail
            _smtp.Host = emailSenderHost;

            //Set PORT number of SMTP
            _smtp.Port = emailSenderPort;

            //Set SSL --> True / False
            _smtp.EnableSsl = emailIsSSL;

            //Set Sender UserEmailID, Password
            NetworkCredential _network = new NetworkCredential(emailSender, emailSenderPassword);
            _smtp.Credentials = _network;

            //Send Method will send your MailMessage create above.
            _smtp.Send(_mailmsg);
        }

        public static void EmailSender(string body, string subject)
        {
            //Fetching Settings from WEB.CONFIG file.

            //Fetching Email Body Text from EmailTemplate File.
            //string FilePath = "D:\\MBK\\SendEmailByEmailTemplate\\EmailTemplates\\SignUp.html";
            //StreamReader str = new StreamReader(FilePath);
            //string mailText = str.ReadToEnd();
            //str.Close();

            string bodyContent = "Đây là nội dung email";

            //Repalce [newusername] = signup user name 
            //mailText = mailText.Replace("[body_content]", bodyContent);


            //string subject = "Thông báo tạo và đồng bộ Tài khoản AD, HRM, MDaemon, SaleOnline";

            //Base class for sending email
            MailMessage _mailmsg = new MailMessage();

            //Make TRUE because our body text is html
            _mailmsg.IsBodyHtml = true;

            //Set From Email ID
            _mailmsg.From = new MailAddress(emailSender);

            //Set To Email ID
            _mailmsg.To.Add("baonx@haiphatland.com.vn");
            _mailmsg.To.Add("hoangnq@haiphatland.com.vn");
            _mailmsg.To.Add("daolq@haiphatland.com.vn");
            _mailmsg.To.Add("trang.nh@haiphatland.com.vn");

            //Set Subject
            _mailmsg.Subject = subject;

            //Set Body Text of Email 
            //_mailmsg.Body = mailText;
            _mailmsg.Body = body;
            //_mailmsg.Body = bodyContent;

            //Now set your SMTP 
            SmtpClient _smtp = new SmtpClient();

            //Set HOST server SMTP detail
            _smtp.Host = emailSenderHost;

            //Set PORT number of SMTP
            _smtp.Port = emailSenderPort;

            //Set SSL --> True / False
            _smtp.EnableSsl = emailIsSSL;

            //Set Sender UserEmailID, Password
            NetworkCredential _network = new NetworkCredential(emailSender, emailSenderPassword);
            _smtp.Credentials = _network;

            //Send Method will send your MailMessage create above.
            _smtp.Send(_mailmsg);
        }

        public static void EmailSender(string body, string subject, List<string> listReceivers)
        {
            //Fetching Settings from WEB.CONFIG file.
           

            //Fetching Email Body Text from EmailTemplate File.
            //string FilePath = "D:\\MBK\\SendEmailByEmailTemplate\\EmailTemplates\\SignUp.html";
            //StreamReader str = new StreamReader(FilePath);
            //string mailText = str.ReadToEnd();
            //str.Close();

            string bodyContent = "Đây là nội dung email";

            //Repalce [newusername] = signup user name 
            //mailText = mailText.Replace("[body_content]", bodyContent);


            //string subject = "Thông báo tạo và đồng bộ Tài khoản AD, HRM, MDaemon";

            //Base class for sending email
            MailMessage _mailmsg = new MailMessage();

            //Make TRUE because our body text is html
            _mailmsg.IsBodyHtml = true;

            //Set From Email ID
            _mailmsg.From = new MailAddress(emailSender, displayName);

            //Set To Email ID
            foreach (var receiver in listReceivers)
            {
                _mailmsg.To.Add(receiver);
            }

            //Set Subject
            _mailmsg.Subject = subject;

            //Set Body Text of Email 
            //_mailmsg.Body = mailText;
            _mailmsg.Body = body;
            //_mailmsg.Body = bodyContent;

            //Now set your SMTP 
            SmtpClient _smtp = new SmtpClient();

            //Set HOST server SMTP detail
            _smtp.Host = emailSenderHost;

            //Set PORT number of SMTP
            _smtp.Port = emailSenderPort;

            //Set SSL --> True / False
            _smtp.EnableSsl = emailIsSSL;

            //Set Sender UserEmailID, Password
            NetworkCredential _network = new NetworkCredential(emailSender, emailSenderPassword);
            _smtp.Credentials = _network;

            //Send Method will send your MailMessage create above.
            _smtp.Send(_mailmsg);
        }

        public static void EmailSender(string body, string subject, string receiver)
        {
            //Fetching Settings from WEB.CONFIG file.

            //Fetching Email Body Text from EmailTemplate File.
            //string FilePath = "D:\\MBK\\SendEmailByEmailTemplate\\EmailTemplates\\SignUp.html";
            //StreamReader str = new StreamReader(FilePath);
            //string mailText = str.ReadToEnd();
            //str.Close();

            string bodyContent = "Đây là nội dung email";

            //Repalce [newusername] = signup user name 
            //mailText = mailText.Replace("[body_content]", bodyContent);


            //string subject = "Thông báo tạo và đồng bộ Tài khoản AD, HRM, MDaemon";

            //Base class for sending email
            MailMessage _mailmsg = new MailMessage();

            //Make TRUE because our body text is html
            _mailmsg.IsBodyHtml = true;

            //Set From Email ID
            _mailmsg.From = new MailAddress(emailSender);

            //Set To Email ID

            _mailmsg.To.Add(receiver);

            //Set Subject
            _mailmsg.Subject = subject;

            //Set Body Text of Email 
            //_mailmsg.Body = mailText;
            _mailmsg.Body = body;
            //_mailmsg.Body = bodyContent;

            //Now set your SMTP 
            SmtpClient _smtp = new SmtpClient();

            //Set HOST server SMTP detail
            _smtp.Host = emailSenderHost;

            //Set PORT number of SMTP
            _smtp.Port = emailSenderPort;

            //Set SSL --> True / False
            _smtp.EnableSsl = emailIsSSL;

            //Set Sender UserEmailID, Password
            NetworkCredential _network = new NetworkCredential(emailSender, emailSenderPassword);
            _smtp.Credentials = _network;

            //Send Method will send your MailMessage create above.
            _smtp.Send(_mailmsg);
        }

        public void SendMailToAdmin()
        {
            //tadsgszyizelkorl

            var fromAddress = new MailAddress("baofsoft@gmail.com", "Ban Công Nghệ");
            var toAddress = new MailAddress("to@example.com", "To Name");
            const string fromPassword = "tadsgszyizelkorl";
            const string subject = "Thông báo tạo và đồng bộ Tài khoản AD, HRM, MDaemon";
            const string body = "Body";

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            };
            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body
            })
            {
                smtp.Send(message);
            }
        }

        public static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public static string GenerateUser(string fullName)
        {

            List<string> lstName = fullName.ToLower().Split(" ").ToList();
            var acv = lstName.Remove("thi");
            switch (lstName.Count)
            {
                case 2:

                    break;
                case 3:

                    break;
                case 4:

                    break;
                case 5:

                    break;
            }

            return "";
        }
    }

    public class EmailNotifications
    {
        public string MaPhongBanCap1 { get; set; }

        public string TenPhongBanCap1 { get; set; }

        public string EmailNotifyReceiver { get; set; }

        public List<string> ListUsers { get; set; }
    }
}