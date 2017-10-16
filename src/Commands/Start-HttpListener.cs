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
    public class Start_HttpListener : HttpListenerCmdlet
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, HelpMessage = "String which uniquely identifies the HTTP Listener to start.")]
        [ValidateNotNullOrEmpty()]
		/// <summary>
		/// String which uniquely identifies the HTTP Listener to start.
		/// </summary>
        public override string[] ID { get; set; }

        protected override void ProcessRecord()
        {
            foreach (HttpListenerInfo item in GetListeners().ToArray())
			{
				if (item.Start(this))
                	WriteObject(item);
			}
        }
    }
}