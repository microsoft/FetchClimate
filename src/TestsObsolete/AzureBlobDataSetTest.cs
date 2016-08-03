using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Research.Science.Data;
using Newtonsoft.Json;
using System.Linq;

namespace AzureBlobSetTests
{
    [TestClass]
    public class AzureBlobDataSetTest
    {
        [TestMethod]
        [DeploymentItem("air_9times.nc")]
        [DeploymentItem("air_8times.nc")]
        [TestCategory("Local")]
        [TestCategory("Requires Storage Emulator running")] 
        public void AzureBlobDataSetTest1()
        {
            string uri = @"msds:nc?file=air_9times.nc&openMode=readOnly";
            DataSet d = DataSet.Open(uri);
            string blobUri = @"msds:ab?UseDevelopmentStorage=true&Container=testcontainer767x9&Blob=testblob767x9";
            var schema = new SerializableVariableSchema("testvar", typeof(Int32), new string[] { "lat" }, new System.Collections.Generic.Dictionary<string, object>());
            schema.Metadata.Add("testData", new ObjectWithTypeString(true));
            DateTime dateTimeNow = DateTime.Now;
            schema.Metadata.Add("dateTime", new ObjectWithTypeString(dateTimeNow));
            DataSet blobD = AzureBlobDataSet.ArrangeData(blobUri, d, new SerializableVariableSchema[] { schema });

            var json1 = JsonConvert.SerializeObject(blobD, Formatting.Indented,
                new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });

            foreach (var i in blobD.Variables) Console.WriteLine(i.Name);

            Single[] a = (Single[])blobD["lat"].GetData();
            Single[] b = (Single[])d["lat"].GetData();
            
            var json2 = JsonConvert.SerializeObject(a, Formatting.Indented,
                new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
            
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
            double deltaTimes = (dateTimeNow - (DateTime)blobD["testvar"].Metadata["dateTime"]).TotalMilliseconds;
            Assert.IsTrue(Math.Abs(deltaTimes) <= 1.0);//JSON somewhat cuts DateTimes :(
            //Assert.AreEqual(dateTimeNow, blobD["testvar"].Metadata["dateTime"]);

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

            Single[, ,] airBlob2 = (Single[, ,])blobD["air0"].GetData(new int[] { 3, 2, 2 }, new int[] { 2, 5, 7 });
            Single[, ,] air2 = (Single[, ,])d["air0"].GetData(new int[] { 3, 2, 2 }, new int[] { 2, 5, 7 });
            for (int i = 0; i < 2; ++i)
                for (int j = 0; j < 5; ++j)
                    for (int k = 0; k < 7; ++k)
                        Assert.AreEqual(air2[i, j, k], airBlob2[i, j, k]);

            blobUri = @"msds:ab?UseDevelopmentStorage=true&Container=testcontainer767x9&Blob=testblob767x10";
            blobD = AzureBlobDataSet.CreateEmptySet(blobUri, d.GetSerializableSchema());

            blobD["_time_subset"].PutData(dates);
            datesBlob = (DateTime[])blobD["_time_subset"].GetData();
            Assert.AreEqual(dates.Length, datesBlob.Length);

            for (int i = 0; i < dates.Length; ++i) Assert.AreEqual(dates[i], datesBlob[i]);
        }


        [TestMethod]
        [DeploymentItem("air_9times.nc")]
        [DeploymentItem("air_8times.nc")]
        [TestCategory("Local")]
        [TestCategory("Requires Storage Emulator running")]
        public void AzureBlobDataSetTest2()
        {
            const int xsize = 157;
            const int ysize = 231;
            const int zsize = 73;
            string blobUri = @"msds:ab?UseDevelopmentStorage=true&Container=testcontainer767x9&Blob=testblob767x10";
            var xaxis = new SerializableVariableSchema("x", typeof(Int32), new string[] { "x" }, null);
            var yaxis = new SerializableVariableSchema("y", typeof(Int32), new string[] { "y" }, null);
            var zaxis = new SerializableVariableSchema("z", typeof(Int32), new string[] { "z" }, null);
            var data = new SerializableVariableSchema("data", typeof(Int32), new string[] { "x", "y", "z" }, null);
            var xdim = new SerializableDimension("x", xsize);
            var ydim = new SerializableDimension("y", ysize);
            var zdim = new SerializableDimension("z", zsize);
            var scheme = new SerializableDataSetSchema(new SerializableDimension[] { xdim, ydim, zdim }, new SerializableVariableSchema[] { xaxis, yaxis, zaxis, data },
                new System.Collections.Generic.Dictionary<string, object>());
            scheme.Metadata.Add("metaTest", new ObjectWithTypeString(new string[] { "test1", "test2", "test3" }));
            AzureBlobDataSet.CreateEmptySet(blobUri, scheme);
            var x = new int[xsize];
            var y = new int[ysize];
            var z = new int[zsize];
            var vals = new int[xsize, ysize, zsize];
            for (int i = 0; i < xsize; ++i) x[i] = i;
            for (int i = 0; i < ysize; ++i) y[i] = i;
            for (int i = 0; i < zsize; ++i) z[i] = i;
            for (int i = 0; i < xsize; ++i)
                for (int j = 0; j < ysize; ++j)
                    for (int k = 0; k < zsize; ++k)
                        vals[i, j, k] = i * j * k;
            AzureBlobDataSet set = new AzureBlobDataSet(blobUri);
            set["x"].PutData(x);
            set["y"].PutData(y);
            set["z"].PutData(z);
            set["data"].PutData(vals);

            var receivedData = (int[, ,])set["data"].GetData();
            for (int i = 0; i < xsize; ++i)
                for (int j = 0; j < ysize; ++j)
                    for (int k = 0; k < zsize; ++k)
                        Assert.AreEqual(i * j * k, receivedData[i, j, k]);

            var meta = (string[])set.Metadata["metaTest"];
            Assert.AreEqual("test1", meta[0]);
            Assert.AreEqual("test2", meta[1]);
            Assert.AreEqual("test3", meta[2]);
        }

