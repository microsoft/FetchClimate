using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System.Runtime.Serialization.Json;
using System.IO;


namespace Microsoft.Research.Science.Data
{
    [DataSetProviderName("ab")]
    [DataSetProviderUriType(typeof(AzureBlobDataSetUri))]
    public class AzureBlobDataSet : DataSet
    {
        const int maxBlobChunk = 4096 * 1024;
        CloudPageBlob blob;
        long[] varOffsets;
        Dictionary<string, int> dimLengthDictionary = new Dictionary<string, int>();
        Dictionary<string, int> datumSizeDictionary = new Dictionary<string, int>();
        bool _IsInitialized = false;
        bool _anonymous = false;

        public AzureBlobDataSet(string uri)
        {
            AzureBlobDataSetUri azureUri = null;
            if (DataSetUri.IsDataSetUri(uri))
                azureUri = new AzureBlobDataSetUri(uri);
            else
                azureUri = AzureBlobDataSetUri.ToUri(uri);

            this.uri = azureUri;

            CloudStorageAccount storageAccount;// = CloudStorageAccount.Parse(azureUri.ConnectionString);

            if (CloudStorageAccount.TryParse(azureUri.ConnectionString, out storageAccount))
            {
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                // Retrieve a reference to a container 
                CloudBlobContainer container = blobClient.GetContainerReference(azureUri.Container);

                blob = container.GetPageBlobReference(azureUri.Blob);
            }
            else
            {
                blob = new CloudPageBlob(@"http://" + azureUri.AccountName + @".blob.core.windows.net/" + azureUri.Container + @"/" + azureUri.Blob);
                _anonymous = true;
            }
            SerializableDataSetSchema info;
            Int32 schemeSize;

            using (BinaryReader br = new BinaryReader(blob.OpenRead()))
            {
                int curOffset = 0;
                int curCount = 512;
                int temp = 0;

                UTF8Encoding utf8 = new UTF8Encoding();
                byte[] buffer = new byte[512];
                do
                {
                    temp = br.BaseStream.Read(buffer, curOffset, curCount);
                    curOffset += temp;
                    curCount -= temp;
                } while (curOffset < 512);
                string sizeStr = utf8.GetString(buffer);
                schemeSize = Int32.Parse(sizeStr);
                br.BaseStream.Seek(512, SeekOrigin.Begin);
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(SerializableDataSetSchema));
                byte[] scheme = new byte[schemeSize];
                curOffset = 0;
                curCount = schemeSize;
                do
                {
                    temp = br.BaseStream.Read(scheme, curOffset, curCount);
                    curOffset += temp;
                    curCount -= temp;
                } while (curOffset < schemeSize);
                info = (SerializableDataSetSchema)serializer.ReadObject(new MemoryStream(scheme));
            }

            bool savedAutoCommitState = IsAutocommitEnabled;
            IsAutocommitEnabled = false;

            Initialize(schemeSize, info);

            Commit();

            if (_anonymous) this.SetCompleteReadOnly();

            IsAutocommitEnabled = savedAutoCommitState;

            _IsInitialized = true;



        }

        protected AzureBlobDataSet(string uri, int schemeSize, SerializableDataSetSchema info)
        {
            AzureBlobDataSetUri azureUri = null;
            if (DataSetUri.IsDataSetUri(uri))
                azureUri = new AzureBlobDataSetUri(uri);
            else
                azureUri = AzureBlobDataSetUri.ToUri(uri);

            this.uri = azureUri;

            CloudStorageAccount storageAccount;// = CloudStorageAccount.Parse(azureUri.ConnectionString);

            if (CloudStorageAccount.TryParse(azureUri.ConnectionString, out storageAccount))
            {
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                // Retrieve a reference to a container 
                CloudBlobContainer container = blobClient.GetContainerReference(azureUri.Container);

                blob = container.GetPageBlobReference(azureUri.Blob);
            }
            else
            {
                blob = new CloudPageBlob(@"http://" + azureUri.AccountName + @".blob.core.windows.net/" + azureUri.Container + @"/" + azureUri.Blob);
                _anonymous = true;
            }

            bool savedAutoCommitState = IsAutocommitEnabled;
            IsAutocommitEnabled = false;

            Initialize(schemeSize, info);

            Commit();

            if (_anonymous) this.SetCompleteReadOnly();

            IsAutocommitEnabled = savedAutoCommitState;

            _IsInitialized = true;
        }      
  
