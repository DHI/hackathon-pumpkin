using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;

namespace DHI.SeaStatus.Deploy.WebAPI
{
    [EnableCors("*", "*", "*")]
    [RoutePrefix("api/Pdf")]
    public class PdfController : ApiController
    {
        [AllowAnonymous]
        [HttpPost]
        [ActionName("Complex")]
        public HttpResponseMessage PostComplex(PdfRequest body)
        {
            if (body.scenarioId != null)
            {                               
                Pdf pdf = new Pdf();
                var templateFileName = HttpContext.Current.Server.MapPath(@"~\App_Data\") + "ReportTemplate.xlsx";
                var directory = Path.Combine(HttpContext.Current.Server.MapPath(@"~\"), @"Scenarios");
                var logFile = Path.Combine(directory, body.scenarioId, "log.txt");
                var responseFileName = string.Empty;
                var stream = pdf.CreatePdfFile(templateFileName,
                    directory,
                    body.scenarioId,
                    body.scenarioData,
                    logFile,
                    out responseFileName);
                var result = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(stream) };
                result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = responseFileName
                };
                return result;
            }
            throw new Exception("Please provide scenario id");
        }
    }

    public class PdfRequest
    {
        public string scenarioId { get; set; }
        public string scenarioData { get; set; }
    }
}
