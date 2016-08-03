using Microsoft.Research.Science.Data;
using Microsoft.Research.Science.FetchClimate2;
using System;
using System.Linq;

namespace CloudFetchSample
{
    public class App
    {
        public static void Main(string[] args)
        {
            RemoteFetchClient fc = new RemoteFetchClient(new Uri("http://fetchclimate2.cloudapp.net"));

            TimeRegion tr = new TimeRegion(firstYear: 2000, lastYear: 2001);
            tr = tr.GetMonthlyTimeseries();
            FetchRequest request = new FetchRequest(
                "airt",
                FetchDomain.CreatePointGrid(
                    Enumerable.Range(0, 150).Select(i => 50.0 + i * 0.1).ToArray(), // 5.0 .. 20.0
                    Enumerable.Range(0, 170).Select(i => 30.0 + i * 0.1).ToArray(),
                    tr));

            var dataSet = fc.FetchAsync(request).Result;
            Console.WriteLine(dataSet.ToString());
            dataSet.View();
        }
    }
}