        private void Initialize(Int64 schemeSize, SerializableDataSetSchema info)
        {
            foreach (var i in info.Dimensions)
            {
                dimLengthDictionary.Add(i.Name, i.Length);
            }

            long offset = 512 + 512 * ((schemeSize + 511) / 512);
            varOffsets = new long[info.Variables.Length];
            for (int i = 0; i < info.Variables.Length; ++i)
            {
                varOffsets[i] = offset;
                datumSizeDictionary.Add(info.Variables[i].Name, info.Variables[i].ValueSize);
                if (info.Variables[i].Dimensions.Length == 1)
                {
                    offset += ((dimLengthDictionary[info.Variables[i].Dimensions[0]] * info.Variables[i].ValueSize + 511) / 512) * 512;
                }
                else
                {
                    int rowSize = 1;
                    for (int j = 1; j < info.Variables[i].Dimensions.Length; ++j) rowSize *= dimLengthDictionary[info.Variables[i].Dimensions[j]];
                    offset += (long)dimLengthDictionary[info.Variables[i].Dimensions[0]] * (long)((rowSize * info.Variables[i].ValueSize + 511) / 512) * 512;
                }
                if (info.Variables[i].Type == typeof(Int32))
                {
                    var variable = new AzureBlobVariable<Int32>(this, info.Variables[i].Name, info.Variables[i].Dimensions, blob, varOffsets[i], dimLengthDictionary, info.Variables[i].Metadata);
                    AddVariableToCollection(variable);
                }
                else if (info.Variables[i].Type == typeof(UInt32))
                {
                    var variable = new AzureBlobVariable<UInt32>(this, info.Variables[i].Name, info.Variables[i].Dimensions, blob, varOffsets[i], dimLengthDictionary, info.Variables[i].Metadata);
                    AddVariableToCollection(variable);
                }
                else if (info.Variables[i].Type == typeof(double))
                {
                    var variable = new AzureBlobVariable<double>(this, info.Variables[i].Name, info.Variables[i].Dimensions, blob, varOffsets[i], dimLengthDictionary, info.Variables[i].Metadata);
                    AddVariableToCollection(variable);
                }
                else if (info.Variables[i].Type == typeof(Single))
                {
                    var variable = new AzureBlobVariable<Single>(this, info.Variables[i].Name, info.Variables[i].Dimensions, blob, varOffsets[i], dimLengthDictionary, info.Variables[i].Metadata);
                    AddVariableToCollection(variable);
                }
                else if (info.Variables[i].Type == typeof(Int16))
                {
                    var variable = new AzureBlobVariable<Int16>(this, info.Variables[i].Name, info.Variables[i].Dimensions, blob, varOffsets[i], dimLengthDictionary, info.Variables[i].Metadata);
                    AddVariableToCollection(variable);
                }
                else if (info.Variables[i].Type == typeof(UInt16))
                {
                    var variable = new AzureBlobVariable<UInt16>(this, info.Variables[i].Name, info.Variables[i].Dimensions, blob, varOffsets[i], dimLengthDictionary, info.Variables[i].Metadata);
                    AddVariableToCollection(variable);
                }
                else if (info.Variables[i].Type == typeof(DateTime))
                {
                    var variable = new AzureBlobVariable<DateTime>(this, info.Variables[i].Name, info.Variables[i].Dimensions, blob, varOffsets[i], dimLengthDictionary, info.Variables[i].Metadata);
                    AddVariableToCollection(variable);
                }
                else if (info.Variables[i].Type == typeof(Byte))
                {
                    var variable = new AzureBlobVariable<Byte>(this, info.Variables[i].Name, info.Variables[i].Dimensions, blob, varOffsets[i], dimLengthDictionary, info.Variables[i].Metadata);
                    AddVariableToCollection(variable);
                }
                else if (info.Variables[i].Type == typeof(SByte))
                {
                    var variable = new AzureBlobVariable<SByte>(this, info.Variables[i].Name, info.Variables[i].Dimensions, blob, varOffsets[i], dimLengthDictionary, info.Variables[i].Metadata);
                    AddVariableToCollection(variable);
                }
                else throw new ArgumentException(@"DataSet scheme contains a variable of a type other than Byte, SByte, UInt16, Int16, UInt32, Int32, DateTime, Single, or double which isn't supported.");
            }

            if (info.Metadata != null)
            {
                foreach (var j in info.Metadata)
                {
                    if (j.Value.Value is Object[])
                    {
                        Type objType = Type.GetType(j.Value.TypeString).GetElementType();
                        Array a = Array.CreateInstance(objType, ((Object[])j.Value.Value).Length);
                        for (int i = 0; i < a.Length; ++i)
                        {
                            a.SetValue(SerializableDataSetSchema.CastMetadataValue(((Object[])j.Value.Value)[i], objType), i);
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

        public static AzureBlobDataSet ArrangeData(string uri, DataSet source, SerializableVariableSchema[] emptyVariables)
        {
            List<SerializableDimension> dimensions = new List<SerializableDimension>();
            foreach (var i in source.Dimensions)
            {
                dimensions.Add(new SerializableDimension(i.Name, i.Length));
            }

            List<SerializableVariableSchema> oldVars = source.Variables.Select<Variable, SerializableVariableSchema>(x => x.GetSchema().AsSerializable()).ToList();
            
            List<SerializableVariableSchema> vars = new List<SerializableVariableSchema>(oldVars);
            vars.AddRange(emptyVariables);

            SerializableDataSetSchema info = new SerializableDataSetSchema(dimensions.ToArray(), vars.ToArray(), source.Metadata.AsDictionary());

            Dictionary<string, int> dimLengthDictionary = new Dictionary<string, int>(dimensions.Count);
            foreach (var i in dimensions) dimLengthDictionary.Add(i.Name, i.Length);

            long estimatedBlobSize = 512;//only scheme size on 1st page

            long[] varOffsets = new long[vars.Count];

            AzureBlobDataSetUri azureUri = null;
            if (DataSetUri.IsDataSetUri(uri))
                azureUri = new AzureBlobDataSetUri(uri);
            else
                azureUri = AzureBlobDataSetUri.ToUri(uri);

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(azureUri.ConnectionString);

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve a reference to a container 
            CloudBlobContainer container = blobClient.GetContainerReference(azureUri.Container);

            container.CreateIfNotExist();
            container.SetPermissions(new BlobContainerPermissions
            {
                PublicAccess = BlobContainerPublicAccessType.Container
            });

            CloudPageBlob blob;
            int schemeSize;
            using (MemoryStream memStream = new MemoryStream())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(SerializableDataSetSchema));
                serializer.WriteObject(memStream, info);

                schemeSize = (int)memStream.Length;
                estimatedBlobSize += 512 * ((schemeSize + 511) / 512);//remembering the need to align data

                for (int i = 0; i < vars.Count; ++i)
                {
                    varOffsets[i] = estimatedBlobSize;
                    if (vars[i].Dimensions.Length == 1)
                    {
                        estimatedBlobSize += ((dimLengthDictionary[vars[i].Dimensions[0]] * vars[i].ValueSize + 511) / 512) * 512;
                    }
                    else
                    {
                        int rowSize = 1;
                        for (int j = 1; j < vars[i].Dimensions.Length; ++j) rowSize *= dimLengthDictionary[vars[i].Dimensions[j]];
                        estimatedBlobSize += dimLengthDictionary[vars[i].Dimensions[0]] * ((rowSize * vars[i].ValueSize + 511) / 512) * 512;
                    }
                }

                blob = container.GetPageBlobReference(azureUri.Blob);
                blob.DeleteIfExists(); // CRITICAL: some may interfere between calls
                blob.Create(estimatedBlobSize);

                //writing scheme size into the 1st page
                UTF8Encoding utf8 = new UTF8Encoding();
                using (MemoryStream sizeStream = new MemoryStream(new byte[512], true))
                {
                    byte[] sizeBuf = utf8.GetBytes(schemeSize.ToString());
                    sizeStream.Write(sizeBuf, 0, sizeBuf.Length);
                    sizeStream.Seek(0, SeekOrigin.Begin);
                    blob.WritePages(sizeStream, 0);
                }

                //writing scheme starting with 2nd page
                int sizeAligned = ((schemeSize + 511) / 512) * 512;
                byte[] scheme = new byte[sizeAligned];
                memStream.Seek(0, SeekOrigin.Begin);
                memStream.Read(scheme, 0, schemeSize);
                for (int i = 0; i < sizeAligned; i += maxBlobChunk)
                    blob.WritePages(new MemoryStream(scheme, i, Math.Min(maxBlobChunk, sizeAligned - i)), 512 + i);
            }

            //populating blob with values from source
            for (int i = 0; i < oldVars.Count; ++i)
            {
                if (oldVars[i].Dimensions.Length == 1)
                {
                    int len = dimLengthDictionary[oldVars[i].Dimensions[0]];
                    var data = source[oldVars[i].Name].GetData();
                    if (oldVars[i].Type == typeof(DateTime))
                    {
                        var temp = new Int64[data.Length];
                        for (int j = 0; j < temp.Length; ++j) temp[j] = ((DateTime)data.GetValue(j)).Ticks;
                        data = temp;
                    }
                    int bufferSize = 512 * ((len * oldVars[i].ValueSize + 511) / 512);
                    byte[] buffer = new byte[bufferSize];
                    Buffer.BlockCopy(data, 0, buffer, 0, len * oldVars[i].ValueSize);
                    for (int j = 0; j < bufferSize; j += maxBlobChunk)
                        blob.WritePages(new MemoryStream(buffer, j, Math.Min(maxBlobChunk, bufferSize - j)), varOffsets[i] + j);
                }
                else
                {
                    int outerDimLen = dimLengthDictionary[oldVars[i].Dimensions[0]];
                    int rowLen = vars[i].ValueSize;
                    for (int j = 1; j < vars[i].Dimensions.Length; ++j) rowLen *= dimLengthDictionary[vars[i].Dimensions[j]];
                    int rowLenUnaligned = rowLen;
                    rowLen = 512 * ((rowLen + 511) / 512);

                    int[] origin = new int[oldVars[i].Dimensions.Length];
                    for (int j = 0; j < origin.Length; ++j) origin[j] = 0;

                    int[] shape = new int[oldVars[i].Dimensions.Length];
                    shape[0] = 1;
                    for (int j = 1; j < origin.Length; ++j) shape[j] = dimLengthDictionary[oldVars[i].Dimensions[j]];

                    byte[] buffer = new byte[rowLen];

                    for (int j = 0; j < outerDimLen; ++j)
                    {
                        origin[0] = j;

                        Array data = source[oldVars[i].Name].GetData(origin, shape);

                        if (oldVars[i].Type == typeof(DateTime))
                        {
                            int[] shapeTemp = new int[data.Rank];
                            for (int k = 0; k < shapeTemp.Length; ++k) shapeTemp[k] = data.GetUpperBound(k) + 1;
                            Array temp = Array.CreateInstance(typeof(Int64), shapeTemp);
                            int[] resPos = new int[shapeTemp.Length];
                            for (int k = 0; k < resPos.Length; ++k) resPos[k] = 0;
                            do
                            {
                                temp.SetValue(((DateTime)data.GetValue(resPos)).Ticks, resPos);
                            }
                            while (Move(resPos, shapeTemp));
                            data = temp;
                        }

                        Buffer.BlockCopy(data, 0, buffer, 0, rowLenUnaligned);
                        for (int k = 0; k < rowLen; k += maxBlobChunk)
                            blob.WritePages(new MemoryStream(buffer, k, Math.Min(maxBlobChunk, rowLen - k)), varOffsets[i] + (long)rowLen * (long)j + (long)k);
                    }
                }
            }
            //blob is prepared: values are where they gotta be, trash is everwhere else!

            return new AzureBlobDataSet(uri, schemeSize, info);
        }

        public static AzureBlobDataSet CreateEmptySet(string uri, SerializableDataSetSchema schema)
        {
            SerializableDataSetSchema info = schema;
            List<SerializableDimension> dimensions = schema.Dimensions.ToList();
            List<SerializableVariableSchema> vars = schema.Variables.ToList();

            Dictionary<string, int> dimLengthDictionary = new Dictionary<string, int>(dimensions.Count);
            foreach (var i in dimensions) dimLengthDictionary.Add(i.Name, i.Length);

            long estimatedBlobSize = 512;//only scheme size on 1st page

            long[] varOffsets = new long[vars.Count];

            AzureBlobDataSetUri azureUri = null;
            if (DataSetUri.IsDataSetUri(uri))
                azureUri = new AzureBlobDataSetUri(uri);
            else
                azureUri = AzureBlobDataSetUri.ToUri(uri);

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(azureUri.ConnectionString);

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve a reference to a container 
            CloudBlobContainer container = blobClient.GetContainerReference(azureUri.Container);

            container.CreateIfNotExist();

            CloudPageBlob blob = container.GetPageBlobReference(azureUri.Blob);

            blob.DeleteIfExists();

            int schemeSize;
            using (MemoryStream memStream = new MemoryStream())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(SerializableDataSetSchema));
                serializer.WriteObject(memStream, info);

                schemeSize = (int)memStream.Length;
                estimatedBlobSize += 512 * ((schemeSize + 511) / 512);//remembering the need to align data

                for (int i = 0; i < vars.Count; ++i)
                {
                    varOffsets[i] = estimatedBlobSize;
                    if (vars[i].Dimensions.Length == 1)
                    {
                        estimatedBlobSize += ((dimLengthDictionary[vars[i].Dimensions[0]] * vars[i].ValueSize + 511) / 512) * 512;
                    }
                    else
                    {
                        int rowSize = 1;
                        for (int j = 1; j < vars[i].Dimensions.Length; ++j) rowSize *= dimLengthDictionary[vars[i].Dimensions[j]];
                        estimatedBlobSize += (long)dimLengthDictionary[vars[i].Dimensions[0]] *(long)( ((rowSize * vars[i].ValueSize + 511) / 512) * 512);
                    }
                }

                blob.Create(estimatedBlobSize);

                //writing scheme size into the 1st page
                UTF8Encoding utf8 = new UTF8Encoding();
                using (MemoryStream sizeStream = new MemoryStream(new byte[512], true))
                {
                    byte[] sizeBuf = utf8.GetBytes(schemeSize.ToString());
                    sizeStream.Write(sizeBuf, 0, sizeBuf.Length);
                    sizeStream.Seek(0, SeekOrigin.Begin);
                    //blob.WritePages(sizeStream, 0);

                    //writing scheme starting with 2nd page
                    int sizeAligned = ((schemeSize + 511) / 512) * 512 + 512;
                    byte[] scheme = new byte[sizeAligned];
                    sizeStream.Seek(0, SeekOrigin.Begin);
                    sizeStream.Read(scheme, 0, 512);
                    memStream.Seek(0, SeekOrigin.Begin);
                    memStream.Read(scheme, 512, schemeSize);
                    for (int i = 0; i < sizeAligned; i += maxBlobChunk)
                        blob.WritePages(new MemoryStream(scheme, i, Math.Min(maxBlobChunk, sizeAligned - i)), i);
                }
            }

            return new AzureBlobDataSet(uri, schemeSize, info);
        }


        public static AzureBlobDataSet CreateSetWithSmallData(string uri, SerializableDataSetSchema schema, IDictionary<string, Array> dataToPut)
        {
            SerializableDataSetSchema info = schema;
            List<SerializableDimension> dimensions = schema.Dimensions.ToList();
            List<SerializableVariableSchema> varsUnsorted = schema.Variables.ToList();
            List<SerializableVariableSchema> vars = new List<SerializableVariableSchema>(varsUnsorted.Count);
            //vars for which data is provided should go first
            int varsWithDataCount = 0;
            foreach (var v in varsUnsorted)
            {
                if (dataToPut.ContainsKey(v.Name))
                {
                    vars.Add(v);
                    ++varsWithDataCount;
                }
            }
            foreach (var v in varsUnsorted)
                if (!dataToPut.ContainsKey(v.Name)) vars.Add(v);

            Dictionary<string, int> dimLengthDictionary = new Dictionary<string, int>(dimensions.Count);
            foreach (var i in dimensions)
            {
                dimLengthDictionary.Add(i.Name, i.Length);
                //System.Diagnostics.Trace.WriteLine(string.Format("ABDS: dimension added {0}[{1}]", i.Name, i.Length));
            }

            long estimatedBlobSize = 512;//only scheme size on 1st page

            long[] varOffsets = new long[vars.Count];

            AzureBlobDataSetUri azureUri = null;
            if (DataSetUri.IsDataSetUri(uri))
                azureUri = new AzureBlobDataSetUri(uri);
            else
                azureUri = AzureBlobDataSetUri.ToUri(uri);

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(azureUri.ConnectionString);

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve a reference to a container 
            CloudBlobContainer container = blobClient.GetContainerReference(azureUri.Container);

            container.CreateIfNotExist();

            CloudPageBlob blob = container.GetPageBlobReference(azureUri.Blob);

            blob.DeleteIfExists();
            int schemeSize;
            using (MemoryStream bufferStream = new MemoryStream())
            {
                using (MemoryStream memStream = new MemoryStream())
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(SerializableDataSetSchema));
                    serializer.WriteObject(memStream, info);

                    schemeSize = (int)memStream.Length;
                    estimatedBlobSize += 512 * ((schemeSize + 511) / 512);//remembering the need to align data

                    for (int i = 0; i < vars.Count; ++i)
                    {
                        varOffsets[i] = estimatedBlobSize;
                        if (vars[i].Dimensions.Length == 1)
                        {
                            //System.Diagnostics.Trace.WriteLine(string.Format("ABDS: looking for dim \"{0}\" for var \"{1}\"", vars[i].Dimensions[0], vars[i].Name));
                            estimatedBlobSize += ((dimLengthDictionary[vars[i].Dimensions[0]] * vars[i].ValueSize + 511) / 512) * 512;
                        }
                        else
                        {
                            int rowSize = 1;
                            for (int j = 1; j < vars[i].Dimensions.Length; ++j)
                            {
                                //System.Diagnostics.Trace.WriteLine(string.Format("ABDS: looking for dim \"{0}\" for var \"{1}\"", vars[i].Dimensions[j], vars[i].Name));
                                rowSize *= dimLengthDictionary[vars[i].Dimensions[j]];
                            }
                            //System.Diagnostics.Trace.WriteLine(string.Format("ABDS: looking for dim \"{0}\" for var \"{1}\"", vars[i].Dimensions[0], vars[i].Name));
                            estimatedBlobSize += (long)dimLengthDictionary[vars[i].Dimensions[0]] * (long)(((rowSize * vars[i].ValueSize + 511) / 512) * 512);
                        }
                    }

                    blob.Create(estimatedBlobSize);

                    //writing scheme size into the 1st page
                    UTF8Encoding utf8 = new UTF8Encoding();
                    using (MemoryStream sizeStream = new MemoryStream(new byte[512], true))
                    {
                        byte[] sizeBuf = utf8.GetBytes(schemeSize.ToString());
                        sizeStream.Write(sizeBuf, 0, sizeBuf.Length);
                        sizeStream.Seek(0, SeekOrigin.Begin);
                        //blob.WritePages(sizeStream, 0);

                        //writing scheme starting with 2nd page
                        int sizeAligned = ((schemeSize + 511) / 512) * 512 + 512;
                        byte[] scheme = new byte[sizeAligned];
                        sizeStream.Seek(0, SeekOrigin.Begin);
                        sizeStream.Read(scheme, 0, 512);
                        memStream.Seek(0, SeekOrigin.Begin);
                        memStream.Read(scheme, 512, schemeSize);
                        bufferStream.Write(scheme, 0, sizeAligned);
                        //for (int i = 0; i < sizeAligned; i += maxBlobChunk)
                        //    blob.WritePages(new MemoryStream(scheme, i, Math.Min(maxBlobChunk, sizeAligned - i)), i);
                    }
                }

                for (int i = 0; i < varsWithDataCount; ++i)
                {
                    if (vars[i].Dimensions.Length == 1)
                    {
                        int len = dimLengthDictionary[vars[i].Dimensions[0]];
                        var data = dataToPut[vars[i].Name];
                        if (vars[i].Type == typeof(DateTime))
                        {
                            var temp = new Int64[data.Length];
                            for (int j = 0; j < temp.Length; ++j) temp[j] = ((DateTime)data.GetValue(j)).Ticks;
                            data = temp;
                        }
                        int bufferSize = 512 * ((len * vars[i].ValueSize + 511) / 512);
                        byte[] buffer = new byte[bufferSize];
                        Buffer.BlockCopy(data, 0, buffer, 0, len * vars[i].ValueSize);
                        bufferStream.Write(buffer, 0, bufferSize);
                        //for (int j = 0; j < bufferSize; j += maxBlobChunk)
                        //    blob.WritePages(new MemoryStream(buffer, j, Math.Min(maxBlobChunk, bufferSize - j)), varOffsets[i] + j);
                    }
                    else
                    {
                        int outerDimLen = dimLengthDictionary[vars[i].Dimensions[0]];
                        int rowLen = vars[i].ValueSize;
                        for (int j = 1; j < vars[i].Dimensions.Length; ++j) rowLen *= dimLengthDictionary[vars[i].Dimensions[j]];
                        int rowLenUnaligned = rowLen;
                        rowLen = 512 * ((rowLen + 511) / 512);

                        byte[] buffer = new byte[rowLen];
                        Array data = dataToPut[vars[i].Name];
                        if (vars[i].Type == typeof(DateTime))
                        {
                            int[] shapeTemp = new int[data.Rank];
                            for (int k = 0; k < shapeTemp.Length; ++k) shapeTemp[k] = data.GetUpperBound(k) + 1;
                            Array temp = Array.CreateInstance(typeof(Int64), shapeTemp);
                            int[] resPos = new int[shapeTemp.Length];
                            for (int k = 0; k < resPos.Length; ++k) resPos[k] = 0;
                            do
                            {
                                temp.SetValue(((DateTime)data.GetValue(resPos)).Ticks, resPos);
                            }
                            while (Move(resPos, shapeTemp));
                            data = temp;
                        }

                        for (int j = 0; j < outerDimLen; ++j)
                        {
                            Buffer.BlockCopy(data, j * rowLenUnaligned, buffer, 0, rowLenUnaligned);
                            bufferStream.Write(buffer, 0, rowLen);
                        }
                    }
                }
                int bufferStreamSize = (int)bufferStream.Length;
                int bufferStreamSizeAligned = ((bufferStreamSize + 511) / 512) * 512;
                byte[] bufferAligned = new byte[bufferStreamSizeAligned + 512];
                bufferStream.Seek(0, SeekOrigin.Begin);
                bufferStream.Read(bufferAligned, 0, bufferStreamSize);
                for (int i = 0; i < bufferStreamSizeAligned; i += maxBlobChunk)
                    blob.WritePages(new MemoryStream(bufferAligned, i, Math.Min(maxBlobChunk, bufferStreamSizeAligned - i)), i);
            }