        [TestMethod]
        [DeploymentItem("air_8times.nc")]
        [TestCategory("Local")]
        [TestCategory("Requires Storage Emulator running")]
        public void AzureBlobDataSetTest3()
        {
            //Microsoft.Research.Science.Data.Factory.DataSetFactory.Register(typeof(Microsoft.Research.Science.Data.NetCDF4.NetCDFDataSet));
            string uri = @"msds:nc?file=air_8times.nc&openMode=readOnly";
            var d = DataSet.Open(uri);
            var blobUri = @"msds:ab?UseDevelopmentStorage=true&Container=testcontainer767x9&Blob=testblob767x9";
            var blobD = AzureBlobDataSet.ArrangeData(blobUri, d, new SerializableVariableSchema[0]);

            int[] airShapeBlob = blobD["air0"].GetShape();
            int[] airShape = d["air0"].GetShape();

            Assert.AreEqual(airShape.Length, airShapeBlob.Length);
            for (int i = 0; i < airShape.Length; ++i) Assert.AreEqual(airShape[i], airShapeBlob[i]);

            int[] ori = new int[] { 3, 0, 0 };
            int[] shp = new int[] { 2, airShapeBlob[1], airShapeBlob[2] };

            Single[, ,] airBlob = (Single[, ,])blobD["air0"].GetData(ori, shp);
            Assert.AreEqual(3, ori[0]);
            Assert.AreEqual(0, ori[1]);
            Assert.AreEqual(0, ori[2]);
            Assert.AreEqual(2, shp[0]);
            Assert.AreEqual(airShapeBlob[1], shp[1]);
            Assert.AreEqual(airShapeBlob[2], shp[2]);


            blobD["air0"].PutData(new int[] { 3, 0, 0 }, airBlob);
            Assert.AreEqual(3, ori[0]);
            Assert.AreEqual(0, ori[1]);
            Assert.AreEqual(0, ori[2]);
            Assert.AreEqual(2, shp[0]);
            Assert.AreEqual(airShapeBlob[1], shp[1]);
            Assert.AreEqual(airShapeBlob[2], shp[2]);

            int[] ori2 = new int[] { 3, 2, 2 };
            int[] shp2 = new int[] { 2, 5, 7 };

            Single[, ,] airBlob2 = (Single[, ,])blobD["air0"].GetData(ori2, shp2);
            Assert.AreEqual(3, ori2[0]);
            Assert.AreEqual(2, ori2[1]);
            Assert.AreEqual(2, ori2[2]);
            Assert.AreEqual(2, shp2[0]);
            Assert.AreEqual(5, shp2[1]);
            Assert.AreEqual(7, shp2[2]);

            int[] ori3 = new int[] { 25 };
            int[] shp3 = new int[] { 50 };
            var airBlob3 = blobD["lon"].GetData(ori3, shp3);
            Assert.AreEqual(25, ori3[0]);
            Assert.AreEqual(50, shp3[0]);
        }

