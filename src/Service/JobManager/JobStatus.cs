using System;

namespace Microsoft.Research.Science.FetchClimate2 
{
    public enum JobOrPartState
    {
        Pending = 0,
        InProgress,
        Completed,
        Failed
    };

    public class JobStatus
    {
        readonly JobOrPartState state;
        public JobOrPartState State { get { return state; } }

        readonly int pendingCount;
        public int PendingCount
        {
            get
            {
                if (state != JobOrPartState.Pending && state != JobOrPartState.InProgress)
                    throw new InvalidOperationException("Can't acess PendingCount of failed or completed job");
                return pendingCount;
            }
        }

        readonly int progressPercent;
        public int ProgressPercent
        {
            get
            {
                if (state != JobOrPartState.InProgress)
                    throw new InvalidOperationException("Can't access ProgressPercent of jobs that are not in-progress");
                return progressPercent;
            }
        }

        readonly string completeDataSetUri;
        public string CompleteDataSetUri
        {
            get
            {
                if (state != JobOrPartState.Completed)
                    throw new InvalidOperationException("Can't access CompleteDataSetUri of jobs that are not complete");
                return completeDataSetUri;
            }
        }

        readonly string errorMessage;
        public string ErrorMessage
        {
            get
            {
                if (state != JobOrPartState.Failed)
                    throw new InvalidOperationException("Can't access ErrorMessage of jobs that are not failed");
                return errorMessage;
            }
        }

        readonly string hash;
        public string Hash 
        { 
            get 
            { 
                if(state == JobOrPartState.Failed)
                    throw new InvalidOperationException("Can't access Hash of failed jobs");
                return hash; 
            } 
        }

        private JobStatus(JobOrPartState state, int pendingCount, int progressPercent, string completeDateSetUri, string hash, string errorMessage)
        {
            this.state = state;
            this.pendingCount = pendingCount;
            this.progressPercent = progressPercent;
            this.completeDataSetUri = completeDateSetUri;
            this.hash = hash;
            this.errorMessage = errorMessage;
        }

        public static JobStatus GetCompletedStatus(string dataSetUri)
        {
            return new JobStatus(JobOrPartState.Completed, 0, 100, dataSetUri, null, null);
        }

        public static JobStatus GetFailedStatus(string errorMessage)
        {
            return new JobStatus(JobOrPartState.Failed, 0, 0, null, null, errorMessage);
        }

        public static JobStatus GetPendingStatus(string hash, int pendingCount)
        {
            return new JobStatus(JobOrPartState.Pending, pendingCount, 0, null, hash, null);
        }

        public static JobStatus GetInProgressStatus(string hash, int progressPercent)
        {
            return new JobStatus(JobOrPartState.InProgress, 0, progressPercent, null, hash, null);
        }

        public override string ToString()
        {
            switch (State)
            {
                case JobOrPartState.Completed:
                    return string.Format("completed={0}", CompleteDataSetUri);
                case JobOrPartState.Failed:                    
                    return string.Format("fault={0}",ErrorMessage);
                case JobOrPartState.InProgress:
                    return string.Format("progress={0}%; hash={1}", ProgressPercent, hash);
                case JobOrPartState.Pending:
                    return string.Format("pending={0}; hash={1}", PendingCount, hash);
                default:
                    return string.Format("fault={0}", "Unexpected job status");
            }
        }
    }
}
