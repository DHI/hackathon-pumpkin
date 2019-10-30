using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHI.Services.ARRWebPortal
{
    public class RetrieveDfs0
    {
        private char[] _delimiters = new char[] { ',' };

        private string _userId;
        private List<string> _fileNameList = new List<string>();
        private List<string> _typeList = new List<string>();
        private List<KeyValuePair<string, string>> _combinedList = new List<KeyValuePair<string, string>>();

        private List<Stream> _streamList = new List<Stream>();

        public RetrieveDfs0(Dictionary<string, string> queryParameters)
        {
            _fileNameList = queryParameters["filename"].Split(_delimiters).ToList();
            _typeList = queryParameters["type"].Split(_delimiters).Select(p => p.ToLower()).ToList();
            _userId = queryParameters["user"];

            bool combineRafts = false;
            if (queryParameters.ContainsKey("combine") && bool.TryParse(queryParameters["combine"], out combineRafts) && combineRafts)
            {
                _combinedList = _getCombinedList();
                _removeCombined();
            }

            _prepareStreamList();
            _renameFileNames();

            _prepareStreamListWithCombined();
        }

        private List<KeyValuePair<string, string>> _getCombinedList()
        {
            List<KeyValuePair<string, string>> combinedList = new List<KeyValuePair<string, string>>();

            for (var i = 0; i < _fileNameList.Count; i++)
            {
                string fileName1 = _fileNameList[i];
                if (fileName1.EndsWith(Definition.ResultType.loc.ToString()))
                {
                    for (var j = 0; j < _fileNameList.Count; j++)
                    {
                        string fileName2 = _fileNameList[j];
                        if (fileName2.EndsWith(Definition.ResultType.tot.ToString()))
                        {
                            if (Path.GetFileNameWithoutExtension(fileName1) == Path.GetFileNameWithoutExtension(fileName2))
                            {
                                combinedList.Add(new KeyValuePair<string, string>(fileName1, fileName2));
                            }
                        }
                    }
                }
            }
            return combinedList;
        }

        private void _prepareStreamList()
        {
            List<string> newFileNameList = new List<string>();
            List<string> oldFileNameList = new List<string>();
            List<Stream> newStreamList = new List<Stream>();
            for (var i = 0; i < _fileNameList.Count; i++)
            {
                string fileName = _fileNameList[i];
                string type = _typeList[i];

                string dataBase64String = Dfs0SqlCache.GetDfs0(_userId, fileName, type);

                byte[] byteArray = Convert.FromBase64String(dataBase64String);
                Stream stream = new MemoryStream(byteArray);

                if (fileName.EndsWith(Definition.ResultType.loc.ToString()) || fileName.EndsWith(Definition.ResultType.tot.ToString()))
                {
                    oldFileNameList.Add(fileName);
                                        
                    var fileList = ProcessModelSetup.GetZipEtries(stream);

                    for (int j = 0; j < fileList.Count; j++)
                    {
                        newFileNameList.Add(fileList[j].Key);
                        MemoryStream memoryStream = new MemoryStream();
                        fileList[j].Value.CopyTo(memoryStream);
                        newStreamList.Add(memoryStream);
                    }
                }
                else
                {
                    _streamList.Add(stream);
                }
            }
            foreach(string oldFileName in oldFileNameList)
            {
                int indexOf = _fileNameList.IndexOf(oldFileName);
                _fileNameList.RemoveAt(indexOf);
            }
            _fileNameList.AddRange(newFileNameList);
            _streamList.AddRange(newStreamList);
        }

        private void _prepareStreamListWithCombined()
        {
            foreach (var vp in _combinedList)
            {
                string locFile = vp.Key;
                string totFile = vp.Value;

                string dataBase64ZippedStringLoc = Dfs0SqlCache.GetDfs0(_userId, locFile, Definition.ResultType.rafts.ToString());
                string dataBase64ZippedStringTot = Dfs0SqlCache.GetDfs0(_userId, totFile, Definition.ResultType.rafts.ToString());

                var locFiles = ProcessModelSetup.GetZipEtries(new MemoryStream(Convert.FromBase64String(dataBase64ZippedStringLoc)));
                var totFiles = ProcessModelSetup.GetZipEtries(new MemoryStream(Convert.FromBase64String(dataBase64ZippedStringTot)));

                for (int j = 0; j < locFiles.Count; j++)
                {
                    MemoryStream memoryStream = new MemoryStream();
                    locFiles[j].Value.CopyTo(memoryStream);
                    string dataBase64StringLoc = Convert.ToBase64String(memoryStream.ToArray());
                    memoryStream = new MemoryStream();
                    totFiles[j].Value.CopyTo(memoryStream);
                    string dataBase64StringTot = Convert.ToBase64String(memoryStream.ToArray());

                    List<KeyValuePair<DateTime, List<double>>> locList = Dfs0Reader.GetTSData(dataBase64StringLoc);
                    List<KeyValuePair<DateTime, List<double>>> totList = Dfs0Reader.GetTSData(dataBase64StringTot);

                    List<string> locTsNames = Dfs0Reader.GetTSItemNames(dataBase64StringLoc);
                    List<string> totTsNames = Dfs0Reader.GetTSItemNames(dataBase64StringTot);

                    //remove velocity from loc nad tot as we will create from scratch
                    locTsNames.RemoveAt(locTsNames.Count - 1);
                    totTsNames.RemoveAt(totTsNames.Count - 1);

                    string fileName = Path.GetTempFileName();
                    DateTime startDateTime = locList[0].Key;
                    List<string> catchmentNames = locTsNames.Concat(totTsNames).ToList();

                    Dfs0Writer dfs0Write = new Dfs0Writer(startDateTime, catchmentNames, "RAFTS", (locList[1].Key - startDateTime).TotalSeconds);

                    for (int i = 0; i < locList.Count; i++)
                    {
                        DateTime current = locList[i].Key;

                        //remove velocity from loc nad tot as we will create from scratch
                        locList[i].Value.RemoveAt(locList[i].Value.Count - 1);
                        totList[i].Value.RemoveAt(totList[i].Value.Count - 1);
                        List<double> values = locList[i].Value.Concat(totList[i].Value).ToList();

                        dfs0Write.AddData(current, values);
                    }

                    dfs0Write.Close();

                    byte[] byteArray = File.ReadAllBytes(dfs0Write.FilePath());

                    Stream stream = new MemoryStream(byteArray);
                    _streamList.Add(stream);
                    _fileNameList.Add(Path.GetFileNameWithoutExtension(locFiles[j].Key).Replace("_tot", string.Empty).Replace("_loc", string.Empty) + "_combined.dfs0");

                    dfs0Write.Dispose();
                }
            }
        }

        private void _renameFileNames()
        {
            for (var i = 0; i < _fileNameList.Count; i++)
            {
                _fileNameList[i] = Path.ChangeExtension(_fileNameList[i], ".dfs0");
            }
        }

        private void _removeCombined()
        {
            foreach (var vp in _combinedList)
            {
                int indexOf = _fileNameList.IndexOf(vp.Key);
                _fileNameList.RemoveAt(indexOf);
                _typeList.RemoveAt(indexOf);

                indexOf = _fileNameList.IndexOf(vp.Value);
                _fileNameList.RemoveAt(indexOf);
                _typeList.RemoveAt(indexOf);
            }
        }

        public List<string> FileNameList()
        {
            return _fileNameList;
        }

        public List<Stream> StreamList()
        {
            return _streamList;
        }
    }
}
