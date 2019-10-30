using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHI.Services.ARRWebPortal
{
    public class TemporalPattern
    {
        public int EventId;
        public int Duration;
        public int TimeStep;
        public string Region;
        public string AEP;
        public List<double> Increments;  
    }
}
