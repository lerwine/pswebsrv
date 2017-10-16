using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Threading;

namespace Erwine.Leonard.T.PSWebSrv
{
    public class QueryParamDictionary : IList<QueryParamEntry>, IDictionary<string, string>, IList, IDictionary, IConvertible
    {
        private IList<QueryParamEntry> _innerList = new List<QueryParamEntry>();
        private object _syncRoot = new object();
        private KeyCollection _keys;
        private ValueCollection _values;

        public QueryParamEntry this[int index]
        {
            get
            {
                return _innerList[index];
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                Monitor.Enter(_syncRoot);
                try { _innerList[index] = value; }
                finally { Monitor.Exit(_syncRoot); }
            }
        }

        public string this[string key]
        {
            get
            {
                if (key == null)
                    throw new KeyNotFoundException();
                Monitor.Enter(_syncRoot);
                try
                {
                    QueryParamEntry entry = _innerList.FirstOrDefault(i => i.Key == key);
                    if (entry == null)
                        throw new KeyNotFoundException();
                    return entry.Value;
                }
                finally { Monitor.Exit(_syncRoot); }
            }
            set
            {
                if (key == null)
                    throw new KeyNotFoundException();
                Monitor.Enter(_syncRoot);
                try
                {
                    QueryParamEntry entry = _innerList.FirstOrDefault(i => i.Key == key);
                    if (entry == null)
                        _innerList.Add(new QueryParamEntry(key, value));
                    else
                        entry.Value = value;
                }
                finally { Monitor.Exit(_syncRoot); }
            }
        }

        object IList.this[int index]
        {
            get
            {
                return this[index];
            }

            set
            {
                this[index] = (QueryParamEntry)((value != null && value is PSObject) ? (value as PSObject).BaseObject : value);
            }
        }

        object IDictionary.this[object key]
        {
            get
            {
                return this[(string)((key != null && key is PSObject) ? (key as PSObject).BaseObject : key)];
            }

            set
            {
                this[(string)((key != null && key is PSObject) ? (key as PSObject).BaseObject : key)] = (string)((value != null && value is PSObject) ? (value as PSObject).BaseObject : value);
            }
        }

        public int Count {
            get
            {
                return _innerList.Count;
            }
        }

        public bool IsReadOnly {
            get
            {
                return _innerList.IsReadOnly;
            }
        }

        public ICollection<string> Keys {
            get
            {
                return _keys;
            }
        }

        ICollection IDictionary.Keys {
            get
            {
                return _keys;
            }
        }

        public ICollection<string> Values {
            get
            {
                return _values;
            }
        }

        ICollection IDictionary.Values {
            get
            {
                return _values;
            }
        }

        bool ICollection<KeyValuePair<string, string>>.IsReadOnly {
            get
            {
                return false;
            }
        }

        bool IList.IsReadOnly {
            get
            {
                return false;
            }
        }

        bool IDictionary.IsReadOnly {
            get
            {
                return false;
            }
        }

        bool IList.IsFixedSize {
            get
            {
                return false;
            }
        }

        bool IDictionary.IsFixedSize {
            get
            {
                return false;
            }
        }

        object ICollection.SyncRoot {
            get
            {
                return _syncRoot;
            }
        }

        bool ICollection.IsSynchronized {
            get
            {
                return true;
            }
        }

        public QueryParamDictionary()
        {
            _keys = new KeyCollection(this);
            _values = new ValueCollection(this);
        }

        public void Add(QueryParamEntry item)
        {
            if (item == null)
                throw new ArgumentNullException("item");
            Monitor.Enter(_syncRoot);
            try { _innerList.Add(item); }
            finally { Monitor.Exit(_syncRoot); }
        }

        public void Add(string key, string value) { Add(new QueryParamEntry(key, value)); }

        void ICollection<KeyValuePair<string, string>>.Add(KeyValuePair<string, string> item) { Add(new QueryParamEntry(item.Key, item.Value)); }

        int IList.Add(object value)
        {
            int index;
            Monitor.Enter(_syncRoot);
            try
            {
                index = _innerList.Count;
                if (value == null || value is QueryParamEntry)
                    Add((QueryParamEntry)value);
                if (value is DictionaryEntry)
                {
                    DictionaryEntry dictionaryEntry = (DictionaryEntry)value;
                    object k = dictionaryEntry.Key;
                    object v = dictionaryEntry.Value;
                    if (k != null && k is PSObject)
                        k = (k as PSObject).BaseObject;
                    if (v != null && v is PSObject)
                        v = (v as PSObject).BaseObject;
                    Add(new QueryParamEntry((string)k, (string)v));
                }
                else
                {
                    KeyValuePair<string, string> kvp = (KeyValuePair<string, string>)value;
                    Add(new QueryParamEntry(kvp.Key, kvp.Value));
                }
            }
            finally { Monitor.Exit(_syncRoot); }
            return index;
        }

