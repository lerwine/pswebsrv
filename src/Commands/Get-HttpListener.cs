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
    [OutputType(typeof(HttpListenerInfo))]
    public class Get_HttpListener : HttpListenerCmdlet
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, HelpMessage = "String which uniquely identifies an HTTP Listener.")]
        [ValidateNotNullOrEmpty()]
		/// <summary>
		/// String which uniquely identifies an HTTP Listener.
		/// </summary>
        public override string[] ID { get; set; }

        protected override void ProcessRecord()
        {
            foreach (HttpListenerInfo item in GetListeners())
                WriteObject(item);
        }
    }
}