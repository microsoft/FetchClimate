using System;
using System.Linq;
using System.Diagnostics.Tracing;
using Debug = System.Diagnostics.Debug;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    /// <summary>
    /// Provides an ability to store .NET assemblies in a folder an Azure BLOB storage container.
    /// </summary>
    /// <remarks>
    /// The AssemblyStore class inherits from <see cref="MarshalByRefObject"/> so that it can work in a separate domain.
    /// When initialized, it installs an <see cref="AppDomain.AssemblyResolve"/> event handler.
    /// The handler searched the requested assembly in the Azure BLOB storage and loads the assembly in the domain if found.
    /// </remarks>
    public partial class AssemblyStore
    {
        [EventSource(Name = "AssemblyStoreEvents")]
        class Trace : EventSource
        {
            public static Trace Log = new Trace();
            public void Connected(int appDomainId, string storeKind, string storePath) { WriteEvent(1, appDomainId, storeKind, storePath); }
            public void ResolvedAssembly(int appDomainId, string assembly) { WriteEvent(2, appDomainId, assembly); }
            [Event(3,Level = EventLevel.Error)]
            public void NotFoundAssembly(int appDomainId, string assembly) { WriteEvent(3, appDomainId, assembly); }
            public void StartDownload(string path) { WriteEvent(4, path); }
            public void FinishDownload(string path) { WriteEvent(5, path); }
            public void StartUpload(string assembly) { WriteEvent(6, assembly); }
            public void FinishUpload(string assembly) { WriteEvent(7, assembly); }
        }
        /// <summary>
        /// Abstracts storage operations.
        /// </summary>
        abstract class BlobStore
        {
            protected BlobStore() { }
            abstract public void Save(Assembly asm);
            abstract public Assembly Load(AssemblyName aname);
            abstract public bool Contains(AssemblyName aname);
            abstract public void Clear();
        }
        private string connectionString;
        private BlobStore store;
        private AppDomain appdomain;
        private AssemblyStore isolatedInstance = null;

        private void SetupIsolated()
        {
            appdomain = AppDomain.CreateDomain(connectionString);
            isolatedInstance = (AssemblyStore)appdomain.CreateInstanceFromAndUnwrap(
                typeof(AssemblyStore).Assembly.Location,
                typeof(AssemblyStore).FullName);
            isolatedInstance.Connect(connectionString, false);
        }
        public AssemblyStore() { }
        /// <summary>
        /// Constructs AssemblyStore object
        /// </summary>
        /// <param name="connectionString">A path to a folder or an azure storage connection string to open the connection with azure blob storage</param>
        public AssemblyStore(string connectionString) { Connect(connectionString, false); }
        public AssemblyStore(string connectionString, bool isolate) { Connect(connectionString, isolate); }
        public void Connect(string connectionString, bool isolate)
        {
            this.connectionString = connectionString;
            try {
                store = new AzureBlobStore(connectionString);
            }
            catch (Exception)
            {
                store = new FolderBlobStore(connectionString);
            }
            if (isolate)
                SetupIsolated();
            else
                AppDomain.CurrentDomain.AssemblyResolve += this.AssemblyResolveHandler;
        }

        public void ConnectFolder(string connectionString)
        {
            if (!System.IO.Directory.Exists(connectionString)) throw new ArgumentException("No such folder: " + connectionString);
            this.connectionString = connectionString;
        }
        // A regular expression to parse AssemblyQualifiedTypeName
        // "Specifying Fully Qualified Type Names" https://msdn.microsoft.com/en-us/library/yfsftwz6.aspx
        private static System.Text.RegularExpressions.Regex typeNameRegEx =
            new System.Text.RegularExpressions.Regex(@"^(?<type>([^,\\]|\\,|\\\+|\\&|\\\*|\\\[|\\\]|\\\.|\\\\)+),\s*(?<assembly>(?<file>[^,<>:""/\\\|\?\*]+).*)$");
        /// <summary>
        /// Tries load a type into current domain.
        /// </summary>
        /// <remarks>This method can cross AppDomain boundary.</remarks>
        /// <param name="assemblyQualifiedTypeName">An assembly qualified name of the type.</param>
        /// <returns>A tpair of 'success' and 'result'. The result is a full assembly qualified type name, or an error message in case success==false.</returns>
        public Tuple<bool, string> TryLoadType(string assemblyQualifiedTypeName)
        {
            if (null != isolatedInstance)
                return isolatedInstance.TryLoadType(assemblyQualifiedTypeName);

            try
            {
                var match = typeNameRegEx.Match(assemblyQualifiedTypeName);
                if (match.Success) //throw new FormatException("Wrong format of assembly qualified type name: " + assemblyQualifiedTypeName);
                {
                    var typeName = match.Groups["type"].Value;
                    var assemblyName = match.Groups["assembly"].Value;
                    var aname = new AssemblyName(assemblyName);
                    var asm = AppDomain.CurrentDomain.Load(aname);
                    var t = asm.GetType(typeName);
                    if (null == t)
                        return Tuple.Create(false, string.Format("The type {0} was not found in {1}.", typeName, asm.FullName));
                    return Tuple.Create(true, t.AssemblyQualifiedName);
                }
                // try interpret the argument as an assembly name and find a proper type in it
                try
                {
                    var aname = new AssemblyName(assemblyQualifiedTypeName);
                    var asm = AppDomain.CurrentDomain.Load(aname);
                    var handlers = asm.GetTypes()
                        .Where(t => typeof(DataSourceHandler).IsAssignableFrom(t))
                        .ToList();
                    if (0 == handlers.Count)
                        return Tuple.Create(false, "No data handler found in " + asm.FullName);
                    if (1<handlers.Count)
                        return Tuple.Create(false, "No data handler found in " + asm.FullName);
                    return Tuple.Create(true, handlers[0].AssemblyQualifiedName);
                }
                catch (FormatException)
                {
                    return Tuple.Create(false, "The argument is not a type name nor an assembly name: " + assemblyQualifiedTypeName);
                }
            }
            catch (Exception e)
            {
                return Tuple.Create(false, e.Message);
            }
        }

        Assembly AssemblyResolveHandler(object sender, ResolveEventArgs args)
        {
            var aname = new AssemblyName(args.Name);
            if (store.Contains(aname))
            {
                Trace.Log.ResolvedAssembly(AppDomain.CurrentDomain.Id, args.Name);
                return store.Load(aname);
            }
            else
            {
                Trace.Log.NotFoundAssembly(AppDomain.CurrentDomain.Id, args.Name);
                return null;
            }
        }

        public void Install(Assembly assembly)
        {
            store.Save(assembly);
        }

        /// <summary>
        /// Clears the <see cref="AssemblyStore"/> of any assembles
        /// </summary>
        public void Reset()
        {
            if (null != isolatedInstance)
            {
                AppDomain.Unload(appdomain);
                SetupIsolated();
            }
            store.Clear();
        }
    }
}