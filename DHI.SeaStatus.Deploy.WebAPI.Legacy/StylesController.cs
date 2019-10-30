using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using DHI.SeaStatus.Business;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DHI.SeaStatus.Deploy.WebAPI
{
    [EnableCors("*", "*", "*")]
    [RoutePrefix("api/Styles")]
    public class StylesController : ApiController
    {
        [AllowAnonymous]
        [HttpGet]
        [Route("GetContourIntervals")]
        public List<double> Get(string style)
        {
            //Debugger.Launch();
            var stylesFile = HttpContext.Current.Server.MapPath(@"~\") + @"App_Data\styles.json";
            var data = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(stylesFile));
            var styleCode = data[style]["StyleCode"].ToString();
            var styleDictionary = new Styles().GetPaletteFromCode(styleCode, 1);

            return styleDictionary.Keys.ToList();
        }

    }

}