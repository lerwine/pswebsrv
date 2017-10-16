using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Net;

namespace Erwine.Leonard.T.PSWebSrv.Commands
{
    /// <summary>
    /// Class for Resume-HttpListener Cmdlet.
    /// </summary>
	[Cmdlet(VerbsLifecycle.Resume, "HttpListener")]
	public class Resume_HttpListener : PSCmdlet
	{
        [Parameter(Mandatory = true, ValueFromPipeline = true, HelpMessage = "String which uniquely identifies the HTTP Listener to resume.")]
        [ValidateNotNullOrEmpty()]
		/// <summary>
		/// String which uniquely identifies the HTTP Listener to resume.
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
                WriteError(new ErrorRecord(e, "Resume_HttpListener", ErrorCategory.InvalidOperation, MyInvocation.BoundParameters));
                throw;
            }
		}
	}
}