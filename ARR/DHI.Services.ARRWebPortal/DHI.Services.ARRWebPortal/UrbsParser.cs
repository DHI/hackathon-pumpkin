using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHI.Services.ARRWebPortal
{
    public class UrbsParser
    {
        private Stream _stream;
        private DateTime _startDateTime;
        private char[] _delimiters = new char[] { ',' };

        private bool _isUrbs = false;

        private List<string> _catchmentNames;

        public UrbsParser(Stream stream, DateTime startDateTime)
        {
            _stream = stream;
            _startDateTime = startDateTime;

            _stream.Position = 0;
            StreamReader streamReader = new StreamReader(_stream);

            while (!streamReader.EndOfStream)
            {
                string line = streamReader.ReadLine().Replace(@"""","");

                if (line.StartsWith(Definition.UrbsGrossRainString))
                {
                    _isUrbs = true;
                    break;
                }
            }
        }

        public bool IsUrbs()
        {
            return _isUrbs;
        }

        public List<string> GetCatchmentNames()
        {            
            _stream.Position = 0;
            StreamReader streamReader = new StreamReader(_stream);
            
            while (!streamReader.EndOfStream)
            {
                string line = streamReader.ReadLine().Replace(@"""", "");

                if (line.StartsWith(Definition.UrbsFlowRatesString))
                {
                    List<string> lineList = line.Split(_delimiters, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToList();
                    return lineList.Skip(1).ToList();
                }
            }

            return _catchmentNames;
        }

        public List<KeyValuePair<DateTime, List<double>>> GetData(string lineString)
        {
            List<KeyValuePair<DateTime, List<double>>> result = new List<KeyValuePair<DateTime, List<double>>>();
            
            _stream.Position = 0;
            StreamReader streamReader = new StreamReader(_stream);

            bool captureLine = false;
            while (!streamReader.EndOfStream)
            {
                string line = streamReader.ReadLine().Replace(@"""", "");
                
                List<string> lineList = line.Split(_delimiters, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToList();

                //if first two arguments not string, means not data
                double arg1double;
                double arg2double;
                if (lineList.Count > 1 && !double.TryParse(lineList[0], out arg1double) && !double.TryParse(lineList[1], out arg2double))
                {
                    captureLine = false;
                }

                if (line.StartsWith(lineString))
                {
                    captureLine = true;
                }

                if (captureLine)
                {
                    try
                    {
                        double dateNumber = double.Parse(lineList[0]);
                        DateTime current;
                        if (dateNumber > 39447) //39447 is Jan 1 2008 in Excel format 
                        {
                            current = DateTime.FromOADate(double.Parse(lineList[0]));
                        }
                        else
                        {
                            current = _startDateTime.AddHours(double.Parse(lineList[0]));
                        }

                        List<double> valueList = lineList.Skip(1).Select(p => double.Parse(p)).ToList();
                        result.Add(new KeyValuePair<DateTime, List<double>>(current, valueList));
                    }
                    catch (Exception)
                    {

                    }
                }
                
            }

            return result;
        }
    }
}
