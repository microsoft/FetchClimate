using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2
{
    public class IndexBoundingBox
    {
        public int first;
        public int last;

        /// <summary>
        /// Is true when bounding box doesn't contain any points
        /// </summary>
        public bool IsSingular
        {
            get
            {
                return first > last;
            }
        }

        public IndexBoundingBox()
        {
            first = int.MaxValue;
            last = int.MinValue;
        }

        public static IndexBoundingBox Singular
        {
            get {
                return new IndexBoundingBox();
            }
        }

        public IndexBoundingBox MemberwiseClone()
        {
            return (IndexBoundingBox)base.MemberwiseClone();
        }

        public static IndexBoundingBox Union(IndexBoundingBox fst, IndexBoundingBox snd)
        {
            return new IndexBoundingBox() { first = Math.Min(fst.first, snd.first), last = Math.Max(fst.last, snd.last) };
        }
    }
}
