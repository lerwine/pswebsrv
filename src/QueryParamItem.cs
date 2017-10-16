using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Erwine.Leonard.T.PSWebSrv
{
    public class QueryParamItem : IEquatable<QueryParamItem>, IEquatable<KeyValuePair<string, string>>
    {
        public string Key { get; private set; }
        public string Value { get; set; }

        public QueryParamItem(string key, string value)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            
            Key = key;
            Value = value;
        }

        public bool Equals(QueryParamItem other)
        {
            return other != null && (ReferenceEquals(this, other) ||
                (Key == other.Key && ((Value == null) ? other.Value == null : (other.Value != null && Value == other.Value))));
        }
        
        public bool Equals(KeyValuePair<string, string> other)
        {
            return Key == other.Key && ((Value == null) ? other.Value == null : (other.Value != null && Value == other.Value));
        }
        
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj is QueryParamDictionary && ReferenceEquals(this, obj))
                return true;
            return ToString() == ((obj is string) ? obj as string : obj.ToString());
        }

        public override int GetHashCode() { return ToString().GetHashCode(); }

        public static QueryParamItem Parse(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            string[] pair = value.Split(new char[] { '=' }, 2);
            return new QueryParamItem(Uri.UnescapeDataString(pair[0]), (pair.Length == 2) ? Uri.UnescapeDataString(pair[1]) : null);
        }

        public override string ToString()
        {
            if (Value == null)
                return Uri.EscapeDataString(Key);
            return Uri.EscapeDataString(Key) + "=" + Uri.EscapeDataString(Value);
        }
    }
}