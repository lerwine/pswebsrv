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
    //[OutputType(typeof(HttpHandlerInfo))]
    public class Get_HttpHandler : PSCmdlet
    {
		/// <summary>
		/// String which uniquely identifies the associated HTTP Listener.
		/// </summary>
        [Parameter(Mandatory = true, HelpMessage = "String which uniquely identifies an HTTP Listener.")]
        [ValidateNotNullOrEmpty()]
        public string Listener { get; set; }

		/// <summary>
		/// Name which uniquely identifies an HTTP Handler.
		/// </summary>
        [Parameter(ValueFromPipeline = true, HelpMessage = "Name which uniquely identifies an HTTP Handler.")]
        [ValidateNotNullOrEmpty()]
        public string[] Name  { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                
                throw new NotImplementedException();
            }
            catch (Exception e)
            {
                WriteError(new ErrorRecord(e, "Get_HttpHandler", ErrorCategory.ReadError, MyInvocation.BoundParameters));
                throw;
            }
        }
    }
}
