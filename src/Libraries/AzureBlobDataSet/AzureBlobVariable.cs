using Microsoft.Research.Science.Data;
using Microsoft.WindowsAzure.StorageClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.Data
{
    //supported types: Int16, Int32, Single, double, 
    internal class AzureBlobVariable<DataType> : DataAccessVariable<DataType> where DataType : struct
    {
        CloudPageBlob blob;
        const int maxBlobChunk = 4096 * 1024;
        long offset;
        int[] _shape;
        int[] dimOffsets;//distances in blob (in bytes) between elements which indices differ by 1 and only in a given dimension
        int size;
        int rowLen;//in bytes, aligned
        int rowLenUnaligned;
        bool singleDimensional;

        internal AzureBlobVariable(DataSet dataSet, string name, string[] dims, CloudPageBlob blob, long offset, Dictionary<string, int> dimLengthDictionary, Dictionary<string, ObjectWithTypeString> metadata)
            : base(dataSet, name, dims)
        {
            this.blob = blob;
            this.offset = offset;
            this._shape = new int[dims.Length];
            for (int i = 0; i < dims.Length; ++i) _shape[i] = dimLengthDictionary[dims[i]];

            if (typeof(DataType) == typeof(double)) size = sizeof(double);
            else if (typeof(DataType) == typeof(Int32)) size = sizeof(Int32);
            else if (typeof(DataType) == typeof(UInt32)) this.size = sizeof(UInt32);
            else if (typeof(DataType) == typeof(Byte)) this.size = sizeof(Byte);
            else if (typeof(DataType) == typeof(SByte)) this.size = sizeof(SByte);
            else if (typeof(DataType) == typeof(Single)) this.size = sizeof(Single);
            else if (typeof(DataType) == typeof(Int16)) this.size = sizeof(Int16);
            else if (typeof(DataType) == typeof(UInt16)) this.size = sizeof(UInt16);
            else if (typeof(DataType) == typeof(DateTime)) this.size = sizeof(Int64);
            else throw new NotImplementedException("Type " + typeof(DataType).ToString() + " is not a valid AzureBlobDataSet variable type. Only Byte, SByte, Uint16, Int16, UInt32, Int32, DateTime, Single, and double are supported.");

            singleDimensional = (dims.Length == 1);
            if (singleDimensional)
            {
                rowLenUnaligned = size * _shape[0];
                rowLen = 512 * ((rowLenUnaligned + 511) / 512);
                this.dimOffsets = new int[1];
                dimOffsets[0] = size;
            }
            else
            {
                rowLen = size;
                for (int j = 1; j < dims.Length; ++j) rowLen *= _shape[j];
                rowLenUnaligned = rowLen;
                rowLen = 512 * ((rowLen + 511) / 512);

                this.dimOffsets = new int[dims.Length];
                dimOffsets[dims.Length - 1] = size;
                for (int i = dims.Length - 2; i > 0; --i) dimOffsets[i] = dimOffsets[i + 1] * _shape[i + 1];
                dimOffsets[0] = rowLen;
            }

            base.Initialize();

            if (metadata != null)
            {
                foreach (var j in metadata)
                {
                    if (j.Value.Value is Array)
                    {
                        Type objType = Type.GetType(j.Value.TypeString).GetElementType();
                        Array a = Array.CreateInstance(objType, ((Array)j.Value.Value).Length);
                        for (int i = 0; i < a.Length; ++i)
                        {
                            a.SetValue(SerializableDataSetSchema.CastMetadataValue(((Array)j.Value.Value).GetValue(i), objType), i);
                        }
                        this.Metadata[j.Key] = a;
                    }
                    else
                    {
                        Type objType = Type.GetType(j.Value.TypeString);
                        this.Metadata[j.Key] = SerializableDataSetSchema.CastMetadataValue(j.Value.Value, objType);
                    }
                }
            }
        }

        protected override Array ReadData(int[] origin, int[] shape)
        {
            int[] workOrigin, workShape;
            if (origin == null)
            {
                workOrigin = new int[_shape.Length];
                for (int i = 0; i < workOrigin.Length; ++i) workOrigin[i] = 0;
            }
            else
            {
                workOrigin = new int[origin.Length];
                for (int i = 0; i < workOrigin.Length; ++i) workOrigin[i] = origin[i];
            }
            if (shape == null)
            {
                workShape = new int[_shape.Length];
                for (int i = 0; i < workOrigin.Length; ++i) workShape[i] = _shape[i];
            }
            else
            {
                workShape = new int[shape.Length];
                for (int i = 0; i < workShape.Length; ++i) workShape[i] = shape[i];
            }
            if (workOrigin.Length != _shape.Length || workShape.Length != _shape.Length) throw new ArgumentException("Wrong number of dimensions.");
            if (workOrigin[0] + workShape[0] > _shape[0]) throw new ArgumentOutOfRangeException(@"Can't read more data than there exists.");

            const int maxReadBufferSize = 100 * 1024 * 1024;//(bytes)

            if (singleDimensional)
            {
                DataType[] result = new DataType[workShape[0]];

                int maxValsInBuffer = Math.Max(maxReadBufferSize / size, 1);
                int k = 0;
                int writePosShift = 0;
                byte[] buffer;
                var getTask = GetBytesFromBlobAsync(offset + (long)workOrigin[0] * (long)size, Math.Min(maxValsInBuffer, workShape[0]) * size);

                while (k < workShape[0])
                {
                    writePosShift = k;
                    buffer = getTask.Result;
                    k += maxValsInBuffer;
                    if (k < workShape[0]) getTask = GetBytesFromBlobAsync(offset + (long)(workOrigin[0] + k) * (long)size,
                        Math.Min(maxValsInBuffer, workShape[0] - k) * size);
                    MemoryStream temp = new MemoryStream(buffer);
                    using (BinaryReader br = new BinaryReader(temp))
                    {
                        for (int i = 0; i < workShape[0]; ++i)
                            if (typeof(DataType) == typeof(double)) result.SetValue(br.ReadDouble(), i + writePosShift);
                            else if (typeof(DataType) == typeof(Single)) result.SetValue(br.ReadSingle(), i + writePosShift);
                            else if (typeof(DataType) == typeof(Int16)) result.SetValue(br.ReadInt16(), i + writePosShift);
                            else if (typeof(DataType) == typeof(UInt16)) result.SetValue(br.ReadUInt16(), i + writePosShift);
                            else if (typeof(DataType) == typeof(UInt32)) result.SetValue(br.ReadUInt32(), i + writePosShift);
                            else if (typeof(DataType) == typeof(Byte)) result.SetValue(br.ReadByte(), i + writePosShift);
                            else if (typeof(DataType) == typeof(SByte)) result.SetValue(br.ReadSByte(), i + writePosShift);
                            else if (typeof(DataType) == typeof(DateTime)) result.SetValue(new DateTime(br.ReadInt64()), i + writePosShift);
                            else result.SetValue(br.ReadInt32(), i + writePosShift);
                    }
                }
                return result;
            }
            else
            {
                Array result = Array.CreateInstance(typeof(DataType), workShape);
                int[] resPos = new int[workShape.Length];
                for (int i = 0; i < resPos.Length; ++i) resPos[i] = 0;
                int lastLastIndex = -5;
                int lastIndexNo = resPos.Length - 1;

                int maxLinesInBuffer = maxReadBufferSize / rowLen;
                if (maxLinesInBuffer > 0)
                {
                    //we can dl at least 1 full slice by outer dimension into buffer
                    int totalLines = workShape[0];
                    byte[] buffer;
                    var getTask = GetBytesFromBlobAsync(offset + (long)workOrigin[0] * (long)rowLen, Math.Min(maxLinesInBuffer, totalLines) * rowLen);

                    int j = 0;
                    int originalOrigin0 = workOrigin[0];
                    int shift = 0;

                    while (j < totalLines)
                    {
                        workOrigin[0] = -j;
                        workShape[0] = Math.Min(maxLinesInBuffer, totalLines - j);
                        shift = -j;
                        buffer = getTask.Result;
                        j += maxLinesInBuffer;
                        if (j < totalLines) getTask = GetBytesFromBlobAsync(offset + (long)(originalOrigin0 + j) * (long)rowLen,
                            Math.Min(maxLinesInBuffer, totalLines - j) * rowLen);

                        MemoryStream temp = new MemoryStream(buffer);
                        temp.Position = 0;
                        using (BinaryReader br = new BinaryReader(temp))
                        {
                            do
                            {
                                if (resPos[lastIndexNo] != lastLastIndex + 1) br.BaseStream.Seek(GetZeroBasedOffset(Sum(workOrigin, resPos)), SeekOrigin.Begin);
                                lastLastIndex = resPos[lastIndexNo];
                                if (typeof(DataType) == typeof(double)) result.SetValue(br.ReadDouble(), resPos);
                                else if (typeof(DataType) == typeof(Single)) result.SetValue(br.ReadSingle(), resPos);
                                else if (typeof(DataType) == typeof(Int16)) result.SetValue(br.ReadInt16(), resPos);
                                else if (typeof(DataType) == typeof(UInt16)) result.SetValue(br.ReadUInt16(), resPos);
                                else if (typeof(DataType) == typeof(UInt32)) result.SetValue(br.ReadUInt32(), resPos);
                                else if (typeof(DataType) == typeof(Byte)) result.SetValue(br.ReadByte(), resPos);
                                else if (typeof(DataType) == typeof(SByte)) result.SetValue(br.ReadSByte(), resPos);
                                else if (typeof(DataType) == typeof(DateTime)) result.SetValue(new DateTime(br.ReadInt64()), resPos);
                                else result.SetValue(br.ReadInt32(), resPos);
                            }
                            while (Move(resPos, workShape, shift));
                        }
                    }
                }
                else
                {
                    //in case we can't
                    int totalLines = workShape[0];
                    byte[] buffer;
                    int maxValsInBuffer = Math.Max(maxReadBufferSize / size, 1);
                    int[] newOrigin = (int[])workOrigin.Clone();
                    int[] currentOrigin;
                    int[] currentEnd;
                    long curOffset = GetOffset(newOrigin);
                    int inLineOffset = (int)(curOffset - offset - newOrigin[0] * rowLen);
                    int inLineValsLeft = (rowLenUnaligned - inLineOffset) / size;
                    int fetchedValsNo = Math.Min(maxValsInBuffer, inLineValsLeft);
                    var getTask = GetBytesFromBlobAsync(curOffset, fetchedValsNo * size);

                    for (int i = 0; i < totalLines; ++i)
                    {
                        do
                        {
                            int[] tempResPos = (int[])resPos.Clone();
                            currentOrigin = newOrigin;
                            currentEnd = SingleDimensionalShift(currentOrigin, _shape, fetchedValsNo);
                            while (LessVector(Sum(workOrigin, tempResPos), currentEnd)) Move(tempResPos, workShape);

                            buffer = getTask.Result;

                            if (tempResPos[0] < totalLines)
                            {
                                newOrigin = Sum(workOrigin, tempResPos);
                                curOffset = GetOffset(newOrigin);
                                inLineOffset = (int)(curOffset - offset - newOrigin[0] * rowLen);
                                inLineValsLeft = (rowLenUnaligned - inLineOffset) / size;
                                fetchedValsNo = Math.Min(maxValsInBuffer, inLineValsLeft);
                                getTask = GetBytesFromBlobAsync(curOffset, fetchedValsNo * size);
                            }

                            MemoryStream temp = new MemoryStream(buffer);
                            temp.Position = 0;
                            using (BinaryReader br = new BinaryReader(temp))
                            {
                                do
                                {
                                    if (resPos[lastIndexNo] != lastLastIndex + 1) br.BaseStream.Seek(GetZeroBasedOffset(Difference(Sum(workOrigin, resPos), currentOrigin)), SeekOrigin.Begin);
                                    lastLastIndex = resPos[lastIndexNo];
                                    if (typeof(DataType) == typeof(double)) result.SetValue(br.ReadDouble(), resPos);
                                    else if (typeof(DataType) == typeof(Single)) result.SetValue(br.ReadSingle(), resPos);
                                    else if (typeof(DataType) == typeof(Int16)) result.SetValue(br.ReadInt16(), resPos);
                                    else if (typeof(DataType) == typeof(UInt16)) result.SetValue(br.ReadUInt16(), resPos);
                                    else if (typeof(DataType) == typeof(UInt32)) result.SetValue(br.ReadUInt32(), resPos);
                                    else if (typeof(DataType) == typeof(Byte)) result.SetValue(br.ReadByte(), resPos);
                                    else if (typeof(DataType) == typeof(SByte)) result.SetValue(br.ReadSByte(), resPos);
                                    else if (typeof(DataType) == typeof(DateTime)) result.SetValue(new DateTime(br.ReadInt64()), resPos);
                                    else result.SetValue(br.ReadInt32(), resPos);
                                    Move(resPos, workShape);
                                }
                                while (LessVector(Sum(workOrigin, resPos), currentEnd));
                            }
                        } while (resPos[0] == i);
                    }
                }
                return result;
            }
        }

        protected virtual byte[] GetBytesFromBlob(long offset, int count)
        {
            byte[] buffer = new byte[count];
            using (var stream = blob.OpenRead(new BlobRequestOptions() { Timeout = TimeSpan.FromMinutes(30.0) }))
            {
                int curOffset = 0;
                int curCount = count;
                int temp = 0;
                stream.Seek(offset, SeekOrigin.Begin);
                do
                {
                    temp = stream.Read(buffer, curOffset, count);
                    curOffset += temp;
                    curCount -= temp;
                }
                while (curOffset < count);
            }
            return buffer;
        }

        protected Task<byte[]> GetBytesFromBlobAsync(long offset, int count)
        {
            var res = new Task<byte[]>(() => GetBytesFromBlob(offset, count));
            res.Start();
            return res;
        }

        int[] Sum(int[] origin, int[] offset)
        {
            if (origin.Length != offset.Length) throw new ArgumentException("Different number of dimensions in summands.");
            int[] res = new int[origin.Length];
            for (int i = 0; i < res.Length; ++i) res[i] = origin[i] + offset[i];
            return res;
        }

        int[] Difference(int[] origin, int[] offset)
        {
            if (origin.Length != offset.Length) throw new ArgumentException("Different number of dimensions in summands.");
            int[] res = new int[origin.Length];
            for (int i = 0; i < res.Length; ++i) res[i] = origin[i] - offset[i];
            return res;
        }

        int[] SingleDimensionalShift(int[] origin, int[] shape, int shift)
        {
            if (origin.Length != shape.Length) throw new ArgumentException("Different number of dimensions in origin and shape.");
            int[] res = new int[origin.Length];
            int[] dimLengths = new int[origin.Length];
            dimLengths[origin.Length - 1] = 1;
            for (int i = origin.Length - 2; i >= 0; --i) dimLengths[i] = dimLengths[i + 1] * shape[i + 1];
            for (int i = 0; i < res.Length; ++i)
            {
                int curDimShift = shift / dimLengths[i];
                shift -= curDimShift * dimLengths[i];
                res[i] = origin[i] + curDimShift;
            }
            return res;
        }

        long GetOffset(int[] pos)
        {
            if (pos.Length != _shape.Length) throw new ArgumentException("Wrong number of dimensions.");
            long res = offset;
            for (int i = 0; i < pos.Length; ++i) res += pos[i] * dimOffsets[i];
            return res;
        }

        long GetZeroBasedOffset(int[] pos)
        {
            if (pos.Length != _shape.Length) throw new ArgumentException("Wrong number of dimensions.");
            long res = 0;
            for (int i = 0; i < pos.Length; ++i) res += pos[i] * dimOffsets[i];
            return res;
        }

        bool LessVector(int[] x, int[] y)
        {
            if (x.Length != y.Length) throw new ArgumentException("Different number of dimensions in operands.");
            int i = 0;
            while (i < x.Length && x[i] == y[i]) ++i;
            return i < x.Length && x[i] < y[i];
        }

        bool Move(int[] cur, int[] shape, int curOuterDimensionShift = 0)
        {
            for (int i = shape.Length - 1; i > 0; --i)
            {
                if (cur[i] < shape[i] - 1)
                {
                    ++cur[i];
                    return true;
                }
                else
                    cur[i] = 0;
            }
            ++cur[0];
            return cur[0] + curOuterDimensionShift < shape[0];
        }

        protected override void WriteData(int[] origin, Array data)
        {
            if (origin.Length != _shape.Length || data.Rank != _shape.Length) throw new ArgumentException("Wrong number of dimensions.");
            for (int i = 1; i < origin.Length; ++i) if (data.GetUpperBound(i) + 1 != _shape[i] || origin[i] != 0) throw new ArgumentException("Can only part data by its 1st dimension.");

            if (typeof(DataType) == typeof(DateTime))
            {
                int[] shapeTemp = new int[data.Rank];
                for (int i = 0; i < shapeTemp.Length; ++i) shapeTemp[i] = data.GetUpperBound(i) + 1;
                Array temp = Array.CreateInstance(typeof(Int64), shapeTemp);
                int[] resPos = new int[shapeTemp.Length];
                for (int i = 0; i < resPos.Length; ++i) resPos[i] = 0;
                do
                {
                    temp.SetValue(((DateTime)data.GetValue(resPos)).Ticks, resPos);
                }
                while (Move(resPos, shapeTemp));
                data = temp;
            }

            int byteLength = Buffer.ByteLength(data);
            int rowsNo = byteLength / rowLenUnaligned;

            if (origin[0] + rowsNo > _shape[0]) throw new ArgumentOutOfRangeException(@"Can't write more data than there is space reserved for.");

            if (singleDimensional)
            {
                //byte[] buffer = new byte[rowLen];
                //if (origin[0] != 0 || data.GetUpperBound(0) + 1 != _shape[0]) throw new ArgumentException(@"Can't part single-dimensional data");
                //Buffer.BlockCopy(data, 0, buffer, 0, rowLenUnaligned);
                //for (int j = 0; j < rowLen; j += maxBlobChunk)
                //    blob.WritePages(new MemoryStream(buffer, j, Math.Min(maxBlobChunk, rowLen - j)), offset + j);
                int datalen = data.GetUpperBound(0) + 1;
                int byteLen = datalen * this.size;
                long byteOrigin = origin[0] * this.size;
                if (byteOrigin % 512 != 0 || (origin[0] + datalen != _shape[0] && byteLen % 512 != 0))
                    throw new ArgumentException(@"Can write only full 512 byte pages of single-dimensional data starting at a 512 bytes aligned position.");
                int totalByteLen = 512 * ((datalen * this.size + 511) / 512);
                byte[] buffer = new byte[totalByteLen];
                Buffer.BlockCopy(data, 0, buffer, 0, byteLen);
                for (int j = 0; j < totalByteLen; j += maxBlobChunk)
                    blob.WritePages(new MemoryStream(buffer, j, Math.Min(maxBlobChunk, totalByteLen - j)), offset + byteOrigin + j);
            }
            else
            {
                int totalSize = rowLen * rowsNo;
                byte[] buffer = new byte[totalSize];
                for (int i = 0; i < rowsNo; ++i)
                {
                    Buffer.BlockCopy(data, i * rowLenUnaligned, buffer, i * rowLen, rowLenUnaligned);
                }
                for (int j = 0; j < totalSize; j += maxBlobChunk)
                    blob.WritePages(new MemoryStream(buffer, j, Math.Min(maxBlobChunk, totalSize - j)), offset + (long)origin[0] * (long)rowLen + (long)j);
            }
        }

        protected override int[] ReadShape()
        {
            return (int[])_shape.Clone();
        }
    }
}
