using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.DataHandlers.ScatteredPoints.LinearCombination
{
    public interface INodes
    {
        double[] Lats { get;}
        double[] Lons { get;}
    }

    public class Nodes : INodes
    {
        public double[] Lats { get; private set; }
        public double[] Lons { get; private set;}

        public Nodes(double[] lats, double[] lons)
        {
            Lats = lats;
            Lons = lons;
        }
    }

    public class RealValueNodes : Nodes
    {
        public double[] Values { get; private set; }

        public RealValueNodes(double[] lats, double[] lons, double[] values)
            :base(lats,lons)
        {
            Values = values;
        }
    }
}
