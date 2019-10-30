using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHI.Services.ARRWebPortal
{
    public class TemporalPatternDfs0
    {
        private char[] _delimiters = new char[] { ',' };

        private List<string> _fileNameList = new List<string>();

        private List<string> _durationList = new List<string>();
        private List<string> _probabiltyList = new List<string>();
        private List<string> _ifdValueList = new List<string>();

        private DateTime _startDateTime;
        private string _userId;
        private string _latitude;
        private string _longitude;
        private Definition.ProbabilityCategoryType _probablityCategory = Definition.ProbabilityCategoryType.point;
        private Definition.TailType _tailType = Definition.TailType.no;
        private double _initialLoss = 0;
        private double _continueLoss = 0;
        private double _area = 0;
        private double _a;
        private double _b;
        private double _c;
        private double _d;
        private double _e;
        private double _f;
        private double _g;
        private double _h;
        private double _i;
        
        private List<Stream> _streamList = new List<Stream>();

        private List<string> _tempPatternStringList = new List<string>();
        private List<string> _arealTempPatternStringList = new List<string>();

        private string _ifdTable = string.Empty;

        public TemporalPatternDfs0(string userId, string latitude, string longitude)
        {
            _userId = userId;
            _latitude = latitude;
            _longitude = longitude;
        }

        public TemporalPatternDfs0(Dictionary<string, string> queryParameters)
        {
            _startDateTime = DateTime.Parse(queryParameters["start"]);

            _userId = queryParameters["user"];
            _latitude = queryParameters["latitude"];
            _longitude = queryParameters["longitude"];

            _initialLoss = double.Parse(queryParameters["initial"]);
            _continueLoss = double.Parse(queryParameters["continue"]);

            if (Enum.TryParse(queryParameters["probabilitycategory"], out _probablityCategory) && _probablityCategory != Definition.ProbabilityCategoryType.point)
            {
                try 
                {
                    _area = double.Parse(queryParameters["area"]);
                }
                catch (Exception e)
                {
                    throw new Exception("Please ensure Area string is specified in correct format");
                }

            }

            _tailType = (Definition.TailType)Enum.Parse(typeof(Definition.TailType), queryParameters["tail"]);

            _probabiltyList = queryParameters["probabilities"].Split(_delimiters, StringSplitOptions.RemoveEmptyEntries).ToList();

            var arfCoefficients = queryParameters["arfcoefficients"].Split(_delimiters, StringSplitOptions.RemoveEmptyEntries).ToList();
            _a = double.Parse(arfCoefficients[0].Replace("E 00", string.Empty));
            _b = double.Parse(arfCoefficients[1].Replace("E 00", string.Empty));
            _c = double.Parse(arfCoefficients[2].Replace("E 00", string.Empty));
            _d = double.Parse(arfCoefficients[3].Replace("E 00", string.Empty));
            _e = double.Parse(arfCoefficients[4].Replace("E 00", string.Empty));
            _f = double.Parse(arfCoefficients[5].Replace("E 00", string.Empty));
            _g = double.Parse(arfCoefficients[6].Replace("E 00", string.Empty));
            _h = double.Parse(arfCoefficients[7].Replace("E 00", string.Empty));
            _i = double.Parse(arfCoefficients[8].Replace("E 00", string.Empty));
            
            _ifdTable = queryParameters["ifdtable"];

            _prepareStreamList();
        }

        private void _prepareStreamList()
        {
            TemporalPatternRequest temporalPatternRequest = new TemporalPatternRequest(_latitude, _longitude);

            List<TemporalPattern> temporalPatternList = GetTemporalPattern(temporalPatternRequest);

            List<ArealTemporalPattern> arealTemporalPatternList = GetArealTemporalPattern(temporalPatternRequest);

            //iterate each probability selected
            for (int i = 0; i < _probabiltyList.Count; i = i + 4)
            {
                string duration = _probabiltyList[i];
                string aep = _probabiltyList[i + 1] + "," + _probabiltyList[i + 2];
                double ifdValue = Convert.ToDouble(_probabiltyList[i + 3]);

                int patternIndex;
                if (_probablityCategory != Definition.ProbabilityCategoryType.area)
                {
                    List<TemporalPattern> relevantTemporalPatternList;

                    if (Definition.Probabilities.Any(q => q.Key == aep))
                    {
                        relevantTemporalPatternList = temporalPatternList.Where(p => p.Duration == Definition.Durations.First(q => q.Key == duration).Value && p.AEP == Definition.Probabilities.First(q => q.Key == aep).Value).ToList();
                    }
                    else
                    {
                        relevantTemporalPatternList = temporalPatternList.Where(p => p.Duration == Definition.Durations.First(q => q.Key == duration).Value && p.AEP == Definition.GetProbability(Convert.ToDouble(_probabiltyList[i + 1].Replace(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.PercentSymbol, string.Empty)))).ToList();
                    }

                    patternIndex = 1;
                    foreach (TemporalPattern temporalPattern in relevantTemporalPatternList)
                    {
                        _prepareDfs0("POINT", string.Empty, patternIndex, duration, temporalPattern.TimeStep, temporalPattern.Increments, _probabiltyList[i + 1], ifdValue);

                        patternIndex = patternIndex + 1;
                    }
                }

                if (_probablityCategory != Definition.ProbabilityCategoryType.point)
                {                    
                    double designatedArea = Definition.GetDesignatedArea(_area);

                    List<ArealTemporalPattern> relevantArealTemporalPatternList = arealTemporalPatternList.Where(p => p.Duration == Definition.Durations.First(q => q.Key == duration).Value && p.Area == designatedArea.ToString()).ToList();
                    List<TemporalPattern> relevantTemporalPatternList = new List<TemporalPattern>();

                    if (Definition.Probabilities.Any(q => q.Key == aep))
                    {
                        relevantTemporalPatternList = temporalPatternList.Where(p => p.Duration == Definition.Durations.First(q => q.Key == duration).Value && p.AEP == Definition.Probabilities.First(q => q.Key == aep).Value).ToList();
                    }
                    else
                    {
                        relevantTemporalPatternList = temporalPatternList.Where(p => p.Duration == Definition.Durations.First(q => q.Key == duration).Value && p.AEP == Definition.GetProbability(Convert.ToDouble(_probabiltyList[i + 1].Replace(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.PercentSymbol, string.Empty)))).ToList();
                    }
                    
                    double aepDouble = double.Parse(_probabiltyList[i + 2].Replace("%",string.Empty)) / 100;
                    double durationDouble = duration.EndsWith("hour") ? (double.Parse(duration.Replace(" hour", string.Empty)) * 60) : double.Parse(duration.Replace(" min", string.Empty));
                    double arf;

                    

                    if (durationDouble > Definition.LongShortCutoffMinutes3)
                    {
                        throw new Exception("Generalised equations not applicable above Max duration: " + Definition.LongShortCutoffMinutes3);
                    }
                    else if (_area <= Definition.Area1)
                    {
                        arf = 1;
                    }
                    else if (_area > Definition.Area1 && _area < Definition.Area2)
                    {
                        double area = 10;
                        if (durationDouble <= Definition.LongShortCutoffMinutes1)
                        {
                            arf = 1 - 0.6614 * (1 - _arfShort(durationDouble, area, aepDouble)) * (System.Math.Pow(_area, 0.4) - 1);
                        }
                        else if (durationDouble >= Definition.LongShortCutoffMinutes2)
                        {
                            arf = 1 - 0.6614 * (1 - _arfLong(durationDouble, area, aepDouble)) * (System.Math.Pow(_area, 0.4) - 1);
                        }
                        else
                        {
                            double durationShort = 720;
                            double arfShort = _arfShort(durationShort, area, aepDouble);
                            
                            double durationLong = 1440;
                            double arfLong = _arfLong(durationLong, area, aepDouble);
                            
                            double arf10 = arfShort + (arfLong - arfShort) * ((durationDouble - durationShort)/durationShort);

                            arf = 1 - 0.6614 * (1 - arf10) * (System.Math.Pow(_area, 0.4) - 1);
                        }
                    }
                    else if (_area >= Definition.Area2 && _area < Definition.Area3)
                    {
                        if (durationDouble <= Definition.LongShortCutoffMinutes1)
                        {
                            arf = _arfShort(durationDouble, _area, aepDouble);
                        }
                        else if (durationDouble >= Definition.LongShortCutoffMinutes2)
                        {
                            arf = _arfLong(durationDouble, _area, aepDouble);
                        }
                        else
                        {
                            double durationShort = 720;
                            double arfShort = _arfShort(durationShort, _area, aepDouble);

                            double durationLong = 1440;
                            double arfLong = _arfLong(durationLong, _area, aepDouble);
                            
                            arf = arfShort + (arfLong - arfShort) * ((durationDouble - durationShort) / durationShort);
                        }
                    }
                    else if (_area >= Definition.Area3 && _area <= Definition.Area4)
                    {
                        if (durationDouble <= Definition.LongShortCutoffMinutes1)
                        {
                            throw new Exception("Generalised equations not applicable for Duration: " + durationDouble + ", Area:" + _area);
                        }
                        else if (durationDouble >= Definition.LongShortCutoffMinutes2)
                        {
                            arf = _arfLong(durationDouble, _area, aepDouble);
                        }
                        else
                        {
                            double durationShort = 720;
                            double arfShort = _arfShort(durationShort, _area, aepDouble);

                            double durationLong = 1440;
                            double arfLong = _arfLong(durationLong, _area, aepDouble);

                            arf = arfShort + (arfLong - arfShort) * ((durationDouble - durationShort) / durationShort);
                        }
                    }
                    else
                    {
                        throw new Exception("Max area is: " + Definition.Area4);
                    }

                    if (relevantArealTemporalPatternList.Count > 0 && _area >= 75 && (durationDouble * 60) >= arealTemporalPatternList.Select(p => p.Duration).Min())
                    {
                        patternIndex = 1;
                        foreach (ArealTemporalPattern arealTemporalPattern in relevantArealTemporalPatternList)
                        {
                            _prepareDfs0("AREAL", designatedArea + "sqkmAreal", patternIndex, duration, arealTemporalPattern.TimeStep, arealTemporalPattern.Increments, _probabiltyList[i + 1], arf * ifdValue);

                            patternIndex = patternIndex + 1;
                        }
                    }
                    else
                    {
                        patternIndex = 1;
                        foreach (TemporalPattern arealTemporalPattern in relevantTemporalPatternList)
                        {
                            _prepareDfs0("AREAL", string.Empty, patternIndex, duration, arealTemporalPattern.TimeStep, arealTemporalPattern.Increments, _probabiltyList[i + 1], arf * ifdValue);

                            patternIndex = patternIndex + 1;
                        }
                    }
                }
            }

            _streamList.Add(ListStringToStream(_ifdTable.Split(new char[]{';'}).ToList()));
            _fileNameList.Add("ifdTable.csv");

            _streamList.Add(ListStringToStream(_tempPatternStringList));
            _fileNameList.Add(_tempPatternStringList[0]);
            _streamList.Add(ListStringToStream(_arealTempPatternStringList));
            _fileNameList.Add(_arealTempPatternStringList[0]);

            string arfFile64String = Dfs0SqlCache.GetDfs0(_userId, _latitude + ";" + _longitude, Definition.ArfTextFile);
            if (!string.IsNullOrEmpty(arfFile64String))
            {
                byte[] byteArray = Convert.FromBase64String(arfFile64String);
                Stream stream = new MemoryStream(byteArray);
                _streamList.Add(stream);
                _fileNameList.Add("Text.txt");
            }

            foreach (Definition.ProbabilityType probabilityType in new List<Definition.ProbabilityType> { Definition.ProbabilityType.veryFrequent, Definition.ProbabilityType.frequentInfrequent, Definition.ProbabilityType.rare })
            {
                string probability64String = Dfs0SqlCache.GetDfs0(_userId, _latitude + ";" + _longitude, probabilityType.ToString());
                if (!string.IsNullOrEmpty(probability64String))
                {
                    byte[] byteArray = Convert.FromBase64String(probability64String);
                    Stream stream = new MemoryStream(byteArray);
                    _streamList.Add(stream);
                    _fileNameList.Add(probabilityType+".csv");
                }
            }
        }

        private double _arfShort(double duration, double area, double aep)
        {
            return System.Math.Min(1, (1 - _a * (System.Math.Pow(area, _b) - (_c * System.Math.Log10(duration))) * System.Math.Pow(duration, -_d) + (_e * System.Math.Pow(area, _f) * System.Math.Pow(duration, _g)) * (0.3 + System.Math.Log10(aep)) + ((_h * System.Math.Pow(10, (_i * area * duration / 1440))) * (0.3 + System.Math.Log10(aep)))));
        }

        private double _arfLong(double duration, double area, double aep)
        {
            return System.Math.Min(1, (1 - (0.287 * (System.Math.Pow(area, 0.265) - (0.439 * System.Math.Log10(duration))) * System.Math.Pow(duration, -0.36)) + (2.26 * System.Math.Pow(10, -3) * System.Math.Pow(area, 0.226) * System.Math.Pow(duration, 0.125) * (0.3 + System.Math.Log10(aep))) + (0.0141 * System.Math.Pow(area, 0.213) * System.Math.Pow(10, -(0.021 * (duration - 180) * (duration - 180) / 1440)) * (0.3 + System.Math.Log10(aep)))));
        }

        private void _prepareDfs0(string filePrefix, string suffix, int patternIndex, string duration, int timeStep, List<double> increments, string prob, double rainfallValue)
        {
            double initialLoss = _initialLoss;

            string fileName = filePrefix + "_AEP" + prob + "_Duration" + duration.Replace(" min", "min").Replace(" hour", "hr") + "_" + suffix + "TP" + patternIndex.ToString().PadLeft(2, '0') + ".dfs0";
            
            List<string> itemsNameList = new List<string> { "Gross", "Gross Rate", "Net", "Net Rate" };
            

            Dfs0Writer dfs0Writer = new Dfs0Writer(_startDateTime, itemsNameList, timeStep * 60);

            DateTime currentDateTime = _startDateTime;

            ////add row of zeros
            dfs0Writer.AddRain(currentDateTime, new List<double> { 0, 0, 0, 0 });
            
            currentDateTime = currentDateTime.AddMinutes(timeStep);
            ////

            foreach (double value in increments)
            {
                List<double> values = new List<double>();
                double grossRain = value / 100 * rainfallValue;
                
                //gross rain
                values.Add(grossRain);

                //gross rate in daily value
                values.Add(grossRain * ((60*24) / (double)timeStep));

                //net rain
                double netRain = System.Math.Max(grossRain - initialLoss - (_continueLoss * (double)timeStep / 60), 0);
                //spread initial loss amongst beginning time steps
                initialLoss = System.Math.Max(initialLoss - grossRain, 0);
                values.Add(netRain);

                //net rate in daily value
                values.Add(netRain * ((60 * 24) / (double)timeStep));
                
                dfs0Writer.AddRain(currentDateTime, values);
                currentDateTime = currentDateTime.AddMinutes(timeStep);
            }

            if (_tailType == Definition.TailType.two)
            {
                for (int i = 0; i < increments.Count * 2; i++ )
                {
                    List<double> values = new List<double>();
                    
                    values.AddRange(new List<double> { 0, 0, 0, 0 });
                    
                    dfs0Writer.AddRain(currentDateTime, values);
                    currentDateTime = currentDateTime.AddMinutes(timeStep);
                }
            }

            dfs0Writer.Close();

            byte[] byteArray = File.ReadAllBytes(dfs0Writer.FilePath());

            Stream stream = new MemoryStream(byteArray);
            _streamList.Add(stream);
            _fileNameList.Add(fileName);

            dfs0Writer.Dispose();
        }

        private Stream ListStringToStream(List<string> listOfStrings)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);

            foreach(string line in listOfStrings)
            {
                writer.WriteLine(line);
            }

            writer.Flush();
            stream.Position = 0;

            return stream;
        }
        
        public List<string> FileNameList()
        {
            return _fileNameList;
        }

        public List<Stream> StreamList()
        {
            return _streamList;
        }

        public List<TemporalPattern> GetTemporalPattern(TemporalPatternRequest temporalPatternRequest)
        {
            string patternString = Dfs0SqlCache.GetDfs0(_userId, _latitude + ";" + _longitude, Definition.TemporalLookup);
            if (!string.IsNullOrEmpty(patternString))
            {
                _tempPatternStringList = patternString.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            else
            {
                _tempPatternStringList = temporalPatternRequest.GetTemporalPattern();
                Dfs0SqlCache.SetDfs0(_userId, _latitude + ";" + _longitude, Definition.TemporalLookup, string.Join(Environment.NewLine, _tempPatternStringList));
            }
            
            List<TemporalPattern> temporalPatternList = new List<TemporalPattern>();

            foreach (string line in _tempPatternStringList)
            {
                try
                {
                    List<string> lineList = line.Split(_delimiters).ToList();

                    TemporalPattern temporalPattern = new TemporalPattern();
                    temporalPattern.EventId = Convert.ToInt32(lineList[0]);
                    temporalPattern.Duration = Convert.ToInt32(lineList[1]);
                    temporalPattern.TimeStep = Convert.ToInt32(lineList[2]);
                    temporalPattern.Region = lineList[3];
                    temporalPattern.AEP = lineList[4];
                    temporalPattern.Increments = new List<double>(lineList.Skip(5).Where(p => p.Length > 0).Select(p => Convert.ToDouble(p)));

                    temporalPatternList.Add(temporalPattern);
                }
                catch (Exception e)
                {

                }
            }

            return temporalPatternList;
        }

        public List<ArealTemporalPattern> GetArealTemporalPattern(TemporalPatternRequest temporalPatternRequest)
        {
            string patternString = Dfs0SqlCache.GetDfs0(_userId, _latitude + ";" + _longitude, Definition.ArealTemporalLookup);
            if (!string.IsNullOrEmpty(patternString))
            {
                _arealTempPatternStringList = patternString.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            else
            {
                _arealTempPatternStringList = temporalPatternRequest.GetArealTemporalPattern();
                Dfs0SqlCache.SetDfs0(_userId, _latitude + ";" + _longitude, Definition.ArealTemporalLookup, string.Join(Environment.NewLine, _arealTempPatternStringList));
            }
            
            List<ArealTemporalPattern> arealTemporalPatternList = new List<ArealTemporalPattern>();

            foreach (string line in _arealTempPatternStringList)
            {
                try
                {
                    List<string> lineList = line.Split(_delimiters).ToList();

                    ArealTemporalPattern arealTemporalPattern = new ArealTemporalPattern();
                    arealTemporalPattern.Duration = Convert.ToInt32(lineList[1]);
                    arealTemporalPattern.TimeStep = Convert.ToInt32(lineList[2]);
                    arealTemporalPattern.Region = lineList[3];
                    arealTemporalPattern.Area = lineList[4];
                    arealTemporalPattern.Increments = new List<double>(lineList.Skip(5).Where(p => p.Length > 0).Select(p => Convert.ToDouble(p)));

                    arealTemporalPatternList.Add(arealTemporalPattern);
                }
                catch (Exception e)
                {

                }
            }

            return arealTemporalPatternList;
        }
    }
}
