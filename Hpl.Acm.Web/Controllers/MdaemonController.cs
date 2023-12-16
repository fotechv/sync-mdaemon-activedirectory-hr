using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hpl.Common.MdaemonServices;
using Hpl.HrmDatabase.Services;
using Hpl.HrmDatabase.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;
using System.Text.Json.Serialization;
using Hpl.Common.Models;
using Unosquare.PassCore.Common;

namespace Hpl.Acm.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MdaemonController : ControllerBase
    {
        // GET: api/<MdaemonController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "Server connecting..." };
        }

        [HttpGet]
        [Route("GetUserInfo")]
        public async Task<string> GetUserInfo(string username)
        {
            var res = await MdaemonXmlApi.GetUserInfo(username);
            return JsonConvert.SerializeObject(res);
        }

        [HttpGet]
        [Route("FixEmailChuaDuocTao")]
        public async Task<string> FixEmailChuaDuocTao()
        {
            var res = await MdaemonXmlApi.FixEmailChuaDuocTao();
            return JsonConvert.SerializeObject(res);
        }

        [HttpGet]
        [Route("CreateEmailByUserName")]
        public async Task<string> CreateEmailByUserName(string userName)
        {
            var res = await MdaemonXmlApi.CreateUserByUserName(userName);

            return JsonConvert.SerializeObject(res);
        }

        [HttpGet]
        [Route("DeleteEmailByUserName")]
        public async Task<string> DeleteEmailByUserName(string userName, string token)
        {
            string delMess = "N/A";
            //var res = await MdaemonXmlApi.DeleteUserByUserName(userName);
            //try
            //{
            //    var json = JsonConvert.SerializeObject(res);
            //    JObject o = JObject.Parse(json);
            //    var message = o["Payload"]?["MDaemon"]?["API"]?["Response"]?["Status"]?["@message"];
            //    //User not found (haiphatland.com.vn\\baonx)
            //    //The operation completed successfully.
            //    if (message != null)
            //    {
            //        string strMes = message.ToString();
            //        string strCheck1 = "User not found (haiphatland.com.vn\\" + userName + ")";
            //        if (strCheck1.Equals(strMes))
            //        {
            //            delMess = "Không tồn tại";
            //        }
            //        else
            //        {
            //            string strCheck2 = "The operation completed successfully.";
            //            if (strCheck2.Equals(strMes))
            //            {
            //                delMess = "Đã xóa";
            //            }
            //        }
            //    }

            //}
            //catch (Exception e)
            //{
            //    delMess = e.Message;
            //}

            return delMess;
        }

        [HttpGet]
        [Route("CreateUser")]
        public async Task<string> CreateUser()
        {
            //var listUser = UtilityHelpers.CreateUserDemo();

            //var res = await MdaemonXmlApi.CreateUser(listUser);
            return JsonConvert.SerializeObject("res");
        }

        [HttpGet]
        [Route("DeleleEmailNoUse31082021")]
        public async Task<string> DeleleEmailNoUse31082021()
        {
            try
            {
                List<object> listRes = new List<object>();

                //var listLog3108 = AbpServices.GetAllEmailCanXoa31082021();

                ////XÓA DANH SÁCH EMAIL
                //foreach (var item in listLog3108)
                //{
                //    try
                //    {
                //        string username = item.Email.Split("@")[0];
                //        var res2 = await MdaemonXmlApi.DeleteUserByUserName(username);
                //        listRes.Add(res2.Payload);

                //        AbpServices.AddDeleteEmailDoTaoLoi(item.Email);
                //    }
                //    catch (Exception e)
                //    {
                //        listRes.Add(e.Message);
                //    }
                //}

                //return JsonConvert.SerializeObject(listEmailNotUse1408);
                return JsonConvert.SerializeObject(listRes);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(e);
            }
        }

        [HttpGet]
        [Route("DeleleEmailCreate14082021")]
        public async Task<string> DeleleEmailCreate14082021()
        {
            try
            {
                var res = await MdaemonXmlApi.GetDomainList();
                dynamic payload = JObject.Parse(JsonConvert.SerializeObject(res.Payload));
                dynamic mdaemon = JObject.Parse(payload.MDaemon.ToString());
                dynamic api = JObject.Parse(mdaemon.API.ToString());
                dynamic response = JObject.Parse(api.Response.ToString());
                dynamic result = JObject.Parse(response.Result.ToString());
                //dynamic domains = JObject.Parse(result.Domains.ToString());
                string str = result.Domains.ToString();
                str = str.Replace("@", "");

                List<object> listRes = new List<object>();

                Domains domains = JsonConvert.DeserializeObject<Domains>(str);
                if (domains != null)
                {
                    //var domainHpl = domains.Domain.FirstOrDefault(x => x.id == "haiphatland.com.vn");
                    //if (domainHpl != null)
                    //{
                    //    var listEmailServer = domainHpl.Users.User;
                    //    List<string> listEmailNew = new List<string>();
                    //    foreach (var email in listEmailServer)
                    //    {
                    //        listEmailNew.Add(email.id + "@haiphatland.com.vn");
                    //    }

                    //    //var listImport = UserService.GetAllEmailImport();
                    //    var listImport = UserService.GetAllNhanVienDangLamViec();
                    //    var listEmailImport = listImport.Select(x => x.Email).ToList();

                    //    var listNotUse = (from e in listEmailNew
                    //                      where !listEmailImport.Contains(e)
                    //                      select e).ToList();

                    //    DateTime dt1 = new DateTime(2021, 7, 1);
                    //    DateTime dt2 = DateTime.Now;
                    //    var listLog1408 = AbpServices.GetHplNhanVienLogByDateCreate(dt1, dt2).Select(x => x.Email).ToList();

                    //    List<string> listEmailNotUse1408 = (from e in listLog1408
                    //                                        where listNotUse.Contains(e)
                    //                                        select e).ToList();
                    //    //XÓA DANH SÁCH EMAIL
                    //    foreach (var email in listEmailNotUse1408)
                    //    {
                    //        try
                    //        {
                    //            string username = email.Split("@")[0];
                    //            var res2 = await MdaemonXmlApi.DeleteUserByUserName(username);
                    //            listRes.Add(res2.Payload);

                    //            AbpServices.AddDeleteEmailDoTaoLoi(email);
                    //        }
                    //        catch (Exception e)
                    //        {
                    //            listRes.Add(e.Message);
                    //        }

                    //    }

                    //    //return JsonConvert.SerializeObject(listEmailNotUse1408);
                    //    return JsonConvert.SerializeObject(listRes);
                    //}
                }
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(e);
            }

            return "Không tồn tại domain";
        }

        [HttpGet]
        [Route("DanhSachEmailKhongDung")]
        public async Task<string> DanhSachEmailKhongDung()
        {
            var resultRes = new ApiResult();

            try
            {
                var res = await MdaemonXmlApi.GetDomainList();
                dynamic payload = JObject.Parse(JsonConvert.SerializeObject(res.Payload));
                dynamic mdaemon = JObject.Parse(payload.MDaemon.ToString());
                dynamic api = JObject.Parse(mdaemon.API.ToString());
                dynamic response = JObject.Parse(api.Response.ToString());
                dynamic result = JObject.Parse(response.Result.ToString());
                //dynamic domains = JObject.Parse(result.Domains.ToString());
                string str = result.Domains.ToString();
                str = str.Replace("@", "");

                Domains domains = JsonConvert.DeserializeObject<Domains>(str);
                if (domains != null)
                {
                    var domainHpl = domains.Domain.FirstOrDefault(x => x.id == "haiphatland.com.vn");
                    if (domainHpl != null)
                    {
                        var listEmailServer = domainHpl.Users.User;
                        List<string> listEmailServer2 = new List<string>();
                        foreach (var email in listEmailServer)
                        {
                            listEmailServer2.Add(email.id + "@haiphatland.com.vn");
                        }
                        var blackList = AbpServices.GetAllEmailBlackList();

                        //var listEmailServe3 = (from e in listEmailServer2
                        //                       where !blackList.Contains(e)
                        //                       select e).ToList();

                        var listEmailHrm = UserService.GetAllEmailHrm();

                        var listNotUse = (from e in listEmailServer2
                                          where !listEmailHrm.Contains(e)
                                          select e).ToList();
                        var listNotUse2 = (from e in listNotUse
                                           where !blackList.Contains(e)
                                           select e).ToList();

                        resultRes.Errors.Add(new ApiErrorItem(ApiErrorCode.Generic, "Tổng email không có trên HRM: " + listNotUse2.Count));
                        resultRes.Payload = listNotUse2;

                        //LẤY THÔNG TIN TRÊN HRM CỦA CÁC EMAIL TƯƠNG ỨNG TRÊN SERVER (CHƯA CẦN)
                        //var nv = new NhanVienViewModel2
                        //{
                        //    Ho = "N/A",
                        //    MaNhanVien = "N/A"
                        //};

                        //int i = 0;
                        //foreach (var email in listEmailServer)
                        //{
                        //    i++;
                        //    var temp = UserService.GetEmail(email.id);

                        //    if (temp != null)
                        //    {
                        //        nv = UserService.GetNhanVienByEmailAndMaNhanVien(temp.Email, temp.MaNhanVien);
                        //    }
                        //    else
                        //    {
                        //        nv.Email = email.id;
                        //    }
                        //}

                        //return JsonConvert.SerializeObject(listEmailServer);
                        return JsonConvert.SerializeObject(resultRes);
                    }
                }
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject("LỖI: " + e);
            }

            return JsonConvert.SerializeObject("OK");
        }

        [HttpGet]
        [Route("UpdateEmailAndUserHrm")]
        public string UpdateEmailAndUserHrm()
        {
            var res = UserService.UpdateEmailAndUserHrm();
            return JsonConvert.SerializeObject(res);
        }

        [HttpGet]
        [Route("GetDomainList")]
        public async Task<string> GetDomainList()
        {
            try
            {
                var res = await MdaemonXmlApi.GetDomainList();
                //return JsonConvert.SerializeObject(res.Payload);

                dynamic payload = JObject.Parse(JsonConvert.SerializeObject(res.Payload));
                dynamic mdaemon = JObject.Parse(payload.MDaemon.ToString());
                dynamic api = JObject.Parse(mdaemon.API.ToString());
                dynamic response = JObject.Parse(api.Response.ToString());
                dynamic result = JObject.Parse(response.Result.ToString());
                dynamic domains = JObject.Parse(result.Domains.ToString());
                string str = domains.ToString();
                str = str.Replace("@", "");

                Domains lstDomains = JsonConvert.DeserializeObject<Domains>(str);
                //Domains domains3 = (Domains)JsonConvert.DeserializeObject(str, typeof(Domains));
                //foreach (var item in domains2.Domain)
                //{
                //    string domain = item.id;
                //    var listUser = item.Users;
                //    if (item.id.Equals("haiphatland.com.vn"))
                //    {
                //        return JsonConvert.SerializeObject(item.Users.User);
                //    }
                //}

                return JsonConvert.SerializeObject(lstDomains);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(e);
            }
        }

        [HttpGet]
        [Route("MailingListCountUsers")]
        public async Task<string> MailingListCountUsers(string listName)
        {
            var res = await MdaemonXmlApi.MailingListCountUsers(listName);
            return JsonConvert.SerializeObject(res);
        }

        [HttpGet]
        [Route("MailingGetListInfo")]
        public async Task<string> MailingGetListInfo(string listName)
        {
            var res = await MdaemonXmlApi.MailingGetListInfo(listName);
            //var listMailHpl = 
            return JsonConvert.SerializeObject(res);
        }

        [HttpGet]
        [Route("MailingCreateList")]
        public async Task<string> MailingCreateList(string listName)
        {
            var res = await MdaemonXmlApi.MailingCreateList(listName, "Đây là mô tả");
            return JsonConvert.SerializeObject(res);
        }

        //[HttpGet]
        //[Route("MailingUpdateList")]
        //public async Task<string> MailingUpdateList(string listName)
        //{
        //    var res = await MdaemonXmlApi.MailingUpdateList(listName);
        //    return JsonConvert.SerializeObject(res);
        //}

        // GET api/<MdaemonController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<MdaemonController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<MdaemonController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<MdaemonController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
