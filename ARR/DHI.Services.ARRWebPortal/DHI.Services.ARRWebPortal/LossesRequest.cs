using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Net.Http;
using System.Collections.Specialized;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace DHI.Services.ARRWebPortal
{
    public class LossesRequest
    {
        private string _userId;
        private string _latitide;
        private string _longitude;

        private char[] _delimiters = new char[] { ',' };
        
        public LossesRequest(string userId, string latitude, string longitude)
        {
            _userId = userId;
            _latitide = latitude;
            _longitude = longitude;
        }

        public string GetLossesInfo()
        {
            List<string> result = new List<string>();
            
            WebClient webClient = new WebClient();
            
            NameValueCollection nameValueCollection = new NameValueCollection();
            nameValueCollection.Add("lon_coord", _longitude);
            nameValueCollection.Add("lat_coord", _latitide);
            nameValueCollection.Add("All", "on");

            webClient.QueryString = nameValueCollection;

            var bytes = webClient.DownloadData(Definition.Url);

            MemoryStream stream = new MemoryStream(bytes);

            //JArray jArray = new JArray();
            JObject jObject = new JObject();

            StreamReader streamReader = new StreamReader(stream);
            while (!streamReader.EndOfStream)
            {
                string line = streamReader.ReadLine();

                foreach (string arfFactor in Definition.arf)
                {
                    string prefix = arfFactor + ",";

                    if (line.StartsWith(prefix))
                    {
                        jObject.Add(arfFactor, line.Split(_delimiters)[1]);
                    }
                }

                if (line.Contains(Definition.InitialLoss))
                {
                    jObject.Add(Definition.InitialLoss, line.Split(_delimiters)[1]);
                }

                if (line.Contains(Definition.ContinueLoss))
                {

                    jObject.Add(Definition.ContinueLoss, line.Split(_delimiters)[1]);
                }
            }

            stream.Position = 0;
            ProcessModelSetup.SetModelSetupSQL(_userId, _latitide + ";" + _longitude, Definition.ArfTextFile, stream);

            TemporalPatternDfs0 temporalPattern = new TemporalPatternDfs0(_userId, _latitide, _longitude);
            TemporalPatternRequest temporalPatternRequest = new TemporalPatternRequest(_latitide, _longitude);
            List<ArealTemporalPattern> arealTemporalPatternList = temporalPattern.GetArealTemporalPattern(temporalPatternRequest);
            jObject.Add(Definition.ArealTemporalLookup, arealTemporalPatternList.Select(p => p.Duration).Min());

            jObject.Add(Definition.ProbabilityCategoryType.area.ToString(), 75);

            return jObject.ToString();
        }
    }
}
