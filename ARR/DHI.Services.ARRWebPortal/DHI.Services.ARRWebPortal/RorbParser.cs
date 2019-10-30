using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHI.Services.ARRWebPortal
{
    public class RorbParser
    {
        private Stream _stream;
        private DateTime _startDateTime;

        public RorbParser(Stream stream, DateTime startDateTime)
        {
            _stream = stream;
            _startDateTime = startDateTime;
        }

        public List<string> GetCatchmentNames()
        {            
            _stream.Position = 0;
            StreamReader streamReader = new StreamReader(_stream);
            
            while (!streamReader.EndOfStream)
            {
                string line = streamReader.ReadLine();
                
                if (line.StartsWith("Inc,Time (hrs)"))
                {
                    string[] lineArray = line.Split(',');
                    return lineArray.Skip(2).ToList();
                }
            }

            return new List<string>();
        }

        public List<KeyValuePair<DateTime, List<double>>> GetData()
        {
            List<KeyValuePair<DateTime, List<double>>> result = new List<KeyValuePair<DateTime, List<double>>>();
            
            _stream.Position = 0;
            StreamReader streamReader = new StreamReader(_stream);
            
            while (!streamReader.EndOfStream)
            {
                string line = streamReader.ReadLine();

                try
                {
                    string[] lineArray = line.Split(',');

                    DateTime current = _startDateTime.AddHours(double.Parse(lineArray[1]));
                    List<double> valueList = lineArray.Skip(2).Select(p => double.Parse(p)).ToList();
                    result.Add(new KeyValuePair<DateTime, List<double>>(current, valueList));
                }
                catch (Exception)
                {

                }
            }

            //add row of zeros if first time step does not equal start time
            if (result[0].Key != _startDateTime)
            {
                List<double> zeroValues = result[0].Value.Select(p => (double)0).ToList();
                result.Insert(0, new KeyValuePair<DateTime, List<double>>(_startDateTime, zeroValues));
            }

            return result;
        }
    }
}
