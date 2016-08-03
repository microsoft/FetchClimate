using Microsoft.Research.Science.Data;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.Web;
using System.Reflection;

namespace Microsoft.Research.Science.FetchClimate2
{
    /// <summary>Describes an instance of data source - a pair of handler and data archive</summary>
    public class DataSourceInstance
    {
        private readonly DataSourceHandler handler;
        private readonly DataSet storage;

        public DataSourceInstance(DataSourceHandler handler, DataSet storage)
        {
            this.handler = handler;
            this.storage = storage;
        }

        public DataSourceHandler Handler
        {
            get { return handler; }
        }

        public DataSet Storage
        {
            get { return storage; }
        }
    }

    /// <summary>Class to store data sources</summary>
    public class DataSourceHandlerCache
    {
        static AutoRegistratingTraceSource ts = new AutoRegistratingTraceSource("DataSourceHandlerCache", SourceLevels.All);

        class TypeUriPair : IEquatable<TypeUriPair>
        {
            public string TypeName { get; set; }
            public string DataUri { get; set; }

            public bool Equals(TypeUriPair other)
            {
                return TypeName == other.TypeName && DataUri == other.DataUri;
            }

            public override bool Equals(object obj)
            {
                TypeUriPair other = obj as TypeUriPair;
                return other == null ? false : Equals(other);
            }

            public override int GetHashCode()
            {
                return TypeName.GetHashCode() ^ DataUri.GetHashCode();
            }
        }

        static Dictionary<TypeUriPair, Task<DataSourceInstance>> instances =
            new Dictionary<TypeUriPair, Task<DataSourceInstance>>();

        /// <summary>Creates data source from handler type name and data uri or return existing one.
        /// This method is thread safe</summary>
        public static async Task<DataSourceInstance> GetInstanceAsync(string typeName, string dataUri)
        {
            var key = new TypeUriPair
            {
                TypeName = typeName,
                DataUri = dataUri
            };
            Task<DataSourceInstance> val;
            lock (typeof(DataSourceHandlerCache))
            {
                if (!instances.TryGetValue(key, out val))
                {
                    val = Task.Factory.StartNew(async obj =>
                    {
                        TypeUriPair pair = (TypeUriPair)obj;
                        ts.TraceEvent(TraceEventType.Information, 1, "loading type " + pair.TypeName + " with data uri " + pair.DataUri);
                        var handlerType = Type.GetType(pair.TypeName);
                        if (handlerType == null)
                            throw new Exception(String.Format("Type {0} is not found", pair.TypeName));

                        Task<DataSet> storageTask = OpenDataSetWithRetriesAsync(pair.DataUri); ;
                        DataSourceHandler handler;
                        bool withoutStorage = false;

                        //trying Async API
                        var methods = handlerType.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

                        Func<Type, bool> checkAsyncReturnType = (t =>
                            {
                                if (!t.IsGenericType) return false;

                                var args = t.GetGenericArguments();
                                if (args.Length != 1) return false;

                                var genericArg = args[0];
                                return genericArg.IsSubclassOf(typeof(DataSourceHandler));
                            });

                        MethodInfo[] asyncCtors = methods.Where(m => m.Name == "CreateAsync" && checkAsyncReturnType(m.ReturnType)).ToArray();
                        MethodInfo asyncCtor = null;

                        //Factory with IDataContext parameter is first priority
                        foreach (var c in asyncCtors)
                        {
                            var parameters = c.GetParameters();
                            if (parameters.Length == 1 && parameters[0].ParameterType.IsInterface && parameters[0].ParameterType == typeof(IStorageContext))
                            {
                                ts.TraceEvent(TraceEventType.Information, 2, pair.TypeName + ": Found Async factory method with IStorageContext parameter");
                                asyncCtor = c; break;
                            }
                        }

                        //if not found, looking for Factory with no parameters
                        if (asyncCtor == null)
                            foreach (var c in asyncCtors)
                            {
                                var parameters = c.GetParameters();
                                if (parameters.Length == 0)
                                {
                                    ts.TraceEvent(TraceEventType.Information, 3, pair.TypeName + ": Found Async factory method with no parameters");
                                    withoutStorage = true;
                                    asyncCtor = c; break;
                                }
                            }


                        if (asyncCtor != null)
                        {
                            Type genericRetType = asyncCtor.ReturnType;
                            Type genericArg = asyncCtor.ReturnType.GetGenericArguments()[0];

                            object o;

                            if (withoutStorage)
                                o = asyncCtor.Invoke(null, new object[0]);
                            else
                                o = asyncCtor.Invoke(null, new object[] { new LinearizingStorageContext(await storageTask) });

                            await (Task)o;
                            handler = (DataSourceHandler)o.GetType().InvokeMember("Result", BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty, null, o, new object[0]);

                            return new DataSourceInstance(handler, await storageTask);
                        }


                        //tring old API
                        var ctor = handlerType.GetConstructor(new Type[] { typeof(IStorageContext) });

                        if (ctor == null)
                        {
                            ctor = handlerType.GetConstructor(new Type[0]);
                            withoutStorage = true;
                        }
                        else
                            ts.TraceEvent(TraceEventType.Information, 4, pair.TypeName + ": Found synchronous constructor with IStorageContext parameter");

                        if (ctor == null)
                            throw new Exception(String.Format("Type {0} does not have required constructor", pair.TypeName));
                        else
                            ts.TraceEvent(TraceEventType.Information, 5, pair.TypeName + ": Found synchronous constructor without parameters");


                        if (withoutStorage)
                            handler = (DataSourceHandler)ctor.Invoke(new object[0]);
                        else
                            handler = (DataSourceHandler)ctor.Invoke(new object[] { new LinearizingStorageContext(await storageTask) });
                        return new DataSourceInstance(handler, await storageTask);
                    }, key, TaskCreationOptions.LongRunning).Unwrap();
                    instances.Add(key, val);
                }
            }
            return await val;
        }

