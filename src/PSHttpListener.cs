using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Threading;
using System.Windows.Threading;

namespace Erwine.Leonard.T.PSWebSrv
{
    /// <summary>
    /// An HTTP protocol listener for use with PowerShell, utilizing an internally managed <seealso cref="HttpListener" /> object.
    /// </summary>
    public class PSHttpListener : IDisposable
    {
        private Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

        /// <summary>
        /// This gets invoked when <see cref="Close()" /> or <see cref="Abort()" /> is called, before the <see cref="PSHttpListener" /> is shut down.
        /// </summary>
        public event EventHandler<PSHttpListenerCloseEventArgs> Closing;

        /// <summary>
        /// This gets invoked when <see cref="Close()" /> or <see cref="Abort()" /> is called, after the <see cref="PSHttpListener" /> has been shut down.
        /// </summary>
        public event EventHandler<PSHttpListenerCloseEventArgs> Closed;
        
        private object _syncRoot = new object();

        HttpListener _listener = null;
        
        private LinkedList<PSHttpHandlerInfo> _handlers = new LinkedList<PSHttpHandlerInfo>();

        /// <summary>
        /// Gets or sets a Boolean value that controls whether, when NTLM is used, additional requests using the same Transmission Control Protocol (TCP) connection are required to authenticate.
        /// </summary>
        public bool UnsafeConnectionNtlmAuthentication
        {
            get
            {
                Monitor.Enter(_syncRoot);
                try
                {
                    if (Status == PSHttpListenerStatus.Closed)
                        throw new ObjectDisposedException(((_isDisposed) ? GetType() : typeof(HttpListener)).FullName);
                    if (_listener == null)
                        _listener = new HttpListener();
                    return _listener.UnsafeConnectionNtlmAuthentication;
                }
                finally { Monitor.Exit(_syncRoot); }
            }
            set
            {
                Monitor.Enter(_syncRoot);
                try
                {
                    if (Status == PSHttpListenerStatus.Closed)
                        throw new ObjectDisposedException(((_isDisposed) ? GetType() : typeof(HttpListener)).FullName);
                    if (_listener == null)
                        _listener = new HttpListener();
                    _listener.UnsafeConnectionNtlmAuthentication = value;
                }
                finally { Monitor.Exit(_syncRoot); }
            }
        }
        
        /// <summary>
        /// Gets or sets a Boolean value that specifies whether exceptions that occur when the <see cref="PSHttpListener" /> sends the response to the client are ignored.
        /// </summary>
        public bool IgnoreWriteExceptions
        {
            get
            {
                Monitor.Enter(_syncRoot);
                try
                {
                    if (Status == PSHttpListenerStatus.Closed)
                        throw new ObjectDisposedException(((_isDisposed) ? GetType() : typeof(HttpListener)).FullName);
                    if (_listener == null)
                        _listener = new HttpListener();
                    return _listener.IgnoreWriteExceptions;
                }
                finally { Monitor.Exit(_syncRoot); }
            }
            set
            {
                Monitor.Enter(_syncRoot);
                try
                {
                    if (Status == PSHttpListenerStatus.Closed)
                        throw new ObjectDisposedException(((_isDisposed) ? GetType() : typeof(HttpListener)).FullName);
                    if (_listener == null)
                        _listener = new HttpListener();
                    _listener.IgnoreWriteExceptions = value;
                }
                finally { Monitor.Exit(_syncRoot); }
            }
        }
        
        /// <summary>
        /// Gets a value that indicates whether <see cref="PSHttpListener" /> has been started.
        /// </summary>
        public bool IsListening
        {
            get
            {
                Monitor.Enter(_syncRoot);
                try { return Status != PSHttpListenerStatus.Closed && _listener != null && _listener.IsListening; }
                finally { Monitor.Exit(_syncRoot); }
            }
        }
        
        /// <summary>
        /// Gets or sets the realm, or resource partition, associated with this <see cref="PSHttpListener" /> object.
        /// </summary>
        public string Realm
        {
            get
            {
                Monitor.Enter(_syncRoot);
                try
                {
                    if (Status == PSHttpListenerStatus.Closed)
                        throw new ObjectDisposedException(((_isDisposed) ? GetType() : typeof(HttpListener)).FullName);
                    if (_listener == null)
                        _listener = new HttpListener();
                    return _listener.Realm;
                }
                finally { Monitor.Exit(_syncRoot); }
            }
            set
            {
                Monitor.Enter(_syncRoot);
                try
                {
                    if (Status == PSHttpListenerStatus.Closed)
                        throw new ObjectDisposedException(((_isDisposed) ? GetType() : typeof(HttpListener)).FullName);
                    if (_listener == null)
                        _listener = new HttpListener();
                    _listener.Realm = value;
                }
                finally { Monitor.Exit(_syncRoot); }
            }
        }

        /// <summary>
        /// Gets the status of the <see cref="PSHttpListener" /> object.
        /// </summary>
        public PSHttpListenerStatus Status { get; private set; }

        /// <summary>
        /// Initializes a new <see cref="PSHttpListener" /> object.
        /// </summary>
        public PSHttpListener()
        {
        }

        ~PSHttpListener() { Dispose(false); }

        /// <summary>
        /// Allows this instance to receive incoming requests.
        /// <summary>
        public void Start()
        {
            Monitor.Enter(_syncRoot);
            try
            {
                if (_listener == null)
                {
                    if (Status != PSHttpListenerStatus.Stopped)
                        throw new ObjectDisposedException(this.GetType().FullName);
                    _listener = new HttpListener();
                }
                _getContextResult = _listener.BeginGetContext(GetContext, null);
            }
            finally { Monitor.Exit(_syncRoot); }
        }

