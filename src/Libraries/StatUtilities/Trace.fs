module StatUtilsTracing

let TraceSwitch = System.Diagnostics.TraceSwitch("StatUtilsTracing","The output of the library mechanisms","Warning")

let private traceif(cond:bool,text:obj) = System.Diagnostics.Trace.WriteLineIf(cond,text)

let trace_error text = traceif(TraceSwitch.TraceError,text)
let trace_warn text = traceif(TraceSwitch.TraceWarning,text)
let trace_info text = traceif(TraceSwitch.TraceInfo,text)
let trace_verbose text = traceif(TraceSwitch.TraceVerbose,text)
