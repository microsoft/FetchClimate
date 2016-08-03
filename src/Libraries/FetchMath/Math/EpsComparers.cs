using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Research.Science.FetchClimate2
{
    public class FloatEpsComparer : IComparer<float>
    {
        public FloatEpsComparer(float eps = 0)
        {
            this.eps = eps;
        }

        float eps = 0;
        static FloatEpsComparer inst = new FloatEpsComparer();
        public static FloatEpsComparer Instance
        {
            get
            {
                return inst;
            }
        }
        public int Compare(float x, float y)
        {
            float effEps = (eps == 0) ? (Math.Abs(x * .000001f)) : (eps);
            float dif = x - y;
            if (dif - effEps > 0)
                return 1;
            else if (dif + effEps < 0)
                return -1;
            else
                return 0;
        }
    }

    public class DoubleEpsComparer : IComparer<double>
    {
        public DoubleEpsComparer(double eps = 0)
        {
            this.eps = eps;
        }

        double eps = 0;
        static DoubleEpsComparer inst = new DoubleEpsComparer();
        public static DoubleEpsComparer Instance
        {
            get
            {
                return inst;
            }
        }
        public int Compare(double x, double y)
        {            
            double dif = x - y;
            if (dif - eps > 0)
                return 1;
            else if (dif + eps < 0)
                return -1;
            else
                return 0;
        }
    }

}
