using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Web.Http.Results;
using System.Net.Http.Formatting;
using System.IO;
using DHI.Services.ARRWebPortal;
using System.IO.Compression;
using Newtonsoft.Json.Linq;

namespace DHI.ARRWebPortal.WebApi.Deploy.Controllers
{
    [EnableCors("*", "*", "*")]
    [RoutePrefix("api/accountarr")]
    public class AccountArrController : ApiController
    {        
        [Route("validation")]
        [AllowAnonymous]
        [HttpPost]
        public async Task<HttpResponseMessage> Post()
        {            
            try
            {
                var queryParameters = Request.GetQueryNameValuePairs().ToDictionary(pair => pair.Key.ToLowerInvariant(), pair => pair.Value);

                var task = Request.Content.ReadAsStringAsync();
                task.Wait();
                JObject jObj = JObject.Parse(task.Result);
                string id = jObj["Id"].ToString();
                string password = jObj["Password"].ToString();
                                        
                JObject jObject = new JObject();
                jObject.Add("Name", id);
                jObject.Add("Roles", "Legend");

                Logging.Log(id);

                var result = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(jObject.ToString()) };
                result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                return result;
            }
            catch (Exception exception)
            {
                try
                {
                    Logging.LogException(exception);
                }
                catch (Exception ex)
                {

                }

                var result = new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent(exception.Message) };
                result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                return result;
            }
        }
    }
}