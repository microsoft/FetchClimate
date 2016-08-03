using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Microsoft.Research.Science.FetchClimate2.Integrators.Temporal;

namespace Microsoft.Research.Science.FetchClimate2.Tests
{
    [TestClass]
    public class MonthlyMeansOverYearsStepInterpolationTests
    {
        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void TestProjectFirstDay()
        {
            Assert.AreEqual(0.0, DaysOfYearConversions.ProjectFirstDay(1, true));//1 Jenuary
            Assert.AreEqual(0.0, DaysOfYearConversions.ProjectFirstDay(1, false));
            Assert.AreEqual(1.0, DaysOfYearConversions.ProjectFirstDay(32, true));//1 February
            Assert.AreEqual(1.0, DaysOfYearConversions.ProjectFirstDay(32, false));
            Assert.AreEqual(3.5, DaysOfYearConversions.ProjectFirstDay(107, true));//16 april (exact start of the second half of april)
            Assert.AreEqual(3.5, DaysOfYearConversions.ProjectFirstDay(106, false));
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void TestProjectLastDay()
        {
            Assert.AreEqual(1.0, DaysOfYearConversions.ProjectLastDay(31, true));//31 Jenuary
            Assert.AreEqual(1.0, DaysOfYearConversions.ProjectLastDay(31, false));

            Assert.AreEqual(2.0, DaysOfYearConversions.ProjectLastDay(60, true));//29 February
            Assert.AreEqual(2.0, DaysOfYearConversions.ProjectLastDay(59, false));//28 February

            Assert.AreEqual(3.5, DaysOfYearConversions.ProjectLastDay(106, true));//15 april (exact end of the first half of april)
            Assert.AreEqual(3.5, DaysOfYearConversions.ProjectLastDay(105, false));

            Assert.AreEqual(12.0, DaysOfYearConversions.ProjectLastDay(365, false));
            Assert.AreEqual(12.0, DaysOfYearConversions.ProjectLastDay(366, true));

            Assert.AreEqual(15.5, DaysOfYearConversions.ProjectLastDay(470, false)); //overlapped 15 april
            Assert.AreEqual(23.0, DaysOfYearConversions.ProjectLastDay(699, false)); //overlapped 30 november
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void TestGetBoundingBox()
        {
            MonthlyMeansOverYearsStepIntegratorFacade a = new MonthlyMeansOverYearsStepIntegratorFacade();
            IndexBoundingBox ibb = a.GetBoundingBox(new TimeSegment(1931,1932,106,166, 0,24));
            Assert.AreEqual(3,ibb.first);
            Assert.AreEqual(5, ibb.last);

            ibb = a.GetBoundingBox(new TimeSegment(1944,1944 ,107,167, 0,24));
            Assert.AreEqual(3, ibb.first);
            Assert.AreEqual(5, ibb.last);

            ibb = a.GetBoundingBox(new TimeSegment(1931, 1931 ,244,  273, 0,24));
            Assert.AreEqual(8, ibb.first);
            Assert.AreEqual(8, ibb.last);

            ibb = a.GetBoundingBox(new TimeSegment(1931,1932, 305, 59, 0,24 ));
            Assert.AreEqual(0, ibb.first);
            Assert.AreEqual(11, ibb.last);            
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("BVT")]
        public void TestGetTempIPs()
        {
            MonthlyMeansOverYearsStepIntegratorFacade a = new MonthlyMeansOverYearsStepIntegratorFacade();
            var ips = a.GetTempIPs(new TimeSegment(1931, 1932 ,106, 166, 0,24)); //16 april till 15 Jun
            Assert.AreEqual(3, ips.Indices.Length);
            Assert.AreEqual(3, ips.Indices[0]);
            Assert.AreEqual(4, ips.Indices[1]);
            Assert.AreEqual(5, ips.Indices[2]);
            Assert.AreEqual(ips.Weights[1], ips.Weights[0] * 2.0);
            Assert.AreEqual(ips.Weights[1], ips.Weights[2] * 2.0);


            ips = a.GetTempIPs(new TimeSegment(1944,1944,107,167,  0,24)); //16 april till 15 Jun
            Assert.AreEqual(3, ips.Indices.Length);
            Assert.AreEqual(3, ips.Indices[0]);
            Assert.AreEqual(4, ips.Indices[1]);
            Assert.AreEqual(5, ips.Indices[2]);
            Assert.AreEqual(ips.Weights[1], ips.Weights[0] * 2.0);
            Assert.AreEqual(ips.Weights[1], ips.Weights[2] * 2.0);

            ips = a.GetTempIPs(new TimeSegment(1931,1931,244, 273,  0,24)); //september
            Assert.AreEqual(1, ips.Indices.Length);
            Assert.AreEqual(8, ips.Indices[0]);

            ips = a.GetTempIPs(new TimeSegment( 1931, 1932, 305, 59, 0,24)); //from start of november till end of feburary
            Assert.AreEqual(4, ips.Indices.Length);
            int[] indeces = new int[]{0, 1, 10, 11};
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(ips.Weights[0], ips.Weights[i]);
                Assert.IsTrue(indeces.Contains(ips.Indices[i]));
            }
        }
    }


}
