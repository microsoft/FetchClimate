using Microsoft.Research.Science.Data;
using Microsoft.Research.Science.Data.Factory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections;

namespace DataHandlersTests
{
    [TestClass()]
    public class Settings
    {
        [AssemblyInitialize()]
        public static void AssemblyInit(TestContext context)
        {

            //overriding bug-containing default azure provider with the fixed one            
            Type t = typeof(DataSetFactory);
            var dict = (IDictionary)t.InvokeMember("providersByName", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.GetField, null,null,null);
            dict.Remove("az");
            DataSetFactory.Register(typeof(Microsoft.Research.Science.Data.Azure.AzureDataSet));

            Trace.WriteLine("Registered Dmitrov Providers");
            Trace.WriteLine(DataSetFactory.RegisteredToString());
        }
    }
}
