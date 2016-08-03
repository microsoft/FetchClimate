using Microsoft.Research.Science.FetchClimate2.DataHandlers.ScatteredPoints.LinearCombination;
using Microsoft.Research.Science.FetchClimate2.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Utils
{
    /// <summary>
    /// Uses hash of lat and lon data to perform comparison
    /// </summary>
    public class HashBasedEquatibleRealValueNodesConverter : IEquatableConverter<RealValueNodes>
    {
        class NodesWithHash : RealValueNodes, IEquatable<RealValueNodes>
        {
            private readonly long hash;
            private readonly int hashCode;

            public NodesWithHash(double[] lats, double[] lons, double[] vals)
                : base(lats, lons, vals)
            {
                int N = lats.Length;
                int elemSize = sizeof(double);
                byte[] bytes = new byte[N * 3 * elemSize];
                for (int i = 0; i < N; i++)
                {
                    byte[] h1 = BitConverter.GetBytes(Lats[i]);
                    byte[] h2 = BitConverter.GetBytes(Lons[i]);
                    byte[] h3 = BitConverter.GetBytes(Values[i]);
                    Buffer.BlockCopy(h1, 0, bytes, elemSize * i * 3, elemSize);
                    Buffer.BlockCopy(h2, 0, bytes, elemSize * i * 3 + elemSize, elemSize);
                    Buffer.BlockCopy(h3, 0, bytes, elemSize * i * 3 + 2 * elemSize, elemSize);
                }

                hash = SHA1Hash.HashAsync(bytes).Result;
                hashCode = BitConverter.ToInt32(BitConverter.GetBytes(hash), 0);


            }

            public bool Equals(RealValueNodes other)
            {
                NodesWithHash nwh = other as NodesWithHash;
                if (nwh == null)
                    nwh = new NodesWithHash(other.Lats, other.Lons, other.Values);
                return nwh.hash == this.hash;

            }

            public override int GetHashCode()
            {
                return hashCode;
            }

            public override bool Equals(object obj)
            {
                RealValueNodes i = obj as RealValueNodes;
                if (i != null)
                    return this.Equals(i);
                else
                    return base.Equals(obj);
            }
        }

        public IEquatable<RealValueNodes> Covert(RealValueNodes obj)
        {
            NodesWithHash nwh = obj as NodesWithHash;
            if (nwh == null)
                nwh = new NodesWithHash(obj.Lats, obj.Lons, obj.Values);
            return nwh;
        }
    }

    public class HashBasedEquatibleINodesConverter : IEquatableConverter<INodes>
    {
        class NodesWithHash : INodes, IEquatable<INodes>
        {
            private double[] lons, lats;
            private readonly long hash;
            private readonly int hashCode;

            public NodesWithHash(double[] lats, double[] lons)
            {
                this.lats = lats;
                this.lons = lons;

                int N = lats.Length;
                int elemSize = sizeof(double);
                byte[] bytes = new byte[N * 3 * elemSize];
                for (int i = 0; i < N; i++)
                {
                    byte[] h1 = BitConverter.GetBytes(lats[i]);
                    byte[] h2 = BitConverter.GetBytes(lons[i]);
                    Buffer.BlockCopy(h1, 0, bytes, elemSize * i * 2, elemSize);
                    Buffer.BlockCopy(h2, 0, bytes, elemSize * i * 2 + elemSize, elemSize);
                }

                hash = SHA1Hash.HashAsync(bytes).Result;
                hashCode = BitConverter.ToInt32(BitConverter.GetBytes(hash), 0);
            }

            public bool Equals(INodes other)
            {
                NodesWithHash nwh = other as NodesWithHash;
                if (nwh == null)
                    nwh = new NodesWithHash(other.Lats, other.Lons);
                return nwh.hash == this.hash;

            }

            public override int GetHashCode()
            {
                return hashCode;
            }

            public override bool Equals(object obj)
            {
                INodes i = obj as INodes;
                if (i != null)
                    return this.Equals(i);
                else
                    return base.Equals(obj);
            }

            public double[] Lats
            {
                get { return lats; }
            }

            public double[] Lons
            {
                get { return lons; }
            }
        }

        public IEquatable<INodes> Covert(INodes obj)
        {
            NodesWithHash nwh = obj as NodesWithHash;
            if (nwh == null)
                nwh = new NodesWithHash(obj.Lats, obj.Lons);
            return nwh;
        }
    }
}