        [TestMethod]
        [DeploymentItem("air_9times.nc")]
        [DeploymentItem("air_8times.nc")]
        [TestCategory("Local")]
        [TestCategory("Requires Storage Emulator running")]
        public void AzureBlobDataSetTest4CreateSetWithSmallData()
        {
            string uri = @"msds:nc?file=air_9times.nc&openMode=readOnly";
            DataSet d = DataSet.Open(uri);
            string blobUri = @"msds:ab?UseDevelopmentStorage=true&Container=testcontainer767x9&Blob=testblob767x9";
            var schema = new SerializableVariableSchema("testvar", typeof(Int32), new string[] { "lat" }, new System.Collections.Generic.Dictionary<string, object>());
            schema.Metadata.Add("testData", new ObjectWithTypeString(true));
            DateTime dateTimeNow = DateTime.Now;
            schema.Metadata.Add("dateTime", new ObjectWithTypeString(dateTimeNow));
            //DataSet blobD = AzureBlobDataSet.ArrangeData(blobUri, d, new SerializableVariableSchema[] { schema });
            System.Collections.Generic.Dictionary<string, Array> dataToPut = new System.Collections.Generic.Dictionary<string, Array>();
            foreach (var v in d.Variables)
                dataToPut.Add(v.Name, v.GetData());
            var dsscheme = d.GetSerializableSchema();
            var varList = dsscheme.Variables.ToList();
            varList.Add(schema);
            dsscheme.Variables = varList.ToArray();
            DataSet blobD = AzureBlobDataSet.CreateSetWithSmallData(blobUri, dsscheme, dataToPut);

            var json1 = JsonConvert.SerializeObject(blobD, Formatting.Indented,
                new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });

            foreach (var i in blobD.Variables) Console.WriteLine(i.Name);

            Single[] a = (Single[])blobD["lat"].GetData();
            Single[] b = (Single[])d["lat"].GetData();

            var json2 = JsonConvert.SerializeObject(a, Formatting.Indented,
                new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });

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
            double deltaTimes = (dateTimeNow - (DateTime)blobD["testvar"].Metadata["dateTime"]).TotalMilliseconds;
            Assert.IsTrue(Math.Abs(deltaTimes) <= 1.0);//JSON somewhat cuts DateTimes :(
            //Assert.AreEqual(dateTimeNow, blobD["testvar"].Metadata["dateTime"]);

            uri = @"msds:nc?file=air_8times.nc&openMode=readOnly";
            d = DataSet.Open(uri);
            blobUri = @"msds:ab?UseDevelopmentStorage=true&Container=testcontainer767x9&Blob=testblob767x9";
            //blobD = AzureBlobDataSet.ArrangeData(blobUri, d, new SerializableVariableSchema[0]);
            dataToPut = new System.Collections.Generic.Dictionary<string, Array>();
            foreach (var v in d.Variables)
                dataToPut.Add(v.Name, v.GetData());
            dsscheme = d.GetSerializableSchema();
            blobD = AzureBlobDataSet.CreateSetWithSmallData(blobUri, dsscheme, dataToPut);

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

            Single[, ,] airBlob2 = (Single[, ,])blobD["air0"].GetData(new int[] { 3, 2, 2 }, new int[] { 2, 5, 7 });
            Single[, ,] air2 = (Single[, ,])d["air0"].GetData(new int[] { 3, 2, 2 }, new int[] { 2, 5, 7 });
            for (int i = 0; i < 2; ++i)
                for (int j = 0; j < 5; ++j)
                    for (int k = 0; k < 7; ++k)
                        Assert.AreEqual(air2[i, j, k], airBlob2[i, j, k]);

            blobUri = @"msds:ab?UseDevelopmentStorage=true&Container=testcontainer767x9&Blob=testblob767x10";
            blobD = AzureBlobDataSet.CreateEmptySet(blobUri, d.GetSerializableSchema());

            blobD["_time_subset"].PutData(dates);
            datesBlob = (DateTime[])blobD["_time_subset"].GetData();
            Assert.AreEqual(dates.Length, datesBlob.Length);