            return new AzureBlobDataSet(uri, schemeSize, info);
        }

        protected override Variable<DataType> CreateVariable<DataType>(string varName, string[] dims)
        {
            throw new InvalidOperationException("Cannot create new variable in this DataSet");
        }

        protected override void OnPrecommit(DataSet.Changes changes)
        {
            if (!_IsInitialized) return;

            DataSetChangeset changeSet = changes.GetChangeset();

            if (changeSet.AddedVariables != null && changeSet.AddedVariables.Length != 0) throw new DataSetException(@"Addition of new variables isn't supported.");
            foreach (var i in changeSet.UpdatedVariables)
            {
                if (i.ChangeType == Variable.ChangeTypes.MetadataUpdated) throw new DataSetException(@"Metadata modification isn't supported.");
                var _shape = this.variables[i.InitialSchema.Name].GetShape();
                if (_shape.Length != i.Shape.Length) throw new DataSetException(@"Shape modification isn't supported.");
                for (int j = 0; j < _shape.Length; ++j) if (_shape[j] != i.Shape[j]) throw new DataSetException(@"Shape modification isn't supported.");
                var origin = i.AffectedRectangle.Origin;
                var shape = i.AffectedRectangle.Shape;
                if (origin.Length != _shape.Length || shape.Length != _shape.Length) throw new DataSetException("Wrong number of dimensions.");
                for (int j = 1; j < shape.Length; ++j) if (_shape[j] != shape[j] || origin[j] != 0) throw new DataSetException("Can only part data by its 1st dimension.");
                if (origin[0] + shape[0] > _shape[0]) throw new DataSetException(@"Can't write more data than there is space reserved for.");
                if (this.variables[i.InitialSchema.Name].Dimensions.Count == 1)
                {
                    //(origin[0] != 0 || shape[0] != _shape[0])) throw new DataSetException(@"Can't part single-dimensional data");
                    int datalen = shape[0];
                    int datumSize = datumSizeDictionary[i.InitialSchema.Name];
                    int byteLen = datalen * datumSize;
                    long byteOrigin = origin[0] * datumSize;
                    if (byteOrigin % 512 != 0 || (origin[0] + datalen != _shape[0] && byteLen % 512 != 0))
                        throw new ArgumentException(@"Can write only full 512 byte pages of single-dimensional data starting at a 512 bytes aligned position.");
                }
            }

            base.OnPrecommit(changes);
        }

        protected override void OnCommit()
        {
            if (!_IsInitialized) return;
            base.OnCommit();
        }

        static bool Move(int[] cur, int[] shape)
        {
            for (int i = shape.Length - 1; i >= 0; --i)
            {
                if (cur[i] < shape[i] - 1)
                {
                    ++cur[i];
                    return true;
                }
                else
                    cur[i] = 0;
            }
            return false;
        }
    }
}
