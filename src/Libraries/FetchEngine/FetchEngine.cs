using Microsoft.Research.Science.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    public class FetchEngine : IFetchEngine
    {
        static internal readonly AutoRegistratingTraceSource traceSource = new AutoRegistratingTraceSource("FetchEngine", SourceLevels.All);

        private IExtendedConfigurationProvider configProvider;

        public FetchEngine(IExtendedConfigurationProvider provider)
        {
            configProvider = provider;
        }

        private static IFetchRequest TranslateRequestIntoDsNamings(IFetchRequest request, ExtendedDataSourceDefinition dataSource)
        {
            string dsName;
            if (dataSource.EnvToDsMapping.TryGetValue(request.EnvironmentVariableName, out dsName))
                return new FetchRequest(dsName, request.Domain, request.ReproducibilityTimestamp, request.ParticularDataSource);
            else
                return request;
        }

        private long bytesRead = 0;

        /// <summary>
        /// The number of bytes read from the data storage
        /// </summary>
        public long BytesRead
        {
            get
            {
                return bytesRead;
            }
        }

        /// <summary>
        /// Requests the remote FetchClimate (federated) to process the FetchRequest
        /// </summary>
        /// <param name="remoteServiceURI">The URI of the remote service</param>
        /// <param name="request">Request to perform</param>
        /// <returns></returns>
        public virtual async Task<IFetchResponseWithProvenance> PerformRemoteRequestAsync(string remoteServiceURI, IFetchRequest request)
        {
            RemoteFetchClient rfc = new RemoteFetchClient(new Uri(remoteServiceURI));
            var resultDs = await rfc.FetchAsync(request);
            return new FetchResponseWithProvenance(
                request,
                resultDs[RequestDataSetFormat.ValuesVariableName].GetData(),
                resultDs[RequestDataSetFormat.UncertaintyVariableName].GetData(),
                resultDs.Variables.Contains(RequestDataSetFormat.ProvenanceVariableName)
                    ? resultDs[RequestDataSetFormat.ProvenanceVariableName].GetData()
                    : null);
        }

        public async Task<IFetchResponseWithProvenance> PerformRequestAsync(IFetchRequest request)
        {
            string error;
            if (!request.Domain.IsContentValid(out error))
                throw new ArgumentException(error);

            var config = configProvider.GetConfiguration(request.ReproducibilityTimestamp);
            string envVariableName = request.EnvironmentVariableName;
            string[] dataSourcesName = request.ParticularDataSource;
           traceSource.TraceEvent(TraceEventType.Start,1,string.Format("Request started: {0},{1},{2}",
                envVariableName, (dataSourcesName == null) ? ("*") : (dataSourcesName.Aggregate(string.Empty, (acc, ds) => acc + " " + ds)), request.ReproducibilityTimestamp));
            var dataSources = (dataSourcesName == null) ?
                config.DataSources.Where(d => d.ProvidedVariables.Contains(envVariableName)).ToArray() :
                config.DataSources.Where(d => dataSourcesName.Contains(d.Name) && d.ProvidedVariables.Contains(envVariableName)).ToArray();

            try
            {
                if (dataSources.Length == 0)
                {
                    traceSource.TraceEvent(TraceEventType.Error,2,"No data sources found for serving the request");
                    throw new InvalidOperationException("No data sources found for serving the request");
                }
                else if (dataSources.Length == 1) // One data source - invoke it directly, no further merging
                {
                    return await ProcessWithDataSourceAsync(request, dataSources[0]);
                }
                else // Full intelligent fetching pipeline
                {
                    return await IntelligentFetchingAsync(request, dataSources);
                }
            }
            catch (Exception exc)
            {
                traceSource.TraceEvent(TraceEventType.Error, 999, string.Format("Request failed: {0}", exc.ToString()));
                traceSource.TraceData(TraceEventType.Error, 999, exc);
                throw exc;
            }
        }

        /// <summary>
        /// Performs the request with the remote data sources, grouping them into requests by remote service URI
        /// </summary>
        /// <param name="request"></param>
        /// <param name="federatedDataSources"></param>
        /// <returns></returns>
        private async Task<Tuple<string, IFetchResponseWithProvenance>[]> ProcessWithFederatedDataSourcesAsync(IFetchRequest request, IEnumerable<ExtendedDataSourceDefinition> federatedDataSources)
        {
            if (federatedDataSources.Any(ds => !ds.IsFederated))
                throw new InvalidOperationException("Expected federated data source definition, while passed definition is local data source");
            var federatedBackNameMappings = FormFederatedBackNameMappings(federatedDataSources);
            var federaedBackIdsMappings = FormFederatedBackIDsMappings(federatedDataSources);

            var groupsByURI = federatedDataSources.GroupBy(d => d.Uri);

            //start federated requests
            var federatedRequestsTasks = groupsByURI.Select(group =>
            {
                string remoteVarName = group.First().EnvToDsMapping[request.EnvironmentVariableName]; //forward env var name mapping
                if (group.Any(ds => ds.EnvToDsMapping[request.EnvironmentVariableName] != remoteVarName))
                    throw new InvalidOperationException("different data sources with the same URI must have the same mappings");
                IFetchRequest remoteRequest = new FetchRequest(
                    remoteVarName, // as all entries in the group must have the same mappings
                    request.Domain,
                    request.ReproducibilityTimestamp,
                    group.Select(ds => ds.RemoteDataSourceName).ToArray()
                    );
                return Tuple.Create(
                    group.Key,
                    PerformRemoteRequestAsync(group.Key, remoteRequest)
                        .ContinueWith<IFetchResponseWithProvenance>(res => //filling up provenance, if remote service didn't put it into the reply
                        {
                            if (group.Count() == 1) //if remote service is constraint to only one data source it won't contain provenance array in the reply. Constructing it
                                return new FetchResponseWithProvenance(remoteRequest,
                                    res.Result.Values,
                                    res.Result.Uncertainty,
                                    ArrayHelper.GetConstantArray<ushort>(request.Domain.GetDataArrayShape(), group.First().RemoteDataSourceID));
                            else
                                return res.Result; //provenance is already there, no need to modify the reply
                        }));
            }).ToArray();
            var federatedResults = new Tuple<string, IFetchResponseWithProvenance>[federatedRequestsTasks.Length];
            for (int i = 0; i < federatedResults.Length; i++)
            {
                var remoteServiceResult = await federatedRequestsTasks[i].Item2;
                var uriResultPair = Tuple.Create(federatedRequestsTasks[i].Item1,
                     (IFetchResponseWithProvenance)(new FetchResponseWithProvenance(request, //backward env var name mapping through assigning request variable (it contains original env var name)
                    remoteServiceResult.Values, remoteServiceResult.Uncertainty, remoteServiceResult.Provenance))
                    );

                //backward IDs mapping if needed
                if (federaedBackIdsMappings[uriResultPair.Item1].Count > 0) //there are some unequal mapping between IDs                
                    ReplaceArrayElements(uriResultPair.Item2.Provenance, federaedBackIdsMappings[uriResultPair.Item1]);

                federatedResults[i] = uriResultPair;
            }
            traceSource.TraceEvent(TraceEventType.Information,3,"Federated data sources results are received");
            return federatedResults;
        }

        /// <summary>
        /// Applies intelligent fetching logic (competition by uncertainties, returning the results from the data sources with lowest uncertainty) for a specified request among specified data sources
        /// </summary>
        /// <param name="request"></param>
        /// <param name="dataSources"></param>
        /// <returns></returns>
        private async Task<IFetchResponseWithProvenance> IntelligentFetchingAsync(IFetchRequest request, ExtendedDataSourceDefinition[] dataSources)
        {
            traceSource.TraceEvent(TraceEventType.Information, 4, string.Format("{0} data sources serve the request", dataSources.Length));
            var localDataSources = dataSources.Where(d => !d.IsFederated);
            var federatedDataSources = dataSources.Where(d => d.IsFederated);


            var localResultTask = ProcessWithLocalDataSourcesAsync(request, localDataSources.ToArray());
            var federatedResultsTask = ProcessWithFederatedDataSourcesAsync(request, federatedDataSources);

            List<Array> provenances = new List<Array>(dataSources.Length);
            List<Array> uncertainties = new List<Array>(dataSources.Length);
            List<Array> values = new List<Array>(dataSources.Length);

            var localResult = await localResultTask;
            if (localResult != null)
            {
                provenances.Add(localResult.Provenance);
                uncertainties.Add(localResult.Uncertainty);
                values.Add(localResult.Values);
            }

            var federatedResults = await federatedResultsTask;
            foreach (var remoteServiceResult in federatedResults)
            {
                provenances.Add(remoteServiceResult.Item2.Provenance);
                uncertainties.Add(remoteServiceResult.Item2.Uncertainty);
                values.Add(remoteServiceResult.Item2.Values);
            }

            var mergedResults = MergeResult(provenances, uncertainties, values);


            if (mergedResults == null)
                throw new Exception("Request can't be processed as there are no data sources which are able to process it. Check your service configurations (especially variables mappings)");

            //int[] dsIDsWithNans = GetIDsOfDataSourcesWithNanInValues(mergedResults.Provenance, mergedResults.Uncertatinty, mergedResults.Values);
            //if (dsIDsWithNans.Length > 0)
            //{
            //    dataSources = dataSources.Where(ds => !dsIDsWithNans.Contains(ds.ID)).ToArray(); //filtering out data sources that returned NAN. One more loop pass for uncertainty competition round                
            //}

            return new FetchResponseWithProvenance(request,
            mergedResults.Values,
            mergedResults.Uncertatinty,
            mergedResults.Provenance);
        }

        /// <summary>
        /// Returns null if there are no local data source that can handle specified variable
        /// </summary>
        /// <param name="request"></param>
        /// <param name="localDataSources"></param>
        /// <returns></returns>
        private async Task<IFetchResponseWithProvenance> ProcessWithLocalDataSourcesAsync(IFetchRequest request, ExtendedDataSourceDefinition[] localDataSources)
        {
            IFetchResponseWithProvenance localResult;
            
            traceSource.TraceEvent(TraceEventType.Start, 6, "Local data sources are starting processing");

            // Start individual fetches
            if (localDataSources.Length == 0)
            {
                localResult = null;
            }
            else if (localDataSources.Length == 1) //merge of local data source won't be performed
            {
                traceSource.TraceEvent(TraceEventType.Start, 5, "Starting processing the request with the only data source (without further results merging)");
                localResult = await ProcessWithLocalDataSourceAsync(request, localDataSources[0]);
                traceSource.TraceEvent(TraceEventType.Stop, 5, "Finished processing the request with the only data source (without further results merging)");

            }
            else
            {
                var localFetches = await Task.WhenAll(
                    localDataSources.Select(s => DataSourceHandlerCache.GetInstanceAsync(s.HandlerTypeName, s.Uri).ContinueWith<Tuple<Task<Array>, DependentRequestContext>>((initTask, dataSource) =>
                    {
                        var handler = initTask.Result;
                        var ds = (ExtendedDataSourceDefinition)dataSource;

                        var ctx = new DependentRequestContext(TranslateRequestIntoDsNamings(request, ds),
                            this,
                            ds.DsToEnvMapping,
                            handler.Storage,
                            ds.ID);

                        return new Tuple<Task<Array>, DependentRequestContext>(handler.Handler.ProcessRequestAsync(ctx), ctx);
                    }, s)));

                // waiting all of the tasks either for returning value or uncertainties. Removing tasks that returned values without uncertainties from fetches list
                var finishedTasks = await Task.WhenAll(localFetches.Select(t => Task.WhenAny(t.Item1, t.Item2.EvaluateUncertaintyTask)));

                // Check if no individual fetch fails
                if (finishedTasks.Any(f => f.Status != TaskStatus.RanToCompletion))
                {
                    Array.ForEach(localFetches, f => f.Item2.SetCanceled());
                    string errorMess = string.Empty;
                    var failedTask = finishedTasks.First(f => f.Status != TaskStatus.RanToCompletion);
                    if (failedTask.Exception != null)
                        errorMess += failedTask.Exception.Flatten().ToString();
                    throw new Exception("One or more dependent tasks failed. " + errorMess);
                }

                var tasksWithValuesReady = finishedTasks.Where(f => f is Task<Array>).ToList();
                var finishedLocalFetches = localFetches.Where(f => tasksWithValuesReady.Contains(f.Item1)).ToList();
                var uncertaintyEvaluatedLocalFetches = localFetches.Where(f => !tasksWithValuesReady.Contains(f.Item1)).ToList();

                // Complete intelligent request of no dependent request waits for uncertainty
                if (!uncertaintyEvaluatedLocalFetches.Any())//one or more data source returned value, but no one returned uncertainties. So returning any (first) data source result, as we have to choose what result to ruturn
                {
                    localResult = new FetchResponseWithProvenance(request,
                        finishedLocalFetches[0].Item1.Result,
                        ArrayHelper.GetConstantArray<double>(request.Domain.GetDataArrayShape(), Double.MaxValue),
                        ArrayHelper.GetConstantArray<ushort>(request.Domain.GetDataArrayShape(), finishedLocalFetches[0].Item2.ID));
                }
                else if (uncertaintyEvaluatedLocalFetches.Count == 1) // exactly one data source reported uncertainty, returning it's values, ignoring others
                {
                    uncertaintyEvaluatedLocalFetches[0].Item2.SetProvenance(null); //all elements are needed

                    localResult = new FetchResponseWithProvenance(request,
                        uncertaintyEvaluatedLocalFetches[0].Item1.Result,
                        uncertaintyEvaluatedLocalFetches[0].Item2.ReportUncertainty(),
                        ArrayHelper.GetConstantArray<ushort>(request.Domain.GetDataArrayShape(), uncertaintyEvaluatedLocalFetches[0].Item2.ID));
                }
                else
                {
                    List<ushort> requiredIDs;
                    var provenance = Array.CreateInstance(typeof(ushort), request.Domain.GetDataArrayShape());
                    var uncertainty = Array.CreateInstance(typeof(double), request.Domain.GetDataArrayShape());
                    var isTimeSeriesRequest = request.Domain.TimeRegion.IsTimeSeries;
                    MergeUncertainty(ref uncertainty, ref provenance, out requiredIDs,
                        uncertaintyEvaluatedLocalFetches.Select(f => new Tuple<Array, ushort>(f.Item2.ReportUncertainty(), f.Item2.ID)).ToArray());

                    // Remove all sources that are not present in merged provenance array
                    for (int i = 0; i < uncertaintyEvaluatedLocalFetches.Count; )
                        if (requiredIDs.Contains(uncertaintyEvaluatedLocalFetches[i].Item2.ID))
                            i++;
                        else
                        {
                            uncertaintyEvaluatedLocalFetches[i].Item2.SetCanceled();
                            uncertaintyEvaluatedLocalFetches.RemoveAt(i);
                        }

                    uncertaintyEvaluatedLocalFetches.ForEach(f => f.Item2.SetProvenance(provenance));

                    Array values = Array.CreateInstance(typeof(double), request.Domain.GetDataArrayShape());
                    Tuple<Array, ushort>[] localResults = new Tuple<Array, ushort>[uncertaintyEvaluatedLocalFetches.Count];
                    for (int i = 0; i < localResults.Length; i++)
                        localResults[i] = Tuple.Create(await uncertaintyEvaluatedLocalFetches[i].Item1, uncertaintyEvaluatedLocalFetches[i].Item2.ID);
                    MergeValues(provenance, ref values, localResults);

                    localResult = new FetchResponseWithProvenance(request, values, uncertainty, provenance);
                }
            }
            traceSource.TraceEvent(TraceEventType.Stop, 6, "Local data sources finished computation");
            return localResult;
        }

        /// <summary>
        /// mapping that associate each of the remote FetchClimate URI to the mapping that can be applied to back convert remote env var names to the local env names (fetch var names)
        /// </summary>
        /// <param name="dataSources"></param>
        /// <returns></returns>
        private static Dictionary<string, Dictionary<string, string>> FormFederatedBackNameMappings(IEnumerable<ExtendedDataSourceDefinition> dataSources)
        {
            var federatedDataSources = dataSources.Where(d => d.IsFederated);
            var groupedFederated = federatedDataSources.GroupBy(d => d.Uri);
            var federaedBackNameMappings = groupedFederated.Select(
                g => new
                {
                    Uri = g.Key,
                    Dict = g.Aggregate(
                    new Dictionary<string, string>(),
                    (acc, ds) =>
                    {
                        foreach (var map in ds.DsToEnvMapping)
                            acc[map.Key] = map.Value;
                        return acc;
                    })
                }).ToDictionary(d => d.Uri, d => d.Dict);
            return federaedBackNameMappings;
        }

        /// <summary>
        /// mapping that associate each of the remote FetchClimate URI to the mapping that can be applied to back convert remote data source id to the local data source id
        /// </summary>
        /// <param name="dataSources"></param>
        /// <returns></returns>
        private static Dictionary<string, Dictionary<ushort, ushort>> FormFederatedBackIDsMappings(IEnumerable<ExtendedDataSourceDefinition> dataSources)
        {
            var federatedDataSources = dataSources.Where(d => d.IsFederated);
            var groupedFederated = federatedDataSources.GroupBy(d => d.Uri);
            var federaedBackIdsMappings = groupedFederated.Select(
                            g => new
                            {
                                Uri = g.Key,
                                Dict = g.Aggregate(
                                new Dictionary<ushort, ushort>(),
                                (acc, ds) =>
                                {
                                    if (ds.RemoteDataSourceID != ds.ID) //do not accumulate equality mapping
                                        acc.Add(ds.RemoteDataSourceID, ds.ID);
                                    return acc;
                                })
                            }).ToDictionary(d => d.Uri, d => d.Dict);
            return federaedBackIdsMappings;
        }

        /// <summary>
        /// Process the request with a data source specified by its definition
        /// </summary>
        /// <param name="request">A request to process</param>
        /// <param name="dataSource">A data source definition to process the request with</param>
        /// <returns></returns>
        private async Task<IFetchResponseWithProvenance> ProcessWithDataSourceAsync(IFetchRequest request, ExtendedDataSourceDefinition dataSource)
        {
            traceSource.TraceEvent(TraceEventType.Information, 7, string.Format("One data source {0} serves the request", dataSource.Name));
            if (!dataSource.IsFederated) //local
            {
                return await ProcessWithLocalDataSourceAsync(request, dataSource);
            }
            else //federated
            {
                return await ProcessWithFederatedDataSourceAsync(request, dataSource);
            }
        }

        /// <summary>
        /// Process the request with federated data source specified by its definition
        /// </summary>
        /// <param name="request">A request to process</param>
        /// <param name="ds">A data source definition to process the request with</param>
        /// <returns></returns>
        private async Task<IFetchResponseWithProvenance> ProcessWithFederatedDataSourceAsync(IFetchRequest request, ExtendedDataSourceDefinition ds)
        {
            if (!ds.IsFederated)
                throw new InvalidOperationException("Expected federated data source definition, while passed definition is local data source");
            var res = await PerformRemoteRequestAsync(ds.Uri, new FetchRequest(
                ds.EnvToDsMapping[request.EnvironmentVariableName], //using remove env names
                request.Domain,
                request.ReproducibilityTimestamp,
                new string[] { ds.RemoteDataSourceName } //using remote ds name
                ));
            return new FetchResponseWithProvenance(request,// note here, that we don't convert env var names back, we simply take the initial request and append it with the result arrays
                res.Values,
                res.Uncertainty,
                ArrayHelper.GetConstantArray<ushort>(request.Domain.GetDataArrayShape(), ds.ID));
        }

        /// <summary>
        /// Process the request with a local data source specified by its definition
        /// </summary>
        /// <param name="request">A request to process</param>
        /// <param name="dataSource">A data source definition to process the request with</param>
        /// <returns></returns>
        private async Task<IFetchResponseWithProvenance> ProcessWithLocalDataSourceAsync(IFetchRequest request, ExtendedDataSourceDefinition dataSource)
        {
            if (dataSource.IsFederated)
                throw new InvalidOperationException("Expected local data source definition, while passed definition is federated data source");
            var instance = await DataSourceHandlerCache.GetInstanceAsync(dataSource.HandlerTypeName, dataSource.Uri);
            StandaloneRequestContext ctx = new StandaloneRequestContext(
                    TranslateRequestIntoDsNamings(request, dataSource),
                    this,
                    dataSource.DsToEnvMapping,
                    instance.Storage,
                    dataSource.ID);
            var values = await instance.Handler.ProcessRequestAsync(ctx);
            var uncertainty = ctx.ReportUncertainty();
            if (uncertainty == null)
                uncertainty = ArrayHelper.GetConstantArray<double>(request.Domain.GetDataArrayShape(), Double.MaxValue);
            return new FetchResponseWithProvenance(request, values, uncertainty,
                ArrayHelper.GetConstantArray<ushort>(request.Domain.GetDataArrayShape(), dataSource.ID));
        }

        private static void FilterNansInUncertainty(ref Array[] provenances, out List<ushort> usedIds, Tuple<Array, ushort>[] unmergedUncertatinties)
        {
            usedIds = new List<ushort>();

            int count = provenances[0].Length;
            int arraysCount = unmergedUncertatinties.Length;
            GCHandle?[] resProvHandle = new GCHandle?[arraysCount];
            IntPtr[] resProvPtr = new IntPtr[arraysCount];            

            GCHandle?[] uncertatiniesHandles = new GCHandle?[arraysCount];
            IntPtr[] uncertaintiesPtrs = new IntPtr[arraysCount];
            for (int i = 0; i < arraysCount; i++)
            {
                uncertatiniesHandles[i] = GCHandle.Alloc(unmergedUncertatinties[i].Item1, GCHandleType.Pinned);
                resProvHandle[i] = GCHandle.Alloc(provenances[i], GCHandleType.Pinned);                
                uncertaintiesPtrs[i] = uncertatiniesHandles[i].Value.AddrOfPinnedObject();
                resProvPtr[i] = resProvHandle[i].Value.AddrOfPinnedObject();                
            }
            try
            {
                unsafe
                {                    
                    double*[] uncs = new double*[arraysCount];                    
                    ushort*[] id = new ushort*[arraysCount];
                    ushort[] ids = new ushort[arraysCount];
                    for (int i = 0; i < arraysCount; i++)
                    {                        
                        uncs[i] = (double*)(uncertaintiesPtrs[i]);
                        id[i] = (ushort*)(resProvPtr[i]);
                        ids[i] = unmergedUncertatinties[i].Item2;
                    }

                    double currUnc;
                    for (int j = 0; j < arraysCount; j++)
                    {
                        ushort currId = ids[j];
                        bool notNanFound = false;
                        for (int i = 0; i < count; i++)
                        {
                            currUnc = uncs[j][i];
                            if (!double.IsNaN(currUnc)) // writing not NAN
                            {
                                notNanFound = true;                                
                                id[j][i] = currId;                                
                            }
                        }
                        if(notNanFound)
                            usedIds.Add(currId);
                    }
                }
            }
            finally
            {
                for (int i = 0; i < arraysCount; i++)
                {
                    uncertatiniesHandles[i].Value.Free();
                    resProvHandle[i].Value.Free();                    
                }
            }
        }


        private static void MergeUncertainty(ref Array resultUncertatinty, ref Array resultProvenance, out List<ushort> usedIds, Tuple<Array, ushort>[] unmergedUncertatinties)
        {
            usedIds = new List<ushort>();

            int count = resultProvenance.Length;
            int arraysCount = unmergedUncertatinties.Length;
            GCHandle? resProvHandle = GCHandle.Alloc(resultProvenance, GCHandleType.Pinned);
            IntPtr resProvPtr = resProvHandle.Value.AddrOfPinnedObject();
            GCHandle? resUncHandle = GCHandle.Alloc(resultUncertatinty, GCHandleType.Pinned);
            IntPtr resUncPtr = resUncHandle.Value.AddrOfPinnedObject();

            GCHandle?[] uncertatiniesHandles = new GCHandle?[arraysCount];
            IntPtr[] uncertaintiesPtrs = new IntPtr[arraysCount];
            for (int i = 0; i < arraysCount; i++)
            {
                uncertatiniesHandles[i] = GCHandle.Alloc(unmergedUncertatinties[i].Item1, GCHandleType.Pinned);
                uncertaintiesPtrs[i] = uncertatiniesHandles[i].Value.AddrOfPinnedObject();
            }
            try
            {
                //making first data source uncertainties as reference values for other data sources
                Buffer.BlockCopy(unmergedUncertatinties[0].Item1, 0, resultUncertatinty, 0, count * sizeof(double));

                unsafe
                {                    
                    ushort[] ids = new ushort[arraysCount];
                    double*[] uncs = new double*[arraysCount];
                    double* unc = (double*)resUncPtr;
                    ushort* id = (ushort*)resProvPtr;
                    for (int i = 0; i < arraysCount; i++)
                    {
                        uncs[i] = (double*)(uncertaintiesPtrs[i]);
                        ids[i] = unmergedUncertatinties[i].Item2;
                    }

                    ushort firstId = ids[0];                    
                    for (int i = 0; i < count; i++)
                    {
                        id[i] = firstId;                        
                    }

                    double currUnc;
                    for (int j = 1; j < arraysCount; j++) //comparing all uncertainties except for first (as its values are already in the output arrays)
                    {                        
                        for (int i = 0; i < count; i++)
                        {
                            currUnc = uncs[j][i];
                            if (!double.IsNaN(currUnc)) // Compare only with non-NaN values
                            {
                                if (double.IsNaN(unc[i]) ||                                   
                                   unc[i] > currUnc)
                                {
                                    unc[i] = currUnc;
                                    id[i] = ids[j];                                    
                                }
                            }
                        }
                    }

                    for (int i = 0; i < count; i++)
                        if (!usedIds.Contains(id[i]))
                            usedIds.Add(id[i]);
                }
            }
            finally
            {
                resProvHandle.Value.Free();
                resUncHandle.Value.Free();
                for (int i = 0; i < arraysCount; i++)
                {
                    uncertatiniesHandles[i].Value.Free();
                }
            }
        }

        private static void MergeValues(Array provArray, ref Array mergedValues, Tuple<Array, ushort>[] items)
        {
            int arraysCount = items.Length;
            int count = provArray.Length;

            //capturing pinned pointers
            GCHandle? resProvHandle = GCHandle.Alloc(provArray, GCHandleType.Pinned);
            IntPtr resProvPtr = resProvHandle.Value.AddrOfPinnedObject();
            GCHandle? resValuesHandle = GCHandle.Alloc(mergedValues, GCHandleType.Pinned);
            IntPtr resValuesPtr = resValuesHandle.Value.AddrOfPinnedObject();

            GCHandle?[] unmergedHandles = new GCHandle?[arraysCount];
            IntPtr[] umergedPtrs = new IntPtr[arraysCount];
            for (int i = 0; i < arraysCount; i++)
            {
                unmergedHandles[i] = GCHandle.Alloc(items[i].Item1, GCHandleType.Pinned);
                umergedPtrs[i] = unmergedHandles[i].Value.AddrOfPinnedObject();
            }
            try
            {
                ushort[] ids = items.Select(i => i.Item2).ToArray();
                Buffer.BlockCopy(items[0].Item1, 0, mergedValues, 0, count * sizeof(double)); //first data source gives a reference values that can be overwriten by other data sources
                unsafe
                {
                    double* currUmnerged;
                    double* val = (double*)resValuesPtr;
                    ushort* prov = (ushort*)resProvPtr;
                    ushort currFilter;
                    for (int i = 1; i < arraysCount; i++)
                    {
                        currFilter = ids[i];
                        currUmnerged = (double*)umergedPtrs[i];
                        for (int j = 0; j < count; j++)
                            if (prov[j] == currFilter)
                                val[j] = currUmnerged[j];
                    }
                }
            }
            finally
            {
                //releasing pinned pointers
                resProvHandle.Value.Free();
                resValuesHandle.Value.Free();
                for (int i = 0; i < arraysCount; i++)
                {
                    unmergedHandles[i].Value.Free();
                }
            }
        }

        /// <summary>Builds list of IDs of data source such as for some point value is NaN and provenance is not NaN (this
        /// means that value cannot be calculated and another data source should be asked)</summary>
        private static int[] GetIDsOfDataSourcesWithNanInValues(Array provenance, Array uncertatinties, Array results)
        {
            HashSet<int> dsIDsWithNans = new HashSet<int>();
            int totalElementCount = provenance.Length;

            GCHandle? capturedProvenanceHandle = GCHandle.Alloc(provenance, GCHandleType.Pinned);
            IntPtr capturedProvenancePtr = capturedProvenanceHandle.Value.AddrOfPinnedObject();
            GCHandle? capturedUncertaintyHandle = GCHandle.Alloc(uncertatinties, GCHandleType.Pinned);
            IntPtr capturedUncertaintyPtr = capturedUncertaintyHandle.Value.AddrOfPinnedObject();
            GCHandle? capturedValuesHandle = GCHandle.Alloc(results, GCHandleType.Pinned);
            IntPtr capturedValuesPtr = capturedValuesHandle.Value.AddrOfPinnedObject();

            try
            {
                unsafe
                {
                    double* U = (double*)capturedUncertaintyPtr;
                    double* V = (double*)capturedValuesPtr;
                    ushort* P = (ushort*)capturedProvenancePtr;

                    for (int i = 0; i < totalElementCount; i++)
                        if (double.IsNaN(V[i]) && !double.IsNaN(U[i]))
                            dsIDsWithNans.Add(P[i]);
                }
            }
            finally
            {
                capturedValuesHandle.Value.Free();
                capturedUncertaintyHandle.Value.Free();
                capturedProvenanceHandle.Value.Free();
            }
            return dsIDsWithNans.ToArray();
        }

        private static MergedResults MergeResult(List<Array> provenance, List<Array> uncertatinties, List<Array> values)
        {
            int resultsToMerge = provenance.Count;
            if (resultsToMerge == 0)
                return null;
            int totalElementCount = provenance[0].Length;
            int[] shape = new int[values[0].Rank];
            for (int i = 0; i < shape.Length; i++)
                shape[i] = values[0].GetLength(i);


#if DEBUG
            Debug.Assert(provenance.Count == uncertatinties.Count && provenance.Count == values.Count);
            for (int i = 0; i < resultsToMerge; i++)
            {
                for (int j = 0; j < shape.Length; j++)
                {
                    Debug.Assert(provenance[i].GetLength(j) == shape[j]);
                    Debug.Assert(uncertatinties[i].GetLength(j) == shape[j]);
                }
            }
#endif



            MergedResults results = new MergedResults(
                Array.CreateInstance(typeof(ushort), shape),
                Array.CreateInstance(typeof(double), shape),
                Array.CreateInstance(typeof(double), shape));

            //capturing pointers
            GCHandle? capturedResultProvenanceHandle = GCHandle.Alloc(results.Provenance, GCHandleType.Pinned);
            IntPtr capturedResultProvenancePtr = capturedResultProvenanceHandle.Value.AddrOfPinnedObject();
            GCHandle? capturedResultUncertaintyHandle = GCHandle.Alloc(results.Uncertatinty, GCHandleType.Pinned);
            IntPtr capturedResultUncertaintyPtr = capturedResultUncertaintyHandle.Value.AddrOfPinnedObject();
            GCHandle? capturedResultValuesHandle = GCHandle.Alloc(results.Values, GCHandleType.Pinned);
            IntPtr capturedResultValuesPtr = capturedResultValuesHandle.Value.AddrOfPinnedObject();

            GCHandle?[] capturedProvenanceHandles = new GCHandle?[resultsToMerge];
            IntPtr[] capturedProvenancePtrs = new IntPtr[resultsToMerge];
            GCHandle?[] capturedUncertatintyHandles = new GCHandle?[resultsToMerge];
            IntPtr[] capturedUncertatintyPtrs = new IntPtr[resultsToMerge];
            GCHandle?[] capturedValuesHandles = new GCHandle?[resultsToMerge];
            IntPtr[] capturedValuesPtrs = new IntPtr[resultsToMerge];
            for (int i = 0; i < resultsToMerge; i++)
            {
                capturedProvenanceHandles[i] = GCHandle.Alloc(provenance[i], GCHandleType.Pinned);
                capturedProvenancePtrs[i] = capturedProvenanceHandles[i].Value.AddrOfPinnedObject();
                capturedUncertatintyHandles[i] = GCHandle.Alloc(uncertatinties[i], GCHandleType.Pinned);
                capturedUncertatintyPtrs[i] = capturedUncertatintyHandles[i].Value.AddrOfPinnedObject();
                capturedValuesHandles[i] = GCHandle.Alloc(values[i], GCHandleType.Pinned);
                capturedValuesPtrs[i] = capturedValuesHandles[i].Value.AddrOfPinnedObject();
            }
            try
            {
                unsafe
                {
                    double* resU = (double*)capturedResultUncertaintyPtr;
                    double* resV = (double*)capturedResultValuesPtr;
                    ushort* resP = (ushort*)capturedResultProvenancePtr;

                    double*[] v = new double*[resultsToMerge];
                    double*[] u = new double*[resultsToMerge];
                    ushort*[] p = new ushort*[resultsToMerge];
                    for (int i = 0; i < resultsToMerge; i++)
                    {
                        v[i] = (double*)capturedValuesPtrs[i];
                        u[i] = (double*)capturedUncertatintyPtrs[i];
                        p[i] = (ushort*)capturedProvenancePtrs[i];
                    }

                    int minIndex;
                    double minValue;
                    for (int i = 0; i < totalElementCount; i++)
                    {
                        minIndex = 0; minValue = u[0][i];
                        for (int j = 1; j < resultsToMerge; j++)
                            if ((u[j][i] < minValue || double.IsNaN(minValue)) && (!double.IsNaN(v[j][i])))
                            {
                                minValue = u[j][i];
                                minIndex = j;
                            }
                        resU[i] = minValue;
                        resP[i] = p[minIndex][i];
                        resV[i] = v[minIndex][i];
                    }
                }
            }
            finally
            {
                //releasing pointers
                for (int i = 0; i < resultsToMerge; i++)
                {
                    capturedProvenanceHandles[i].Value.Free();
                    capturedUncertatintyHandles[i].Value.Free();
                    capturedValuesHandles[i].Value.Free();
                }

                capturedResultValuesHandle.Value.Free();
                capturedResultUncertaintyHandle.Value.Free();
                capturedResultProvenanceHandle.Value.Free();
            }
            return results;
        }


        private static void ReplaceArrayElements(Array targetArray, Dictionary<ushort, ushort> replacementMap)
        {
            int count = targetArray.Length;
            GCHandle? capturedHandle = GCHandle.Alloc(targetArray, GCHandleType.Pinned);
            IntPtr ptr = capturedHandle.Value.AddrOfPinnedObject();
            try
            {
                unsafe
                {
                    ushort* p = (ushort*)ptr;
                    ushort val;
                    for (int i = 0; i < count; i++)
                        if (replacementMap.TryGetValue(p[i], out val))
                            p[i] = val;
                }
            }
            finally
            {
                capturedHandle.Value.Free();
            }
        }

        class MergedResults
        {
            public MergedResults(Array Provenance, Array Uncertatinty, Array Values)
            {
                this.Provenance = Provenance;
                this.Uncertatinty = Uncertatinty;
                this.Values = Values;
            }
            public Array Provenance { get; private set; }
            public Array Uncertatinty { get; private set; }
            public Array Values { get; private set; }
        }
    }
}
