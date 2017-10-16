using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Erwine.Leonard.T.PSWebSrv
{
    public class HttpListenerInfo
    {
        private object _syncRoot = new object();
        private Collection<HttpHostBinding> _hostBindings = new Collection<HttpHostBinding>();
        private ReadOnlyCollection<HttpHostBinding> _roHostBindings = null;
        private Collection<HttpHandlerInfo> _handlers = new Collection<HttpHandlerInfo>();
        private ReadOnlyCollection<HttpHandlerInfo> _roHandlers = null;
        
        private const string GlobalVarName = "global:HttpListenerInfo";
        private CancellationTokenSource _tokenSource = null;
        private HttpListener _listener = null;

        public string ID { get; private set; }

        public ReadOnlyCollection<HttpHostBinding> HostBindings
        {
            get
            {
                if (_roHostBindings == null)
                    _roHostBindings = new ReadOnlyCollection<HttpHostBinding>(_hostBindings);
                return _roHostBindings;
            }
        }

        public ReadOnlyCollection<HttpHandlerInfo> Handlers
        {
            get
            {
                if (_roHandlers == null)
                    _roHandlers = new ReadOnlyCollection<HttpHandlerInfo>(_handlers);
                return _roHandlers;
            }
        }

        public HttpListernState State { get; private set; }

        internal HttpListenerInfo(string id, Uri uri, UriHostNameType hostNameType)
        {
            if (id == null)
                throw new ArgumentNullException("id");

            if (uri == null)
                throw new ArgumentNullException("uri");

            if (!uri.IsAbsoluteUri)
                throw new ArgumentException("Uri must be absolute", "uri");
            
            ID = id;
            _hostBindings.Add(new HttpHostBinding(uri, hostNameType));
        }

        public bool TryGetRequest(int millisecondsTimeout, out HttpRequestInfo result)
        {
            Monitor.Enter(_syncRoot);
            try
            {
                if (State != HttpListernState.Started)
                {
                    result = null;
                    return false;
                }
                using (CancellationTokenSource tokenSource = new CancellationTokenSource())
                {
                    _tokenSource = tokenSource;
                    bool success = HttpRequestInfo.TryGetContext(_listener.GetContextAsync(), this, millisecondsTimeout, _tokenSource.Token,
                        out result);
                    _tokenSource = null;
                    return success;
                }
            }
            finally { Monitor.Exit(_syncRoot); }
        }

        public static HttpListenerInfo Get(PSCmdlet hostCmdlet, string id)
        {
            if (hostCmdlet == null)
                throw new ArgumentNullException("hostCmdlet");

            PSVariable psVar;
            ListenerCollection listeners;
            if (id == null || (psVar = hostCmdlet.SessionState.PSVariable.Get(GlobalVarName)) == null ||
                    (listeners = psVar.Value as ListenerCollection) == null)
                return null;
            return listeners.Get(id);
        }

        public static HttpListenerInfo RegisterNew(PSCmdlet hostCmdlet, string id, Uri uri, UriHostNameType hostNameType)
        {
            if (hostCmdlet == null)
                throw new ArgumentNullException("hostCmdlet");

            PSVariable psVar = hostCmdlet.SessionState.PSVariable.Get(GlobalVarName);
            ListenerCollection listeners = (psVar == null) ? null : psVar.Value as ListenerCollection;
            if (listeners == null)
            {
                listeners = new ListenerCollection();
                hostCmdlet.SessionState.PSVariable.Set(GlobalVarName, listeners);
            }
            return listeners.RegisterNew(hostCmdlet, id, uri, hostNameType);
        }
        
        public static HttpListenerInfo Unregister(PSCmdlet hostCmdlet, string id)
        {
            PSVariable psVar;
            ListenerCollection listeners;
            if (id == null || (psVar = hostCmdlet.SessionState.PSVariable.Get(GlobalVarName)) == null ||
                    (listeners = psVar.Value as ListenerCollection) == null)
                return null;
            return listeners.Unregister(id);
        }

        internal bool Start(PSCmdlet hostCmdlet)
        {
            Monitor.Enter(_syncRoot);
            HttpListener listener = null;
            try
            {
                if (State == HttpListernState.Started)
                    return true;
                if (State != HttpListernState.NotStarted)
                    return false;
                listener = new HttpListener();
                UriBuilder builder = new UriBuilder();
                foreach (HttpHostBinding item in _hostBindings)
                    listener.Prefixes.Add(item.ToString());
                listener.Start();
                State = HttpListernState.Started;
                _listener = listener;
            }
            catch (Exception e)
            {
                try
                {
                    if (listener != null)
                        listener.Close();
                }
                finally { hostCmdlet.WriteError(new ErrorRecord(e, "StartHttpListener", ErrorCategory.OpenError, this)); }
                
                return false;
            }
            finally { Monitor.Exit(_syncRoot); }

            return true;
        }

        internal bool Suspend(PSCmdlet hostCmdlet)
        {
            Monitor.Enter(_syncRoot);
            try
            {
                if (State == HttpListernState.Suspended)
                    return true;
                if (State != HttpListernState.Started)
                    return false;
                
                _listener.Stop();
                State = HttpListernState.Suspended;
            }
            catch (Exception e)
            {
                hostCmdlet.WriteError(new ErrorRecord(e, "SuspendHttpListener", ErrorCategory.CloseError, this));
                return false;
            }
            finally { Monitor.Exit(_syncRoot); }

            return true;
        }

        internal bool Resume(PSCmdlet hostCmdlet)
        {
            Monitor.Enter(_syncRoot);
            try
            {
                if (State == HttpListernState.Started)
                    return true;
                if (State != HttpListernState.Suspended)
                    return false;
                
                _listener.Start();
                State = HttpListernState.Started;
            }
            catch (Exception e)
            {
                hostCmdlet.WriteError(new ErrorRecord(e, "ResumeHttpListener", ErrorCategory.CloseError, this));
                return false;
            }
            finally { Monitor.Exit(_syncRoot); }
            return true;
        }

        internal bool Stop(PSCmdlet hostCmdlet)
        {
            Monitor.Enter(_syncRoot);
            try
            {
                if (State == HttpListernState.Stopped)
                    return true;
                if (State == HttpListernState.Started)
                {
                    _listener.Stop();   
                    State = HttpListernState.Suspended;
                }
                _listener.Close();
                State = HttpListernState.Stopped;
            }
            catch (Exception e)
            {
                hostCmdlet.WriteError(new ErrorRecord(e, "StopHttpListener", ErrorCategory.CloseError, this));
                return false;
            }
            finally { Monitor.Exit(_syncRoot); }
            return true;
        }

        internal void OnRegistering() { }
        
        internal void OnRegistered() { }
        
        internal void OnUnregistering()
        {
            CancellationTokenSource tokenSource = _tokenSource;
            if (tokenSource != null)
                try { _tokenSource.Cancel(false); } catch { }
            Task.Factory.StartNew(() =>
            {
                Monitor.Enter(_syncRoot);
                try
                {
                    if (State == HttpListernState.Started)
                    {
                        _listener.Stop();   
                        State = HttpListernState.Suspended;
                    }
                    _listener.Close();
                    _listener = null;
                    State = HttpListernState.Stopped;
                }
                finally { Monitor.Exit(_syncRoot); }
            }).Wait();
        }
        
        internal void OnUnregistered()
        {
            CancellationTokenSource tokenSource = _tokenSource;
            if (tokenSource != null)
            {
                try { _tokenSource.Cancel(false); } catch { }
                Thread.Sleep(100);
            }

            Task.Factory.StartNew(() =>
            {
                Monitor.Enter(_syncRoot);
                try
                {
                    HttpListernState state = State;
                    State = HttpListernState.Stopped;
                    HttpListener listener = _listener;
                    if (listener != null)
                    {
                        _listener = null;
                        if (state == HttpListernState.Started)
                            listener.Stop();
                        listener.Close();
                    }
                }
                finally { Monitor.Exit(_syncRoot); }
            });
        }
        
        public bool RegisterHandler(HttpHandlerInfo handler)
        {
            if (handler == null)
                return false;
            Monitor.Enter(_syncRoot);
            try
            {
                if (_handlers.Any(h => h.Name == handler.Name))
                    return false;
                _handlers.Add(handler);
            }
            finally { Monitor.Exit(_syncRoot); }
            return true;
        }

        class ListenerCollection : ReadOnlyCollection<HttpListenerInfo>
        {
            private object _syncRoot = new object();

            internal ListenerCollection() : base(new Collection<HttpListenerInfo>()) { }
            
            internal HttpListenerInfo Get(string id)
            {
                if (id == null)
                    return null;
                Monitor.Enter(_syncRoot);
                try
                {
                    for (int i = 0; i < Items.Count; i++)
                    {
                        if (String.Equals(Items[i].ID, id, StringComparison.InvariantCultureIgnoreCase))
                            return Items[i];
                    }
                }
                finally { Monitor.Exit(_syncRoot); }
                
                return null;
            }

            internal HttpListenerInfo RegisterNew(PSCmdlet hostCmdlet, string id, Uri uri, UriHostNameType hostNameType)
            {
                if (hostCmdlet == null)
                    throw new ArgumentNullException("hostCmdlet");
                    
                if (id == null)
                    throw new ArgumentNullException("id");

                if (uri == null)
                    throw new ArgumentNullException("uri");

                if (!uri.IsAbsoluteUri)
                    throw new ArgumentException("Uri must be absolute", "uri");
                
                HttpListenerInfo item = null;
                Monitor.Enter(_syncRoot);
                try
                {
                    if (Get(id) != null)
                        throw new ArgumentException("An HTTP Listener with that ID already exists", "id");
                    
                    if (Items.Any(i => i.HostBindings.Any(b => String.Equals(b.Scheme, uri.Scheme, StringComparison.InvariantCultureIgnoreCase) &&
                            String.Equals(b.Host, uri.Host, StringComparison.InvariantCultureIgnoreCase) && b.Port == uri.Port)))
                        throw new ArgumentException("An HTTP Listener with that Scheme, Host and Port already exists", "uri");
                    try
                    {
                        item = new HttpListenerInfo(id, uri, hostNameType);
                        item.OnRegistering();
                        Items.Add(item);
                    }
                    catch
                    {
                        item = null;
                        throw;
                    }
                }
                finally
                {
                    Monitor.Exit(_syncRoot);
                    if (item != null)
                        item.OnRegistered();
                }

                return item;
            }

            internal HttpListenerInfo Unregister(string id)
            {
                if (id == null)
                    return null;
                
                HttpListenerInfo item = null;
                Monitor.Enter(_syncRoot);
                try
                {
                    for (int i = 0; i < Items.Count; i++)
                    {
                        if (String.Equals(Items[i].ID, id, StringComparison.InvariantCultureIgnoreCase))
                        {
                            item = Items[i];
                            item.OnUnregistering();
                            Items.RemoveAt(i);
                            break;
                        }
                    }
                }
                finally
                {
                    Monitor.Exit(_syncRoot);
                    if (item != null)
                        item.OnUnregistered();
                }

                return item;
            }
        }
    }
}