        void IDictionary.Add(object key, object value)
        {
            if (key != null && key is PSObject)
                key = (key as PSObject).BaseObject;
            if (value != null && value is PSObject)
                value = (value as PSObject).BaseObject;
            Add(new QueryParamEntry((string)key, (string)value));
        }

        public void Clear()
        {
            Monitor.Enter(_syncRoot);
            try { _innerList.Clear(); }
            finally { Monitor.Exit(_syncRoot); }
        }

        public bool Contains(QueryParamEntry item)
        {
            Monitor.Enter(_syncRoot);
            try { return _innerList.Contains(item);}
            finally { Monitor.Exit(_syncRoot); }
        }

        bool ICollection<KeyValuePair<string, string>>.Contains(KeyValuePair<string, string> item)
        {
            string k = item.Key;
            string v = item.Value;
            Monitor.Enter(_syncRoot);
            try
            {
                return (v == null) ? _innerList.Any(i => i.Key == k && i.Value == null) :
                    _innerList.Any(i => i.Key == k && i.Value != null && i.Value == v);
            }
            finally { Monitor.Exit(_syncRoot); }
        }

        bool IList.Contains(object value)
        {
            if (value == null)
                return false;
            if (value is PSObject)
                value = (value as PSObject).BaseObject;
            if (value is QueryParamEntry)
                return Contains((QueryParamEntry)value);
            if (value is KeyValuePair<string, string>)
            {
                KeyValuePair<string, string> item = (KeyValuePair<string, string>)value;
                string k = item.Key;
                string v = item.Value;
                Monitor.Enter(_syncRoot);
                try
                {
                    return (v == null) ? _innerList.Any(i => i.Key == k && i.Value == null) :
                        _innerList.Any(i => i.Key == k && i.Value != null && i.Value == v);
                }
                finally { Monitor.Exit(_syncRoot); }
            }
            return false;
        }

        bool IDictionary.Contains(object key)
        {
            if (key != null && key is PSObject)
                key = (key as PSObject).BaseObject;
            return ContainsKey(key as string);
        }

        public bool ContainsKey(string key)
        {
            if (key == null)
                return false;

            Monitor.Enter(_syncRoot);
            try { return _innerList.Any(i => i.Key == key); }
            finally { Monitor.Exit(_syncRoot); }
        }

        public void CopyTo(QueryParamEntry[] array, int arrayIndex)
        {
            Monitor.Enter(_syncRoot);
            try { _innerList.CopyTo(array, arrayIndex); }
            finally { Monitor.Exit(_syncRoot); }
        }

        void ICollection<KeyValuePair<string, string>>.CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            _innerList.Select(i => new KeyValuePair<string, string>(i.Key, i.Value)).ToArray().CopyTo(array, arrayIndex);
        }

        void ICollection.CopyTo(Array array, int index) { _innerList.ToArray().CopyTo(array, index); }

        public IEnumerator<QueryParamEntry> GetEnumerator() { return _innerList.GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return _innerList.GetEnumerator(); }

        IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator() { return new DictionaryEnumerator(this); }

        IDictionaryEnumerator IDictionary.GetEnumerator() { return new DictionaryEnumerator(this); }

        public override int GetHashCode() { return ToString().GetHashCode(); }

        public int IndexOf(QueryParamEntry item)
        {
            Monitor.Enter(_syncRoot);
            try { return _innerList.IndexOf(item); }
            finally { Monitor.Exit(_syncRoot); }
        }

        int IList.IndexOf(object value)
        {
            if (value == null)
                return -1;
            if (value is PSObject)
                value = (value as PSObject).BaseObject;
            if (value is QueryParamEntry)
                return IndexOf((QueryParamEntry)value);
            if (value is KeyValuePair<string, string>)
            {
                KeyValuePair<string, string> item = (KeyValuePair<string, string>)value;
                string k = item.Key;
                string v = item.Value;
                Monitor.Enter(_syncRoot);
                try
                {
                    if (v == null)
                    {
                        for (int i = 0; i < _innerList.Count; i++)
                        {
                            if (_innerList[i].Key == k && _innerList[i].Value == null)
                                return i;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < _innerList.Count; i++)
                        {
                            if (_innerList[i].Key == k && _innerList[i].Value != null && _innerList[i].Value == v)
                                return i;
                        }
                    }
                }
                finally { Monitor.Exit(_syncRoot); }
            }
            return -1;
        }

        public void Insert(int index, QueryParamEntry item)
        {
            if (item == null)
                throw new ArgumentNullException("item");
            Monitor.Enter(_syncRoot);
            try { _innerList.Insert(index, item); }
            finally { Monitor.Exit(_syncRoot); }
        }

