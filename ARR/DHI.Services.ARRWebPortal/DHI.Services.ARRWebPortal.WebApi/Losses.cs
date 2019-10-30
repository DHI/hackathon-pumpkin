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

namespace DHI.Services.ARRWebPortal.WebApi
{
    [EnableCors("*", "*", "*")]
    [RoutePrefix("api/losses")]
    public class LossesController : ApiController
    {
        [HttpGet]
        public HttpResponseMessage Get()
        {
            var queryParameters = Request.GetQueryNameValuePairs().ToDictionary(pair => pair.Key.ToLowerInvariant(), pair => pair.Value);

            string userId = queryParameters["user"];
            string latitude = queryParameters["latitude"];
            string longitude = queryParameters["longitude"];

            LossesRequest lossesRequest = new LossesRequest(userId, latitude, longitude);
            string responseString = lossesRequest.GetLossesInfo();

            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(responseString, System.Text.Encoding.UTF8, "application/json");
            return response;
        }
    }
}