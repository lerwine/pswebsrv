using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Net;

namespace Erwine.Leonard.T.PSWebSrv.Commands
{
    /// <summary>
    /// Base Class for HttpListener Control Cmdlet.
    /// </summary>
    public abstract class HttpHandlerCmdlet : PSCmdlet
    {
		/// <summary>
		/// String which uniquely identifies the associated HTTP Listener.
		/// </summary>
        public virtual string Listener { get; set; }

		/// <summary>
		/// Name which uniquely identifies an HTTP Handler
		/// </summary>
        public virtual string[] Name { get; set; }

        protected HttpListenerInfo GetListener()
        {
            if (Listener == null)
                return null;
            try { return HttpListenerInfo.Get(this, Listener); }
            catch (Exception e)
            {
                WriteError(new ErrorRecord(e, "HttpHandlerListener", ErrorCategory.ReadError, Listener));
                return null;
            }
        }

        protected IEnumerable<HttpHandlerInfo> GetHandlers()
        {
            HttpListenerInfo listener = GetListener();
            if (listener == null)
                return new HttpHandlerInfo[0];

            if (Name == null || Name.Length == 0)
                return listener.Handlers.AsEnumerable();

            return Name.Where(n => n != null).Select(n => listener.Handlers.FirstOrDefault(h => h.Equals(n))).Where(n => n != null);
        }
    }
}