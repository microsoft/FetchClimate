using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace Microsoft.Research.Science.FetchClimate2.Tests.Configuration
{
    [TestClass]
    public class AzureGacTest
    {
        const string connString = "UseDevelopmentStorage=true";

        [TestInitialize]
        [TestCleanup]
        public void Cleaning()
        {
            AzureGAC.Reset(connString);
        }        

        [TestMethod]
        [TestCategory("Local")]
        [TestCategory("Requires Storage Emulator running")]
        public void AssemblyInstallationTest()
        {
            Assembly current = Assembly.GetExecutingAssembly();

            Assembly toLoad = null;

            AzureGAC azureGAC = new AzureGAC(connString);
            bool res = azureGAC.TryLoadAssembly(current.FullName, out toLoad);
            Assert.IsFalse(res);
            Assert.IsNull(toLoad);

            AzureGAC.Install(connString, current);
            res = azureGAC.TryLoadAssembly(current.FullName, out toLoad);
            Assert.IsTrue(res);
            Assert.IsNotNull(toLoad);

            var someExtractedData = toLoad.GetExportedTypes();
        }

        [TestMethod]
        [DeploymentItem(@"Configuration\testAss1.exe")]
        [DeploymentItem(@"Configuration\testAss2.exe")]
        [ExpectedException(typeof(InvalidOperationException))]
        [TestCategory("Local")]
        [TestCategory("Requires Storage Emulator running")]
        public void AssemblyConflictTest()
        {            
            Assembly toLoad1 = Assembly.LoadFile(System.IO.Path.GetFullPath("testAss1.exe"));
            Assembly toLoad2 = Assembly.LoadFile(System.IO.Path.GetFullPath("testAss2.exe"));

            AzureGAC azureGAC = new AzureGAC(connString);

             //System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
             //using (System.IO.FileStream assemblyStream = new System.IO.FileStream("1.exe",System.IO.FileMode.Create))
             //{
             //    formatter.Serialize(assemblyStream, toLoad1);
             //}

             //using (System.IO.FileStream assemblyStream = new System.IO.FileStream("2.exe", System.IO.FileMode.Create))
             //{
             //    formatter.Serialize(assemblyStream, toLoad2);
             //}

            AzureGAC.Install(connString, toLoad1);
            AzureGAC.Install(connString, toLoad2);
        }

        [TestMethod]
        [DeploymentItem(@"Configuration\testAss1.exe")]
        [TestCategory("Local")]
        [TestCategory("Requires Storage Emulator running")]
        public void DoubleInstallationTest()
        {
            Assembly current = Assembly.LoadFile(System.IO.Path.GetFullPath("testAss1.exe"));

            Assembly toLoad = null;

            AzureGAC azureGAC = new AzureGAC(connString);
            bool res = azureGAC.TryLoadAssembly(current.FullName, out toLoad);
            Assert.IsFalse(res);
            Assert.IsNull(toLoad);

            AzureGAC.Install(connString, current);
            AzureGAC.Install(connString, current);
            res = azureGAC.TryLoadAssembly(current.FullName, out toLoad);
            Assert.IsTrue(res);
            Assert.IsNotNull(toLoad);

            var someExtractedData = toLoad.GetExportedTypes();
        }
    }
}
