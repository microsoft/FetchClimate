using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.DataSources
{
    class RTGCVtable : Dictionary<Tuple<double, double, double, double>, double[]>
    {
        static double[] months = Enumerable.Range(0, 24).Select(a => (double)a).ToArray();
        double unVal;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="unkonwnValue">The value to return if there is no mathcing regions for coordinates of the request</param>
        public RTGCVtable(double unkonwnValue)
        {
            unVal = unkonwnValue;
        }

        public double GetRTGCV(double latmin, double latmax, double lonmin, double lonmax, int daystart, int daystop, bool isLeap)
        {
            if (lonmax > 180.0)
            {
                lonmax -= 180.0;
                lonmin -= 180.0;
            }

            Tuple<double, double, double, double> areaStart = null, areaStop = null;
            foreach (var t in this.Keys)
            {
                if (latmin > t.Item1 && latmin < t.Item3 && lonmin > t.Item2 && lonmin < t.Item4)                
                    areaStart = t;
                if (latmax > t.Item1 && latmax < t.Item3 && lonmax > t.Item2 && lonmax < t.Item4)
                    areaStop = t;
            }

            if (areaStart == null || areaStop == null || areaStart != areaStop)
                return unVal;

            double sum = 0;

            var p = new WeightProviders.StepFunctionInterpolation();
            double startProj = DaysOfYearConversions.ProjectFirstDay(daystart, isLeap);
            double stopProj = DaysOfYearConversions.ProjectLastDay(daystop, isLeap);
            int startIndex, stopIndex;
            var weights = p.GetWeights(months, startProj, stopProj, out startIndex, out stopIndex);

            var area = this[areaStart];
            for (int i = startIndex; i <= stopIndex; i++)
                sum += area[i % 12] * weights[i - startIndex];
            return sum / (stopIndex - startIndex + 1);
        }


        public static readonly RTGCVtable DurnalTempRange, WetDays, FrostDays, Temp, RelHum, PureSky, WindSpeed, Precip;

        static RTGCVtable()
        {
            DurnalTempRange = new RTGCVtable(2.0);
            DurnalTempRange.Add(Tuple.Create(-60.0, 90.0, 12.0, 180.0), new double[] { 2.2, 2.3, 2.3, 2.2, 2.2, 2.2, 2.3, 2.6, 2.3, 2.2, 2.2, 2.1 });//australia
            DurnalTempRange.Add(Tuple.Create(-20.0, 45.0, 40.0, 160.0), new double[] { 2.4, 2.3, 2.4, 2.3, 2.4, 2.4, 2.5, 2.5, 2.6, 2.7, 2.5, 2.4 }); //asia
            DurnalTempRange.Add(Tuple.Create(25.0, 55.0, 90.0, 180.0), new double[] { 1.8, 1.4, 1.7, 1.6, 1.6, 1.4, 1.3, 1.3, 1.5, 1.6, 1.7, 1.7 }); //c.asia
            DurnalTempRange.Add(Tuple.Create(0.0, -120.0, 20.0, -25.0), new double[] { 2.5, 2.6, 2.5, 2.6, 2.4, 2.2, 2.1, 2.1, 2.1, 2.2, 2.4, 2.6 }); //c.america
            DurnalTempRange.Add(Tuple.Create(30.0, -20.0, 85.0, 60.0), new double[] { 1.5, 1.5, 1.7, 1.8, 2.0, 2.1, 2.2, 2.2, 2.1, 2.0, 1.7, 1.5 }); //europe
            DurnalTempRange.Add(Tuple.Create(-5.0, -25.0, 40.0, 60.0), new double[] { 3.1, 3.1, 3.2, 3.2, 3.5, 3.8, 3.8, 3.9, 3.8, 3.6, 3.2, 3.0 }); //n. africa
            DurnalTempRange.Add(Tuple.Create(20.0, -180.0, 85.0, -105.0), new double[] { 1.3, 1.4, 1.4, 1.4, 1.4, 1.4, 1.4, 1.5, 1.5, 1.4, 1.3, 1.3 }); //n. america
            DurnalTempRange.Add(Tuple.Create(20.0, -105.0, 85.0, -85.0), new double[] { 1.4, 1.5, 1.5, 1.5, 1.4, 1.3, 1.3, 1.3, 1.3, 1.3, 1.4, 1.4 }); //n. america
            DurnalTempRange.Add(Tuple.Create(20.0, -85.0, 85.0, -20.0), new double[] { 0.9, 1.0, 0.9, 0.9, 1.0, 1.0, 1.0, 1.0, 1.0, 0.9, 0.8, 0.9 }); //n. america
            DurnalTempRange.Add(Tuple.Create(-40.0, 0.0, 0.0, 60.0), new double[] { 2.8, 2.9, 2.8, 2.8, 3.2, 3.7, 3.7, 3.6, 3.2, 2.9, 2.6, 2.7 }); //n. africa
            DurnalTempRange.Add(Tuple.Create(-60.0, -110.0, 0.0, -20.0), new double[] { 1.7, 1.7, 1.6, 1.6, 1.7, 1.8, 1.9, 1.9, 1.8, 1.7, 1.7, 1.9 }); //s. america

            RelHum = new RTGCVtable(4.9);
            RelHum.Add(Tuple.Create(-60.0, 90.0, 12.0, 180.0), new double[] { 3.9, 3.8, 3.6, 3.8, 3.7, 3.5, 3.7, 3.5, 3.4, 3.6, 3.6, 3.7 });//australia
            RelHum.Add(Tuple.Create(-20.0, 45.0, 40.0, 160.0), new double[] { 5.7, 5.4, 5.2, 4.8, 4.8, 4.7, 4.7, 4.7, 4.9, 4.9, 5.4, 5.7 });//asia
            RelHum.Add(Tuple.Create(25.0, 55.0, 90.0, 180.0), new double[] { 6.7, 6.4, 6.3, 5.5, 5.4, 5.4, 5.1, 5.1, 4.9, 5.7, 6.0, 6.5 });//c. asia
            RelHum.Add(Tuple.Create(0.0, -120.0, 20.0, -25.0), new double[] { 4.8, 4.9, 4.8, 4.8, 4.6, 4.2, 4.4, 4.3, 4.2, 3.8, 4.1, 4.5 });//c. america
            RelHum.Add(Tuple.Create(30.0, -20.0, 85.0, 60.0), new double[] { 3.6, 3.5, 3.4, 3.4, 3.8, 4.0, 4.1, 3.9, 3.7, 3.3, 3.3, 3.5 });//europe
            RelHum.Add(Tuple.Create(-5.0, -25.0, 40.0, 60.0), new double[] { 4.8, 4.6, 4.7, 4.7, 5.0, 5.4, 5.6, 5.6, 5.2, 4.7, 4.7, 4.8 });//N. africa
            RelHum.Add(Tuple.Create(20.0, -180.0, 85.0, -20.0), new double[] { 4.8, 5.1, 5.2, 5.4, 5.1, 4.3, 4.2, 3.9, 3.7, 3.9, 4.3, 4.6 });//N. america
            RelHum.Add(Tuple.Create(-40.0, 0.0, 0.0, 60.0), new double[] { 5.0, 4.7, 4.7, 5.0, 5.2, 5.6, 5.6, 5.5, 5.2, 5.1, 5.3, 5.1 });//s. africa
            RelHum.Add(Tuple.Create(-60.0, -110.0, 0.0, -20.0), new double[] { 4.4, 4.5, 4.2, 4.1, 3.9, 4.2, 4.5, 4.7, 4.5, 3.8, 3.9, 4.4 });//s. america

            WindSpeed = new RTGCVtable(2.93); //see wind_base_uncertainty_hykl_work.zip for calculation
            WindSpeed.Add(Tuple.Create(-60.0, 90.0, 12.0, 180.0), new double[] { 3.2, 3.1, 3.0, 2.9, 2.8, 2.8, 3.0, 3.3, 3.5, 3.5, 3.4, 3.2 });//australia
            WindSpeed.Add(Tuple.Create(-20.0, 45.0, 40.0, 160.0), new double[] { 2.2, 2.4, 2.6, 2.6, 2.7, 2.7, 2.7, 2.6, 2.4, 2.1, 2.1, 2.1 });//asia
            WindSpeed.Add(Tuple.Create(25.0, 55.0, 90.0, 180.0), new double[] { 2.8, 2.9, 3.2, 3.5, 3.6, 3.4, 3.0, 2.9, 2.9, 3.0, 2.9, 2.8 });//c. asia
            WindSpeed.Add(Tuple.Create(0.0, -120.0, 20.0, -25.0), new double[] { 2.8, 2.9, 2.9, 2.7, 2.5, 2.5, 2.5, 2.4, 2.3, 2.3, 2.4, 2.5 });//c. america
            WindSpeed.Add(Tuple.Create(30.0, -20.0, 85.0, 60.0), new double[] { 3.8, 3.8, 3.9, 3.8, 3.5, 3.5, 3.3, 3.2, 3.3, 3.5, 3.6, 3.8 });//europe
            WindSpeed.Add(Tuple.Create(-5.0, -25.0, 40.0, 60.0), new double[] { 2.5, 2.6, 2.7, 2.7, 2.7, 2.8, 2.8, 2.6, 2.4, 2.2, 2.2, 2.3 });//N. africa
            WindSpeed.Add(Tuple.Create(20.0, -180.0, 85.0, -20.0), new double[] { 4.3, 4.3, 4.4, 4.4, 4.3, 4.2, 4.0, 4.0, 4.2, 4.4, 4.4, 4.3 });//N. america
            WindSpeed.Add(Tuple.Create(-40.0, 0.0, 0.0, 60.0), new double[] { 2.0, 1.9, 1.8, 1.8, 1.8, 1.9, 2.0, 2.2, 2.4, 2.4, 2.3, 2.1 });//s. africa
            WindSpeed.Add(Tuple.Create(-60.0, -110.0, 0.0, -20.0), new double[] { 2.8, 2.6, 2.5, 2.3, 2.3, 2.3, 2.6, 2.8, 3.0, 3.1, 3.1, 3.0 });//s. america

            Precip = new RTGCVtable(0.85); //see prate_base_uncertainty_hykl_work.zip for calculation
            Precip.Add(Tuple.Create<double, double, double, double>(-20, 45, 40, 160), new double[] { 1.024854087, 1.053687297, 1.053614057, 1.110017608, 1.035897325, 0.951345119, 0.98114191, 0.936855708, 0.799241993, 0.824922444, 0.905619266, 0.901047968 });
            Precip.Add(Tuple.Create<double, double, double, double>(-60, 90, 12, 140), new double[] { 1.042567983, 0.987237495, 0.911936757, 0.829372951, 0.790905148, 0.766055516, 0.911184712, 0.941637408, 0.993581533, 0.99947203, 0.998085149, 0.99714622 });
            Precip.Add(Tuple.Create<double, double, double, double>(-60, 140, 12, 180), new double[] { 0.672518403, 0.676041311, 0.631104516, 0.651512287, 0.65501461, 0.7085565, 0.753734117, 0.852910133, 0.977159944, 0.847699023, 0.771181496, 0.658366297 });
            Precip.Add(Tuple.Create<double, double, double, double>(25, 55, 90, 180), new double[] { 1.656427494, 1.569217575, 1.682153715, 2.089577056, 1.670672518, 2.234832873, 1.854985457, 1.546948282, 1.29607525, 1.215564875, 0.619391331, 1.553613023 });
            Precip.Add(Tuple.Create<double, double, double, double>(8, -120, 20, -25), new double[] { 2.085641714, 2.228388271, 2.09876424, 1.344133534, 0.913475574, 0.73552603, 0.796371063, 0.684563418, 0.58005618, 0.671662202, 1.063789983, 1.721750329 });
            Precip.Add(Tuple.Create<double, double, double, double>(0, -50, 20, -25), new double[] { 0.515151516, 0.492656564, 0.40954546, 0.460363644, 0.527969687, 0.655011421, 0.824727277, 0.913414139, 1.302161614, 1.245555574, 0.87381817, 0.691939404 });
            Precip.Add(Tuple.Create<double, double, double, double>(0, -120, 20, -80), new double[] { 1.990227717, 1.854343985, 1.833804714, 1.450031109, 1.040484211, 0.781356149, 0.831130501, 0.773856897, 0.669427903, 0.867184125, 1.415102347, 2.039300046 });
            Precip.Add(Tuple.Create<double, double, double, double>(0, -80, 20, -50), new double[] { 0.891451625, 0.822339596, 0.763775523, 0.619461885, 0.552097065, 0.566424775, 0.708342334, 0.648455373, 0.569625079, 0.533051137, 0.637376115, 0.878982296 });
            Precip.Add(Tuple.Create<double, double, double, double>(30, -20, 85, 10), new double[] { 0.793133552, 0.769895871, 0.786125566, 0.634770467, 0.48048316, 0.503609959, 0.509988735, 0.498196101, 0.580720535, 0.634757975, 0.673624528, 0.796021238 });
            Precip.Add(Tuple.Create<double, double, double, double>(30, 10, 85, 60), new double[] { 1.154991961, 1.135431577, 1.138872283, 0.93311039, 0.732589693, 0.722634517, 0.708948394, 0.72479599, 1.003872469, 1.070625755, 1.046332452, 1.183490133 });
            Precip.Add(Tuple.Create<double, double, double, double>(-5, -25, 40, 60), new double[] { 0.724747059, 0.736590432, 0.766330821, 0.972716255, 1.07625695, 0.969571186, 0.945782762, 0.820014404, 0.769070324, 0.854189261, 0.842297552, 0.794803492 });
            Precip.Add(Tuple.Create<double, double, double, double>(35, -80, 85, -20), new double[] { 0.530652788, 0.472676588, 0.407321274, 0.404443741, 0.330650077, 0.321289481, 0.287034637, 0.332198409, 0.39114948, 0.471825016, 0.410829453, 0.440022167 });
            Precip.Add(Tuple.Create<double, double, double, double>(20, -180, 35, -20), new double[] { 0.977323375, 1.323849053, 1.625146548, 1.668065447, 1.19239923, 0.808098403, 0.800766825, 0.725206626, 0.730822434, 0.916878579, 1.288576026, 1.200737217 });
            Precip.Add(Tuple.Create<double, double, double, double>(35, -180, 85, -105), new double[] { 0.991297113, 0.996787236, 0.977083311, 0.90550627, 0.699761413, 0.567392055, 0.489795278, 0.484814485, 0.699554915, 1.108678899, 1.012227966, 0.963148078 });
            Precip.Add(Tuple.Create<double, double, double, double>(35, -105, 85, -80), new double[] { 0.728640799, 0.520965088, 0.482939235, 0.39266315, 0.380629613, 0.373171239, 0.432086798, 0.346257223, 0.466612696, 0.484560977, 0.434435863, 0.513785005 });
            Precip.Add(Tuple.Create<double, double, double, double>(-40, 0, 0, 60), new double[] { 0.576402553, 0.538676899, 0.490784511, 0.5994908, 0.740302633, 0.6513262, 0.668283693, 0.683969771, 0.757856835, 0.95104743, 0.723878874, 0.602232864 });
            Precip.Add(Tuple.Create<double, double, double, double>(-60, -110, -15, -20), new double[] { 0.607420803, 0.646546973, 0.606873096, 0.733478497, 0.894980673, 0.991916099, 1.128723571, 1.196272507, 0.988382218, 0.843028623, 0.797473804, 0.70821232 });
            Precip.Add(Tuple.Create<double, double, double, double>(-15, -110, 0, -50), new double[] { 0.52372836, 0.470531457, 0.499313296, 0.484794503, 0.54653482, 0.628339482, 0.806504114, 0.852103052, 0.793177137, 0.684248667, 0.659321697, 0.660190046 });
            Precip.Add(Tuple.Create<double, double, double, double>(-15, -50, 0, -20), new double[] { 0.364768986, 0.356148027, 0.28810362, 0.341250605, 0.465605511, 0.627627906, 0.989053984, 1.206381454, 1.282276989, 0.841338897, 0.607018141, 0.482295099 });

            PureSky = new RTGCVtable(4.0);
            PureSky.Add(Tuple.Create(-60.0, 90.0, 12.0, 180.0), new double[] { 5.9, 6.2, 5.8, 6.9, 6.2, 6.5, 6.4, 6.1, 6.5, 6.2, 6.0, 6.7 });//australia
            PureSky.Add(Tuple.Create(-20.0, 45.0, 40.0, 160.0), new double[] { 6.6, 6.5, 5.9, 6.0, 6.1, 6.9, 7.5, 7.1, 6.8, 5.8, 5.9, 6.9 });//asia
            PureSky.Add(Tuple.Create(25.0, 55.0, 90.0, 180.0), new double[] { 7.3, 6.4, 5.3, 4.8, 4.9, 5.8, 6.4, 6.2, 5.7, 5.1, 5.9, 7.1 });//c. asia
            PureSky.Add(Tuple.Create(0.0, -120.0, 20.0, -25.0), new double[] { 7.2, 7.2, 6.6, 6.9, 7.1, 7.1, 7.0, 6.6, 6.2, 6.1, 6.7, 7.3 });//c. america
            PureSky.Add(Tuple.Create(30.0, -20.0, 85.0, 60.0), new double[] { 3.7, 3.7, 3.1, 3.0, 3.3, 3.7, 3.7, 3.4, 3.3, 2.9, 3.0, 3.5 });//europe
            PureSky.Add(Tuple.Create(-5.0, -25.0, 40.0, 60.0), new double[] { 5.1, 5.0, 4.3, 4.3, 4.5, 5.1, 5.3, 5.3, 4.7, 4.5, 4.3, 5.1 });//N. africa
            PureSky.Add(Tuple.Create(20.0, -180.0, 85.0, -20.0), new double[] { 4.8, 5.0, 5.2, 5.3, 4.9, 4.8, 4.5, 4.3, 4.2, 4.3, 4.4, 5.0 });//N. america
            PureSky.Add(Tuple.Create(-40.0, 0.0, 0.0, 60.0), new double[] { 5.2, 5.1, 5.1, 5.5, 5.4, 5.7, 5.8, 6.0, 5.4, 5.5, 5.2, 5.6 });//s. africa
            PureSky.Add(Tuple.Create(-60.0, -110.0, 0.0, -20.0), new double[] { 7.5, 7.7, 7.6, 7.1, 7.0, 7.7, 8.1, 8.3, 7.6, 6.8, 7.0, 7.9 });//s. america


            Temp = new RTGCVtable(1.0);
            Temp.Add(Tuple.Create(-60.0, 90.0, 12.0, 180.0), new double[] { 0.9, 0.9, 0.9, 0.9, 0.9, 1.0, 1.0, 1.2, 1.0, 1.0, 1.0, 0.9 });//australia
            Temp.Add(Tuple.Create(-20.0, 45.0, 40.0, 160.0), new double[] { 1.1, 1.1, 1.1, 1.0, 1.0, 1.2, 1.2, 1.2, 1.2, 1.2, 1.1, 1.1 }); //asia
            Temp.Add(Tuple.Create(25.0, 55.0, 90.0, 180.0), new double[] { 1.1, 1.1, 1.0, 0.8, 0.8, 1.0, 1.0, 1.0, 1.0, 0.8, 1.0, 1.0 }); //c.asia
            Temp.Add(Tuple.Create(0.0, -120.0, 20.0, -25.0), new double[] { 1.1, 1.1, 1.1, 1.1, 1.1, 1.0, 1.0, 1.0, 1.0, 1.0, 1.1, 1.1 }); //c.america
            Temp.Add(Tuple.Create(30.0, -20.0, 85.0, 60.0), new double[] { 1.1, 1.0, 0.8, 0.8, 0.8, 0.9, 0.9, 0.9, 0.9, 0.9, 1.0, 1.1 }); //europe
            Temp.Add(Tuple.Create(-5.0, -25.0, 40.0, 60.0), new double[] { 1.8, 1.6, 1.4, 1.4, 1.5, 1.6, 1.7, 1.7, 1.6, 1.5, 1.6, 1.7 }); //n. africa
            Temp.Add(Tuple.Create(20.0, -180.0, 85.0, -110.0), new double[] { 1.5, 0.9, 0.9, 0.7, 0.6, 0.7, 0.7, 0.7, 0.7, 0.6, 0.8, 0.9 }); //n. america
            Temp.Add(Tuple.Create(20.0, -110.0, 85.0, -90.0), new double[] { 1.3, 0.9, 1.0, 0.8, 0.8, 0.7, 0.7, 0.7, 0.7, 0.7, 0.8, 0.9 }); //n. america
            Temp.Add(Tuple.Create(20.0, -90.0, 85.0, -75.0), new double[] { 0.6, 0.7, 0.5, 0.6, 0.6, 0.5, 0.5, 0.5, 0.5, 0.6, 0.5, 0.6 }); //n. america
            Temp.Add(Tuple.Create(20.0, -75.0, 85.0, -20.0), new double[] { 0.7, 0.8, 0.6, 0.6, 0.7, 0.6, 0.7, 0.6, 0.6, 0.6, 0.6, 0.7 }); //n. america
            Temp.Add(Tuple.Create(-40.0, 0.0, 0.0, 60.0), new double[] { 1.5, 1.5, 1.4, 1.4, 1.5, 1.7, 1.7, 1.6, 1.5, 1.4, 1.4, 1.4 }); //s. africa
            Temp.Add(Tuple.Create(-60.0, -110.0, 0.0, -20.0), new double[] { 0.9, 0.9, 0.9, 0.9, 0.9, 0.8, 0.9, 0.9, 0.9, 0.9, 0.9, 0.9 }); //s. america

            FrostDays = new RTGCVtable(1.1);
            FrostDays.Add(Tuple.Create(-60.0, 90.0, 12.0, 180.0), new double[] { 0.8, 0.9, 1.4, 2.7, 3.6, 4.1, 4.2, 3.9, 3.3, 2.8, 1.9, 1.1 });//australia
            FrostDays.Add(Tuple.Create(-20.0, 45.0, 40.0, 160.0), new double[] { 1.9, 1.6, 1.8, 1.5, 1.0, 0.7, 0.5, 0.5, 0.9, 1.4, 2.0, 2.0 }); //asia
            FrostDays.Add(Tuple.Create(25.0, 55.0, 90.0, 180.0), new double[] { 1.8, 1.5, 1.8, 1.6, 1.2, 0.8, 0.5, 0.7, 1.1, 1.5, 2.0, 2.1 }); //c.asia
            FrostDays.Add(Tuple.Create(0.0, -120.0, 20.0, -25.0), new double[] { 2.2, 1.9, 1.5, 1.0, 0.7, 0.5, 0.5, 0.5, 0.5, 0.9, 1.5, 2.0 }); //c.america
            FrostDays.Add(Tuple.Create(30.0, -20.0, 85.0, 60.0), new double[] { 2.5, 2.3, 2.5, 2.3, 1.6, 0.8, 0.4, 0.6, 1.1, 1.9, 2.4, 2.6 }); //europe
            FrostDays.Add(Tuple.Create(-5.0, -25.0, 40.0, 60.0), new double[] { 2.5, 2.1, 2.3, 1.7, 0.8, 0.7, 0.7, 0.6, 0.6, 1.5, 2.1, 2.5 }); //n. africa
            FrostDays.Add(Tuple.Create(20.0, -180.0, 50.0, -95.0), new double[] { 2.1, 1.9, 2.0, 1.7, 1.3, 1.1, 0.6, 0.8, 1.3, 1.7, 1.7, 1.9 }); //n. america
            FrostDays.Add(Tuple.Create(50.0, -180.0, 85.0, -20.0), new double[] { 1.5, 1.3, 1.8, 1.8, 1.8, 1.4, 0.8, 1.2, 1.7, 2.1, 1.4, 1.3 }); //n. america
            FrostDays.Add(Tuple.Create(20.0, -95.0, 50.0, -20.0), new double[] { 0.4, 0.4, 0.8, 1.4, 1.3, 1.1, 0.3, 0.6, 1.5, 1.7, 1.2, 0.6 }); //n. america
            FrostDays.Add(Tuple.Create(-40.0, 0.0, 0.0, 60.0), new double[] { 0.5, 0.5, 0.4, 0.5, 1.4, 2.1, 2.3, 1.8, 0.9, 0.5, 0.4, 0.5 }); //n. africa
            FrostDays.Add(Tuple.Create(-60.0, -110.0, 0.0, -20.0), new double[] { 0.6, 0.5, 0.7, 0.9, 1.2, 1.3, 1.3, 1.3, 0.9, 0.9, 0.7, 0.8 }); //s. america

            WetDays = new RTGCVtable(1.5);
            WetDays.Add(Tuple.Create(-60.0, 90.0, 12.0, 180.0), new double[] { 1.8, 1.7, 1.9, 1.8, 2.1, 2.1, 2.1, 2.1, 2.1, 2.1, 1.9, 2.0 });//australia
            WetDays.Add(Tuple.Create(-20.0, 45.0, 40.0, 160.0), new double[] { 1.8, 1.6, 1.6, 1.7, 1.7, 2.0, 2.6, 2.5, 1.9, 1.6, 1.7, 1.8 }); //asia
            WetDays.Add(Tuple.Create(25.0, 55.0, 90.0, 180.0), new double[] { 1.5, 1.3, 1.3, 1.2, 1.2, 1.7, 2.4, 2.3, 1.6, 1.2, 1.2, 1.5 }); //c.asia
            WetDays.Add(Tuple.Create(0.0, -120.0, 20.0, -25.0), new double[] { 2.5, 2.1, 2.3, 2.2, 2.5, 3.1, 3.6, 3.5, 3.2, 2.8, 2.4, 2.5 }); //c.america
            WetDays.Add(Tuple.Create(30.0, -20.0, 85.0, 60.0), new double[] { 1.8, 1.6, 1.6, 1.4, 1.3, 1.2, 1.3, 1.4, 1.6, 1.6, 1.6, 1.8 }); //europe
            WetDays.Add(Tuple.Create(-5.0, -25.0, 40.0, 60.0), new double[] { 1.8, 1.8, 1.9, 1.8, 1.9, 1.6, 1.8, 1.9, 1.7, 1.9, 1.9, 1.9 }); //n. africa
            WetDays.Add(Tuple.Create(20.0, -180.0, 50.0, -95.0), new double[] { 2.3, 1.9, 1.8, 1.5, 1.5, 1.6, 1.7, 1.9, 1.8, 1.9, 1.7, 2.2 }); //n. america
            WetDays.Add(Tuple.Create(47.0, -180.0, 85.0, -20.0), new double[] { 2.3, 1.9, 1.8, 1.6, 1.7, 1.5, 1.4, 1.5, 1.5, 1.5, 1.8, 2.2 }); //n. america            
            WetDays.Add(Tuple.Create(-40.0, 0.0, 0.0, 60.0), new double[] { 2.1, 2.3, 2.4, 2.2, 2.3, 1.9, 1.9, 2.0, 2.1, 2.4, 2.5, 2.2 }); //n. africa
            WetDays.Add(Tuple.Create(-60.0, -110.0, 0.0, -20.0), new double[] { 3.5, 3.1, 3.5, 3.4, 3.7, 4.1, 4.3, 4.2, 3.8, 3.4, 3.3, 3.6 }); //s. america
        }
    }

}
