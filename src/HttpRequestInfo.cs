using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace Erwine.Leonard.T.PSWebSrv
{
    public class HttpRequestInfo
    {
        private HttpListenerContext _context;

        public string Scheme { get; private set; }
        
        public string UserName { get; private set; }
        
        public string Password { get; private set; }
        
        public string Host { get; private set; }
        
        public int Port { get; private set; }
        
        public string ParentPath { get; private set; }
        
        public string LeafName { get; private set; }
        
        public string LeafExtension { get; private set; }
        
        public QueryParamDictionary Query { get; private set; }

        public string Fragment { get; private set; }

        public string ListenerID { get; private set; }

        public ReadOnlyCollection<HttpHostBinding> HostBindings { get; private set; }

        public HttpListenerRequest ListenerRequest { get; private set; }

        public HttpListenerResponse ListenerResponse { get; private set; }

        public IPrincipal User { get; private set; }

        public ErrorRecord Error { get; private set; }

        private HttpRequestInfo(HttpListenerContext context, HttpListenerInfo listener)
        {
            ListenerID = listener.ID;
            HostBindings = new ReadOnlyCollection<HttpHostBinding>(listener.HostBindings.ToArray());
            Error = null;
            try
            {
                _context = context;
                if (context != null)
                {
                    User = context.User;
                    ListenerRequest = context.Request;
                    ListenerResponse = context.Response;
                    UriBuilder uriBuilder;
                    if (context.Request.Url == null || !context.Request.Url.IsAbsoluteUri)
                        uriBuilder = HostBindings[0].ToUriBuilder(context.Request.RawUrl);
                    else
                        uriBuilder = new UriBuilder(context.Request.Url);
                    Scheme = uriBuilder.Scheme;
                    UserName = uriBuilder.UserName;
                    Password = uriBuilder.Password;
                    Host = uriBuilder.Host;
                    Port = uriBuilder.Port;
                    string leaf;
                    ParentPath = SplitUriPath(uriBuilder.Path, out leaf);
                    if (ParentPath.Length == 0)
                        ParentPath = "/";
                    else
                    {
                        if (ParentPath[0] != '/')
                            ParentPath = "/" + ParentPath;
                        if (ParentPath[ParentPath.Length - 1] != '/')
                            ParentPath = ParentPath + "/";
                    }
                    int index = leaf.LastIndexOf('.');
                    if (index < 0)
                    {
                        LeafName = leaf;
                        LeafExtension = "";
                    }
                    else
                    {
                        LeafName = leaf.Substring(0, index);
                        LeafExtension = leaf.Substring(index);
                    }
                    Query = QueryParamDictionary.Parse(uriBuilder.Query);
                    Fragment = uriBuilder.Fragment;
                }
            }
            catch (Exception e)
            {
                Error = new ErrorRecord(e, "GetHtpListenerContext", ErrorCategory.OpenError, (new object[] { ListenerID, HostBindings }) as object);
            }
        }
        
        private HttpRequestInfo(Exception error, ErrorCategory category, HttpListenerInfo listener)
        {
            ListenerID = listener.ID;
            HostBindings = new ReadOnlyCollection<HttpHostBinding>(listener.HostBindings.ToArray());
            Error = new ErrorRecord(error, "GetHtpListenerContext", category, (new object[] { ListenerID, HostBindings }) as object);
            _context = null;
            User = null;
            ListenerRequest = null;
            ListenerResponse = null;
            Scheme = listener.HostBindings[0].Scheme;
            UserName = null;
            Password = null;
            Host = listener.HostBindings[0].Host;
            Port = listener.HostBindings[0].Port;
            ParentPath = "";
            LeafName = "";
            LeafExtension = "";
        }

        public static string SplitUriPath(string path, out string leaf)
        {
            if (path.Contains('\\'))
                path = path.Replace('\\', '/');
            
            if (String.IsNullOrEmpty(path))
            {
                leaf = path;
                return null;
            }

            if (path.Length == 1 && path[0] == '/')
            {
                leaf = "";
                return "";
            }

            int index = path.LastIndexOf('/');
            while (index == path.Length - 1)
            {
                if (index == 0)
                {
                    leaf = "";
                    return "";
                }
                path = path.Substring(0, path.Length - 1);
                index = path.LastIndexOf('/');
            }
            if (index < 0)
            {
                leaf = path;
                return "";
            }
            leaf = path.Substring(index + 1);
            if (index == 0)
                return "/";
            return path.Substring(0, index);
        }

        public static bool TryGetContext(Task<HttpListenerContext> getContext, HttpListenerInfo listener, int millisecondsTimeout,
            CancellationToken cancellationToken, out HttpRequestInfo result)
        {
            if (getContext.IsCompleted || getContext.Wait(millisecondsTimeout, cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    result = new HttpRequestInfo(null, listener);
                    return false;
                }
                try
                {
                    result = new HttpRequestInfo(getContext.Result, listener);
                    return false;
                }
                catch (Exception e) { result = new HttpRequestInfo(e, ErrorCategory.ReadError, listener); }
            }
            else
                result = null;

            return false;
        }
    }
}