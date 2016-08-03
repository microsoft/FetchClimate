using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Research.Science.Data;
using Microsoft.Research.Science.Data.Imperative;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace LandOceanMaskGenerationUtility
{
    class Program
    {
        const string defaultUri = @"msds:nc?file=C:\ClimateData\GTOPO30nc.nc&openMode=readOnly";

        static int Main(string[] args)
        {
            string uri;
            string path;
            string latVarName = "lat";
            string lonVarName = "lon";
            string dataVarName = "";

            if (args.Length == 5)
            {
                uri = args[0];                
                dataVarName = args[1];
                latVarName = args[2];
                lonVarName = args[3];
                path = args[4];
            }
            else
            {
                Console.WriteLine(@"Usage: DataMaskGenerationUtility <dataset uri> <data variable name> <lat variable name> <lon variable name> <output file path>");
                return 1;
            }

            try
            {
                DataSet d = DataSet.Open(uri);
                Array tmean;
                var dims = d[dataVarName].Dimensions;
                Int32 latLen = dims[latVarName].Length;
                Int32 lonLen = dims[lonVarName].Length;
                int latChunkSize = 268435456 / (Marshal.SizeOf(d.Variables[dataVarName].TypeOfData) * lonLen);//256 Mb
                object missingValue = d[dataVarName].MissingValue;
                if(missingValue == null)
                    missingValue = d[dataVarName].Metadata["missing_value"];

                int bitFieldLineLen = ((lonLen + 7) / 8);
                byte[] bitField = new byte[latLen * bitFieldLineLen];
                int startLat = 0;

                //Console.WriteLine(" Variables:");
                //foreach (Variable v in d.Variables)
                //{
                //    Console.WriteLine(v.ToString());
                //}

                int latDimNum=0, lonDimNum=1,timeDimNum=-1;
                int[] origin = new int[d.Variables[dataVarName].Rank], shape = new int[d.Variables[dataVarName].Rank];
                string latDim = d.Variables[latVarName].Dimensions[0].Name;
                string lonDim = d.Variables[lonVarName].Dimensions[0].Name;                             

                while (startLat < latLen - 1)
                {                    
                    int endLat = Math.Min(startLat + latChunkSize, latLen - 1);
                    Console.WriteLine(@"processing lats {0} - {1}",startLat,endLat);
                    for (int i = 0; i < d.Variables[dataVarName].Rank; i++)
                {
                    if (d.Variables[dataVarName].Dimensions[i].Name == latDim)
                    {
                        origin[i] = startLat;
                        shape[i] = endLat - startLat + 1;                        
                        latDimNum = i;
                    }
                    else if (d.Variables[dataVarName].Dimensions[i].Name == lonDim)
                    {
                        origin[i] = 0;
                        shape[i] = lonLen;                        
                        lonDimNum = i;
                    }
                    else
                    {
                        origin[i] = 0;
                        shape[i] = 1;
                        timeDimNum = i;
                    }
                }
                    tmean = d.Variables[dataVarName].GetData(origin,shape);                    

                    //System.Threading.Tasks.Parallel.For(startLat, endLat + 1, i =>
                    for (int i = startLat; i <= endLat; ++i)
                    {
                        int bitFieldLonCur = i * bitFieldLineLen;
                        byte accumulator = 0;
                        byte cursor = 128;
                        int[] indeces = new int[d.Variables[dataVarName].Rank];
                        if (timeDimNum>=0)
                            indeces[timeDimNum] = 0;
                        indeces[latDimNum] = i - startLat;
                        for (int j = 0; j < lonLen; ++j)
                        {
                            indeces[lonDimNum] = j;
                            if (!tmean.GetValue(indeces).Equals(missingValue))
                                accumulator += cursor;//1 - land, 0 - ocean                            
                            if (cursor == 1)
                            {
                                bitField[bitFieldLonCur] = accumulator;
                                ++bitFieldLonCur;
                                cursor = 128;
                                accumulator = 0;
                            }
                            else
                                cursor /= 2;
                        }
                        if (cursor != 128) bitField[bitFieldLonCur] = accumulator;//most unlikely to ever happen)
                    }//);
                    startLat = endLat + 1;
                }

                double firstLat = Convert.ToDouble(((Array)d.Variables[latVarName].GetData(new int[] { 0 }, new int[] { 1 })).GetValue(0));
                double firstLon = Convert.ToDouble(((Array)d.Variables[lonVarName].GetData(new int[] { 0 }, new int[] { 1 })).GetValue(0));
                double lastLat =  Convert.ToDouble(((Array)d.Variables[latVarName].GetData(new int[] { latLen - 1 }, new int[] { 1 })).GetValue(0));
                double lastLon = Convert.ToDouble(((Array)d.Variables[lonVarName].GetData(new int[] { lonLen - 1 }, new int[] { 1 })).GetValue(0));

                d.Dispose();

                Console.WriteLine("Compressing bit field");

                using (BinaryWriter writer = new BinaryWriter(new GZipStream(File.Create(path), CompressionMode.Compress, false)))
                {
                    writer.Write(latLen);
                    writer.Write(lonLen);
                    writer.Write(firstLat);
                    writer.Write((lastLat-firstLat)/(latLen-1));
                    writer.Write(firstLon);
                    writer.Write((lastLon - firstLon) / (lonLen - 1));
                    writer.Write(bitField);
                }
                Console.WriteLine("Done");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return 1;
            }

            return 0;
        }
    }
}
