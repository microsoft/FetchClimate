using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.Research.Science.FetchClimate2
{
    public class DataMaskAnalyzer
    {
        double latCoef, lonCoef;
        double latStart;
        double lonStart;
        Int32 latLen;
        Int32 lonLen;
        int bitFieldLineLen;
        byte[] bitField;
        //byte[] masks = { 1, 2, 4, 8, 16, 32, 64, 128 };
        byte[] masks = { 128, 64, 32, 16, 8, 4, 2, 1 };
        byte[] leftMasks = { 128, 192, 224, 240, 248, 252, 254, 255 };
        byte[] rightMasks = { 1, 3, 7, 15, 31, 63, 127, 255 };
        byte[] byteSums = {
                              0, // 0000 0000
                              1, // 0000 0001
                              1, // 0000 0010
                              2, // 0000 0011
                              1, // 0000 0100
                              2, // 0000 0101
                              2, // 0000 0110
                              3, // 0000 0111
                              1, // 0000 1000
                              2, // 0000 1001
                              2, // 0000 1010
                              3, // 0000 1011
                              2, // 0000 1100
                              3, // 0000 1101
                              3, // 0000 1110
                              4, // 0000 1111
                              1, // 0001 0000
                              2, // 0001 0001
                              2, // 0001 0010
                              3, // 0001 0011
                              2, // 0001 0100
                              3, // 0001 0101
                              3, // 0001 0110
                              4, // 0001 0111
                              2, // 0001 1000
                              3, // 0001 1001
                              3, // 0001 1010
                              4, // 0001 1011
                              3, // 0001 1100
                              4, // 0001 1101
                              4, // 0001 1110
                              5, // 0001 1111
                              1, // 0010 0000
                              2, // 0010 0001
                              2, // 0010 0010
                              3, // 0010 0011
                              2, // 0010 0100
                              3, // 0010 0101
                              3, // 0010 0110
                              4, // 0010 0111
                              2, // 0010 1000
                              3, // 0010 1001
                              3, // 0010 1010
                              4, // 0010 1011
                              3, // 0010 1100
                              4, // 0010 1101
                              4, // 0010 1110
                              5, // 0010 1111
                              2, // 0011 0000
                              3, // 0011 0001
                              3, // 0011 0010
                              4, // 0011 0011
                              3, // 0011 0100
                              4, // 0011 0101
                              4, // 0011 0110
                              5, // 0011 0111
                              3, // 0011 1000
                              4, // 0011 1001
                              4, // 0011 1010
                              5, // 0011 1011
                              4, // 0011 1100
                              5, // 0011 1101
                              5, // 0011 1110
                              6, // 0011 1111
                              1, // 0100 0000
                              2, // 0100 0001
                              2, // 0100 0010
                              3, // 0100 0011
                              2, // 0100 0100
                              3, // 0100 0101
                              3, // 0100 0110
                              4, // 0100 0111
                              2, // 0100 1000
                              3, // 0100 1001
                              3, // 0100 1010
                              4, // 0100 1011
                              3, // 0100 1100
                              4, // 0100 1101
                              4, // 0100 1110
                              5, // 0100 1111
                              2, // 0101 0000
                              3, // 0101 0001
                              3, // 0101 0010
                              4, // 0101 0011
                              3, // 0101 0100
                              4, // 0101 0101
                              4, // 0101 0110
                              5, // 0101 0111
                              3, // 0101 1000
                              4, // 0101 1001
                              4, // 0101 1010
                              5, // 0101 1011
                              4, // 0101 1100
                              5, // 0101 1101
                              5, // 0101 1110
                              6, // 0101 1111
                              2, // 0110 0000
                              3, // 0110 0001
                              3, // 0110 0010
                              4, // 0110 0011
                              3, // 0110 0100
                              4, // 0110 0101
                              4, // 0110 0110
                              5, // 0110 0111
                              3, // 0110 1000
                              4, // 0110 1001
                              4, // 0110 1010
                              5, // 0110 1011
                              4, // 0110 1100
                              5, // 0110 1101
                              5, // 0110 1110
                              6, // 0110 1111
                              3, // 0111 0000
                              4, // 0111 0001
                              4, // 0111 0010
                              5, // 0111 0011
                              4, // 0111 0100
                              5, // 0111 0101
                              5, // 0111 0110
                              6, // 0111 0111
                              4, // 0111 1000
                              5, // 0111 1001
                              5, // 0111 1010
                              6, // 0111 1011
                              5, // 0111 1100
                              6, // 0111 1101
                              6, // 0111 1110
                              7, // 0111 1111
                              1, // 1000 0000
                              2, // 1000 0001
                              2, // 1000 0010
                              3, // 1000 0011
                              2, // 1000 0100
                              3, // 1000 0101
                              3, // 1000 0110
                              4, // 1000 0111
                              2, // 1000 1000
                              3, // 1000 1001
                              3, // 1000 1010
                              4, // 1000 1011
                              3, // 1000 1100
                              4, // 1000 1101
                              4, // 1000 1110
                              5, // 1000 1111
                              2, // 1001 0000
                              3, // 1001 0001
                              3, // 1001 0010
                              4, // 1001 0011
                              3, // 1001 0100
                              4, // 1001 0101
                              4, // 1001 0110
                              5, // 1001 0111
                              3, // 1001 1000
                              4, // 1001 1001
                              4, // 1001 1010
                              5, // 1001 1011
                              4, // 1001 1100
                              5, // 1001 1101
                              5, // 1001 1110
                              6, // 1001 1111
                              2, // 1010 0000
                              3, // 1010 0001
                              3, // 1010 0010
                              4, // 1010 0011
                              3, // 1010 0100
                              4, // 1010 0101
                              4, // 1010 0110
                              5, // 1010 0111
                              3, // 1010 1000
                              4, // 1010 1001
                              4, // 1010 1010
                              5, // 1010 1011
                              4, // 1010 1100
                              5, // 1010 1101
                              5, // 1010 1110
                              6, // 1010 1111
                              3, // 1011 0000
                              4, // 1011 0001
                              4, // 1011 0010
                              5, // 1011 0011
                              4, // 1011 0100
                              5, // 1011 0101
                              5, // 1011 0110
                              6, // 1011 0111
                              4, // 1011 1000
                              5, // 1011 1001
                              5, // 1011 1010
                              6, // 1011 1011
                              5, // 1011 1100
                              6, // 1011 1101
                              6, // 1011 1110
                              7, // 1011 1111
                              2, // 1100 0000
                              3, // 1100 0001
                              3, // 1100 0010
                              4, // 1100 0011
                              3, // 1100 0100
                              4, // 1100 0101
                              4, // 1100 0110
                              5, // 1100 0111
                              3, // 1100 1000
                              4, // 1100 1001
                              4, // 1100 1010
                              5, // 1100 1011
                              4, // 1100 1100
                              5, // 1100 1101
                              5, // 1100 1110
                              6, // 1100 1111
                              3, // 1101 0000
                              4, // 1101 0001
                              4, // 1101 0010
                              5, // 1101 0011
                              4, // 1101 0100
                              5, // 1101 0101
                              5, // 1101 0110
                              6, // 1101 0111
                              4, // 1101 1000
                              5, // 1101 1001
                              5, // 1101 1010
                              6, // 1101 1011
                              5, // 1101 1100
                              6, // 1101 1101
                              6, // 1101 1110
                              7, // 1101 1111
                              3, // 1110 0000
                              4, // 1110 0001
                              4, // 1110 0010
                              5, // 1110 0011
                              4, // 1110 0100
                              5, // 1110 0101
                              5, // 1110 0110
                              6, // 1110 0111
                              4, // 1110 1000
                              5, // 1110 1001
                              5, // 1110 1010
                              6, // 1110 1011
                              5, // 1110 1100
                              6, // 1110 1101
                              6, // 1110 1110
                              7, // 1110 1111
                              4, // 1111 0000
                              5, // 1111 0001
                              5, // 1111 0010
                              6, // 1111 0011
                              5, // 1111 0100
                              6, // 1111 0101
                              6, // 1111 0110
                              7, // 1111 0111
                              5, // 1111 1000
                              6, // 1111 1001
                              6, // 1111 1010
                              7, // 1111 1011
                              6, // 1111 1100
                              7, // 1111 1101
                              7, // 1111 1110
                              8  // 1111 1111
                          };

        public DataMaskAnalyzer(Stream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream))
            {
                latLen = reader.ReadInt32();
                lonLen = reader.ReadInt32();
                latStart = reader.ReadDouble();
                latCoef = reader.ReadDouble();
                lonStart = reader.ReadDouble();
                lonCoef = reader.ReadDouble();

                bitFieldLineLen = ((lonLen + 7) / 8);
                int size = bitFieldLineLen * latLen;
                bitField = new byte[size];
                reader.Read(bitField, 0, size);
            }
        }

        public bool HasData(double lat, double lon)
        {
            int latIndex = (int)Math.Round((lat - latStart) / latCoef);
            int lonIndex = (int)Math.Round((lon - lonStart) / lonCoef);
            latIndex = Math.Min(Math.Max(latIndex, 0), latLen - 1);
            lonIndex = Math.Min(Math.Max(lonIndex, 0), lonLen - 1);
            byte shell = bitField[latIndex * bitFieldLineLen + (lonIndex / 8)];
            int position = lonIndex % 8;
            return (masks[position] & shell) == masks[position];
        }


        /// <summary>
        /// computes the percentage of the data points without missing values in the given rectangle
        /// </summary>
        /// <param name="latmin">Min latitude</param>
        /// <param name="latmax">Max latitude</param>
        /// <param name="lonmin">Min longitude</param>
        /// <param name="lonmax">Max longitude</param>
        /// <returns>value from 0.0 to 1.0</returns>
        public double GetDataPercentage(double latmin, double latmax, double lonmin, double lonmax)
        {
            if (latmin > latmax) throw new ArgumentException("latmin must be less than latmax");
            if (lonmin > lonmax) throw new ArgumentException("lonmin must be less than lonmax");

            if (latmin == latmax && lonmin == lonmax) // Special case - point requested
                return HasData(latmin, lonmin) ? 1.0 : 0.0;

            int latMinIndex = (int)Math.Round((latmin - latStart) / latCoef);
            int lonMinIndex = (int)Math.Round((lonmin - lonStart) / lonCoef);
            int latMaxIndex = (int)Math.Round((latmax - latStart) / latCoef);
            int lonMaxIndex = (int)Math.Round((lonmax - lonStart) / lonCoef);
            latMinIndex = Math.Min(Math.Max(latMinIndex, 0), latLen - 1);
            lonMinIndex = Math.Min(Math.Max(lonMinIndex, 0), lonLen - 1);
            latMaxIndex = Math.Min(Math.Max(latMaxIndex, 0), latLen - 1);
            lonMaxIndex = Math.Min(Math.Max(lonMaxIndex, 0), lonLen - 1);
            if (
                (latMaxIndex == 0 && latMinIndex == 0) || (lonMaxIndex == 0 && lonmin == 0) ||
                (latMaxIndex == latLen - 1 && latMinIndex == latLen - 1) || (lonMaxIndex == lonLen - 1 && lonmin == lonLen - 1))
            {
                //the specified region is out of the data
                return 0.0;
            }
            if (latMinIndex > latMaxIndex || lonMinIndex > lonMaxIndex) throw new ArgumentException("Bad arguments");
            int totalPoints = (latMaxIndex - latMinIndex + 1) * (lonMaxIndex - lonMinIndex + 1);
            int landPoints = 0;

            int leftShellColumn = lonMinIndex / 8;
            int rightShellColumn = lonMaxIndex / 8;
            if (leftShellColumn == rightShellColumn)
            {
                int leftDelta = lonMinIndex % 8;
                
                int pos = 7 - leftDelta;
                for (int i = latMinIndex; i <= latMaxIndex; ++i) landPoints += byteSums[bitField[i * bitFieldLineLen + leftShellColumn] & rightMasks[pos]];

                int rightDelta = lonMaxIndex % 8;
                if (7 != rightDelta)
                {
                    pos = 6 - rightDelta;// = 7 - rightDelta - 1
                    for (int i = latMinIndex; i <= latMaxIndex; ++i) landPoints -= byteSums[bitField[i * bitFieldLineLen + rightShellColumn] & rightMasks[pos]];
                }
            }
            else
            {
                int leftDelta = lonMinIndex % 8;
                if (leftDelta != 0)
                {
                    int pos = 7 - leftDelta;
                    for (int i = latMinIndex; i <= latMaxIndex; ++i) landPoints += byteSums[bitField[i * bitFieldLineLen + leftShellColumn] & rightMasks[pos]];
                    ++leftShellColumn;
                }
                int rightDelta = lonMaxIndex % 8;
                if (rightDelta != 7)
                {
                    int pos = rightDelta;
                    for (int i = latMinIndex; i <= latMaxIndex; ++i) landPoints += byteSums[bitField[i * bitFieldLineLen + rightShellColumn] & leftMasks[pos]];
                    --rightShellColumn;
                }
                for (int i = latMinIndex; i <= latMaxIndex; ++i)
                    for (int j = leftShellColumn; j <= rightShellColumn; ++j) landPoints += byteSums[bitField[i * bitFieldLineLen + j]];
            }
            return ((double)landPoints) / totalPoints;
        }


    }
}
