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
using Newtonsoft.Json;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Web.Http.Results;
using System.Net.Http.Formatting;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Collections.Specialized;

namespace DHI.Services.ARRWebPortal
{
    public class IfdTableRequest
    {
        private List<KeyValuePair<string, string>> _frequentInfrequent = new List<KeyValuePair<string, string>> { 
            new KeyValuePair<string, string> ("coordinate_type", "dd"),
            new KeyValuePair<string, string> ("sdmin", "true"),
            new KeyValuePair<string, string> ("sdhr", "true"),
            new KeyValuePair<string, string> ("sdday", "true"),
            new KeyValuePair<string, string> ("user_label", string.Empty),
            new KeyValuePair<string, string> ("year", "2016")
        };
        
        private List<KeyValuePair<string, string>> _veryFrequent = new List<KeyValuePair<string, string>> { 
            new KeyValuePair<string, string> ("design", "very_frequent"),
            new KeyValuePair<string, string> ("sdmin", "true"),
            new KeyValuePair<string, string> ("sdhr", "true"),
            new KeyValuePair<string, string> ("sdday", "true"),
            new KeyValuePair<string, string> ("nsd%5B%5D", string.Empty),
            new KeyValuePair<string, string> ("nsdunit%5B%5D", "m"),
            new KeyValuePair<string, string> ("coordinate_type", "dd"),
            new KeyValuePair<string, string> ("user_label", string.Empty),
            new KeyValuePair<string, string> ("values", "depths"),
            new KeyValuePair<string, string> ("update", string.Empty),
            new KeyValuePair<string, string> ("year", "2016")
        };
        private List<KeyValuePair<string,string>> _rare = new List<KeyValuePair<string,string>> { 
            new KeyValuePair<string, string> ("design", "rare"),
            new KeyValuePair<string, string> ("sdmin", "true"),
            new KeyValuePair<string, string> ("sdhr", "true"),
            new KeyValuePair<string, string> ("sdday", "true"),
            new KeyValuePair<string, string> ("coordinate_type", "dd"),
            new KeyValuePair<string, string> ("user_label", string.Empty),
            new KeyValuePair<string, string> ("values", "depths"),
            new KeyValuePair<string, string> ("update", string.Empty),
            new KeyValuePair<string, string> ("year", "2016")
        };
        private string _cookieString = "acknowledgedConditions=true; acknowledgedCoordinateCaveat=true; ifdCookieTest=true; __utmt=1; __utma=172860464.1108135718.1538441292.1538441292.1538612594.2; __utmb=172860464.2.10.1538612594; __utmc=172860464; __utmz=172860464.1538441292.1.1.utmcsr=(direct)|utmccn=(direct)|utmcmd=(none)";
        
        private string _url = "http://www.bom.gov.au/water/designRainfalls/revised-ifd/";
        private char[] _delimiters = new char[] { '\n', '\r' };
        private string _styleCell = "border-left:1px solid lightgray; overflow:hidden;";
        private static string _backgroundRowColor = "background-color: #F0FAF9;";
        private string _styleRow = "border-bottom:1px solid lightgray; " + _backgroundRowColor;

        private string _userId;
        private string _latitide;
        private string _longitude;

        private List<KeyValuePair<string, double>> _ifdFrequentInfrequent;
        private List<KeyValuePair<string, double>> _ifdVeryfrequent;
        private List<KeyValuePair<string, double>> _ifdRare;  

        public IfdTableRequest(string userId, string latitude, string longitude)
        {
            _userId = _userId;
            _latitide = latitude;
            _longitude = longitude;
        }

        public string GetIfdTable()
        {
            _ifdFrequentInfrequent = _getMainPage(_frequentInfrequent, Definition.ProbabilityType.frequentInfrequent);
            _ifdVeryfrequent = _getMainPage(_veryFrequent, Definition.ProbabilityType.veryFrequent);
            _ifdRare = _getMainPage(_rare, Definition.ProbabilityType.rare);
            
            return _prepareJson();
        }

        public string GetIfdTable(string urlFrequentInfrequent, string urlVeryFrequent, string urlRare)
        {
            _ifdFrequentInfrequent = GetCsvData(urlFrequentInfrequent, Definition.ProbabilityType.frequentInfrequent);
            _ifdVeryfrequent = GetCsvData(urlVeryFrequent, Definition.ProbabilityType.veryFrequent);
            _ifdRare = GetCsvData(urlRare, Definition.ProbabilityType.rare);

            return _prepareJson();
        }