        void IList.Insert(int index, object value) { Insert(index, (QueryParamEntry)value); }

        private static QueryParamEntry ObjectAsQueryParamEntry(object value)
        {
            if (value == null)
                return null;

            object obj = (value is PSObject) ? (value as PSObject).BaseObject : value;
                
            if (obj is KeyValuePair<string, string>)
            {
                KeyValuePair<string, string> kvp = (KeyValuePair<string, string>)value;
                return new QueryParamEntry(kvp.Key, kvp.Value);
            }

            return obj as QueryParamEntry;
        }

        private static string ObjectAsString(object value)
        {
            if (value == null || value is string)
                return value as string;
            
            if (value is PSObject)
            {
                object b = (value as PSObject).BaseObject;
                if (b is string)
                    return b as string;
            }

            return value.ToString();
        }
        
        public bool Remove(QueryParamEntry item)
        {
            Monitor.Enter(_syncRoot);
            try { return _innerList.Remove(item); }
            finally { Monitor.Exit(_syncRoot); }
        }

        public void RemoveAt(int index)
        {
            Monitor.Enter(_syncRoot);
            try { _innerList.RemoveAt(index); }
            finally { Monitor.Exit(_syncRoot); }
        }

        public bool Remove(string key)
        {
            Monitor.Enter(_syncRoot);
            try
            {
                throw new NotImplementedException();
            }
            finally { Monitor.Exit(_syncRoot); }
        }

        bool ICollection<KeyValuePair<string, string>>.Remove(KeyValuePair<string, string> item)
        {
            throw new NotImplementedException();
        }

        void IList.Remove(object value)
        {
            throw new NotImplementedException();
        }

        void IDictionary.Remove(object key)
        {
            throw new NotImplementedException();
        }

        public override string ToString() { return String.Join("&", _innerList.Select(i => i.ToString()).ToArray()); }

        TypeCode IConvertible.GetTypeCode() { return TypeCode.String; }

        bool IConvertible.ToBoolean(IFormatProvider provider) { throw new NotSupportedException(); }

        byte IConvertible.ToByte(IFormatProvider provider) { throw new NotSupportedException(); }

        char IConvertible.ToChar(IFormatProvider provider) { throw new NotSupportedException(); }

        DateTime IConvertible.ToDateTime(IFormatProvider provider) { throw new NotSupportedException(); }

        decimal IConvertible.ToDecimal(IFormatProvider provider) { throw new NotSupportedException(); }

        double IConvertible.ToDouble(IFormatProvider provider) { throw new NotSupportedException(); }

        short IConvertible.ToInt16(IFormatProvider provider) { throw new NotSupportedException(); }

        int IConvertible.ToInt32(IFormatProvider provider) { throw new NotSupportedException(); }

        long IConvertible.ToInt64(IFormatProvider provider) { throw new NotSupportedException(); }

        sbyte IConvertible.ToSByte(IFormatProvider provider) { throw new NotSupportedException(); }

        float IConvertible.ToSingle(IFormatProvider provider) { throw new NotSupportedException(); }

        string IConvertible.ToString(IFormatProvider provider) { return ToString(); }

        object IConvertible.ToType(Type conversionType, IFormatProvider provider)
        {
            if (conversionType != null && conversionType.AssemblyQualifiedName == (typeof(string)).AssemblyQualifiedName)
                return ToString();
            throw new NotSupportedException();
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider) { throw new NotSupportedException(); }

        uint IConvertible.ToUInt32(IFormatProvider provider) { throw new NotSupportedException(); }

        ulong IConvertible.ToUInt64(IFormatProvider provider) { throw new NotSupportedException(); }

        public bool TryGetValue(string key, out string value)
        {
            Monitor.Enter(_syncRoot);
            try
            {
                throw new NotImplementedException();
            }
            finally { Monitor.Exit(_syncRoot); }
        }

        class KeyCollection : ICollection<string>, ICollection
        {
            private QueryParamDictionary _parent;

            internal KeyCollection(QueryParamDictionary parent) { _parent = parent; }

            public int Count {
                get
                {
                    return _parent.Count;
                }
            }

            bool ICollection<string>.IsReadOnly {
                get
                {
                    return false;
                }
            }

            object ICollection.SyncRoot {
                get
                {
                    return (_parent as ICollection).SyncRoot;
                }
            }

            bool ICollection.IsSynchronized {
                get
                {
                    return true;
                }
            }

            void ICollection<string>.Add(string item) { throw new NotImplementedException(); }

            void ICollection<string>.Clear() { throw new NotImplementedException(); }

