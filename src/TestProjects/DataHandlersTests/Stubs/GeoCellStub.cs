using Microsoft.Research.Science.FetchClimate2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlersTests.Stubs
{
    public class GeoCellStub : IGeoCell
    {

        public double LatMin
        {
            get;
            set;
        }

        public double LonMin
        {
            get;
            set;
        }

        public double LatMax
        {
            get;
            set;
        }

        public double LonMax
        {
            get;
            set;
        }

        public ITimeSegment Time
        {
            get;
            set;
        }
    }
}
