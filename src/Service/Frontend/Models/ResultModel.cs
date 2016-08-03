using Microsoft.Research.Science.FetchClimate2;
using System;
using System.Linq;

namespace Frontend.Models
{
    public enum RegionResultStatus
    {
        Pending,
        InProgress,
        Succeeded,
        PartiallySucceeded,
        Failed        
    }

    public class RegionResultModel
    {
        private readonly RegionResultStatus status;
        private readonly object parameter;
        private readonly bool isPointSet;

        public RegionResultModel(RegionResultStatus status, object parameter, bool isPointSet)
        {
            this.status = status;
            this.parameter = parameter;
            this.isPointSet = isPointSet;
        }

        public RegionResultStatus Status {
            get { return status; }
        }

        public object Parameter {
            get { return parameter; }
        }

        public bool IsPointSet {
            get { return isPointSet; }
        }
    }

    public class ResultModel
    {
        private readonly RegionResultModel[] regions;

        public ResultModel(RegionResultModel[] regions)
        {
            this.regions = regions;
        }

        public RegionResultModel[] Regions
        {
            get { return regions;  }
        }

        public bool IsFinished
        {
            get { return !regions.Any(r => r.Status == RegionResultStatus.InProgress || r.Status == RegionResultStatus.Pending);  }
        }
    }
}