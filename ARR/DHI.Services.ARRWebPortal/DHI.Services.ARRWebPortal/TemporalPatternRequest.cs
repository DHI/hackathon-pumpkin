using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Net.Http;

namespace DHI.Services.ARRWebPortal
{
    public class TemporalPatternRequest
    {
        private string _latitide;
        private string _longitude;

        public TemporalPatternRequest(string latitude, string longitude)
        {
            _latitide = latitude;
            _longitude = longitude;
        }

        public List<string> GetTemporalPattern()
        {
            return _getPattern(Definition.TemporalPattern, Definition.TemporalLookup);
        }

        public List<string> GetArealTemporalPattern()
        {
            return _getPattern(Definition.ArealTemporalPattern, Definition.ArealTemporalLookup);
        }

        private List<string> _getPattern(string pattern, string lookupText)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "DHI ARR Webportal WebAPI Service");

            using (var content = new MultipartFormDataContent())
            {
                content.Add(new StringContent(_longitude), "lon_coord");
                content.Add(new StringContent(_latitide), "lat_coord");
                content.Add(new StringContent("on"), pattern);

                Task<HttpResponseMessage> message = client.PostAsync(Definition.Url, content);

                var input = message.Result.Content.ReadAsStringAsync();
                char[] delimiters = new char[] { '\n' };

                var lineArray = input.Result.Split(delimiters);
                foreach (var line in lineArray)
                {
                    if (line.Contains(lookupText) && line.Contains("Download (.zip)"))
                    {
                        //@"<h4>Temporal Patterns | <a href=\"./temporal_patterns/tp/Central_Slopes.zip\">Download (.zip)</a> </h4>"                 "

                        string linkPart = line.Split(new string[] { "href=\"./", "\">Download" }, StringSplitOptions.RemoveEmptyEntries)[1];
                        string newUrl = Definition.Url + linkPart;

                        return _getIncrements(newUrl, pattern);
                    }

                }
            }

            throw new Exception("Patern not received for longitude: " + _longitude + ", latitude: " + _latitide + ", pattern: " + pattern );
        }

        private List<string> _getIncrements(string url, string pattern)
        {
            List<string> result = new List<string>();
            
            WebClient client = new WebClient();
            var bytes = client.DownloadData(url);

            var stream = new MemoryStream(bytes);

            ZipArchive zipArchive = new ZipArchive(stream);

            StreamReader streamReader = new StreamReader(zipArchive.Entries[1].Open());
            while (!streamReader.EndOfStream)
            {
                result.Add(streamReader.ReadLine());
            }

            result.Insert(0, zipArchive.Entries[1].Name);

            return result;
        }

    }
}
