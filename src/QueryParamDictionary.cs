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
    public class QueryParamDictionary : IList<QueryParamItem>, IDictionary<string, string>, IList, IDictionary
    {
        private object _syncRoot = new object();
        private List<QueryParamItem> _innerList = new List<QueryParamItem>();
        private KeyCollection _keyCollection;
        private ValueCollection _valueCollection;
        
        public int Count { get { return _innerList.Count; } }

        bool IList.IsFixedSize { get { return false; } }

        bool IDictionary.IsFixedSize { get { return false; } }

        bool ICollection<QueryParamItem>.IsReadOnly { get { return false; } }

        bool ICollection<KeyValuePair<string, string>>.IsReadOnly { get { return false; } }

        bool IList.IsReadOnly { get { return false; } }
        
        bool IDictionary.IsReadOnly { get { return false; } }
        
        bool ICollection.IsSynchronized { get { return true; } }
        
        public ICollection<string> Keys { get { return _keyCollection; } }
        
        ICollection IDictionary.Keys { get { return _keyCollection; } }

        public object SyncRoot { get { return _syncRoot; } }
        
        public ICollection<string> Values { get { return _valueCollection; } }
        
        ICollection IDictionary.Values { get { return _valueCollection; } }
        
        public QueryParamItem this[int index]
        {
            get { return _innerList[index]; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();

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
                    throw new ArgumentNullException("key");

                Monitor.Enter(_syncRoot);
                try
                {
                    foreach (QueryParamItem item in _innerList)
                    {
                        if (item.Key == key)
                            return item.Value;
                    }
                }
                finally { Monitor.Exit(_syncRoot); }
                throw new KeyNotFoundException();
            }
            set
            {
                if (key == null)
                    throw new ArgumentNullException("key");

                Monitor.Enter(_syncRoot);
                try
                {
                    using (IEnumerator<QueryParamItem> enumerator = _innerList.GetEnumerator())
                    {
                        for (int i = 0; i < _innerList.Count; i++)
                        {
                            if (_innerList[i].Key == key)
                            {
                                _innerList[i].Value = value;
                                i++;
                                while (i < _innerList.Count)
                                {
                                    if (_innerList[i].Key == key)
                                        _innerList.RemoveAt(i);
                                    else
                                        i++;
                                }
                                return;
                            }
                        }
                    }
                }
                finally { Monitor.Exit(_syncRoot); }
                throw new KeyNotFoundException();
            }
        }
	    // IEnumerable<KeyValuePair<string, string>>

        object IList.this[int index]
        {
            get { return _innerList[index]; }
            set { this[index] = (QueryParamItem)value; }
        }
        
        object IDictionary.this[object key]
        {
            get
            {
                object baseObject = key;
                if (baseObject != null && baseObject is PSObject)
                    baseObject = (baseObject as PSObject).BaseObject;
                if (baseObject == null || baseObject is string)
                    return this[baseObject as string];
                return this[key.ToString()];
            }
            set
            {
                object baseObjectKey = key;
                if (baseObjectKey != null && baseObjectKey is PSObject)
                    baseObjectKey = (baseObjectKey as PSObject).BaseObject;
                object baseObjectValue = value;
                if (baseObjectValue != null && baseObjectValue is PSObject)
                    baseObjectValue = (baseObjectValue as PSObject).BaseObject;
                if (!(baseObjectValue == null || baseObjectValue is string))
                    baseObjectValue = value.ToString();
                if (baseObjectKey == null || baseObjectKey is string)
                    this[baseObjectKey as string] = baseObjectValue as string;
                else
                    this[key.ToString()] = baseObjectValue as string;
            }
        }
        public QueryParamDictionary()
        {
            _keyCollection = new KeyCollection(_innerList, _syncRoot);
            _valueCollection = new ValueCollection(_innerList, _syncRoot);
        }

        public static QueryParamDictionary Parse(string queryString)
        {
            QueryParamDictionary result = new QueryParamDictionary();
            if (String.IsNullOrEmpty(queryString) || (queryString.Length == 1 && queryString[0] == '?'))
                return result;
            foreach (QueryParamItem item in ((queryString[0] == '?') ? queryString.Substring(1) : queryString).Split('&')
                    .Select(p => QueryParamItem.Parse(p)))
                result.Add(item);
            return result;
        }

        public void Add(QueryParamItem item)
        {
            if (item == null)
                throw new ArgumentNullException("item");
            Monitor.Enter(_syncRoot);
            try { _innerList.Add(item); }
            finally { Monitor.Exit(_syncRoot); }
        }

        public void Add(string key, string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            Monitor.Enter(_syncRoot);
            try
            {
                if (_innerList.Any(i => i.Key == key))
                    throw new ArgumentException("A query parameter with the same key already exists", "key");
                Add(new QueryParamItem(key, value));
            }
            finally { Monitor.Exit(_syncRoot); }
        }

        void ICollection<KeyValuePair<string, string>>.Add(KeyValuePair<string, string> item) { Add(item.Key, item.Value); }

        int IList.Add(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            Monitor.Enter(_syncRoot);
            try
            {
                int index = _innerList.Count;
                _innerList.Add((QueryParamItem)value);
                return index;
            }
            catch (Exception e) { throw new ArgumentException(e.Message, "value", e); }
            finally { Monitor.Exit(_syncRoot); }
        }

        void IDictionary.Add(object key, object value)
        {
            object baseObjectKey = key;
            if (baseObjectKey != null && baseObjectKey is PSObject)
                baseObjectKey = (baseObjectKey as PSObject).BaseObject;
            object baseObjectValue = value;
            if (baseObjectValue != null && baseObjectValue is PSObject)
                baseObjectValue = (baseObjectValue as PSObject).BaseObject;
            if (!(baseObjectValue == null || baseObjectValue is string))
                baseObjectValue = value.ToString();
            if (baseObjectKey == null || baseObjectKey is string)
                Add(baseObjectKey as string, baseObjectValue as string);
            else
                Add(key.ToString(), baseObjectValue as string);
        }

        public void AddRange(IEnumerable<QueryParamItem> collection)
        {
            if (collection == null)
                return;
            Monitor.Enter(_syncRoot);
            try { _innerList.AddRange(collection.Where(i => i != null)); }
            finally { Monitor.Exit(_syncRoot); }
        }

        public void Clear()
        {
            Monitor.Enter(_syncRoot);
            try { _innerList.Clear(); }
            finally { Monitor.Exit(_syncRoot); }
        }

        public bool Contains(QueryParamItem item)
        {
            if (item == null)
                return false;
            Monitor.Enter(_syncRoot);
            try { return _innerList.Contains(item); }
            finally { Monitor.Exit(_syncRoot); }
        }

        bool ICollection<KeyValuePair<string, string>>.Contains(KeyValuePair<string, string> item)
        {
            Monitor.Enter(_syncRoot);
            try { return _innerList.Any(i => i.Equals(item)); }
            finally { Monitor.Exit(_syncRoot); }
        }

        bool IList.Contains(object value)
        {
            return value != null && value is QueryParamItem && Contains((QueryParamItem)value);
        }

        bool IDictionary.Contains(object key)
        { 
            object baseObjectKey = key;
            if (baseObjectKey != null && baseObjectKey is PSObject)
                baseObjectKey = (baseObjectKey as PSObject).BaseObject;
            if (baseObjectKey == null || baseObjectKey is string)
                return ContainsKey(baseObjectKey as string);
            
            return ContainsKey(key.ToString());
        }
        public bool ContainsKey(string key)
        {
            if (key == null)
                return false;

            Monitor.Enter(_syncRoot);
            try { return _innerList.Any(i => i.Key == key); }
            finally { Monitor.Exit(_syncRoot); }
        }
        public void CopyTo(QueryParamItem[] array, int arrayIndex)
        {
            Monitor.Enter(_syncRoot);
            try { _innerList.CopyTo(array, arrayIndex); }
            finally { Monitor.Exit(_syncRoot); }
        }

        void ICollection<KeyValuePair<string, string>>.CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            Monitor.Enter(_syncRoot);
            try { _innerList.Select(i => new KeyValuePair<string, string>(i.Key, i.Value)).ToList().CopyTo(array, arrayIndex); }
            finally { Monitor.Exit(_syncRoot); }
        }

        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            Monitor.Enter(_syncRoot);
            try { _innerList.ToArray().CopyTo(array, arrayIndex); }
            finally { Monitor.Exit(_syncRoot); }
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj is QueryParamDictionary && ReferenceEquals(this, obj))
                return true;
            return ToString() == ((obj is string) ? obj as string : obj.ToString());
        }

        public IEnumerator<QueryParamItem> GetEnumerator() { return _innerList.GetEnumerator(); }

        IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator()
        { 
            return new DictionaryEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator() { return (_innerList as IEnumerable).GetEnumerator(); }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        { 
            return new DictionaryEnumerator(this);
        }

        public override int GetHashCode() { return ToString().GetHashCode(); }

        public IEnumerable<string> GetValues(string key) { return (key != null) ? _innerList.Where(i => i.Key == key).Select(i => i.Value) : new string[0]; }

        public int IndexOf(QueryParamItem item)
        {
            if (item == null)
                return -1;
            Monitor.Enter(_syncRoot);
            try { return _innerList.IndexOf(item); }
            finally { Monitor.Exit(_syncRoot); }
        }

        int IList.IndexOf(object value)
        {
            return (value != null && value is QueryParamItem) ? IndexOf((QueryParamItem)value) : -1;
        }

        public void Insert(int index, QueryParamItem item)
        {
            if (item == null)
                throw new ArgumentNullException("item");
            Monitor.Enter(_syncRoot);
            try { _innerList.Insert(index, item); }
            finally { Monitor.Exit(_syncRoot); }
        }

        void IList.Insert(int index, object value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            Monitor.Enter(_syncRoot);
            try { _innerList.Insert(index, (QueryParamItem)value); }
            catch (ArgumentOutOfRangeException e) { throw new ArgumentOutOfRangeException("value", index, e.Message); }
            catch (Exception e) { throw new ArgumentException(e.Message, "value", e); }
            finally { Monitor.Exit(_syncRoot); }
        }

        public void InsertRange(int index, IEnumerable<QueryParamItem> collection)
        {
            if (collection == null)
                return;
            Monitor.Enter(_syncRoot);
            try { _innerList.InsertRange(index, collection); }
            finally { Monitor.Exit(_syncRoot); }
        }

        public bool Remove(QueryParamItem item)
        {
            if (item == null)
                return false;
            Monitor.Enter(_syncRoot);
            try { return _innerList.Remove(item); }
            finally { Monitor.Exit(_syncRoot); }
        }

        public bool Remove(string key)
        {
            if (key == null)
                return false;
            bool result = false;
            Monitor.Enter(_syncRoot);
            try
            {
                for (int i = 0; i < _innerList.Count; i++)
                {
                    if (_innerList[i].Key == key)
                    {
                        result = true;
                        _innerList.RemoveAt(i);
                        while (i < _innerList.Count)
                        {
                            if (_innerList[i].Key == key)
                                _innerList.RemoveAt(i);
                            else
                                i++;
                        }
                        break;
                    }
                }
            }
            finally { Monitor.Exit(_syncRoot); }
            return result;
        }

        bool ICollection<KeyValuePair<string, string>>.Remove(KeyValuePair<string, string> item)
        {
            Monitor.Enter(_syncRoot);
            try
            {
                for (int i = 0; i < _innerList.Count; i++)
                {
                    if (_innerList[i].Equals(item))
                    {
                        _innerList.RemoveAt(i);
                        return true;
                    }
                }
            }
            finally { Monitor.Exit(_syncRoot); }
            return false;
        }
        void IList.Remove(object value)
        {
            if (value != null && value is QueryParamItem)
                Remove((QueryParamItem)value);
        }

        void IDictionary.Remove(object key)
        {
            object baseObjectKey = key;
            if (baseObjectKey != null && baseObjectKey is PSObject)
                baseObjectKey = (baseObjectKey as PSObject).BaseObject;
            if (baseObjectKey == null || baseObjectKey is string)
                Remove(baseObjectKey as string);
            else
                Remove(key.ToString());
        }
        public void RemoveAt(int index)
        {
            Monitor.Enter(_syncRoot);
            try { _innerList.RemoveAt(index); }
            finally { Monitor.Exit(_syncRoot); }
        }

        public QueryParamItem[] ToArray() { return _innerList.ToArray(); }

        public override string ToString() { return String.Join("&", _innerList.Select(i => i.ToString()).ToArray()); }

        public bool TryGetValue(string key, out string value)
        {
            if (key == null)
            {
                value = null;
                return false;
            }
            Monitor.Enter(_syncRoot);
            try
            {
                for (int i = 0; i < _innerList.Count; i++)
                {
                    if (_innerList[i].Key == key)
                    {
                        value = _innerList[i].Value;
                        return true;
                    }
                }
            }
            finally { Monitor.Exit(_syncRoot); }
            value = null;
            return false;
        }

        class KeyCollection : ICollection<string>, ICollection
        {
            private object _syncRoot;
            private List<QueryParamItem> _list;
            public KeyCollection(List<QueryParamItem> list, object syncRoot)
            {
                _syncRoot = syncRoot;
                _list = list;
            }
            public int Count { get { return _list.Count; } }
            bool ICollection<string>.IsReadOnly { get { return true; } }
            bool ICollection.IsSynchronized { get { return true; } }
            public object SyncRoot { get { return _syncRoot; } }
            internal IEnumerable<string> AsEnumerable()
            {
                foreach (QueryParamItem item in _list)
                    yield return item.Key;
            }
            void ICollection<string>.Add(string item) { throw new NotSupportedException(); }
            void ICollection<string>.Clear() { throw new NotSupportedException(); }
            bool ICollection<string>.Remove(string item) { throw new NotSupportedException(); }
            public bool Contains(string item)
            {
                if (item == null)
                    return false;
                Monitor.Enter(_syncRoot);
                try { return AsEnumerable().Any(i => i == item); }
                finally { Monitor.Exit(_syncRoot); }
            }
            public void CopyTo(Array array, int index) { AsEnumerable().ToArray().CopyTo(array, index); }
            void ICollection<string>.CopyTo(string[] array, int arrayIndex)
            {
                Monitor.Enter(_syncRoot);
                try { AsEnumerable().ToList().CopyTo(array, arrayIndex); }
                finally { Monitor.Exit(_syncRoot); }
            }
            public IEnumerator<string> GetEnumerator() { return AsEnumerable().GetEnumerator(); }
            IEnumerator IEnumerable.GetEnumerator() { return AsEnumerable().ToArray().GetEnumerator(); }
        }
        
        class ValueCollection : ICollection<string>, ICollection
        {
            private object _syncRoot;
            private List<QueryParamItem> _list;
            public ValueCollection(List<QueryParamItem> list, object syncRoot)
            {
                _syncRoot = syncRoot;
                _list = list;
            }
            public int Count { get { return _list.Count; } }
            bool ICollection<string>.IsReadOnly { get { return true; } }
            bool ICollection.IsSynchronized { get { return true; } }
            public object SyncRoot { get { return _syncRoot; } }
            internal IEnumerable<string> AsEnumerable()
            {
                foreach (QueryParamItem item in _list)
                    yield return item.Value;
            }
            void ICollection<string>.Add(string item) { throw new NotSupportedException(); }
            void ICollection<string>.Clear() { throw new NotSupportedException(); }
            bool ICollection<string>.Remove(string item) { throw new NotSupportedException(); }
            public bool Contains(string item)
            {
                if (item == null)
                    return false;
                Monitor.Enter(_syncRoot);
                try
                {
                    if (item == null)
                        return AsEnumerable().Any(i => i == null);
                    return AsEnumerable().Any(i => i != null && i == item);
                }
                finally { Monitor.Exit(_syncRoot); }
            }
            public void CopyTo(Array array, int index) { AsEnumerable().ToArray().CopyTo(array, index); }
            void ICollection<string>.CopyTo(string[] array, int arrayIndex)
            {
                Monitor.Enter(_syncRoot);
                try { AsEnumerable().ToList().CopyTo(array, arrayIndex); }
                finally { Monitor.Exit(_syncRoot); }
            }
            public IEnumerator<string> GetEnumerator() { return AsEnumerable().GetEnumerator(); }
            IEnumerator IEnumerable.GetEnumerator() { return AsEnumerable().ToArray().GetEnumerator(); }
        }

        sealed class DictionaryEnumerator : IEnumerator<KeyValuePair<string, string>>, IDictionaryEnumerator
        {
            private QueryParamDictionary _queryParamDictionary;
            private string _currentKey = null;
            private string _currentValue = null;
            private int _position = -1;
            public DictionaryEnumerator(QueryParamDictionary queryParamDictionary)
            {
                _queryParamDictionary = queryParamDictionary;
            }

            public KeyValuePair<string, string> Current
            {
                get
                {
                    if (_queryParamDictionary == null)
                        throw new ObjectDisposedException(this.GetType().FullName);

                    if (_position < 0)
                        throw new InvalidOperationException("Enumerator position is before the beginning of the enumeration.");
                    
                    string currentKey = _currentKey;
                    string currentValue = _currentValue;
                    if (currentKey == null)
                        throw new InvalidOperationException("Enumeration has no elements.");
                    return new KeyValuePair<string, string>(currentKey, currentValue);
                }
            }

            object IEnumerator.Current => Current;

            public object Key => _currentKey;

            public object Value => _currentValue;

            public DictionaryEntry Entry
            {
                get
                {
                    if (_queryParamDictionary == null)
                        throw new ObjectDisposedException(this.GetType().FullName);

                    if (_position < 0)
                        throw new InvalidOperationException("Enumerator position is before the beginning of the enumeration.");
                    
                    string currentKey = _currentKey;
                    string currentValue = _currentValue;
                    if (currentKey == null)
                        throw new InvalidOperationException("Enumeration has no elements.");
                    return new DictionaryEntry(currentKey, currentValue);
                }
            }

            public bool MoveNext()
            {
                QueryParamDictionary queryParamDictionary = _queryParamDictionary;
                if (queryParamDictionary == null)
                    throw new ObjectDisposedException(this.GetType().FullName);

                if (_position >= queryParamDictionary.Count)
                    return false;

                _position++;
                if (_position >= queryParamDictionary.Count)
                    return false;
                try
                {
                    QueryParamItem item = queryParamDictionary[_position];
                    _currentKey = item.Key;
                    _currentValue = item.Value;
                    return true;
                } catch { }
                return false;
            }

            public void Reset()
            {
                if (_queryParamDictionary == null)
                    throw new ObjectDisposedException(this.GetType().FullName);
                _currentKey = null;
                _currentValue = null;
                _position = -1;
            }

            public void Dispose() { }
        }
    }
}