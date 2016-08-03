using System;

namespace Microsoft.Research.Science.FetchClimate2.Frontend
{
    public static class Constants
    {
        public const string FetchRequestContainer = "requests";
        
        public const string CompleteReply = "result={0}";
        public const string FaultReply = "fault={0}";
        public const string ProgressReply = "progress={0}%; key={1}";
        public const string PendingReply = "pending={0}; key={1}";
    }
}