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
    [RoutePrefix("api/Dfs0Converter")]
    public class Dfs0ConverterController : ApiController
    {        
        [HttpGet]
        public HttpResponseMessage Get()
        {
            try
            {            
                var queryParameters = Request.GetQueryNameValuePairs().ToDictionary(pair => pair.Key.ToLowerInvariant(), pair => pair.Value);

                List<Stream> streamList;
                List<string> fileNameList;

                if(queryParameters.ContainsKey("filename"))
                {
                    RetrieveDfs0 retrieveDfs0 = new RetrieveDfs0(queryParameters);
                    streamList = retrieveDfs0.StreamList();
                    fileNameList = retrieveDfs0.FileNameList();
                }
                else
                {
                    TemporalPatternDfs0 temporalPatternDfs0 = new TemporalPatternDfs0(queryParameters);
                    streamList = temporalPatternDfs0.StreamList();
                    fileNameList = temporalPatternDfs0.FileNameList();
                }
                
                if (streamList.Count > 0)
                {                
                    var ms = new MemoryStream();
                    var zipArchive = new ZipArchive(ms, ZipArchiveMode.Create);
                    for (var i = 0; i < fileNameList.Count; i++)
                    {
                        var entry = zipArchive.CreateEntry(fileNameList[i], CompressionLevel.Fastest);
                        using (var entryStream = entry.Open())
                        {
                            streamList[i].CopyTo(entryStream);
                        }
                    }
                       
                    ms.Position = 0;
                    var result = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(ms) };
                    result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    result.Content.Headers.ContentDisposition =
                        new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
                        {
                            FileName = "HydrologyDfs0s.zip"
                        };
                    return result;
                }
                else
                {
                    var result = new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("Empty Result Set") };
                    result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    return result;
                }
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
        
        [AllowAnonymous]
        [HttpPost]
        public async Task<string> Post()
        {
            var task = Request.Content.ReadAsStreamAsync();
            task.Wait();

            var queryParameters = Request.GetQueryNameValuePairs().ToDictionary(pair => pair.Key.ToLowerInvariant(), pair => pair.Value);
            string fileName = SetDfs0.WriteDfs0File(queryParameters, task.Result);

            return fileName;
        }
    }
}