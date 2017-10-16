using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Net;

namespace Erwine.Leonard.T.PSWebSrv.Commands
{
    /// <summary>
    /// Class for Get-HttpHandler Cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "HttpHandler")]
    [OutputType(typeof(HttpHandlerInfo))]
    public class Get_HttpHandler : HttpHandlerCmdlet
    {
        public const string ParameterSetName_Name = "Name";
        public const string ParameterSetName_Request = "Request";

        [Parameter(Mandatory = true, HelpMessage = "String which uniquely identifies an HTTP Listener.")]
        [ValidateNotNullOrEmpty()]
		/// <summary>
		/// String which uniquely identifies the associated HTTP Listener.
		/// </summary>
        public override string Listener { get { return base.Listener; } set { base.Listener = value; } }

        [Parameter(ValueFromPipeline = true, HelpMessage = "Name which uniquely identifies an HTTP Handler.",
            ParameterSetName = ParameterSetName_Name)]
        [ValidateNotNullOrEmpty()]
		/// <summary>
		/// Name which uniquely identifies an HTTP Handler.
		/// </summary>
        public override string[] Name { get { return base.Name; } set { base.Name = value; } }

        [Parameter(Mandatory = true, HelpMessage = "Request for which to find a handler.",
            ParameterSetName = ParameterSetName_Request)]
        [ValidateNotNull()]
		/// <summary>
		/// Request for which to find a handler.
		/// </summary>
        public HttpRequestInfo Request { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                if (ParameterSetName == ParameterSetName_Name)
                {
                    foreach (HttpHandlerInfo item in GetHandlers())
                        WriteObject(item);
                }
                else
                {
                    if (Request == null)
                        return;
                    HttpListenerInfo listener = GetListener();
                    if (listener == null)
                        return;
                    HttpHandlerInfo handler = listener.Handlers.FirstOrDefault(h => h.CanHandle(Request, this));
                    if (handler != null)
                        WriteObject(handler);
                }
            }
            catch (Exception e)
            {
                WriteError(new ErrorRecord(e, "GetHttpHandler", ErrorCategory.ReadError, MyInvocation.BoundParameters));
                throw;
            }
        }
    }
}