            public bool Contains(string item) { return item != null && _parent._innerList.Any(i => i.Key == item); }

            public void CopyTo(string[] array, int arrayIndex)
            {
                _parent._innerList.Select(i => i.Key).ToArray().CopyTo(array, arrayIndex);
            }

            void ICollection.CopyTo(Array array, int index)
            {
                _parent._innerList.Select(i => i.Key).ToArray().CopyTo(array, index);
            }

            public IEnumerator<string> GetEnumerator() { return _parent._innerList.Select(i => i.Key).GetEnumerator(); }

            IEnumerator IEnumerable.GetEnumerator() { return _parent._innerList.Select(i => i.Key).ToArray().GetEnumerator(); }

            bool ICollection<string>.Remove(string item) { throw new NotImplementedException(); }
        }
        
        class ValueCollection : ICollection<string>, ICollection
        {
            private QueryParamDictionary _parent;

            internal ValueCollection(QueryParamDictionary parent) { _parent = parent; }

            public int Count {
                get
                {
                    return _parent.Count;
                }
            }

            bool ICollection<string>.IsReadOnly {
                get
                {
                    return false;
                }
            }

            object ICollection.SyncRoot {
                get
                {
                    return (_parent as ICollection).SyncRoot;
                }
            }

            bool ICollection.IsSynchronized {
                get
                {
                    return true;
                }
            }

            void ICollection<string>.Add(string item) { throw new NotImplementedException(); }

            void ICollection<string>.Clear() { throw new NotImplementedException(); }

            public bool Contains(string item)
            {
                if (item == null)
                    return _parent._innerList.Any(i => i.Value == null);
                return _parent._innerList.Select(i => i.Value).Any(s => s != null && s == item);
            }

            public void CopyTo(string[] array, int arrayIndex)
            {
                _parent._innerList.Select(i => i.Value).ToArray().CopyTo(array, arrayIndex);
            }

            void ICollection.CopyTo(Array array, int index)
            {
                _parent._innerList.Select(i => i.Value).ToArray().CopyTo(array, index);
            }

            public IEnumerator<string> GetEnumerator() { return _parent._innerList.Select(i => i.Value).GetEnumerator(); }

            IEnumerator IEnumerable.GetEnumerator() { return _parent._innerList.Select(i => i.Value).ToArray().GetEnumerator(); }

            bool ICollection<string>.Remove(string item) { throw new NotImplementedException(); }
        }
        
        class DictionaryEnumerator : IEnumerator<KeyValuePair<string, string>>, IDictionaryEnumerator
        {
            private QueryParamDictionary _parent;
            private int _position = -1;
            private string _key = null;
            private string _value = null;

            internal DictionaryEnumerator(QueryParamDictionary parent) { _parent = parent; }

            public KeyValuePair<string, string> Current
            {
                get
                {
                    string key = _key;
                    if (key == null)
                    {
                        if (_parent == null)
                            throw new ObjectDisposedException(this.GetType().FullName);
                        if (_position < 0)
                            throw new InvalidOperationException("Enumerator is before the start of the enumeration");
                        throw new InvalidOperationException("Enumeration has no values");
                    }
                    return new KeyValuePair<string, string>(key, _value);
                }
            }

            object IEnumerator.Current {
                get
                {
                    return Current;
                }
            }

            public object Key {
                get
                {
                    return _key;
                }
            }

            public object Value {
                get
                {
                    return _value;
                }
            }

            public DictionaryEntry Entry
            {
                get
                {
                    string key = _key;
                    if (key == null)
                    {
                        if (_parent == null)
                            throw new ObjectDisposedException(this.GetType().FullName);
                        if (_position < 0)
                            throw new InvalidOperationException("Enumerator is before the start of the enumeration");
                        throw new InvalidOperationException("Enumeration has no values");
                    }
                    return new DictionaryEntry(key, _value);
                }
            }

            public bool MoveNext()
            {
                QueryParamDictionary parent = _parent;
                if (parent == null)
                    throw new ObjectDisposedException(this.GetType().FullName);

                Monitor.Enter(parent._syncRoot);
                try
                {
                    if (_position < parent._innerList.Count)
                        _position++;
                        
                    if (_position >= parent._innerList.Count)
                        return false;
                    QueryParamEntry entry = parent._innerList[_position];
                    _key = entry.Key;
                    _value = entry.Value;
                }
                finally { Monitor.Exit(parent._syncRoot); }
                return true;
            }

            public void Reset()
            {
                if (_parent == null)
                    throw new ObjectDisposedException(this.GetType().FullName);
                _key = null;
                _value = null;
                _position = -1;
            }

            // This code added to correctly implement the disposable pattern.
            public void Dispose()
            {
                _parent = null;
                _key = null;
            }
        }
    }
}