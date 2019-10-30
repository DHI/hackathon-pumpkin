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

namespace DHI.ARRWebPortal.WebApi.Deploy.Controllers
{
    [EnableCors("*", "*", "*")]
    [RoutePrefix("api/ifdtable")]
    public class IfdTableController : ApiController
    {
        [HttpGet]
        public HttpResponseMessage Get()
        {            
            try 
            { 
                var queryParameters = Request.GetQueryNameValuePairs().ToDictionary(pair => pair.Key.ToLowerInvariant(), pair => pair.Value);
                
                string latitude = queryParameters["latitude"];
                string longitude = queryParameters["longitude"];
                string userId = queryParameters["user"];

                IfdTableRequest ifdTableRequest = new IfdTableRequest(userId, latitude, longitude);
                string responseString;
                bool testUrl = false;
                if (queryParameters.ContainsKey("test") && bool.TryParse(queryParameters["test"], out testUrl) && testUrl)
                {
                    responseString = ifdTableRequest.GetIfdTable("http://au.dhigroup.com/arr/frequentInfrequent.csv", "http://au.dhigroup.com/arr/very frequent.csv", "http://au.dhigroup.com/arr/rare.csv");
                }
                else
                {
                    responseString = ifdTableRequest.GetIfdTable();
                }

                var response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(responseString, System.Text.Encoding.UTF8, "application/json");
                return response;
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