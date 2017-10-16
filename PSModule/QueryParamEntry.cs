using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace Erwine.Leonard.T.PSWebSrv
{
    public class QueryParamEntry : IEquatable<QueryParamEntry>, IComparable<QueryParamEntry>, IComparable, IConvertible
    {
        public string Key { get; private set; }

        public string Value { get; set; }

        public QueryParamEntry(string key, string value)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            Key = key;
            Value = value;
        }

        public static bool TryParse(string value, out QueryParamEntry result)
        {
            if (value == null)
            {
                result = null;
                return false;
            }

            int index = value.IndexOf('=');
            if (index < 0)
                result = new QueryParamEntry(Uri.UnescapeDataString(value), null);
            else
                result = new QueryParamEntry(Uri.UnescapeDataString(value.Substring(0, index)), Uri.UnescapeDataString(value.Substring(index + 1)));
            return true;
        }

        public QueryParamEntry Parse(string value)
        {
            if (value == null)
                return null;

            int index = value.IndexOf('=');
            if (index < 0)
                return new QueryParamEntry(Uri.UnescapeDataString(value), null);
            
            return new QueryParamEntry(Uri.UnescapeDataString(value.Substring(0, index)), Uri.UnescapeDataString(value.Substring(index + 1)));
        }

        public bool Equals(QueryParamEntry other)
        {
            if (other == null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            string v, o;
            return Key == other.Key && (((v = Value) == null) ? other.Value == null : ((o = other.Value) != null && v == o));
        }

        public override bool Equals(object obj)
        {
            if (obj != null && obj is string)
                return ToString() == (string)obj;
            return Equals(obj as QueryParamEntry);
        }

        public override int GetHashCode() { return ToString().GetHashCode(); }

        public override string ToString()
        {
            string value = Value;
            if (value == null)
                return Uri.EscapeDataString(Key);
            
            return Uri.EscapeDataString(Key) + "=" + Uri.EscapeDataString(value);
        }
        
        public int CompareTo(QueryParamEntry other)
        {
            if (other == null)
                return 1;
            if (ReferenceEquals(this, other))
                return 0;
            int i;
            if ((i = Key.CompareTo(other.Key)) != 0)
                return i;
            string v = Value;
            string o = other.Value;
            if (v == null)
                return (o == null) ? 0 : -1;
            return (o == null) ? 1 : v.CompareTo(o);
        }

        public int CompareTo(object obj)
        {
            if (obj != null && obj is string)
                return ToString().CompareTo((string)obj);
            return CompareTo(obj as QueryParamEntry);
        }

        TypeCode IConvertible.GetTypeCode() { return TypeCode.String; }

        bool IConvertible.ToBoolean(IFormatProvider provider) { throw new NotSupportedException(); }

        char IConvertible.ToChar(IFormatProvider provider) { throw new NotSupportedException(); }

        sbyte IConvertible.ToSByte(IFormatProvider provider) { throw new NotSupportedException(); }

        byte IConvertible.ToByte(IFormatProvider provider) { throw new NotSupportedException(); }

        short IConvertible.ToInt16(IFormatProvider provider) { throw new NotSupportedException(); }

        ushort IConvertible.ToUInt16(IFormatProvider provider) { throw new NotSupportedException(); }

        int IConvertible.ToInt32(IFormatProvider provider) { throw new NotSupportedException(); }

        uint IConvertible.ToUInt32(IFormatProvider provider) { throw new NotSupportedException(); }

        long IConvertible.ToInt64(IFormatProvider provider) { throw new NotSupportedException(); }

        ulong IConvertible.ToUInt64(IFormatProvider provider) { throw new NotSupportedException(); }

        float IConvertible.ToSingle(IFormatProvider provider) { throw new NotSupportedException(); }

        double IConvertible.ToDouble(IFormatProvider provider) { throw new NotSupportedException(); }

        decimal IConvertible.ToDecimal(IFormatProvider provider) { throw new NotSupportedException(); }

        DateTime IConvertible.ToDateTime(IFormatProvider provider) { throw new NotSupportedException(); }

        string IConvertible.ToString(IFormatProvider provider) { return ToString(); }

        object IConvertible.ToType(Type conversionType, IFormatProvider provider)
        {
            if (conversionType == null || conversionType.AssemblyQualifiedName == (typeof(string).AssemblyQualifiedName))
                return ToString();
            throw new NotSupportedException();
        }
    }
}