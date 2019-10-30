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

namespace DHI.Services.ARRWebPortal.WebApi
{
    [EnableCors("*", "*", "*")]
    [RoutePrefix("api/modelsetup")]
    public class ModelSetupController : ApiController
    {
        [HttpGet]
        public HttpResponseMessage Get()
        {
            try
            {                
                var queryParameters = Request.GetQueryNameValuePairs().ToDictionary(pair => pair.Key.ToLowerInvariant(), pair => pair.Value);

                List<Stream> streamList = new List<Stream>();
                List<string> fileNameList = new List<string>();

                Stream zipStream = null;

                string dataBase64String = string.Empty;
                if (queryParameters.ContainsKey("user") && queryParameters.ContainsKey("filename") && queryParameters.ContainsKey("type"))
                {
                    dataBase64String = Dfs0SqlCache.GetDfs0(queryParameters["user"], queryParameters["filename"], queryParameters["type"]);
                }
                else if (queryParameters.ContainsKey("probabilitycategory"))
                {
                    string probability64String = Dfs0SqlCache.GetDfs0(queryParameters["user"], queryParameters["latitude"] + ";" + queryParameters["longitude"], Definition.RainfallResultsLookup);
                    byte[] byteArray = Convert.FromBase64String(probability64String);
                    zipStream = new MemoryStream(byteArray);
                }
                
                if (queryParameters.ContainsKey("image"))
                {
                    byte[] byteArray = Convert.FromBase64String(dataBase64String);

                    Stream outputStream = queryParameters["type"] == Definition.IndexFileType.dfs2.ToString() ? GridProcess.GetIndexMapImage(byteArray) : (queryParameters["type"] == Definition.IndexFileType.dfsu.ToString() ? MeshProcess.GetIndexMapImage(byteArray) : NetworkProcess.GetNetworkMapImage(byteArray));

                    var result = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(outputStream) };
                    result.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                    result.Content.Headers.ContentDisposition =
                        new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
                        {
                            FileName = "IndexMap.png"
                        };
                    return result;

                }
                else if (queryParameters.ContainsKey("ensembles"))
                {
                    zipStream = new MemoryStream(File.ReadAllBytes(Path.Combine(Definition.FolderPrefix, queryParameters["user"], queryParameters["filename"])));
                }
                else if (!string.IsNullOrEmpty(dataBase64String))
                {
                    byte[] byteArray = Convert.FromBase64String(dataBase64String);
                    Stream stream = new MemoryStream(byteArray);

                    streamList.Add(stream);
                    fileNameList.Add("IndexFile." + queryParameters["type"]);
                }
                else if (queryParameters.ContainsKey("filename"))
                {
                    RetrieveDfs0 retrieveDfs0 = new RetrieveDfs0(queryParameters);
                    streamList = retrieveDfs0.StreamList();
                    fileNameList = retrieveDfs0.FileNameList();
                }

                if (zipStream == null && streamList.Count > 0)
                {
                    zipStream = ProcessModelSetup.ZipStreamList(streamList, fileNameList);
                }

                if (zipStream != null)
                {
                    zipStream.Position = 0;
                    
                    var result = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(zipStream) };
                    result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    result.Content.Headers.ContentDisposition =
                        new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
                        {
                            FileName = "ZippedSetup.zip"
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
        public async Task<HttpResponseMessage> Post()
        {
            try
            {
                var queryParameters = Request.GetQueryNameValuePairs().ToDictionary(pair => pair.Key.ToLowerInvariant(), pair => pair.Value);
                
                if (queryParameters.ContainsKey("probabilitycategory"))
                {
                    var task = Request.Content.ReadAsStringAsync();
                    task.Wait();
                    JObject jObj = JObject.Parse(task.Result);
                    queryParameters["ifdtable"] = jObj["ifdTableString"].ToString();
                    queryParameters["probabilities"] = jObj["probabilities"].ToString();

                    TemporalPatternDfs0 temporalPatternDfs0 = new TemporalPatternDfs0(queryParameters);
                    List<Stream> streamList = temporalPatternDfs0.StreamList();
                    List<string> fileNameList = temporalPatternDfs0.FileNameList();

                    Stream zipStream = ProcessModelSetup.ZipStreamList(streamList, fileNameList);
                    ProcessModelSetup.SetModelSetupSQL(queryParameters["user"], queryParameters["latitude"] + ";" + queryParameters["longitude"], Definition.RainfallResultsLookup, zipStream);

                    JObject jObject = new JObject();
                    jObject.Add("Message", "Ok");

                    var result = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(jObject.ToString()) };
                    result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    return result;
                }
                //else if (queryParameters.ContainsKey("execute"))
                //{
                //    JObject jObject = new JObject();

                //    if (Directory.Exists(Path.Combine(Definition.FolderPrefix, queryParameters["user"], Path.GetFileNameWithoutExtension(queryParameters["filename"]))))
                //    {
                //        jObject.Add("Message", "Error: Model Setupe Folder Exists, has it been ran already");
                //    }
                //    else
                //    {
                //        ProcessModelSetup.UnZipStreamToFile(new MemoryStream(File.ReadAllBytes(Path.Combine(Definition.FolderPrefix, queryParameters["user"], queryParameters["filename"]))), Path.Combine(Definition.FolderPrefix, queryParameters["user"], Path.GetFileNameWithoutExtension(queryParameters["filename"])));

                //        Process process = new Process();
                //        process.StartInfo.WorkingDirectory = Path.Combine(Definition.FolderPrefix, queryParameters["user"], Path.GetFileNameWithoutExtension(queryParameters["filename"]), Path.GetFileNameWithoutExtension(queryParameters["filename"]));
                //        process.StartInfo.FileName = "Run Ensemble.bat";
                //        process.Start();
                        
                //        jObject.Add("Message", "Ensemble Execution Started");
                //    }
                    
                //    var result = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(jObject.ToString()) };
                //    result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                //    return result;
                //}
                else if (queryParameters.ContainsKey("ensembles"))
                {
                    var task = Request.Content.ReadAsStringAsync();
                    task.Wait();
                    JObject jObj = JObject.Parse(task.Result);
                    if (jObj["r1d"] != null) { queryParameters["r1d"] = jObj["r1d"].ToString(); }
                    if (jObj["r2d"] != null) { queryParameters["r2d"] = jObj["r2d"].ToString(); }
                    if (jObj["z1d"] != null) { queryParameters["z1d"] = jObj["z1d"].ToString(); }
                    if (jObj["z2d"] != null) { queryParameters["z2d"] = jObj["z2d"].ToString(); }
                    if (jObj["Chainages"] != null) { queryParameters["Chainages"] = jObj["Chainages"].ToString(); }
                    
                    string dataBase64String = Dfs0SqlCache.GetDfs0(queryParameters["user"], queryParameters["filename"], queryParameters["type"]);
                    byte[] byteArray = Convert.FromBase64String(dataBase64String);

                    Stream zipStream = ProcessModelSetup.RandomizeModelSetup(queryParameters, new MemoryStream(byteArray));
                    if(!Directory.Exists(Path.Combine(Definition.FolderPrefix, queryParameters["user"])))
                    {
                        Directory.CreateDirectory(Path.Combine(Definition.FolderPrefix, queryParameters["user"]));
                    }
                    GridProcess.Stream2File(zipStream, Path.Combine(Definition.FolderPrefix, queryParameters["user"], queryParameters["filename"]));

                    JObject jObject = new JObject();
                    jObject.Add("Message", "Randomised successfully");

                    var result = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(jObject.ToString()) };
                    result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    return result;
                }
                else if (queryParameters.ContainsKey("tocsv"))
                {
                    var provider = new MultipartMemoryStreamProvider();
                    await Request.Content.ReadAsMultipartAsync(provider);
                    var stream = await provider.Contents[0].ReadAsStreamAsync();

                    GridProcess.Stream2File(stream, Path.Combine(Definition.FolderPrefix, "latest.zip"));

                    string response = queryParameters["filename"].EndsWith(Definition.IndexFileType.dfs2.ToString()) ? GridProcess.CreateCsv(stream, Convert.ToInt32(queryParameters["timestep"])) : MeshProcess.CreateCsv(stream);

                    var result = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(response) };
                    result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    return result;
                }
                else
                {
                    var provider = new MultipartMemoryStreamProvider();
                    await Request.Content.ReadAsMultipartAsync(provider);
                    var stream = await provider.Contents[0].ReadAsStreamAsync();

                    ////
                    //testing Jim Logging.Log("test 3");
                    //GridProcess.Stream2File(stream, Path.Combine(Definition.FolderPrefix, queryParameters["user"], queryParameters["filename"]));
           
                    string responseString;
                    if (queryParameters.ContainsKey("start"))
                    {
                        responseString = SetDfs0.WriteDfs0File(queryParameters, stream);
                    }
                    else
                    {
                        responseString = ProcessModelSetup.DetermineModelType(queryParameters, stream);
                    }

                    var result = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(responseString) };
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

                //return Request.CreateErrorResponse(HttpStatusCode.BadRequest, exception.Message, exception);

                JObject jObject = new JObject();
                jObject.Add("Message", "Error: " + exception.Message);

                var result = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(jObject.ToString()) };
                result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                return result;
            }
        }
    }
}