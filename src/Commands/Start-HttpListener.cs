using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Net;

namespace Erwine.Leonard.T.PSWebSrv.Commands
{
    /// <summary>
    /// Class for Start-HttpListener Cmdlet.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Start, "HttpListener")]
    public class Start_HttpListener : PSCmdlet
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, HelpMessage = "String which uniquely identifies the HTTP Listener to start.")]
        [ValidateNotNullOrEmpty()]
		/// <summary>
		/// String which uniquely identifies the HTTP Listener to start.
		/// </summary>
        public string[] ID { get; set; }

		protected override void ProcessRecord()
		{
            try
            {
                throw new NotImplementedException();
            }
            catch (Exception e)
            {
                WriteError(new ErrorRecord(e, "Start_HttpListener", ErrorCategory.InvalidOperation, MyInvocation.BoundParameters));
                throw;
            }
		}
    }
}