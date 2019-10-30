using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHI.Services.ARRWebPortal
{
    public class ArealTemporalPattern
    {
        public int Duration;
        public int TimeStep;
        public string Region;
        public string Area;
        public List<double> Increments;  
    }
}
