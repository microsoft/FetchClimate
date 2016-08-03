using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    /// <summary>
    /// Registers itself in a preconfigured set of trace listeners (preconfigred via static method RegisterTraceListener)
    /// </summary>
    public class AutoRegistratingTraceSource : TraceSource
    {
        private static readonly HashSet<TraceSource> traceSources = new HashSet<TraceSource>();
        private static readonly HashSet<TraceListener> traceListeners = new HashSet<TraceListener>();

        public static void RegisterTraceListener(TraceListener listener)
        {
            lock (traceSources)
            {
                traceListeners.Add(listener);
                foreach (var item in traceSources)
                    item.Listeners.Add(listener);
            }
        }

        private static void RegisterTraceSource(TraceSource traceSource)
        {
            lock (traceSources)
            {
                foreach (var item in traceListeners)
                    traceSource.Listeners.Add(item);
                traceSources.Add(traceSource);
            }
        }

        public AutoRegistratingTraceSource(string name, SourceLevels sourceLevels = SourceLevels.All)
            : base(name, sourceLevels)
        {
            RegisterTraceSource(this);
        }

        public void TraceVerbose(string fmt, params object[] args)
        {
            TraceEvent(TraceEventType.Verbose, 1, string.Format(fmt, args));
        }

        public void TraceInfo(string fmt, params object[] args)
        {
            TraceEvent(TraceEventType.Information, 2, string.Format(fmt, args));
        }

        public void TraceWarning(string fmt, params object[] args)
        {
            TraceEvent(TraceEventType.Warning, 3, string.Format(fmt, args));
        }

        public void TraceError(string fmt, params object[] args)
        {
            TraceEvent(TraceEventType.Error, 4, string.Format(fmt, args));
        }
    }
}
