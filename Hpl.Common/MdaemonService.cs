using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hpl.Common.MdaemonServices;
using Hpl.HrmDatabase.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hpl.Common
{
    public class MdaemonService
    {
        public async Task<int> CountEmailKhongDung()
        {
            var lstEmail = await DanhSachEmailKhongDung();
            return lstEmail.Count;
        }

        public async Task<List<string>> DanhSachEmailKhongDung()
        {
            var listNotUse2 = new List<string>();

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

                    var listEmailHrm = UserService.GetAllEmailHrm();

                    var listNotUse = (from e in listEmailServer2
                                      where !listEmailHrm.Contains(e)
                                      select e).ToList();
                    listNotUse2 = (from e in listNotUse
                                       where !blackList.Contains(e)
                                       select e).ToList();
                }
            }

            return listNotUse2;
        }
    }
}