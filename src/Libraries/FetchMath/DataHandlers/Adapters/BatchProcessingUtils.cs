using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    public static class RequestToBatchAdapter
    {
        public static IEnumerable<IGeoCell> Stratch(IFetchRequest request, Array mask = null)
        {
            IEnumerable<IGeoCell> streched = null;

            switch (request.Domain.SpatialRegionType)
            {
                case SpatialRegionSpecification.Points:
                    streched = BatchProcessingUtils.StretchCells(request.Domain.Lats, request.Domain.Lats, request.Domain.Lons,request.Domain.Lons, request.Domain.TimeRegion, mask);
                    break;
                case SpatialRegionSpecification.Cells:
                    streched = BatchProcessingUtils.StretchCells(request.Domain.Lats, request.Domain.Lats2, request.Domain.Lons, request.Domain.Lons2, request.Domain.TimeRegion, mask);                    
                    break;
                case SpatialRegionSpecification.PointGrid:
                    streched = BatchProcessingUtils.StretchGrid(request.Domain.Lats, request.Domain.Lons, request.Domain.TimeRegion, mask);                    
                    break;
                case SpatialRegionSpecification.CellGrid:
                    streched = BatchProcessingUtils.StretchCellGrid(request.Domain.Lats, request.Domain.Lons, request.Domain.TimeRegion, mask);                    
                    break;
                default:
                    throw new NotImplementedException();
            }

            return streched;
        }

        public static Array Fold(double[] strached, IFetchRequest request, Array mask = null)
        {
            return BatchProcessingUtils.Fold(strached, request.Domain.GetDataArrayShape(), mask);
        }
    }

    /// <summary>
    /// Converts multidim arrays into stretched arrays and folds them back
    /// </summary>
    static class BatchProcessingUtils
    {                
        /// <summary>
        /// Stretches the point grid into the sequence of points
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lon"></param>
        /// <param name="region"></param>
        /// <param name="mask"></param>
        /// <returns></returns>
        public static IEnumerable<IGeoCell> StretchGrid(double[] lat, double[] lon, ITimeRegion region, Array mask = null)
        {
            //stretching to lon,lat,t
            int latLen = lat.Length;
            int lonLen = lon.Length;
            int times = region.SegmentsCount;

            ITimeSegment[] segments = region.GetSegments().ToArray();

            if (mask == null)
            {
                for (int i = 0; i < lonLen; i++)
                {
                    double lonV = lon[i];
                    for (int j = 0; j < latLen; j++)
                    {
                        double latV = lat[j];
                        for (int t = 0; t < times; t++)
                            yield return new GeoCell() { LatMin = latV, LatMax = latV, LonMin = lonV, LonMax = lonV, Time = segments[t] };
                    }
                }
            }
            else
            {

                GCHandle? maskHandle = GCHandle.Alloc(mask, GCHandleType.Pinned);
                IntPtr maskPtr = maskHandle.Value.AddrOfPinnedObject();

                try
                {
                    int lastTwoDimsLen = times * latLen;

                    for (int i = 0; i < lonLen; i++)
                    {
                        double lonV = lon[i];
                        for (int j = 0; j < latLen; j++)
                        {
                            double latV = lat[j];
                            for (int t = 0; t < times; t++)
                                if (checkMdBoolArray(maskPtr, t + j * times + i * lastTwoDimsLen))
                                    yield return new GeoCell { LatMin = latV, LatMax = latV, LonMin = lonV, LonMax = lonV, Time = segments[t] };
                        }
                    }
                }
                finally
                {
                    maskHandle.Value.Free();
                }
            }
        }

        /// <summary>
        /// Stretches the cell set definition (with optional timseries) into the GeoCellTuple sequence
        /// </summary>
        /// <param name="latmin"></param>
        /// <param name="latmax"></param>
        /// <param name="lonmin"></param>
        /// <param name="lonmax"></param>
        /// <param name="time"></param>
        /// <param name="mask"></param>
        /// <returns></returns>
        public static IEnumerable<IGeoCell> StretchCells(double[] latmin, double[] latmax, double[] lonmin, double[] lonmax, ITimeRegion time, Array mask = null)
        {
            int cellsLen = latmin.Length;
            int timeLen = time.SegmentsCount;

            ITimeSegment[] segments = time.GetSegments().ToArray();

            if (mask == null)
            {
                for (int i = 0; i < cellsLen; i++)
                {
                    double lonminV = lonmin[i];
                    double lonmaxV = lonmax[i];
                    double latminV = latmin[i];
                    double latmaxV = latmax[i];
                    for (int t = 0; t < timeLen; t++)
                        yield return new GeoCell { LatMin = latminV, LonMin = lonminV, LatMax = latmaxV, LonMax = lonmaxV, Time = segments[t] };
                }
            }
            else
            {
                GCHandle? maskHandle = GCHandle.Alloc(mask, GCHandleType.Pinned);
                IntPtr maskPtr = maskHandle.Value.AddrOfPinnedObject();
                try
                {
                    for (int i = 0; i < cellsLen; i++)
                    {
                        double lonminV = lonmin[i];
                        double lonmaxV = lonmax[i];
                        double latminV = latmin[i];
                        double latmaxV = latmax[i];
                        for (int t = 0; t < timeLen; t++)
                            if (checkMdBoolArray(maskPtr, t + timeLen * i))
                                yield return new GeoCell { LatMin = latminV, LonMin = lonminV, LatMax = latmaxV, LonMax = lonmaxV, Time = segments[t] };
                    }

                }
                finally
                {
                    maskHandle.Value.Free();
                }
            }
        }

        /// <summary>
        /// Folds the stretched double array into the multidim array
        /// </summary>
        /// <param name="stretched"></param>        
        /// <param name="mask">Boolean array marking the position of stretched values</param>
        /// <returns></returns>
        public static Array Fold(double[] stretched, int[] dimLengths, Array mask = null)
        {
            Array res = Array.CreateInstance(typeof(double), dimLengths);
            if (mask == null)
                Buffer.BlockCopy(stretched, 0, res, 0, sizeof(double) * stretched.Length);
            else
            {
                GCHandle? maskHandle = GCHandle.Alloc(mask, GCHandleType.Pinned);
                IntPtr maskPtr = maskHandle.Value.AddrOfPinnedObject();
                GCHandle? stratchedHandle = GCHandle.Alloc(stretched, GCHandleType.Pinned);
                IntPtr stretchedPtr = stratchedHandle.Value.AddrOfPinnedObject();
                GCHandle? resHandle = GCHandle.Alloc(res, GCHandleType.Pinned);
                IntPtr resPtr = resHandle.Value.AddrOfPinnedObject();
                try
                {
                    int len = mask.Length;
                    unsafe
                    {
                        double* r = (double*)resPtr;
                        double* s = (double*)stretchedPtr;
                        bool* m = (bool*)maskPtr;
                        int j = 0;
                        for (int i = 0; i < len; i++)
                            if (m[i])
                                r[i] = s[j++];
                    }

                }
                finally
                {
                    maskHandle.Value.Free();
                    stratchedHandle.Value.Free();
                    resHandle.Value.Free();
                }
            }
            return res;
        }


        /// <summary>
        /// Stretches the 3D cell cube into the sequence of GeoCellTuple
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lon"></param>
        /// <param name="region"></param>
        /// <param name="mask"></param>
        /// <returns></returns>
        public static IEnumerable<IGeoCell> StretchCellGrid(double[] lat, double[] lon, ITimeRegion region, Array mask = null)
        {
            int latLen = lat.Length;
            int lonLen = lon.Length;
            int timeLen = region.SegmentsCount;

            ITimeSegment[] segments = region.GetSegments().ToArray();

            if (mask == null)
            {
                for (int i = 0; i < lonLen - 1; i++)
                {
                    double lonminV = lon[i];
                    double lonmaxV = lon[i + 1];
                    for (int j = 0; j < latLen - 1; j++)
                    {
                        double latminV = lat[j];
                        double latmaxV = lat[j + 1];
                        for (int t = 0; t < timeLen; t++)
                            yield return new GeoCell { LatMin = latminV, LatMax = latmaxV, LonMin = lonminV, LonMax = lonmaxV, Time = segments[t] };
                    }
                }
            }
            else
            {
                GCHandle? maskHandle = GCHandle.Alloc(mask, GCHandleType.Pinned);
                IntPtr maskPtr = maskHandle.Value.AddrOfPinnedObject();
                try
                {
                    int firstDimLen = lonLen - 1;
                    int secondDimLen = latLen - 1;
                    int lastTwoDimsLen = timeLen * secondDimLen;
                    for (int i = 0; i < firstDimLen; i++)
                    {
                        double lonminV = lon[i];
                        double lonmaxV = lon[i + 1];

                        for (int j = 0; j < secondDimLen; j++)
                        {
                            double latminV = lat[j];
                            double latmaxV = lat[j + 1];
                            for (int t = 0; t < timeLen; t++)
                                if (checkMdBoolArray(maskPtr, i * lastTwoDimsLen + j * timeLen + t))
                                    yield return new GeoCell { LatMin = latminV, LatMax = latmaxV, LonMin = lonminV, LonMax = lonmaxV, Time = segments[t] };
                        }
                    }
                }
                finally
                {
                    maskHandle.Value.Free();
                }
            }
        }

        static bool checkMdBoolArray(IntPtr pinnedPtr, int stractchedIndex)
        {
            unsafe
            {
                bool* m = (bool*)pinnedPtr;
                return m[stractchedIndex];
            }
        }
    }
}
