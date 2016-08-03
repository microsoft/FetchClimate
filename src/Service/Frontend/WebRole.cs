using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.Research.Science.FetchClimate2;
using System.Diagnostics.Tracing;

namespace Frontend
{
    public class WebRole : RoleEntryPoint
    {
        public override bool OnStart()
        {
            FrontendRoleEvents.Log.TraceInformation(
                string.Format("Starting {0}", System.Reflection.Assembly.GetExecutingAssembly().FullName));
            RoleEnvironment.Changed += RoleEnvironment_Changed;


            return base.OnStart();
        }

        void RoleEnvironment_Changed(object sender, RoleEnvironmentChangedEventArgs e)
        {
            FrontendRoleEvents.Log.TraceInformation("Role configuration has been changed. Requesting instance recycle");
            RoleEnvironment.RequestRecycle();
        }

        public static void TraceInfo(string Message) { FrontendRoleEvents.Log.TraceInformation(Message); }

        sealed class FrontendRoleEvents : EventSource
        {
            public static FrontendRoleEvents Log = new FrontendRoleEvents();
            [Event(1,Level = EventLevel.Informational)]
            public void TraceInformation(string Message) { WriteEvent(1, Message); }
        }
    }
}
