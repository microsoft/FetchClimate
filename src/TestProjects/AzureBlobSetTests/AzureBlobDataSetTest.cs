using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Research.Science.Data;
using AzureBlobSet;

namespace AzureBlobSetTests
{
    [TestClass]
    public class AzureBlobDataSetTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            string uri = @"msds:nc?file=air_9times.nc&openMode=readOnly";
            DataSet d = DataSet.Open(uri);
            string blobUri = @"msds:ab?UseDevelopmentStorage=true&Container=testcontainer767x9&Blob=testblob767x9";
            var schema = new SerializableVariableSchema("testvar", typeof(Int32), new string[] { "lat" }, new System.Collections.Generic.Dictionary<string, object>());
            schema.Metadata.Add("testData", new ObjectWithTypeString(true));
            DataSet blobD = AzureBlobDataSet.ArrangeData(blobUri, d, new SerializableVariableSchema[] { schema });

            foreach (var i in blobD.Variables) Console.WriteLine(i.Name);

            Single[] a = (Single[])blobD["lat"].GetData();
            Single[] b = (Single[])d["lat"].GetData();

            Assert.AreEqual(b.Length, a.Length);

            for (int i = 0; i < a.Length; ++i) Assert.AreEqual(a[i], b[i]);

            Assert.AreEqual(d["lat"].Metadata.Count, blobD["lat"].Metadata.Count);

            foreach (var i in blobD["lat"].Metadata)
            {
                Assert.IsTrue(d["lat"].Metadata.ContainsKey(i.Key));
                if (i.Value is Array)
                    for (int j = 0; j < ((Array)i.Value).Length; ++j) Assert.AreEqual(((Array)d["lat"].Metadata[i.Key]).GetValue(j), ((Array)i.Value).GetValue(j));
                else Assert.AreEqual(d["lat"].Metadata[i.Key], i.Value);
            }

            foreach (var i in blobD.Metadata)
            {
                Assert.IsTrue(d.Metadata.ContainsKey(i.Key));
                if (i.Value is Array)
                    for (int j = 0; j < ((Array)i.Value).Length; ++j) Assert.AreEqual(((Array)d.Metadata[i.Key]).GetValue(j), ((Array)i.Value).GetValue(j));
                else Assert.AreEqual(d.Metadata[i.Key], i.Value);
            }

            Assert.AreEqual(true, blobD["testvar"].Metadata["testData"]);

            uri = @"msds:nc?file=air_8times.nc&openMode=readOnly";
            d = DataSet.Open(uri);
            blobUri = @"msds:ab?UseDevelopmentStorage=true&Container=testcontainer767x9&Blob=testblob767x9";
            blobD = AzureBlobDataSet.ArrangeData(blobUri, d, new SerializableVariableSchema[0]);

            int[] airShapeBlob = blobD["air0"].GetShape();
            int[] airShape = d["air0"].GetShape();

            Assert.AreEqual(airShape.Length, airShapeBlob.Length);
            for (int i = 0; i < airShape.Length; ++i) Assert.AreEqual(airShape[i], airShapeBlob[i]);

            Single[, ,] airBlob = (Single[, ,])blobD["air0"].GetData(new int[] { 3, 0, 0 }, new int[] { 2, airShapeBlob[1], airShapeBlob[2] });
            Single[, ,] air = (Single[, ,])d["air0"].GetData(new int[] { 3, 0, 0 }, new int[] { 2, airShape[1], airShape[2] });
            for (int i = 0; i < 2; ++i)
                for (int j = 0; j < airShapeBlob[1]; ++j)
                    for (int k = 0; k < airShapeBlob[2]; ++k)
                        Assert.AreEqual(air[i, j, k], airBlob[i, j, k]);

            blobD["air0"].PutData(new int[] { 3, 0, 0 }, air);
            airBlob = (Single[, ,])blobD["air0"].GetData(new int[] { 3, 0, 0 }, new int[] { 2, airShapeBlob[1], airShapeBlob[2] });

            for (int i = 0; i < 2; ++i)
                for (int j = 0; j < airShapeBlob[1]; ++j)
                    for (int k = 0; k < airShapeBlob[2]; ++k)
                        Assert.AreEqual(air[i, j, k], airBlob[i, j, k]);

            DateTime[] datesBlob = (DateTime[])blobD["_time_subset"].GetData();
            DateTime[] dates = (DateTime[])d["_time_subset"].GetData();
            Assert.AreEqual(dates.Length, datesBlob.Length);

            for (int i = 0; i < dates.Length; ++i) Assert.AreEqual(dates[i], datesBlob[i]);

            blobD["_time_subset"].PutData(dates);
            datesBlob = (DateTime[])blobD["_time_subset"].GetData();
            Assert.AreEqual(dates.Length, datesBlob.Length);

            for (int i = 0; i < dates.Length; ++i) Assert.AreEqual(dates[i], datesBlob[i]);
        }
    }
}
