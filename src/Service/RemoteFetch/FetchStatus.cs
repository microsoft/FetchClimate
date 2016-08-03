using Microsoft.Research.Science.Data;
using System;

namespace Microsoft.Research.Science.FetchClimate2
{
    /// <summary>Describe completion state of the request</summary>
    public enum FetchStatusCode
    {
        /// <summary>Request is waiting is the queue</summary>
        Pending,
        /// <summary>Request is being processed</summary>
        InProgress,
        /// <summary>Request is succesfully completed</summary>
        Completed,
        /// <summary>Request is failed</summary>
        Failed
    }

    /// <summary>Describe status of the request
    /// 
    /// </summary>
    public class FetchStatus
    {
        private readonly FetchStatusCode code;
        private readonly int positionOrPercent;
        private string data;

        internal FetchStatus(FetchStatusCode code, int p, string h)
        {
            this.code = code;
            positionOrPercent = p;
            data = h;
        }

        /// <summary>Constructs object describing 'Pending' status</summary>
        /// <param name="position">Position in queue</param>
        /// <param name="hash">Request hash code</param>
        /// <returns>Request status object</returns>
        public static FetchStatus Pending(int position, string hash)
        {
            return new FetchStatus(FetchStatusCode.Pending, position, hash);
        }

        /// <summary>Constructs object describing 'InProgress' status</summary>
        /// <param name="percent">Completion percent</param>
        /// <param name="hash">Request hash code</param>
        /// <returns>Request status object</returns>
        public static FetchStatus InProgress(int percent, string hash)
        {
            return new FetchStatus(FetchStatusCode.InProgress, percent, hash);
        }

        /// <summary>Constructs object describing 'Completed' status</summary>
        /// <param name="hash">Request hash code</param>
        /// <returns>Request status object</returns>
        public static FetchStatus Completed(string hash)
        {
            return new FetchStatus(FetchStatusCode.Completed, 0, hash);
        }

        /// <summary>Constructs object describing request with 'Failed' status</summary>
        /// <param name="error">Error message</param>
        /// <returns>Request status object</returns>
        public static FetchStatus Failed(string error)
        {
            return new FetchStatus(FetchStatusCode.Failed, 0, error);
        }

        /// <summary>Gets status code for the request</summary>
        public FetchStatusCode StatusCode
        {
            get { return code; }
        }

        /// <summary>Gets unique hash code for the request</summary>
        public string Hash
        {
            get 
            {
                if (code == FetchStatusCode.Failed)
                    throw new InvalidOperationException("Hash is not available for failed requests");
                if (code == FetchStatusCode.Completed)
                {
                    AzureBlobDataSetUri uri = new AzureBlobDataSetUri(data);
                    return uri.Blob;
                }
                else
                    return data; 
            }
        }

        /// <summary>Gets position in queue for the pending request</summary>
        public int PositionInQueue
        {
            get
            {
                if (code != FetchStatusCode.Pending)
                    throw new InvalidOperationException("PositionInQueue is available only for pending requests");
                return positionOrPercent;
            }
        }

        /// <summary>Gets completion percent for the requests in progress</summary>
        public int PercentComplete
        {
            get
            {
                if (code != FetchStatusCode.InProgress)
                    throw new InvalidOperationException("PercentComplete is available only for requests in progress");
                return positionOrPercent;
            }
        }

        /// <summary>Gets error message for failed requests</summary>
        public string ErrorMessage
        {
            get
            {
                if (code != FetchStatusCode.Failed)
                    throw new InvalidOperationException("Error message is available only for failed requests");
                return data;
            }
        }

        public override string ToString()
        {
            switch (code)
            {
                case FetchStatusCode.Pending:
                    return String.Format("Request {0} is {1} in queue", data, positionOrPercent);
                case FetchStatusCode.InProgress:
                    return string.Format("Request {0} is {1}% ready", data, positionOrPercent);
                case FetchStatusCode.Completed:
                    return string.Format("Request {0} completed", Hash);
                case FetchStatusCode.Failed:
                    return string.Format("Request failed: {0}", data);
                default:
                    return string.Format("Request has unknown status code");
            }
        }
    }
}