        private List<KeyValuePair<string, double>> _getMainPage(List<KeyValuePair<string, string>> paramList, Definition.ProbabilityType probabilityType)
        {
            WebClient client = new WebClient();
            client.Headers.Add(HttpRequestHeader.Cookie, _cookieString);

            //client.Headers.Add(HttpRequestHeader.ContentType, "text / html; charset = UTF - 8");

            client.Headers.Add(HttpRequestHeader.Accept, "image/webp,image/apng,image/*,*/*;q=0.8");
            client.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
            client.Headers.Add(HttpRequestHeader.AcceptLanguage, "en-GB,en;q=0.9,en-US;q=0.8,el;q=0.7,da;q=0.6");
            //client.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/69.0.3497.100 Safari/537.36");

            NameValueCollection nameValueCollection = new NameValueCollection();

            foreach (KeyValuePair<string, string> vp in paramList)
            {
                nameValueCollection.Add(vp.Key, vp.Value);
            }            

            nameValueCollection.Add("latitude", _latitide);
            nameValueCollection.Add("longitude", _longitude);

            client.QueryString = nameValueCollection;

            var bytes = client.DownloadData(_url);

            System.IO.Compression.GZipStream gZipStream = new System.IO.Compression.GZipStream(new MemoryStream(bytes), System.IO.Compression.CompressionMode.Decompress);
            MemoryStream memoryStream = new MemoryStream();
            gZipStream.CopyTo(memoryStream);
            var unZippedBytes = memoryStream.ToArray();

            gZipStream.Close();
            memoryStream.Close();


            string result = Encoding.UTF8.GetString(unZippedBytes);

            foreach (string line in result.Split(_delimiters))
            {
                if (line.Contains("Download as CSV"))
                {
                    foreach (string linePart in line.Split(' '))
                    {
                        if (linePart.Contains("href=") && linePart.Contains("save=table"))
                        {
                            string urlPrefix = _url + HttpUtility.HtmlDecode(linePart.Replace("href=", string.Empty).Replace(@"""", string.Empty));
                            return GetCsvData(urlPrefix, probabilityType);
                        }
                    }
                }
            }
            return new List<KeyValuePair<string, double>>();
        }

        public List<KeyValuePair<string, double>> GetCsvData(string url, Definition.ProbabilityType probabilityType)
        {
            WebClient client = new WebClient();
            client.Headers.Add(HttpRequestHeader.Cookie, _cookieString);

            client.Headers.Add(HttpRequestHeader.Accept, "image/webp,image/apng,image/*,*/*;q=0.8");
            client.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
            client.Headers.Add(HttpRequestHeader.AcceptLanguage, "en-GB,en;q=0.9,en-US;q=0.8,el;q=0.7,da;q=0.6");
            //client.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/69.0.3497.100 Safari/537.36");

            var bytes = client.DownloadData(url);

            System.IO.Compression.GZipStream gZipStream = new System.IO.Compression.GZipStream(new MemoryStream(bytes), System.IO.Compression.CompressionMode.Decompress);
            MemoryStream memoryStream = new MemoryStream();
            gZipStream.CopyTo(memoryStream);
            var unZippedBytes = memoryStream.ToArray();

            gZipStream.Close();
            memoryStream.Close();

            ProcessModelSetup.SetModelSetupSQL(_userId, _latitide + ";" + _longitude, probabilityType.ToString(), new MemoryStream(unZippedBytes));
            
            string response = Encoding.UTF8.GetString(unZippedBytes);

            List<KeyValuePair<string, double>> result = new List<KeyValuePair<string, double>>();

            string[] headers = null;

            bool capture = false;
            foreach (string line in response.Split(_delimiters))
            {
                if (line.Contains("Duration,Duration in min"))
                {
                    capture = true;
                    headers = line.Split(',');
                    continue;
                }
                if (capture)
                {
                    string[] lineSplit = line.Split(',');
                    for (int i = 2; i < lineSplit.Length; i++)
                    {
                        double value;
                        if (double.TryParse(lineSplit[i], out value))
                        {
                            result.Add(new KeyValuePair<string, double>((headers[i] + ";" + lineSplit[1]).Replace("\r", string.Empty), value));
                        }
                    }
                }
            }
            return result;
        }

        private string _prepareJson()
        {
            List<KeyValuePair<List<KeyValuePair<string, double>>, string>> bigList = new List<KeyValuePair<List<KeyValuePair<string, double>>, string>>();
            bigList.Add(new KeyValuePair<List<KeyValuePair<string, double>>, string>(_ifdFrequentInfrequent, "63.2%"));
            bigList.Add(new KeyValuePair<List<KeyValuePair<string, double>>, string>(_ifdFrequentInfrequent, "50%"));
            bigList.Add(new KeyValuePair<List<KeyValuePair<string, double>>, string>(_ifdVeryfrequent, "0.5EY"));
            bigList.Add(new KeyValuePair<List<KeyValuePair<string, double>>, string>(_ifdFrequentInfrequent, "20%"));
            bigList.Add(new KeyValuePair<List<KeyValuePair<string, double>>, string>(_ifdVeryfrequent, "0.2EY"));
            bigList.Add(new KeyValuePair<List<KeyValuePair<string, double>>, string>(_ifdFrequentInfrequent, "10%"));
            bigList.Add(new KeyValuePair<List<KeyValuePair<string, double>>, string>(_ifdFrequentInfrequent, "5%"));
            bigList.Add(new KeyValuePair<List<KeyValuePair<string, double>>, string>(_ifdFrequentInfrequent, "2%"));
            bigList.Add(new KeyValuePair<List<KeyValuePair<string, double>>, string>(_ifdFrequentInfrequent, "1%"));
            bigList.Add(new KeyValuePair<List<KeyValuePair<string, double>>, string>(_ifdRare, "1 in 200"));
            bigList.Add(new KeyValuePair<List<KeyValuePair<string, double>>, string>(_ifdRare, "1 in 500"));
            bigList.Add(new KeyValuePair<List<KeyValuePair<string, double>>, string>(_ifdRare, "1 in 1000"));
            bigList.Add(new KeyValuePair<List<KeyValuePair<string, double>>, string>(_ifdRare, "1 in 2000"));

            JArray jArray = new JArray();
            int row = 0;

            JArray lineArray = new JArray();
            for (int i = 0; i < 16; i++)
            {
                JObject jObject = new JObject();
                if (i == 1)
                {
                    jObject.Add("value", string.Empty);
                }
                else
                {
                    jObject.Add("value", false);
                    jObject.Add("editable", true);
                }
                jObject.Add("style", _styleCell);
                lineArray.Add(jObject);
            }
            JObject lineObject = new JObject();
            lineObject.Add("data", lineArray);
            lineObject.Add("style", (row % 2 == 0 ? _styleRow : _styleRow.Replace(_styleRow, string.Empty)) + "height:25px");
            jArray.Add(lineObject);
            row++;

            //------------------

            lineArray = new JArray();
            JObject blankObject = new JObject();
            blankObject.Add("value", string.Empty);
            blankObject.Add("style", _styleCell);
            lineArray.Add(blankObject);
            lineArray.Add(blankObject);
            for (int i = 0; i < 13; i++)
            {
                JObject jObject = new JObject();
                jObject.Add("value", Definition.Probabilities[i].Key.Split(new char[] { ',' })[0].Trim() + ",");
                jObject.Add("style", _styleCell);
                lineArray.Add(jObject);
            }

            JObject userARIObject = new JObject();
            userARIObject.Add("value", string.Empty);
            userARIObject.Add("editable", true);
            userARIObject.Add("suffix", "%");
            userARIObject.Add("style", _styleCell + ";font-weight:bold");
            lineArray.Add(userARIObject);

            lineObject = new JObject();
            lineObject.Add("data", lineArray);
            lineObject.Add("style", (row % 2 == 0 ? _styleRow : _styleRow.Replace(_styleRow, string.Empty)) + "height:50px;font-weight:bold");
            jArray.Add(lineObject);

            lineArray = new JArray();
            lineArray.Add(blankObject);
            lineArray.Add(blankObject);
            for (int i = 0; i < 13; i++)
            {
                JObject jObject = new JObject();
                jObject.Add("value", Definition.Probabilities[i].Key.Split(new char[] { ',' })[1].Trim());
                jObject.Add("style", _styleCell);
                lineArray.Add(jObject);
            }

            lineArray.Add(blankObject);

            lineObject = new JObject();
            lineObject.Add("data", lineArray);
            lineObject.Add("style", (row % 2 == 0 ? _styleRow : _styleRow.Replace(_styleRow, string.Empty)) + "height:25px;font-weight:bold");
            jArray.Add(lineObject);
            row++;

            //------------------
            
            foreach (int duration in Definition.Durations.Select(p => p.Value))
            {
                lineArray = new JArray();

                JObject jObject = new JObject();
                jObject.Add("value", false);
                jObject.Add("editable", true);
                jObject.Add("style", _styleCell);
                lineArray.Add(jObject);

                jObject = new JObject();
                jObject.Add("value", Definition.Durations[row - 2].Key);
                jObject.Add("style", _styleCell + "; font-weight: bold;width:150px;");
                lineArray.Add(jObject);

                foreach (var vp in bigList)
                { 
                    jObject = new JObject();
                    jObject.Add("value", _GetValue(vp.Key, vp.Value + ";" + duration));
                    jObject.Add("style", _styleCell);
                    lineArray.Add(jObject);
                }

                //add blank for user ARI
                lineArray.Add(blankObject);

                lineObject = new JObject();
                lineObject.Add("data", lineArray);
                lineObject.Add("style", (row % 2 == 0 ? _styleRow : _styleRow.Replace(_styleRow, string.Empty)) + "height:25px");
                jArray.Add(lineObject);
                row++;
            }
                                
            return jArray.ToString();
        }

        private string _GetValue(List<KeyValuePair<string, double>> listValues, string text)
        {
            if (listValues.Any(p => p.Key == text))
            {
                return listValues.First(p => p.Key == text).Value.ToString();
            }

            return string.Empty;
        }
    }
}
