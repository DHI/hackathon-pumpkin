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

namespace DHI.Services.ARRWebPortal.WebApi
{
    [EnableCors("*", "*", "*")]
    [RoutePrefix("api/stochasticmanager")]
    public class StochasticManagerController : ApiController
    {                
        [AllowAnonymous]
        [HttpPost]
        public async Task<string> Post()
        {            
            var provider = new MultipartMemoryStreamProvider();
            await Request.Content.ReadAsMultipartAsync(provider);
            var stream = await provider.Contents[0].ReadAsStreamAsync();

            var queryParameters = Request.GetQueryNameValuePairs().ToDictionary(pair => pair.Key.ToLowerInvariant(), pair => pair.Value);
            List<KeyValuePair<string, Stream>> streamList = ProcessModelSetup.GetZipEtries(stream);

            return ProcessModelSetup.DetermineModelType(streamList).ToString();
        }
    }
}