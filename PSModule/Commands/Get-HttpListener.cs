using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Net;

namespace Erwine.Leonard.T.PSWebSrv.Commands
{
    /// <summary>
    /// Class for Get-HttpListener Cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "HttpListener")]
    //[OutputType(typeof(HttpListenerInfo))]
    public class Get_HttpListener : PSCmdlet
    {
		/// <summary>
		/// String which uniquely identifies an HTTP Listener.
		/// </summary>
        [Parameter(Mandatory = true, ValueFromPipeline = true, HelpMessage = "String which uniquely identifies an HTTP Listener.")]
        [ValidateNotNullOrEmpty()]
        public string[] ID { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                throw new NotImplementedException();
            }
            catch (Exception e)
            {
                WriteError(new ErrorRecord(e, "Get_HttpListener", ErrorCategory.ReadError, MyInvocation.BoundParameters));
                throw;
            }
        }
    }
}