            for (int i = 0; i < dates.Length; ++i) Assert.AreEqual(dates[i], datesBlob[i]);
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("Requires Storage Emulator running")]
        public void SingleDimVarUpdate512Aligned()
        {
            using (DataSet sourceDs = DataSet.Open("msds:memory"))
            {
                sourceDs.AddVariable<double>("b", Enumerable.Repeat(0, 1024).Select(a => (double)a).ToArray(), "i");
                var svs = new SerializableVariableSchema("a", typeof(double), new string[] { "i" }, new System.Collections.Generic.Dictionary<string, object>());

                using (AzureBlobDataSet ds = AzureBlobDataSet.ArrangeData(string.Format(@"msds:ab?Container=tests&Blob={0}&UseDevelopmentStorage=true", new Random().NextDouble()), sourceDs, new SerializableVariableSchema[] { svs }))
                {
                    ds.Variables["a"].PutData(Enumerable.Repeat(1, 64).Select(a => (double)a).ToArray()); //64x8=512 bytes shape
                }
            }
        }

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("Requires Storage Emulator running")]
        public void ByteAndSByteDataTest()
        {

            const int xsize = 627;
            string blobUri = @"msds:ab?UseDevelopmentStorage=true&Container=testcontainer767x9&Blob=testblob767x17";
            var xaxis = new SerializableVariableSchema("x", typeof(Int32), new string[] { "x" }, null);
            var ByteData = new SerializableVariableSchema("ByteData", typeof(Byte), new string[] { "x" }, null);
            var SByteData = new SerializableVariableSchema("SByteData", typeof(SByte), new string[] { "x" }, null);
            var xdim = new SerializableDimension("x", xsize);
            var scheme = new SerializableDataSetSchema(new SerializableDimension[] { xdim }, new SerializableVariableSchema[] { xaxis, ByteData, SByteData },
                new System.Collections.Generic.Dictionary<string, object>());
            AzureBlobDataSet.CreateEmptySet(blobUri, scheme);
            var x = new int[xsize];
            var Bvals = new Byte[xsize];
            var SBvals = new SByte[xsize];
            for (int i = 0; i < xsize; ++i) x[i] = i;
            for (int i = 0; i < xsize; ++i)
            {
                Bvals[i] = (Byte)(i % 256);
                SBvals[i] = (SByte)(i % 256 - 128);
            }
            AzureBlobDataSet set = new AzureBlobDataSet(blobUri);
            set["x"].PutData(x);
            set["ByteData"].PutData(Bvals);
            set["SByteData"].PutData(SBvals);

            var receivedByteData = (Byte[])set["ByteData"].GetData();
            var receivedSByteData = (SByte[])set["SByteData"].GetData();
            for (int i = 0; i < xsize; ++i)
            {
                Assert.AreEqual((Byte)(i % 256), receivedByteData[i]);
                Assert.AreEqual((SByte)(i % 256 - 128), receivedSByteData[i]);
            }
        }


        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("Requires Storage Emulator running")]
        public void ByteAndSByte3DDataTest()
        {
            const int xsize = 157;
            const int ysize = 231;
            const int zsize = 73;
            string blobUri = @"msds:ab?UseDevelopmentStorage=true&Container=testcontainer767x9&Blob=testblob767x10";
            var xaxis = new SerializableVariableSchema("x", typeof(Int32), new string[] { "x" }, null);
            var yaxis = new SerializableVariableSchema("y", typeof(Int32), new string[] { "y" }, null);
            var zaxis = new SerializableVariableSchema("z", typeof(Int32), new string[] { "z" }, null);
            var ByteData = new SerializableVariableSchema("ByteData", typeof(Byte), new string[] { "x", "y", "z" }, null);
            var SByteData = new SerializableVariableSchema("SByteData", typeof(SByte), new string[] { "x", "y", "z" }, null);
            var xdim = new SerializableDimension("x", xsize);
            var ydim = new SerializableDimension("y", ysize);
            var zdim = new SerializableDimension("z", zsize);
            var scheme = new SerializableDataSetSchema(new SerializableDimension[] { xdim, ydim, zdim }, new SerializableVariableSchema[] { xaxis, yaxis, zaxis, ByteData, SByteData },
                new System.Collections.Generic.Dictionary<string, object>());
            AzureBlobDataSet.CreateEmptySet(blobUri, scheme);
            var x = new int[xsize];
            var y = new int[ysize];
            var z = new int[zsize];
            var Bvals = new Byte[xsize, ysize, zsize];
            var SBvals = new SByte[xsize, ysize, zsize];
            for (int i = 0; i < xsize; ++i) x[i] = i;
            for (int i = 0; i < ysize; ++i) y[i] = i;
            for (int i = 0; i < zsize; ++i) z[i] = i;
            for (int i = 0; i < xsize; ++i)
                for (int j = 0; j < ysize; ++j)
                    for (int k = 0; k < zsize; ++k)
                    {
                        Bvals[i, j, k] = (Byte)((i + j + k) % 256);
                        SBvals[i, j, k] = (SByte)((i + j + k) % 256 - 128);
                    }
            AzureBlobDataSet set = new AzureBlobDataSet(blobUri);
            set["x"].PutData(x);
            set["y"].PutData(y);
            set["z"].PutData(z);
            set["ByteData"].PutData(Bvals);
            set["SByteData"].PutData(SBvals);

            var receivedByteData = (Byte[, ,])set["ByteData"].GetData();
            var receivedSByteData = (SByte[, ,])set["SByteData"].GetData();
            for (int i = 0; i < xsize; ++i)
                for (int j = 0; j < ysize; ++j)
                    for (int k = 0; k < zsize; ++k)
                    {
                        Assert.AreEqual((Byte)((i + j + k) % 256), receivedByteData[i, j, k]);
                        Assert.AreEqual((SByte)((i + j + k) % 256 - 128), receivedSByteData[i, j, k]);
                    }
        }
    }
}
