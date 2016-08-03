using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Microsoft.Research.Science.FetchClimate2.DataHandlers
{
    class EvaluationResults
    {
        internal EvaluationResults(Array uncertainties, IFetchResponse[] dependentResults, Array precomputesValues)
        {
            Uncertainties = uncertainties;
            DependantsResults = dependentResults;
            PrecomputedValues = precomputesValues;
        }

        public Array Uncertainties { get; private set; }
        public IFetchResponse[] DependantsResults { get; private set; }
        public Array PrecomputedValues { get; private set; }
    }

    

    /// <summary>
    /// The class that can be inherited to define virtual variable specification. The class reflects its content and find suitable static method (double[] params -> double) that can be considered as virtual variable definition
    /// </summary>
    public class VirtualDataSource : DataSourceHandler
    {
        private const int MonteCarloCount = 40;

        /// <summary>
        /// For dependant variables
        /// </summary>
        class FetchRequestProxy : IFetchRequest
        {
            private readonly IFetchRequest component;
            private readonly string name;
            public FetchRequestProxy(string newVarName, IFetchRequest component)
            {
                this.component = component;
                this.name = newVarName;

            }
            public string EnvironmentVariableName
            {
                get { return this.name; }
            }

            public IFetchDomain Domain
            {
                get { return component.Domain; }
            }

            public string[] ParticularDataSource
            {
                get { return null; } //nor particular datasource
            }

            public DateTime ReproducibilityTimestamp
            {
                get { return component.ReproducibilityTimestamp; }
            }
        }

        enum UncertaintyType
        {
            Constant, CustomCS, CustomFS
        }

        static AutoRegistratingTraceSource ts = new AutoRegistratingTraceSource("VirtualDataSource", SourceLevels.All);

        Dictionary<string, string[]> variableDefinitionsFound = new Dictionary<string, string[]>();
        Dictionary<string, VirtualDataSource.UncertaintyType> variableUncertaintyTypes = new Dictionary<string, VirtualDataSource.UncertaintyType>();
        Dictionary<string, Func<object[], object>> virtualVariableFunctions = new Dictionary<string, Func<object[], object>>();        

        public VirtualDataSource()
            : base(null)
        {
            var dynamicType = GetType();
            //looking for available static methods
            MethodInfo[] methods = dynamicType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            foreach (var item in methods)
            {
                //if ((item.ReturnType != typeof(double)) || item.GetParameters().Any(p => p.ParameterType != typeof(double))) //filter out all except double[] params -> double
                //    continue;
                if ((item.ReturnType == typeof(double)) || item.GetParameters().All(p => p.ParameterType == typeof(double))) //found a variable with constant uncertainty
                {
                    variableDefinitionsFound.Add(item.Name, item.GetParameters().Select(p => p.Name).ToArray()); //saving variable and it's parameters names
                    variableUncertaintyTypes.Add(item.Name, UncertaintyType.Constant); //saving variable's uncertainty type
                    virtualVariableFunctions.Add(item.Name, parameters => item.Invoke(null, parameters)); //generating funcs for future invocation

                    ts.TraceEvent(TraceEventType.Information,1,string.Format("Found float virtual variable definition \"{0}\" of {1} parameters in \"{2}\" type of \"{3}\" assembly",
                        item.Name,
                        variableDefinitionsFound[item.Name].Length,
                        dynamicType.FullName,
                        Assembly.GetAssembly(dynamicType).FullName));                    
                }
                else if ((item.ReturnType == typeof(GaussValue)) || item.GetParameters().All(p => p.ParameterType == typeof(GaussValue))) //found a variable with custom uncertainty
                {
                    variableDefinitionsFound.Add(item.Name, item.GetParameters().Select(p => p.Name).ToArray()); //saving variable and it's parameters names
                    variableUncertaintyTypes.Add(item.Name, UncertaintyType.CustomCS); //saving variable's uncertainty type
                    virtualVariableFunctions.Add(item.Name, parameters => item.Invoke(null, parameters)); //generating funcs for future invocation

                    ts.TraceEvent(TraceEventType.Information,3,string.Format("Found Gaussian virtual variable (CSharp) definition \"{0}\" of {1} parameters in \"{2}\" type of \"{3}\" assembly",
                        item.Name,
                        variableDefinitionsFound[item.Name].Length,
                        dynamicType.FullName,
                        Assembly.GetAssembly(dynamicType).FullName));
                }
                else if ((item.ReturnType == typeof(Tuple<double, double>)) || item.GetParameters().All(p => p.ParameterType == typeof(Tuple<double, double>))) //found a variable with custom uncertainty
                {
                    variableDefinitionsFound.Add(item.Name, item.GetParameters().Select(p => p.Name).ToArray()); //saving variable and it's parameters names
                    variableUncertaintyTypes.Add(item.Name, UncertaintyType.CustomFS); //saving variable's uncertainty type
                    virtualVariableFunctions.Add(item.Name, parameters => item.Invoke(null, parameters)); //generating funcs for future invocation

                    ts.TraceEvent(TraceEventType.Information, 4, string.Format("Found Gaussian virtual variable (FSharp) definition \"{0}\" of {1} parameters in \"{2}\" type of \"{3}\" assembly",
                        item.Name,
                        variableDefinitionsFound[item.Name].Length,
                        dynamicType.FullName,
                        Assembly.GetAssembly(dynamicType).FullName));
                }
            }
        }

        // <summary>
        /// Implements the logic of sequential uncertainties evaluation, reporting it, acquiring data mask (a subset of the data which is actually needs to be processed), generating the mean values for points left
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override sealed async Task<Array> ProcessRequestAsync(IRequestContext context)
        {
            var uncertaintyEvaluationResults = await EvaluateAsync(context);
            //WARNING!!!
            //TODO: Implicit dependency here. The user of the ProcessRequestAsync EXPECTS that context.GetMaskAsync is called during the execution and waits for it. Separate the method!
            var mask = await context.GetMaskAsync(uncertaintyEvaluationResults.Uncertainties);
            return await AggregateAsync(context,uncertaintyEvaluationResults, mask);
        }

        private async Task<EvaluationResults> EvaluateAsync(IRequestContext context)
        {
            IFetchRequest request = context.Request;
            string requestedVar = context.Request.EnvironmentVariableName;
            
            //generating requests for dependant variables            
            string[] dependantVars;
            if (!variableDefinitionsFound.TryGetValue(requestedVar, out dependantVars))
                throw new InvalidOperationException(string.Format("The virtual data source was requested to handle \"{0}\" variable. But this variable definition was not found in the virtual data source code", requestedVar));
            IFetchRequest[] dependants = dependantVars.Select(var => new FetchRequestProxy(var, request)).ToArray();
            var dependantsDataTask = context.FetchDataAsync(dependants);

            var dependantsResults = (await dependantsDataTask).ToArray();            

            Array sd = Array.CreateInstance(typeof(double), request.Domain.GetDataArrayShape());

            int depCount = dependants.Length;

            GCHandle? capturedValuesHandle = null;
            GCHandle? capturedSdHandle = null;

            GCHandle?[] capturedDependantsValuesHandles = new GCHandle?[depCount];
            GCHandle?[] capturedDependantsSdHandles = new GCHandle?[depCount];

            try
            {
                capturedSdHandle = GCHandle.Alloc(sd, GCHandleType.Pinned);
                IntPtr sdPtr = capturedSdHandle.Value.AddrOfPinnedObject();


                IntPtr[] dependantsValuesPtrs = new IntPtr[depCount];
                IntPtr[] dependantsSdPtrs = new IntPtr[depCount];
                for (int i = 0; i < depCount; i++)
                {
                    capturedDependantsValuesHandles[i] = GCHandle.Alloc(dependantsResults[i].Values, GCHandleType.Pinned);
                    dependantsValuesPtrs[i] = capturedDependantsValuesHandles[i].Value.AddrOfPinnedObject();
                    capturedDependantsSdHandles[i] = GCHandle.Alloc(dependantsResults[i].Uncertainty, GCHandleType.Pinned);
                    dependantsSdPtrs[i] = capturedDependantsSdHandles[i].Value.AddrOfPinnedObject();
                }

                if (variableUncertaintyTypes[requestedVar] == UncertaintyType.Constant)
                {
                    //checking for NANs in dependent values or uncertainties                                        
                    int len = sd.Length;
                    Task[] evaluationTasks = new Task[len];
                    unsafe
                    {
                        double* doublePtr = (double*)sdPtr;
                        double*[] parametersValues = new double*[depCount];
                        double*[] parametersSd = new double*[depCount];
                        for (int i = 0; i < depCount; i++)
                        {
                            parametersValues[i] = (double*)dependantsValuesPtrs[i];
                            parametersSd[i] = (double*)dependantsSdPtrs[i];
                        }

                        for (int i = 0; i < len; i++)
                        {                            
                            evaluationTasks[i] = Task.Factory.StartNew(obj =>
                                {
                                    int idx = (int)obj;
                                    bool isNan = false;
                                    bool isMax = false;

                                    Random r = new Random(idx);

                                    object[][] dependedSimulation = new object[MonteCarloCount][];
                                    double[] simulatedSample = new double[MonteCarloCount];
                                    Angara.Distribution.Normal[] distributions = new Angara.Distribution.Normal[depCount];


                                    for (int j = 0; j < depCount; j++) //analyzing for NANs in dependent values. if there are any, put NAN in uncertainty
                                    {
                                        double pVal = parametersValues[j][idx], pSD = parametersSd[j][idx];
                                        if (double.IsNaN(pSD) || double.IsNaN(pVal))
                                        {
                                            isNan = true;
                                            break;
                                        }
                                        if (pSD == double.MaxValue)
                                        {
                                            isMax = true;
                                            continue;
                                        }

                                        distributions[j] = (Angara.Distribution.Normal)Angara.Distribution.NewNormal(pVal, pSD);
                                    }

                                    if (isNan)
                                        doublePtr[idx] = double.NaN;
                                    else if (isMax)
                                        doublePtr[idx] = double.MaxValue;
                                    else
                                    {
                                        Func<object[], object> virtVarFunc = virtualVariableFunctions[request.EnvironmentVariableName];

                                        for (int j = 0; j < MonteCarloCount; j++)
                                        {
                                            dependedSimulation[j] = new object[depCount];
                                            for (int k = 0; k < depCount; k++)
                                            {
                                                var sample = Angara.rng(distributions[k], r);
                                                if(Math.Abs(sample-distributions[k].Item1) >  3.0*distributions[k].Item2) //out of 3*sigma
                                                {
                                                    k--;
                                                    continue;
                                                }
                                                dependedSimulation[j][k] = sample;
                                            }
                                            simulatedSample[j] = (double)virtVarFunc(dependedSimulation[j]);
                                        }

                                        var summary = Angara.summary(simulatedSample);
                                        doublePtr[idx] = Math.Sqrt(summary.variance);
                                    }
                                },i);
                        }                        
                    }
                    await Task.WhenAll(evaluationTasks);

                    return new EvaluationResults(sd, dependantsResults,null);
                }
                else
                {
                    Array values = Array.CreateInstance(typeof(double), request.Domain.GetDataArrayShape());

                    capturedValuesHandle = GCHandle.Alloc(values, GCHandleType.Pinned);
                    IntPtr valuesPtr = capturedValuesHandle.Value.AddrOfPinnedObject();

                    //performing per element function invocation
                    int len = dependantsResults[0].Values.Length;
                    Func<object[], object> virtVarFunc = virtualVariableFunctions[request.EnvironmentVariableName];

                    unsafe
                    {
                        double* val = (double*)valuesPtr;
                        double* uncertainty = (double*)sdPtr;
                        double*[] parametersValues = new double*[depCount];
                        double*[] parametersSd = new double*[depCount];
                        for (int i = 0; i < depCount; i++)
                        {
                            parametersValues[i] = (double*)dependantsValuesPtrs[i];
                            parametersSd[i] = (double*)dependantsSdPtrs[i];
                        }

                        object[] parArray = new object[depCount];
                        for (int i = 0; i < len; i++)
                        {
                            if (variableUncertaintyTypes[requestedVar] == UncertaintyType.CustomCS)
                            {
                                for (int j = 0; j < depCount; j++)
                                    parArray[j] = new GaussValue(parametersValues[j][i], parametersSd[j][i]);
                                var temp = (GaussValue)virtVarFunc(parArray);
                                val[i] = temp.value;
                                uncertainty[i] = temp.sd;
                            }
                            else
                            {
                                for (int j = 0; j < depCount; j++)
                                    parArray[j] = new Tuple<double, double>(parametersValues[j][i], parametersSd[j][i]);
                                var temp = (Tuple<double, double>)virtVarFunc(parArray);
                                val[i] = temp.Item1;
                                uncertainty[i] = temp.Item2;
                            }
                        }
                    }
                    return new EvaluationResults(sd, dependantsResults, values);                    
                }
            }
            finally
            {
                for (int i = 0; i < depCount; i++)
                {
                    capturedDependantsValuesHandles[i].Value.Free();
                    capturedDependantsSdHandles[i].Value.Free();
                }
                if (capturedValuesHandle != null)
                    capturedValuesHandle.Value.Free();
                if (capturedSdHandle != null)
                    capturedSdHandle.Value.Free();
            }
        }

        private async Task<Array> AggregateAsync(IRequestContext context, EvaluationResults computationalContext, Array mask = null)
        {
            string requestedVar = context.Request.EnvironmentVariableName;
            var request = context.Request;

            if (variableUncertaintyTypes[requestedVar] == UncertaintyType.Constant)
            {
                IFetchResponse[] dependantsResults = computationalContext.DependantsResults;
                Array[] dependantsData = dependantsResults.Select(r => r.Values).ToArray();

                int depCount = dependantsResults.Length;

                Array result = Array.CreateInstance(typeof(double), request.Domain.GetDataArrayShape());

                //performing per element function invocation
                GCHandle? capturedResultHandle;
                capturedResultHandle = GCHandle.Alloc(result, GCHandleType.Pinned);
                IntPtr resultPtr = capturedResultHandle.Value.AddrOfPinnedObject();

                GCHandle?[] capturedDependantsHandles = new GCHandle?[depCount];
                IntPtr[] dependantsPtrs = new IntPtr[depCount];
                for (int i = 0; i < depCount; i++)
                {
                    capturedDependantsHandles[i] = GCHandle.Alloc(dependantsData[i], GCHandleType.Pinned);
                    dependantsPtrs[i] = capturedDependantsHandles[i].Value.AddrOfPinnedObject();
                }

                try
                {
                    int len = dependantsData[0].Length;
                    Func<object[], object> virtVarFunc = virtualVariableFunctions[request.EnvironmentVariableName];

                    unsafe
                    {
                        double* res = (double*)resultPtr;
                        double*[] parameters = new double*[depCount];
                        for (int i = 0; i < depCount; i++)
                            parameters[i] = (double*)dependantsPtrs[i];

                        object[] parArray = new object[depCount];
                        for (int i = 0; i < len; i++)
                        {
                            for (int j = 0; j < depCount; j++)
                                parArray[j] = parameters[j][i];

                            res[i] = (double)virtVarFunc(parArray);
                        }

                    }
                }
                finally
                {
                    for (int i = 0; i < depCount; i++)
                        capturedDependantsHandles[i].Value.Free();
                    capturedResultHandle.Value.Free();
                }

                return result;
            }
            else
            {
                return computationalContext.PrecomputedValues;
            }
        }
    }  

    /// <summary>
    /// This struct represents Gauss-distributed value, which cand be used to define a virtual variable with custom uncertainty.
    /// </summary>
    public struct GaussValue
    {
        public GaussValue(double value, double sd)
        {
            this.value = value;
            this.sd = sd;
        }

        public double value;
        public double sd;
    }    
}
