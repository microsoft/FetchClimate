using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Research.Science.FetchClimate2.UncertaintyEvaluators;
using System.Threading.Tasks;
using Microsoft.Research.Science.FetchClimate2;

namespace DataHandlersTests.UncertatintyEvaluators
{
    [TestClass]
    public class LinearCombination1DVarianceCalcTests
    {
        class Stub1 : IGaussianProcessDescriptionFactory {

            public IGaussianProcessDescription Create(string varName)
            {
                return new Stub2();
            }

            class Stub2 : IGaussianProcessDescription
            {
                class Stub3 : VariogramModule.IVariogram
                {
                    public double GetGamma(double value)
                    {
                        return value > Range ? Sill : value+1.0;
                    }

                    public double Nugget
                    {
                        get { return 1.0; }
                    }

                    public double Range
                    {
                        get { return 10.0; }
                    }

                    public double Sill
                    {
                        get { return 11.0; }
                    }
                }

                public double Dist(double location1, double location2)
                {
                    return System.Math.Abs(location1 - location2);
                }

                public VariogramModule.IVariogram Variogram
                {
                    get { return new Stub3(); }
                }
            }
            
        }

        class Stub4 : ITimeAxisLocator
        {
            public double[] getAproximationGrid(Microsoft.Research.Science.FetchClimate2.ITimeSegment timeSegment)
            {
                return new double[] { 0.0 };
            }

            public double[] AxisValues
            {
                get { return new double[] {0.0}; }
            }
        }

        class Stub5 : ICellRequest
        {
            public string VariableName
            {
                get { return ""; }
            }

            public double LatMin
            {
                get { throw new NotImplementedException(); }
            }

            public double LonMin
            {
                get { throw new NotImplementedException(); }
            }

            public double LatMax
            {
                get { throw new NotImplementedException(); }
            }

            public double LonMax
            {
                get { throw new NotImplementedException(); }
            }

            public ITimeSegment Time
            {
                get { return new Microsoft.Research.Science.FetchClimate2.Tests.TimeSegment(); }
            }
        }

        [TestMethod]
        [TestCategory("BVT")]
        public async Task Variance1DWithZeroDistTest()
        {
            LinearCombination1DVarianceCalc calc = new LinearCombination1DVarianceCalc(new Stub1(), new Stub4());

            IPs ips = new IPs(){ BoundingIndices = new IndexBoundingBox(){ first=0, last=0}, Weights = new double[] {1.0}, Indices = new int[]{0}};



            double variance = await calc.GetVarianceForCombinationAsync(ips, new Stub5(), 100.0);

            Assert.AreEqual(100.0, variance);
        }
    }
}
