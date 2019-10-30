using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHI.Services.ARRWebPortal
{
    public class RaftsParser
    {
        private Stream _stream;
        private DateTime _startDateTime;
        private char[] _delimiters = new char[] { ' ', '\r', '\n', '\t' };
        private List<KeyValuePair<string, List<double>>> _catchmentData;
        private Definition.ResultType _resultType;

        public List<Storm> StormList;

        public RaftsParser(Stream stream, DateTime startDateTime, Definition.ResultType resultType)
        {
            StormList = new List<Storm>();
            _stream = stream;
            _startDateTime = startDateTime;

            _resultType = resultType;

            _stream.Position = 0;
            StreamReader streamReader = new StreamReader(_stream);

            while (!streamReader.EndOfStream)
            {
                string line = streamReader.ReadLine();
                string[] lineArray = line.Split(_delimiters, StringSplitOptions.RemoveEmptyEntries);

                if (lineArray.Length == 4)
                {
                    try
                    {
                        Storm storm = new Storm();
                        storm.Name = "Storm" + (StormList.Count + 1);
                        storm.NumberTimeSteps = int.Parse(lineArray[2]);
                        storm.MinuteOffset = double.Parse(lineArray[3]);
                        StormList.Add(storm);
                    }
                    catch(Exception)
                    {

                    }
                }
            }
        }

        public List<KeyValuePair<string, List<double>>> GetCatchmentData()
        {
            _catchmentData = new List<KeyValuePair<string, List<double>>>();
            
            _stream.Position = 0;
            StreamReader streamReader = new StreamReader(_stream);

            int stormInt = 0;
            string catchmentName = string.Empty;
            int previousLineLength = 0;
            while (!streamReader.EndOfStream)
            {
                string line = streamReader.ReadLine();
                string[] lineArray = line.Split(_delimiters, StringSplitOptions.RemoveEmptyEntries);

                double arg1double;
                double arg2double;
                if (lineArray.Length == 4)
                {
                    try
                    {
                        int int1 = int.Parse(lineArray[2]);
                        double double1 = double.Parse(lineArray[3]);
                        stormInt = stormInt + 1;
                    }
                    catch (Exception)
                    {

                    }
                }
                //as the html form add extra lines of text, we must test line has 2 arguments, 1st must be double, 2nd must not, as it is string being catchment name
                else if (lineArray.Length == 2 && double.TryParse(lineArray[0], out arg1double) && !double.TryParse(lineArray[1], out arg2double))
                {
                    catchmentName = lineArray[1] + (_resultType == Definition.ResultType.loc ? "_L" : "_T") + "_Storm" + stormInt;

                    _catchmentData.Add(new KeyValuePair<string, List<double>>(catchmentName, new List<double>()));
                }
                else if (lineArray.Length == 5 || previousLineLength == 5)
                {
                    _catchmentData.First(p => p.Key == catchmentName).Value.AddRange(lineArray.Select(p => double.Parse(p)));
                }
                previousLineLength = lineArray.Length;
            }

            return _catchmentData;
        }

        public List<KeyValuePair<DateTime, List<double>>> GetData(Storm storm)
        {
            List<KeyValuePair<DateTime, List<double>>> result = new List<KeyValuePair<DateTime,List<double>>>();

            for (int timeStep=0; timeStep < storm.NumberTimeSteps; timeStep++)
            {
                DateTime current = _startDateTime.AddMinutes(storm.MinuteOffset * (timeStep + 1));
                List<double> values = new List<double>();
                foreach(KeyValuePair<string, List<double>> vp in _catchmentData.Where(p => p.Key.EndsWith(storm.Name)))
                {
                    values.Add(vp.Value[timeStep]);
                }
                result.Add(new KeyValuePair<DateTime, List<double>>(current, values));
            }

            //add row of zeros
            List<double> zeroValues = result[0].Value.Select(p => (double)0).ToList();
            result.Insert(0, new KeyValuePair<DateTime, List<double>>(_startDateTime, zeroValues));

            return result;
        }

        public class Storm
        {
            public string Name;
            public double MinuteOffset;
            public int NumberTimeSteps;
        }
    }
}