        private void GetContext(IAsyncResult getContextResult)
        {
            HttpListenerContext context;
            Exception error;
            Monitor.Enter(_syncRoot);
            try
            {
                context = _listener.EndGetContext(getContextResult);
                error = null;
            }
            catch (Exception exc)
            {
                error = exc;
                context = null;
            }
            finally
            {
                _getContextResult = null;
                Monitor.Exit(_syncRoot);
            }

            try { OnGetContext(context, error); }
            finally
            {
                Monitor.Enter(_syncRoot);
                try
                {
                    _getContextResult = _listener.BeginGetContext(GetContext, null);
                    error = null;
                }
                catch (Exception exc) { error = exc; }
                finally { Monitor.Exit(_syncRoot); }
                if (error != null)
                    try { OnGetContext(null, error); } catch { }
            }
        }

        private void OnGetContext(HttpListenerContext context, Exception error)
        {
            if (!_dispatcher.CheckAccess())
            {
                DispatcherOperation op = _dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => OnGetContext(context, error)));
                Thread.Sleep(100);
                op.Wait();
                return;
            }

            UrlDetails url = new UrlDetails(context.Request.RawUrl);
        }

        /// <summary>
        /// Causes this instance to stop receiving incoming requests.
        /// <summary>
        public void Stop()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Shuts down the <see cref="PSHttpListener" /> object immediately, discarding all currently queued requests.
        /// <summary>
        public void Abort() { _Close(_listener.Abort, true); }

        /// <summary>
        /// Shuts down the <see cref="PSHttpListener" />.
        /// <summary>
        public void Close() { _Close(_listener.Close, false); }

        private void _Close(Action closeAction, bool isAborting)
        {
            Monitor.Enter(_syncRoot);
            try
            {
                if (_listener == null)
                {
                    if (Status == PSHttpListenerStatus.Stopped)
                        Status = PSHttpListenerStatus.Closed;
                    return;
                }

                PSHttpListenerCloseEventArgs args = new PSHttpListenerCloseEventArgs(isAborting);
                EventHandler<PSHttpListenerCloseEventArgs> eventHandler = Closing;
                try
                {
                    if (eventHandler != null)
                        eventHandler(this, args);
                }
                finally
                {
                    closeAction();
                    _listener = null;
                    Status = PSHttpListenerStatus.Closed;
                    if ((eventHandler = Closed) != null)
                        eventHandler(this, args);
                }
            }
            finally { Monitor.Exit(_syncRoot); }
        }

        internal PSHttpHandlerInfo RegisterPathHandler(PSCmdlet hostCmdlet, string name, string pagePath, string virtualPath, string pattern, string exclude)
        {
            return RegisterPSHttpHandler(hostCmdlet, name, () => new PathHttpHandler(name, pagePath, virtualPath, pattern, exclude));
        }

        internal PSHttpHandlerInfo RegisterDataStoreHandler(PSCmdlet hostCmdlet, string name, string dataStore, string virtualPath, string pattern, string exclude)
        {
            return RegisterPSHttpHandler(hostCmdlet, name, () => new DataStoreHttpHandler(name, dataStore, virtualPath, pattern, exclude));
        }

        internal PSHttpHandlerInfo RegisterScriptHandler(PSCmdlet hostCmdlet, string name, ScriptBlock script, string virtualPath, string pattern, string exclude)
        {
            return RegisterPSHttpHandler(hostCmdlet, name, () => new ScriptHttpHandler(name, script, virtualPath, pattern, exclude));
        }

        private PSHttpHandlerInfo RegisterPSHttpHandler(PSCmdlet hostCmdlet, string name, Func<PSHttpHandler> createHandler)
        {
            PSHttpHandlerInfo info;
            Monitor.Enter(_syncRoot);
            try
            {
                if (Status == PSHttpListenerStatus.Closed)
                    throw new InvalidOperationException("HTTP listener is closed");
                if (Status != PSHttpListenerStatus.Stopped)
                    throw new InvalidOperationException("HTTP listener is not stopped");
                if (_handlers.Any(h => h.Matches(name)))
                    throw new ArgumentException("A handler with that name already exists.", "name");
                PSHttpHandler handler = createHandler();
                info = new PSHttpHandlerInfo(this, handler);
                _handlers.AddLast(info);
                try { handler.OnRegistered(hostCmdlet, this); }
                catch
                {
                    _handlers.Remove(info);
                    throw;
                }
            }
            finally { Monitor.Exit(_syncRoot); }
            return info;
        }

        public PSHttpHandlerInfo Find(string name)
        {
            Monitor.Enter(_syncRoot);
            try { return _handlers.FirstOrDefault(h => h.Matches(name)); }
            finally { Monitor.Exit(_syncRoot); }
            throw new NotImplementedException();
        }

        #region IDisposable Support

        private bool _isDisposed = false; // To detect redundant calls
        private IAsyncResult _getContextResult;

        /// <summary>
        /// This gets invoked when the current <see cref="PSHttpListener" /> is being disposed.
        /// <summary>
        protected virtual void Dispose(bool disposing)
        {
            Monitor.Enter(_syncRoot);
            try
            {
                if (_isDisposed)
                    return;
                _isDisposed = true;

                if (!disposing)
                    return;
                
                if (Status == PSHttpListenerStatus.Stopped)
                    Close();
                else
                    Abort();
            }
            finally { Monitor.Exit(_syncRoot); }
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}