using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Net;

namespace Erwine.Leonard.T.PSWebSrv.Commands
{
    /// <summary>
    /// Waits for a request on an HTTP Listener.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Wait, "HttpListener")]
    [OutputType(typeof(HttpRequestInfo))]
    public class Wait_HttpListener : PSCmdlet
    {
        [Parameter(ValueFromPipeline = true, HelpMessage = "String which uniquely identifies the HTTP Listener to wait for.")]
        [ValidateNotNullOrEmpty()]
		/// <summary>
		/// String which uniquely identifies the HTTP Listener to wait for.
		/// </summary>
        public string ID { get; set; }

        [Parameter(HelpMessage = "String which uniquely identifies the HTTP Listener to wait for.")]
        [ValidateNotNullOrEmpty()]
        [Alias("MillisecondsTimeout")]
		/// <summary>
		/// Milliseconds to wait for a response.
		/// </summary>
        public int Timeout { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                HttpListenerInfo listener = HttpListenerInfo.Get(this, ID);
                HttpRequestInfo request;
                if (listener != null && listener.TryGetRequest((Timeout < 1) ? 100 : Timeout, out request))
                    WriteObject(request);
            }
            catch (Exception e)
            {
                WriteError(new ErrorRecord(e, "WaitHttpListener", ErrorCategory.ReadError, MyInvocation.BoundParameters));
                throw;
            }
        }
    }
}