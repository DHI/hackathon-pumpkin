using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHI.Services.ARRWebPortal
{
    public static class SetDfs0
    {
        public static string WriteDfs0File(Dictionary<string, string> queryParameters, Stream stream)
        {
            DateTime startDateTime = DateTime.Parse(queryParameters["start"]);

            string userId = queryParameters["user"];
            string fileName = queryParameters["filename"];
            string type = queryParameters["type"].ToLower();

            bool addRiverLevels = false;

            RaftsParser raftsParser = null;

            List<string> catchmentNames = new List<string>();
            List<KeyValuePair<DateTime, List<double>>> dataList = new List<KeyValuePair<DateTime,List<double>>>();

            if (fileName.EndsWith(Definition.ResultType.loc.ToString()))
            {
                raftsParser = new RaftsParser(stream, startDateTime, Definition.ResultType.loc);
            }
            else if (fileName.EndsWith(Definition.ResultType.tot.ToString()))
            {
                raftsParser = new RaftsParser(stream, startDateTime, Definition.ResultType.tot);
            }
            else
            {
                UrbsParser urbsParser = new UrbsParser(stream, startDateTime);
                if (urbsParser.IsUrbs())
                {
                    catchmentNames = urbsParser.GetCatchmentNames();
                    List<KeyValuePair<DateTime, List<double>>> dataListFlowRate = urbsParser.GetData(Definition.UrbsFlowRatesString);
                    List<KeyValuePair<DateTime, List<double>>> dataListRiverLevels = urbsParser.GetData(Definition.UrbsRiverLevelsString);
                    if (dataListRiverLevels.Count > 0)
                    {
                        addRiverLevels = true;
                        dataList = new List<KeyValuePair<DateTime, List<double>>>();
                        foreach (KeyValuePair<DateTime, List<double>> vp in dataListFlowRate)
                        {
                            dataList.Add(new KeyValuePair<DateTime, List<double>>(vp.Key, vp.Value.Concat(dataListRiverLevels.First(p => p.Key == vp.Key).Value).ToList()));
                        }
                    }
                    else
                    {
                        dataList = dataListFlowRate;
                    }
                }
                else
                {
                    RorbParser rorbParser = new RorbParser(stream, startDateTime);
                    catchmentNames = rorbParser.GetCatchmentNames();
                    dataList = rorbParser.GetData();
                }
            }

            byte[] byteArray;
            if (fileName.EndsWith(Definition.ResultType.loc.ToString()) || fileName.EndsWith(Definition.ResultType.tot.ToString()))
            {
                List<KeyValuePair<string, List<double>>> catchmentDataList = raftsParser.GetCatchmentData();
                List<Stream> streamList = new List<Stream>();
                List<string> fileNameList = new List<string>();

                foreach (DHI.Services.ARRWebPortal.RaftsParser.Storm storm in raftsParser.StormList)
                {
                    string newFileName = Path.GetFileNameWithoutExtension(fileName) + "_" + storm.Name + (fileName.EndsWith(Definition.ResultType.tot.ToString()) ? "_tot.dfs0" : "_loc.dfs0");
                    
                    catchmentNames = catchmentDataList.Select(p => p.Key).ToList();
                    dataList = raftsParser.GetData(storm);

                    string stormSuffix = "_" + storm.Name;
                    Dfs0Writer dfs0Write = new Dfs0Writer(dataList[0].Key, catchmentNames.Where(p => p.EndsWith(stormSuffix)).Select(p => p.Replace(stormSuffix, string.Empty)).ToList(), type, (dataList[1].Key - dataList[0].Key).TotalSeconds, addRiverLevels);
                    foreach (KeyValuePair<DateTime, List<double>> timeStep in dataList)
                    {
                        dfs0Write.AddData(timeStep.Key, timeStep.Value);
                    }
                    dfs0Write.Close();

                     //get stream
                    byteArray = File.ReadAllBytes(dfs0Write.FilePath());
                    streamList.Add(new MemoryStream(byteArray));
                    fileNameList.Add(newFileName);
                    dfs0Write.Dispose();
                }

                MemoryStream memoryStream = new MemoryStream();
                ProcessModelSetup.ZipStreamList(streamList, fileNameList).CopyTo(memoryStream);
                byteArray = memoryStream.ToArray();
            }
            else
            {
                Dfs0Writer dfs0Write = new Dfs0Writer(dataList[0].Key, catchmentNames, type, (dataList[1].Key - dataList[0].Key).TotalSeconds, addRiverLevels);
                foreach (KeyValuePair<DateTime, List<double>> timeStep in dataList)
                {
                    dfs0Write.AddData(timeStep.Key, timeStep.Value);
                }
                dfs0Write.Close();

                //get stream
                byteArray = File.ReadAllBytes(dfs0Write.FilePath());
                dfs0Write.Dispose();
            }

            string dfs0String = Convert.ToBase64String(byteArray);

            Dfs0SqlCache.SetDfs0(userId, fileName, type, dfs0String);

            JObject jObject = new JObject();
            jObject.Add("Message", fileName);
            return jObject.ToString();
        }
    }
}
