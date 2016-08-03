using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Microsoft.Research.Science.Data
{
    [DataContract]
    public struct ObjectWithTypeString
    {
        [DataMember]
        public string TypeString;

        [DataMember]
        public object Value;

        public ObjectWithTypeString(string typeStr, object obj)
        {
            TypeString = typeStr;
            Value = obj;
        }

        public ObjectWithTypeString(object obj)
        {
            Value = obj;
            TypeString = obj.GetType().ToString();
        }
    }

    [DataContract]
    [KnownType(typeof(Double))]
    [KnownType(typeof(Single))]
    [KnownType(typeof(Int16))]
    [KnownType(typeof(Int32))]
    [KnownType(typeof(Int64))]
    [KnownType(typeof(UInt64))]
    [KnownType(typeof(UInt32))]
    [KnownType(typeof(UInt16))]
    [KnownType(typeof(Byte))]
    [KnownType(typeof(SByte))]
    [KnownType(typeof(DateTime))]
    [KnownType(typeof(String))]
    [KnownType(typeof(Boolean))]
    [KnownType(typeof(Double[]))]
    [KnownType(typeof(Single[]))]
    [KnownType(typeof(Int16[]))]
    [KnownType(typeof(Int32[]))]
    [KnownType(typeof(Int64[]))]
    [KnownType(typeof(UInt64[]))]
    [KnownType(typeof(UInt32[]))]
    [KnownType(typeof(UInt16[]))]
    [KnownType(typeof(Byte[]))]
    [KnownType(typeof(SByte[]))]
    [KnownType(typeof(DateTime[]))]
    [KnownType(typeof(String[]))]
    [KnownType(typeof(Boolean[]))]
    public class SerializableVariableSchema
    {
        [DataMember]
        public string Name;

        public Type Type
        {
            get { return Type.GetType(TypeString); }
            set { this.TypeString = value.ToString(); }
        }

        [DataMember]
        public string[] Dimensions;

        [DataMember]
        public Dictionary<string, ObjectWithTypeString> Metadata; // string in Tuple is for Type of Object

        [DataMember]
        private int Size;

        [DataMember]
        private string TypeString;

        public int ValueSize { get { return Size; } }

        public SerializableVariableSchema(string variableName, Type variableType, string[] variableDimensions, Dictionary<string, object> variableMetadata)
        {
            this.Name = variableName;
            this.Type = variableType;
            this.Dimensions = variableDimensions;
            this.Metadata = variableMetadata.AsTypedMetadata();
            if (variableType ==typeof(Int32)) this.Size = sizeof(Int32);
            else if (variableType == typeof(UInt32)) this.Size = sizeof(UInt32);
            else if (variableType == typeof(Byte)) this.Size = sizeof(Byte);
            else if (variableType == typeof(SByte)) this.Size = sizeof(SByte);
            else if (variableType == typeof(double)) this.Size = sizeof(double);
            else if (variableType == typeof(Single)) this.Size = sizeof(Single);
            else if (variableType == typeof(Int16)) this.Size = sizeof(Int16);
            else if (variableType == typeof(UInt16)) this.Size = sizeof(UInt16);
            else if (variableType == typeof(DateTime)) this.Size = sizeof(Int64);
            else throw new NotImplementedException("Type " + variableType.ToString() + " is not a valid AzureBlobDataSet variable type. Only Byte, Sbyte, UInt16, Int16, UInt32, Int32, DateTime, Single, and double are supported.");
        }
    }

    [DataContract]
    public class SerializableDimension
    {
        [DataMember]
        public string Name;

        [DataMember]
        public int Length;

        public SerializableDimension(string dimensionName, int dimensionLength)
        {
            this.Name = dimensionName;
            this.Length = dimensionLength;
        }
    }

    [DataContract]
    [KnownType(typeof(Double))]
    [KnownType(typeof(Single))]
    [KnownType(typeof(Int16))]
    [KnownType(typeof(Int32))]
    [KnownType(typeof(Int64))]
    [KnownType(typeof(UInt64))]
    [KnownType(typeof(UInt32))]
    [KnownType(typeof(UInt16))]
    [KnownType(typeof(Byte))]
    [KnownType(typeof(SByte))]
    [KnownType(typeof(DateTime))]
    [KnownType(typeof(String))]
    [KnownType(typeof(Boolean))]
    [KnownType(typeof(Double[]))]
    [KnownType(typeof(Single[]))]
    [KnownType(typeof(Int16[]))]
    [KnownType(typeof(Int32[]))]
    [KnownType(typeof(Int64[]))]
    [KnownType(typeof(UInt64[]))]
    [KnownType(typeof(UInt32[]))]
    [KnownType(typeof(UInt16[]))]
    [KnownType(typeof(Byte[]))]
    [KnownType(typeof(SByte[]))]
    [KnownType(typeof(DateTime[]))]
    [KnownType(typeof(String[]))]
    [KnownType(typeof(Boolean[]))]
    public class SerializableDataSetSchema
    {
        [DataMember]
        public SerializableDimension[] Dimensions;

        [DataMember]
        public SerializableVariableSchema[] Variables;

        [DataMember]
        public Dictionary<string, ObjectWithTypeString> Metadata;

        public SerializableDataSetSchema(SerializableDimension[] dimensions, SerializableVariableSchema[] variables, Dictionary<string, object> metadata)
        {
            this.Dimensions = dimensions;
            this.Variables = variables;
            this.Metadata = metadata.AsTypedMetadata();
        }

        public static object CastMetadataValue(object value, Type targetType)
        {
            string s = value.GetType().ToString();
            if (value is Int32)
            {
                if (targetType == typeof(Double)) return (Double)(Int32)value;
                else if (targetType == typeof(Single)) return (Single)(Int32)value;
                else if (targetType == typeof(Int16)) return (Int16)(Int32)value;
                else if (targetType == typeof(Int32)) return (Int32)(Int32)value;
                else if (targetType == typeof(Int64)) return (Int64)(Int32)value;
                else if (targetType == typeof(UInt64)) return (UInt64)(Int32)value;
                else if (targetType == typeof(UInt32)) return (UInt32)(Int32)value;
                else if (targetType == typeof(UInt16)) return (UInt16)(Int32)value;
                else if (targetType == typeof(Byte)) return (Byte)(Int32)value;
                else if (targetType == typeof(SByte)) return (SByte)(Int32)value;
                else throw new DataSetException("Unsupported metadata type.");
            }
            else if (value is Decimal)
            {
                if (targetType == typeof(Double)) return (Double)(Decimal)value;
                else if (targetType == typeof(Single)) return (Single)(Decimal)value;
                else if (targetType == typeof(Int16)) return (Int16)(Decimal)value;
                else if (targetType == typeof(Int32)) return (Int32)(Decimal)value;
                else if (targetType == typeof(Int64)) return (Int64)(Decimal)value;
                else if (targetType == typeof(UInt64)) return (UInt64)(Decimal)value;
                else if (targetType == typeof(UInt32)) return (UInt32)(Decimal)value;
                else if (targetType == typeof(UInt16)) return (UInt16)(Decimal)value;
                else if (targetType == typeof(Byte)) return (Byte)(Decimal)value;
                else if (targetType == typeof(SByte)) return (SByte)(Decimal)value;
                else throw new DataSetException("Unsupported metadata type.");
            }
            else
            {
                if (targetType == typeof(Double)) return (Double)value;
                else if (targetType == typeof(Single)) return (Single)value;
                else if (targetType == typeof(Int16)) return (Int16)value;
                else if (targetType == typeof(Int32)) return (Int32)value;
                else if (targetType == typeof(Int64)) return (Int64)value;
                else if (targetType == typeof(UInt64)) return (UInt64)value;
                else if (targetType == typeof(UInt32)) return (UInt32)value;
                else if (targetType == typeof(UInt16)) return (UInt16)value;
                else if (targetType == typeof(Byte)) return (Byte)value;
                else if (targetType == typeof(SByte)) return (SByte)value;
                else if (targetType == typeof(DateTime))
                {
                    if (value is DateTime) return (DateTime)value;
                    string dtString = (string)value;
                    var jsonSerializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(DateTime));
                    DateTime deserializedDate;
                    using (var stream = new System.IO.MemoryStream())
                    {
                        UTF8Encoding utf8 = new UTF8Encoding();
                        byte[] buf = utf8.GetBytes("\"\\" + dtString.Replace(")/", ")\\/\""));
                        stream.Write(buf, 0, buf.Length);
                        stream.Seek(0, System.IO.SeekOrigin.Begin);
                        deserializedDate = (DateTime)jsonSerializer.ReadObject(stream);
                    }
                    return deserializedDate;
                }
                else if (targetType == typeof(String)) return (String)value;
                else if (targetType == typeof(Boolean)) return (Boolean)value;
                else throw new DataSetException("Unsupported metadata type.");
            }
        }
    }

    public static class ConvUtils
    {
        public static SerializableVariableSchema AsSerializable(this VariableSchema schema)
        {
            return new SerializableVariableSchema(schema.Name, schema.TypeOfData, schema.Dimensions.Select<Dimension, string>(x => x.Name).ToArray(), schema.Metadata.AsDictionary());
        }

        public static SerializableDimension AsSerializble(this Dimension dim)
        {
            return new SerializableDimension(dim.Name, dim.Length);
        }

        public static SerializableDataSetSchema GetSerializableSchema(this DataSet ds)
        {
            var schema = ds.GetSchema();
            Type empty = typeof(EmptyValueType);
            //ignoring global metadata in variable list
            return new SerializableDataSetSchema(schema.GetDimensions().Select<Dimension, SerializableDimension>(x => x.AsSerializble()).ToArray(),
                schema.Variables.Where(x => x.TypeOfData != empty || x.ID != 0).Select<VariableSchema, SerializableVariableSchema>(x => x.AsSerializable()).ToArray(),
                ds.Metadata.AsDictionary());
        }

        public static Dictionary<string, ObjectWithTypeString> AsTypedMetadata(this Dictionary<string, object> metadata)
        {
            Dictionary<string, ObjectWithTypeString> res = new Dictionary<string, ObjectWithTypeString>();
            if(metadata != null)
                foreach (var i in metadata) res.Add(i.Key, new ObjectWithTypeString(i.Value));
            return res;
        }
    }
}
