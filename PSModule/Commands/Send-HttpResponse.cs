using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Net;

namespace Erwine.Leonard.T.PSWebSrv.Commands
{
    /// <summary>
    /// Class for Send-HttpResponse Cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommunications.Send, "HttpResponse")]
    [OutputType(typeof(bool))]
    public class Send_HttpResponse : PSCmdlet
    {
        [Parameter(Mandatory = true, HelpMessage = "String which uniquely identifies an HTTP Listener.")]
        [ValidateNotNullOrEmpty()]
		/// <summary>
		/// String which uniquely identifies the associated HTTP Listener.
		/// </summary>
        public string Listener { get; set; }

        // [Parameter(Mandatory = true, HelpMessage = "Object which will handle the request.")]
        // [ValidateNotNull()]
		// /// <summary>
		// /// Object which will handle the request.
		// /// </summary>
        // public HttpHandlerInfo Handler { get; set; }

        // [Parameter(Mandatory = true, HelpMessage = "Request object which will be used to generate a response.")]
        // [ValidateNotNull()]
		// /// <summary>
		// /// Request object which will be used to generate a response.
		// /// </summary>
        // public HttpRequestInfo Request { get; set; }

		protected override void ProcessRecord()
		{
            try
            {
                throw new NotImplementedException();
            }
            catch (Exception e)
            {
                WriteError(new ErrorRecord(e, "Send_HttpResponse", ErrorCategory.WriteError, MyInvocation.BoundParameters));
                throw;
            }
		}
    }
}