        static readonly int[] RetryTimeouts = new int[] { 1000, 3000, 10000, 60000, 180000 }; // 1 sec, 3 sec, 10 sec, 1 min, 3 min.
        static readonly Random random = new Random();

        private static Task<DataSet> OpenDataSetWithRetriesAsync(string uri)
        {
            return Task.Run(async () =>
                {
                    if (string.IsNullOrEmpty(uri))
                    {
                        FetchEngine.traceSource.TraceEvent(TraceEventType.Warning, 14, "Specified URI is empty. Opening empty memory dataset.");
                        uri = "msds:memory";
                    }
                    Uri u = new Uri(uri);
                    var parsed = HttpUtility.ParseQueryString(u.Query);
                    bool doCloneToMemory = parsed.AllKeys.Any(key => key == "cloneToMemory" || key == "copyToMemory");
                    bool isRemoteFile = new string[] {"http","https"}.Contains(u.Scheme.ToLower());                    
                    for (var i = 0; i < RetryTimeouts.Length; i++)
                    {
                        try
                        {
                            DataSet result = null;

                            if (isRemoteFile)
                            {
                                FetchEngine.traceSource.TraceEvent(TraceEventType.Information, 13, "Requested http available dataset file {0}", uri);
                                result = await NetCDFlocalReplicator.OpenOrCloneAsync(u.ToString());
                            }
                            else
                            {
                                FetchEngine.traceSource.TraceEvent(TraceEventType.Information, 10, "Opening dataset {0} for remote access", uri);
                                result = DataSet.Open(uri);
                            }

                            if (doCloneToMemory)
                            {
                                FetchEngine.traceSource.TraceEvent(TraceEventType.Start, 11, "cloning dataset \"{0}\" into the memory", uri);
                                var inMemory = result.Clone("msds:memory");
                                FetchEngine.traceSource.TraceEvent(TraceEventType.Stop, 11, "Dataset {0} cloned to the memory", uri);
                                result.Dispose();
                                result = inMemory;
                            }
                            return result;
                        }
                        catch (Exception e)
                        {
                            FetchEngine.traceSource.TraceEvent(TraceEventType.Error, 12, "Error opening dataset {0}: {1}", uri, e);
                        }
                        await Task.Delay((int)((0.9 + 0.2 * random.NextDouble()) * RetryTimeouts[i]));
                    }
                    throw new InvalidOperationException(String.Format("Cannot open dataset {0} after {1} retries", uri, RetryTimeouts.Length));
                });
        }
    }
}
