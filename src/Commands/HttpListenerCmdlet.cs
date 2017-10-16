using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Net;

namespace Erwine.Leonard.T.PSWebSrv.Commands
{
    /// <summary>
    /// Base Class for HttpListener Control Cmdlet
    /// </summary>
    public abstract class HttpListenerCmdlet : PSCmdlet
    {
		/// <summary>
		/// String which uniquely identifies an HTTP Listener
		/// </summary>
        public virtual string[] ID { get; set; }

        protected IEnumerable<HttpListenerInfo> GetListeners()
        {
            if (ID != null)
            {
                foreach (string id in ID)
                {
                    HttpListenerInfo listener;
                    try { listener = (id == null) ? null : HttpListenerInfo.Get(this, id); }
                    catch (Exception e)
                    {
                        WriteError(new ErrorRecord(e, "GetHttpListener", ErrorCategory.ReadError, id));
                        listener = null;
                    }
                    if (listener != null)
                        yield return listener;
                }
            }
        }
    }
}